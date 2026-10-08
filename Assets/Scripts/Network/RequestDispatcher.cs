#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.Network
{
    using YuJanggi.Protocol.Messages;

    internal interface ISendOnlyRequestDispatcher : IDisposable
    {
        UniTask<ServerMessage> SendRequestAsync(
            ClientMessage request,
            ServerMessageType expectedResponseType,
            CancellationToken cancellationToken = default);
    }

    /// <summary>전달받은 요청을 보내고 RequestId에 대응하는 응답을 반환합니다.</summary>
    public sealed class RequestDispatcher : ISendOnlyRequestDispatcher
    {
        private readonly NetworkClient _client;
        private readonly PendingRequestTracker _pendingRequestTracker = new();
        private bool _disposed;

        public RequestDispatcher(NetworkClient client)
        {
            _client = client
                ?? throw new ArgumentNullException(nameof(client));
        }

        public async UniTask<ServerMessage> SendRequestAsync(
            ClientMessage request,
            ServerMessageType expectedResponseType,
            CancellationToken token = default)
        {
            EnsureAvailable(token);

            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (string.IsNullOrWhiteSpace(request.RequestId))
                throw new ArgumentException(
                    "RequestId가 없는 요청입니다.",
                    nameof(request));

            string requestId = request.RequestId;
            var responseTask = _pendingRequestTracker.Add(
                requestId,
                expectedResponseType);
            try
            {
                await _client.SendAsync(request, token);
                token.ThrowIfCancellationRequested();

                var response = await responseTask.AttachExternalCancellation(token);
                token.ThrowIfCancellationRequested();
                return response;
            }
            finally
            {
                _pendingRequestTracker.Remove(requestId);
            }
        }

        public void HandleMessage(ServerMessage message)
        {
            if (_disposed)
                return;

            if (message is null)
                throw new ArgumentNullException(
                    nameof(message));

            if (!string.IsNullOrWhiteSpace(message.RequestId))
                _pendingRequestTracker.Complete(message);
        }

        public void Clear()
        {
            if (!_disposed)
                _pendingRequestTracker.Clear();
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _pendingRequestTracker.Clear();
        }

        private void EnsureAvailable(
            CancellationToken token)
        {
            if (_disposed)
                throw new ObjectDisposedException(
                    nameof(RequestDispatcher));

            token.ThrowIfCancellationRequested();
        }
    }
}
