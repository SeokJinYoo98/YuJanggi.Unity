#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Protocol.Messages;
using YuJanggi.Network.Status;

namespace YuJanggi.Network
{
    internal interface ISendOnlyRequestDispatcher : IDisposable
    {
        UniTask<ServerMessage> SendRequestAsync<TPayload>(
            ClientMessageType requestType,
            TPayload payload,
            ServerMessageType expectedResponseType,
            CancellationToken cancellationToken = default);
    }
    /// <summary>RequestId로 응답을 연결하고 요청 대기와 취소를 관리합니다.</summary>
    public sealed class RequestDispatcher : ISendOnlyRequestDispatcher
    {
        private readonly NetworkConnection _connection;
        private readonly PendingRequestTracker _pendingRequestTracker = new();
        private bool _disposed;

        public RequestDispatcher(NetworkConnection connection)
        {
            _connection = connection;
            _connection.OnDataChanged += HandleConnectionChanged;
        }

        public void HandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
                _pendingRequestTracker.Complete(message);
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection.OnDataChanged -= HandleConnectionChanged;
            _pendingRequestTracker.Clear();
        }
        public async UniTask<ServerMessage> SendRequestAsync<TPayload>(
            ClientMessageType requestType,
            TPayload payload,
            ServerMessageType expectedResponseType,
            CancellationToken cancellationToken = default)
        {
            EnsureAvailable(cancellationToken);

            var request = CreateRequest(requestType, payload);
            string requestId = GetRequestId(request);
            int version = _connection.Version;

            using var requestCts = CreateRequestCts(cancellationToken);
            var requestToken = requestCts.Token;

            _pendingRequestTracker.Add(requestId, expectedResponseType);

            try
            {
                await SendAsync(request, version, requestToken);

                return await WaitResponseAsync(
                    requestId,
                    version,
                    requestToken);
            }
            finally
            {
                _pendingRequestTracker.Remove(requestId);
            }
        }

        private void EnsureAvailable(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RequestDispatcher));

            cancellationToken.ThrowIfCancellationRequested();
        }

        private static ClientMessage CreateRequest<TPayload>(
            ClientMessageType requestType,
            TPayload payload)
        {
            return ClientMessageFactory.CreateRequest(
                requestType,
                payload);
        }

        private static string GetRequestId(ClientMessage request)
        {
            return request.RequestId
                ?? throw new InvalidOperationException(
                    "RequestId가 생성되지 않았습니다.");
        }

        private CancellationTokenSource CreateRequestCts(
            CancellationToken cancellationToken)
        {
            return CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _connection.LifetimeToken);
        }

        private async UniTask SendAsync(
            ClientMessage request,
            int version,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _connection.SendAsync(
                request,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _connection.EnsureCurrentConnection(version);
        }

        private async UniTask<ServerMessage> WaitResponseAsync(
            string requestId,
            int version,
            CancellationToken cancellationToken)
        {
            var response =
                await _pendingRequestTracker.WaitAsync(
                    requestId,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _connection.EnsureCurrentConnection(version);

            return response;
        }
        private void HandleConnectionChanged()
        {
            if (_connection.State == ConnectionState.Disconnected)
                _pendingRequestTracker.Clear();
        }

    }
}


