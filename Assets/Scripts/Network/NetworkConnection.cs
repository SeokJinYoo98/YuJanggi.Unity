#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Core.V2;
using YuJanggi.Protocol.V2;
using YuJanggi.Protocol.V2.Connection;
using YuJanggi.Protocol.V2.Messages;
using YuJanggi.Protocol.V2.Messages.MessageFactory;
using YuJanggi.Network.Status;

namespace YuJanggi.Network
{
    /// <summary>TCP 연결, Handshake 및 수신 루프의 수명주기를 관리합니다.</summary>
    public sealed class NetworkConnection : IDisposable
    {
        private readonly TcpTransport _tcpClient;
        private CancellationTokenSource? _connectionCts;
        private int _connectionVersion;
        private bool _disposed;
        private bool _closing;

        public ProtocolHandshakeResponse? HandshakeResponse { get; private set; }
        public Exception? Failure { get; private set; }
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public bool IsOnline => State == ConnectionState.Connected;
        public int Version => _connectionVersion;
        public CancellationToken LifetimeToken => _connectionCts?.Token
            ?? throw new InvalidOperationException("서버에 연결되어 있지 않습니다.");

        public event Action? OnDataChanged;
        public event Action<ServerMessage>? MessageReceived;
        public event Action? ConnectionClosed;

        public NetworkConnection(string host, int port)
        {
            _tcpClient = new TcpTransport(host, port);
        }

        public UniTask SendAsync(ClientMessage message, CancellationToken cancellationToken = default)
        {
            EnsureOnline();
            return _tcpClient.SendAsync(message, cancellationToken);
        }

        public void EnsureOnline()
        {
            ThrowIfDisposed();
            if (!IsOnline)
                throw new InvalidOperationException("Handshake가 완료된 연결이 필요합니다.");
        }

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

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_closing || State != ConnectionState.Disconnected)
                throw new InvalidOperationException("이미 연결 중이거나 서버에 연결되어 있습니다.");

            int connectionVersion = ++_connectionVersion;
            _connectionCts = new CancellationTokenSource();
            CancellationToken connectionToken = _connectionCts.Token;
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, connectionToken);
            var connectToken = connectCts.Token;
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
        public void Disconnect()
            => CloseConnection();

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _tcpClient.Dispose();
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
                    MessageReceived?.Invoke(serverMsg);
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

        private void CloseConnection(Exception? failure = null)
        {
            if (_closing)
                return;
            _closing = true;
            ++_connectionVersion;
            var connectionCts = _connectionCts;
            _connectionCts = null;
            Failure = failure;
            if (failure is null)
                HandshakeResponse = null;
            State = ConnectionState.Disconnected;

            try
            {
                _tcpClient.Disconnect();
                // 상태를 먼저 초기화하고, 취소 continuation과 pending 정리를 이어서 실행합니다.
                ConnectionClosed?.Invoke();
            }
            finally
            {
                try
                {
                    connectionCts?.Cancel();
                }
                finally
                {
                    connectionCts?.Dispose();
                    try { OnDataChanged?.Invoke(); }
                    finally { _closing = false; }
                }
            }
        }

        public void EnsureCurrentConnection(int connectionVersion)
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
                throw new ObjectDisposedException(nameof(NetworkConnection));
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
