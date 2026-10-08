using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.Network.Handler
{
    using YuJanggi.Protocol.Messages;
    internal interface INetworkHandler : IDisposable
    {
        void HandleMessage(ServerMessage message);
    }

    internal abstract class NetworkHandler : INetworkHandler
    {
        #region Fields
        private readonly NetworkClient      _client;
        private readonly RequestDispatcher  _requests;

        private bool _disposed;
        #endregion

        #region Constructors
        protected NetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client));

            _requests = requests
                ?? throw new ArgumentNullException(nameof(requests));
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
        protected async UniTask SendAsync(
            ClientMessage message,
            CancellationToken token = default)
        {
            EnsureAvailable(token);

            await _client.SendAsync(message, token);
        }

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
            CancellationToken token = default)
        {
            EnsureAvailable(token);

            var message = ClientMessageFactory.Create(
                messageType,
                payload);

            await _client.SendAsync(
                message,
                token);
        }

        /// <summary>
        /// Request를 보내고 대응 Response를 기다림
        /// </summary>
        protected async UniTask<ServerMessage> SendRequestAsync<TPayload>(
            ClientMessageType requestType,
            TPayload payload,
            ServerMessageType responseType,
            CancellationToken token = default)
        {
            EnsureAvailable(token);

            var request = ClientMessageFactory.CreateRequest(
                requestType,
                payload);

            var response = await _requests.SendRequestAsync(
                request,
                responseType,
                token);

            return response;
        }


        protected virtual void OnDispose()
        {
        }

        protected void EnsureAvailable(
            CancellationToken token = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);

            token.ThrowIfCancellationRequested();
        }
        #endregion
    }
}
