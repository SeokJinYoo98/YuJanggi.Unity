#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

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
        bool                        IsOnline { get; }
        bool                        IsConnected { get; }
        ConnectionState             State { get; }
        ProtocolHandshakeResponse?  HandshakeResponse { get; }
        Exception?                  Failure { get; }
        event Action?               OnDataChanged;
        UniTask ConnectAsync(CancellationToken cancellationToken = default);
        UniTask MatchRequestAsync(CancellationToken cancellationToken = default);
        void Disconnect();
    }

    /// <summary>
    /// TCP 클라이언트를 기반으로 서버 연결, 핸드셰이크, 메시지 수신 루프,
    /// 서버 메시지 해석 및 온라인 상태 관리를 담당합니다.
    /// </summary>
    public sealed class OnlineGameClient_V2 : IOnlineGameClient_V2
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        private readonly TcpGameClient_V2       _tcpClient;
        private readonly PendingRequestTracker  _pendingRequestTracker;
        private CancellationTokenSource?        _receiveCts;

        private bool _disposed;

        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        public ProtocolHandshakeResponse? HandshakeResponse { get; private set; }
        public bool IsOnline =>
            State is ConnectionState.Connected or
            ConnectionState.Matching;
        public bool IsConnected =>
            State == ConnectionState.Connected &&
            _tcpClient.IsConnected;

        public ConnectionState State { get; private set; }
            = ConnectionState.Disconnected;

        public Exception? Failure { get; private set; }
        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트
        public event Action? OnDataChanged;
        #endregion

        #region Constructors
        // 순수 C#
        public OnlineGameClient_V2(string host, int port)
        {
            _pendingRequestTracker = new();
            _tcpClient      = new TcpGameClient_V2(host, port);
        }
        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        public async UniTask MatchRequestAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            EnsureConnected();

            var request = new MatchingRequest();

            var requestMsg =
                ClientMessageFactory.Create(
                    ClientMessageType.MatchingRequest,
                    request);

            string requestId = requestMsg.RequestId!;

            // 어떤 응답을 기다리는 요청인지 등록
            _pendingRequestTracker.Add(
                requestId,
                ServerMessageType.MatchingResponse);

            try
            {
                await _tcpClient.SendAsync(
                    requestMsg,
                    cancellationToken);

                // 여기서 MatchingResponse가 올 때까지 기다림
                ServerMessage responseMsg =
                    await _pendingRequestTracker.WaitAsync(
                        requestId,
                        cancellationToken);

                var response =
                    responseMsg.GetPayload<MatchingResponse>();

                // Accepted라면 상태 변경
                ChangeState(ConnectionState.Matching);
            }
            finally
            {
                _pendingRequestTracker.Remove(requestId);
            }
        }
        public async UniTask ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (State != ConnectionState.Disconnected)
                throw new InvalidOperationException(
                    "이미 연결 중이거나 서버에 연결되어 있습니다.");

            HandshakeResponse = null;
            Failure = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Connecting);

                await _tcpClient.ConnectAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Handshaking);

                await HandshakeAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ReceiveLoopAsync(CancellationToken.None).Forget();
                ChangeState(ConnectionState.Connected);
            }
            catch (Exception)
                when (cancellationToken.IsCancellationRequested)
            {
                _tcpClient.Disconnect();

                HandshakeResponse = null;
                Failure = null;

                ChangeState(ConnectionState.Disconnected);

                throw new OperationCanceledException(
                    cancellationToken);
            }
            catch (Exception exception)
            {
                _tcpClient.Disconnect();

                Failure = exception;

                ChangeState(ConnectionState.Disconnected);

                throw;
            }
        }
        public void Disconnect()
        {
            _receiveCts?.Cancel();

            _tcpClient.Disconnect();

            HandshakeResponse = null;
            Failure = null;

            ChangeState(ConnectionState.Disconnected);
        }
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _tcpClient.Dispose();
        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        private async UniTask HandshakeAsync(
            CancellationToken cancellationToken)
        {
            var request = new ProtocolHandshakeRequest
            {
                YuJanggiProtocolVersion = ProtocolVersion.Current,
                YuJanggiCoreVersion = CoreVersion.Current
            };
            ClientMessage requestMsg = ClientMessageFactory.Create(
                ClientMessageType.ProtocolHandshake, request);

            await _tcpClient.SendAsync(requestMsg, cancellationToken);

            ServerMessage responseMsg =
                await _tcpClient.ReceiveAsync(cancellationToken);

            ValidateCheck(
                requestMsg,
                responseMsg,
                ServerMessageType.ProtocolHandshake);

            HandshakeResponse = responseMsg.GetPayload<ProtocolHandshakeResponse>();

            if (HandshakeResponse.Result != ProtocolHandshakeResult.Success)
                throw new InvalidOperationException($"핸드셰이크 실패: {HandshakeResponse.Result}");
        }
        private async UniTask ReceiveLoopAsync(
            CancellationToken cancellationToken)
        {
            using var receiveCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            _receiveCts = receiveCts;

            try
            {
                while (!receiveCts.Token.IsCancellationRequested)
                {
                    ServerMessage serverMsg =
                        await _tcpClient.ReceiveAsync(
                            receiveCts.Token);

                    HandleMessage(serverMsg);
                }
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested || receiveCts.IsCancellationRequested)
            {
                // Disconnect / Dispose 등 정상적인 취소
            }
            catch (Exception exception)
            {
                Failure = exception;

                _tcpClient.Disconnect();

                ChangeState(
                    ConnectionState.Disconnected);
            }
            finally
            {
                // 연결 종료 시 매칭 응답 대기도 끝냅니다.
                receiveCts.Cancel();
                if (_receiveCts == receiveCts)
                    _receiveCts = null;
            }
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
        private void EnsureConnected()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException(
                    "서버에 연결되어 있지 않습니다.");
            }
        }

        private void HandleMessage(ServerMessage serverMsg)
        {
            switch (serverMsg.Type)
            {
                case ServerMessageType.MatchingResponse:
                    ChangeState(ConnectionState.Matching);
                    break;
            }
        }

        private static void ValidateCheck(
            ClientMessage requestMsg,
            ServerMessage responseMsg,
            ServerMessageType expectedType)
        {
            ValidateResponseType(
                responseMsg,
                expectedType);

            ValidateRequestId(
                requestMsg,
                responseMsg);
        }
        private static void ValidateRequestId(
            ClientMessage requestMsg,
            ServerMessage responseMsg)
        {
            if (string.IsNullOrEmpty(requestMsg.RequestId))
            {
                throw new InvalidOperationException(
                    "요청 메시지에 RequestId가 없습니다.");
            }

            if (requestMsg.RequestId != responseMsg.RequestId)
            {
                throw new InvalidOperationException(
                    $"RequestId가 일치하지 않습니다. " +
                    $"Expected: {requestMsg.RequestId}, " +
                    $"Actual: {responseMsg.RequestId}");
            }
        }
        private static void ValidateResponseType(
            ServerMessage responseMsg,
            ServerMessageType expectedType)
        {
            if (responseMsg.Type != expectedType)
            {
                throw new InvalidOperationException(
                    $"잘못된 서버 응답입니다. " +
                    $"Expected: {expectedType}, " +
                    $"Actual: {responseMsg.Type}");
            }
        }
        #endregion

    }
}
