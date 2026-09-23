#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.Network.V2
{
    using Core.V2;
    using Protocol.V2;
    using Protocol.V2.Connection;
    using Protocol.V2.Matching;
    using Protocol.V2.Messages;
    using Protocol.V2.Messages.MessageFactory;
    using Status;

    public interface IOnlineGameClient_V2 : IDisposable
    {
        bool IsOnline { get; }
        ConnectionState State { get; }
        ProtocolHandshakeResponse? HandshakeResponse { get; }
        MatchingFound? CurrentMatch { get; }
        Exception? Failure { get; }
        NetworkError? Error { get; }
        event Action? OnDataChanged;
        UniTask ConnectAsync(CancellationToken cancellationToken = default);
        UniTask MatchRequestAsync(CancellationToken cancellationToken = default);
        UniTask MatchCancelRequestAsync(CancellationToken cancellationToken = default);
        void Disconnect();
    }

    /// <summary>TCP 연결, Handshake, 요청 응답 및 서버 이벤트에 따른 상태를 관리합니다.</summary>
    public sealed class OnlineGameClient_V2 : IOnlineGameClient_V2
    {
        private readonly TcpGameClient_V2 _tcpClient;
        private readonly PendingRequestTracker _pendingRequestTracker;
        private CancellationTokenSource? _connectionCts;
        private int _connectionVersion;
        private bool _matchingRequestInProgress;
        private bool _matchingCancelInProgress;
        private MatchingFound? _deferredMatchingFound;
        private bool _disposed;

        public ProtocolHandshakeResponse? HandshakeResponse { get; private set; }
        public MatchingFound? CurrentMatch { get; private set; }
        public Exception? Failure { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public bool IsOnline => State is ConnectionState.Connected or
            ConnectionState.Matching or ConnectionState.Matched;

        // 프로토콜 결과 해석은 클라이언트에서 끝내고 Manager에는 상태만 전달합니다.
        public NetworkError? Error
        {
            get
            {
                if (Failure is null)
                    return null;

                var error = NetworkError.ConnectionFailed;
                var result = HandshakeResponse?.Result ?? ProtocolHandshakeResult.Success;
                if ((result & ProtocolHandshakeResult.CoreVersionMismatch) != 0)
                    error |= NetworkError.CoreVersionMismatch;
                if ((result & ProtocolHandshakeResult.ProtocolVersionMismatch) != 0)
                    error |= NetworkError.ProtocolVersionMismatch;
                return error;
            }
        }

        public event Action? OnDataChanged;

        public OnlineGameClient_V2(string host, int port)
        {
            _pendingRequestTracker = new PendingRequestTracker();
            _tcpClient = new TcpGameClient_V2(host, port);
        }

        public async UniTask MatchRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureMatchingOperationAllowed(ConnectionState.Connected);
            cancellationToken.ThrowIfCancellationRequested();
            int connectionVersion = _connectionVersion;
            _matchingRequestInProgress = true;
            _deferredMatchingFound = null;

            try
            {
                var responseMsg = await SendRequestAsync(
                    ClientMessageType.MatchingRequest, new MatchingRequest(),
                    ServerMessageType.MatchingResponse, cancellationToken);
                EnsureCurrentConnection(connectionVersion);
                var response = responseMsg.GetPayload<MatchingResponse>();
                if (response.Result != MatchingResult.Accepted)
                    return;

                ChangeState(ConnectionState.Matching);
                // 응답과 이벤트가 연속 수신되면 SendAsync의 continuation보다 이벤트가 빠를 수 있습니다.
                // 미리 받은 이벤트는 Accepted로 Matching 전환한 뒤에만 적용합니다. 이벤트를 기다리지는 않습니다.
                if (connectionVersion == _connectionVersion && _deferredMatchingFound is not null)
                    ApplyMatchingFound(_deferredMatchingFound);
            }
            finally
            {
                if (connectionVersion == _connectionVersion)
                {
                    _matchingRequestInProgress = false;
                    _deferredMatchingFound = null;
                }
            }
        }

        public async UniTask MatchCancelRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureMatchingOperationAllowed(ConnectionState.Matching);
            cancellationToken.ThrowIfCancellationRequested();
            int connectionVersion = _connectionVersion;
            _matchingCancelInProgress = true;

            try
            {
                var responseMsg = await SendRequestAsync(
                    ClientMessageType.MatchingCancelRequest, new MatchingCancelRequest(),
                    ServerMessageType.MatchingCancelResponse, cancellationToken);
                EnsureCurrentConnection(connectionVersion);
                var response = responseMsg.GetPayload<MatchingCancelResponse>();
                if (response.Result != MatchingCancelResult.Cancelled)
                    return;

                // TODO:
                // 취소 응답 대기 중 MatchingFound가 먼저 도착하면 이미 Matched 상태일 수 있습니다.
                // 현재는 늦은 Cancelled로 매칭 정보를 지우지 않고 Matched와 CurrentMatch를 보존합니다.
                // 서버 취소/확정 정책에 맞춰 MatchSession / GameSession에서 우선순위와 복구를 결정해야 합니다.
                if (State == ConnectionState.Matched)
                    return;

                CurrentMatch = null;
                ChangeState(ConnectionState.Connected);
            }
            finally
            {
                if (connectionVersion == _connectionVersion)
                    _matchingCancelInProgress = false;
            }
        }

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (State != ConnectionState.Disconnected)
                throw new InvalidOperationException("이미 연결 중이거나 서버에 연결되어 있습니다.");

            int connectionVersion = ++_connectionVersion;
            _connectionCts = new CancellationTokenSource();
            CancellationToken connectionToken = _connectionCts.Token;
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, connectionToken);
            var connectToken = connectCts.Token;
            CurrentMatch = null;
            _deferredMatchingFound = null;
            HandshakeResponse = null;
            Failure = null;

            try
            {
                connectToken.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Connecting);
                await _tcpClient.ConnectAsync(connectToken);
                connectToken.ThrowIfCancellationRequested();
                EnsureCurrentConnection(connectionVersion);
                ChangeState(ConnectionState.Handshaking);

                var response = await HandshakeAsync(connectToken);
                connectToken.ThrowIfCancellationRequested();
                EnsureCurrentConnection(connectionVersion);
                HandshakeResponse = response;
                if (response.Result != ProtocolHandshakeResult.Success)
                    throw new InvalidOperationException($"핸드셰이크 실패: {response.Result}");

                ChangeState(ConnectionState.Connected);
                if (connectionVersion == _connectionVersion)
                    ReceiveLoopAsync(connectionVersion, connectionToken).Forget();
            }
            catch (Exception) when (connectToken.IsCancellationRequested)
            {
                if (connectionVersion == _connectionVersion)
                    CloseConnection();
                throw new OperationCanceledException(connectToken);
            }
            catch (Exception exception)
            {
                if (connectionVersion == _connectionVersion)
                    CloseConnection(exception);
                throw;
            }
        }

        public void Disconnect() => CloseConnection();

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _tcpClient.Dispose();
        }

        private async UniTask<ServerMessage> SendRequestAsync<TPayload>(
            ClientMessageType requestType, TPayload payload, ServerMessageType responseType,
            CancellationToken cancellationToken)
        {
            var request = ClientMessageFactory.Create(requestType, payload);
            string requestId = request.RequestId!;
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _connectionCts!.Token);
            var requestToken = requestCts.Token;
            _pendingRequestTracker.Add(requestId, responseType);

            try
            {
                requestToken.ThrowIfCancellationRequested();
                await _tcpClient.SendAsync(request, requestToken);
                requestToken.ThrowIfCancellationRequested();
                return await _pendingRequestTracker.WaitAsync(requestId, requestToken);
            }
            finally
            {
                _pendingRequestTracker.Remove(requestId);
            }
            // TODO:
            // 전송 후 호출자 토큰만 취소되면 서버는 요청을 처리했어도 로컬 응답 대기는 끝납니다.
            // 현재 상태는 확정되지 않고 늦은 응답은 무시되므로 서버와 대기 상태가 다를 수 있습니다.
            // MatchSession에서 상태 재조회 또는 명시적 서버 취소 정책을 결정해야 합니다.
        }

        private async UniTask<ProtocolHandshakeResponse> HandshakeAsync(CancellationToken cancellationToken)
        {
            var request = new ProtocolHandshakeRequest
            {
                YuJanggiProtocolVersion = ProtocolVersion.Current,
                YuJanggiCoreVersion = CoreVersion.Current
            };
            var requestMsg = ClientMessageFactory.Create(ClientMessageType.ProtocolHandshake, request);
            await _tcpClient.SendAsync(requestMsg, cancellationToken);
            var responseMsg = await _tcpClient.ReceiveAsync(cancellationToken);
            ValidateResponse(requestMsg, responseMsg, ServerMessageType.ProtocolHandshake);
            return responseMsg.GetPayload<ProtocolHandshakeResponse>();
        }

        private async UniTask ReceiveLoopAsync(int connectionVersion, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var serverMsg = await _tcpClient.ReceiveAsync(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (connectionVersion != _connectionVersion)
                        return;
                    HandleMessage(serverMsg);
                }
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // Disconnect / Dispose가 이미 연결과 요청을 정리했습니다.
            }
            catch (Exception exception)
            {
                // 이전 연결의 지연된 실패가 새 연결을 종료하지 않도록 합니다.
                if (connectionVersion == _connectionVersion)
                    CloseConnection(exception);
            }
        }

        private void HandleMessage(ServerMessage serverMsg)
        {
            if (serverMsg.RequestId is not null)
            {
                _pendingRequestTracker.Complete(serverMsg);
                return;
            }

            switch (serverMsg.Type)
            {
                case ServerMessageType.MatchingFound:
                    HandleMatchingFound(serverMsg);
                    break;
            }
        }

        private void HandleMatchingFound(ServerMessage serverMsg)
        {
            var matchingFound = serverMsg.GetPayload<MatchingFound>();
            if (CurrentMatch?.MatchId == matchingFound.MatchId)
                return;

            if (State == ConnectionState.Connected && _matchingRequestInProgress)
            {
                // 응답 처리가 재개되기 전 수신된 첫 이벤트만 보관합니다.
                _deferredMatchingFound ??= matchingFound;
                return;
            }

            ApplyMatchingFound(matchingFound);
        }

        private void ApplyMatchingFound(MatchingFound matchingFound)
        {
            if (State != ConnectionState.Matching)
            {
                // TODO:
                // 취소 성공 뒤 늦은 MatchingFound 또는 Matched 중 다른 MatchId가 도착할 수 있습니다.
                // 현재는 Matching 이외의 이벤트를 무시하여 Connected나 기존 매칭 정보를 보존합니다.
                // MatchSession / GameSession에서 서버 상태 재동기화와 취소/확정 우선순위를 결정해야 합니다.
                return;
            }

            CurrentMatch = matchingFound;
            ChangeState(ConnectionState.Matched);
        }

        private void CloseConnection(Exception? failure = null)
        {
            ++_connectionVersion;
            var connectionCts = _connectionCts;
            _connectionCts = null;
            _matchingRequestInProgress = false;
            _matchingCancelInProgress = false;
            _deferredMatchingFound = null;
            CurrentMatch = null;
            Failure = failure;
            if (failure is null)
                HandshakeResponse = null;
            State = ConnectionState.Disconnected;

            _tcpClient.Disconnect();
            _pendingRequestTracker.Clear();
            connectionCts?.Cancel();
            connectionCts?.Dispose();
            OnDataChanged?.Invoke();
            // TODO:
            // MatchingFound 직후 연결이 끊겨도 서버에 확정된 매칭이 남아 있을 수 있습니다.
            // 현재는 로컬 CurrentMatch를 지우고 Disconnected로 돌아가며 재연결 시 복원하지 않습니다.
            // GameSession / MatchSession / GameScene에서 재접속 복원과 진행 중 대국 이탈 정책을 결정해야 합니다.
        }

        private void EnsureMatchingOperationAllowed(ConnectionState requiredState)
        {
            ThrowIfDisposed();
            if (State != requiredState)
                throw new InvalidOperationException($"현재 상태에서는 매칭 요청을 처리할 수 없습니다: {State}");
            if (_matchingRequestInProgress || _matchingCancelInProgress)
                throw new InvalidOperationException("이미 매칭 신청 또는 취소 응답을 기다리고 있습니다.");
        }

        private void EnsureCurrentConnection(int connectionVersion)
        {
            if (connectionVersion != _connectionVersion)
                throw new OperationCanceledException("요청을 시작한 연결이 종료되었습니다.");
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;
            OnDataChanged?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OnlineGameClient_V2));
        }

        private static void ValidateResponse(
            ClientMessage request, ServerMessage response, ServerMessageType expectedType)
        {
            if (response.Type != expectedType)
                throw new InvalidOperationException(
                    $"잘못된 서버 응답입니다. Expected: {expectedType}, Actual: {response.Type}");
            if (string.IsNullOrWhiteSpace(request.RequestId))
                throw new InvalidOperationException("요청 메시지에 RequestId가 없습니다.");
            if (request.RequestId != response.RequestId)
                throw new InvalidOperationException(
                    $"RequestId가 일치하지 않습니다. Expected: {request.RequestId}, Actual: {response.RequestId}");
        }
    }
}
