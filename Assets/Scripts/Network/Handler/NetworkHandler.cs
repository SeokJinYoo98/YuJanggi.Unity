using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Protocol.Messages;

namespace YuJanggi.Network.Handler
{
    internal interface INetworkHandler : IDisposable
    {
        void HandleMessage(ServerMessage message);
    }

    internal abstract class NetworkHandler : INetworkHandler
    {
        protected NetworkConnection Connection => _connection;
        #region Fields
        private readonly NetworkConnection _connection;
        private readonly ISendOnlyRequestDispatcher _requests;

        private bool _disposed;
        #endregion

        #region Constructors
        protected NetworkHandler(
            NetworkConnection connection,
            RequestDispatcher requests)
        {
            _connection = connection;
            _requests = requests;
        }
        #endregion

        #region Public Methods
        public void HandleMessage(ServerMessage message)
        {
            if (_disposed)
                return;

            OnHandleMessage(message);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            OnDispose();
        }

        #endregion

        #region Protected Methods
        /// <summary>
        /// Handler별 서버 이벤트 처리
        /// </summary>
        protected abstract void OnHandleMessage(ServerMessage message);

        /// <summary>
        /// Response를 기다리지 않는 메시지 전송
        /// </summary>
        protected async UniTask SendAsync<TPayload>(
            ClientMessageType messageType,
            TPayload payload,
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);

            int version = _connection.Version;

            using var cts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _connection.LifetimeToken);

            var message = ClientMessageFactory.Create(
                messageType,
                payload);

            await _connection.SendAsync(
                message,
                cts.Token);

            cts.Token.ThrowIfCancellationRequested();
            _connection.EnsureCurrentConnection(version);
        }

        /// <summary>
        /// Request를 보내고 대응 Response를 기다림
        /// </summary>
        protected async UniTask<ServerMessage> SendRequestAsync<TPayload>(
            ClientMessageType requestType,
            TPayload payload,
            ServerMessageType responseType,
            CancellationToken cancellationToken = default)
        {
            var response = await _requests.SendRequestAsync(
                requestType,
                payload,
                responseType,
                cancellationToken);
            return response;
        }

        /// <summary>
        /// Handler별 종료 처리
        /// </summary>
        protected virtual void OnDispose()
        {
        }
        /// <summary>
        /// Handler가 이미 Dispose됐는가? → 그러면 예외 
        /// 요청이 이미 취소됐는가? → 그러면 OperationCanceledException
        /// 서버에 연결되어 있는가?→ 아니면 연결 관련 예외
        ///  전부 정상 → 요청 전송
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        protected void EnsureConnected(
            CancellationToken cancellationToken = default)
        {
            
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            cancellationToken.ThrowIfCancellationRequested();

            _connection.EnsureOnline();
        }
        #endregion
    }
}
