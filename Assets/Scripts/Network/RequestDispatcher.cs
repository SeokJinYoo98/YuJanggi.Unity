#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Protocol.V2.Messages;
using YuJanggi.Protocol.V2.Messages.MessageFactory;
using YuJanggi.Network.Status;

namespace YuJanggi.Network
{
    /// <summary>RequestId로 응답을 연결하고 요청 대기와 취소를 관리합니다.</summary>
    public sealed class RequestDispatcher : IDisposable
    {
        private readonly NetworkConnection _connection;
        private readonly PendingRequestTracker _pendingRequestTracker = new();
        private bool _disposed;

        public RequestDispatcher(NetworkConnection connection)
        {
            _connection = connection;
            _connection.MessageReceived += HandleMessage;
            // ConnectionClosed에서 도메인 초기화가 끝난 뒤 pending을 정리합니다.
            _connection.OnDataChanged += HandleConnectionChanged;
        }

        public async UniTask<ServerMessage> SendAsync<TPayload>(
            ClientMessageType requestType, TPayload payload, ServerMessageType expectedResponseType,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(RequestDispatcher));
            _connection.EnsureOnline();
            int version = _connection.Version;
            var request = ClientMessageFactory.Create(requestType, payload);
            string requestId = request.RequestId!;
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _connection.LifetimeToken);
            var requestToken = requestCts.Token;
            _pendingRequestTracker.Add(requestId, expectedResponseType);

            try
            {
                requestToken.ThrowIfCancellationRequested();
                await _connection.SendAsync(request, requestToken);
                requestToken.ThrowIfCancellationRequested();
                _connection.EnsureCurrentConnection(version);
                var response = await _pendingRequestTracker.WaitAsync(requestId, requestToken);
                requestToken.ThrowIfCancellationRequested();
                _connection.EnsureCurrentConnection(version);
                return response;
            }
            finally
            {
                _pendingRequestTracker.Remove(requestId);
            }
            // TODO:
            // 전송 후 호출자 토큰만 취소되면 서버가 처리했어도 로컬 응답 대기는 끝납니다.
            // 현재 늦은 응답은 무시되므로 서버와 로컬 상태가 달라질 수 있습니다.
            // MatchSession / GameSession에서 상태 재조회 또는 서버 취소 정책을 결정해야 합니다.
        }

        private void HandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
                _pendingRequestTracker.Complete(message);
        }

        private void HandleConnectionChanged()
        {
            if (_connection.State == ConnectionState.Disconnected)
                _pendingRequestTracker.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection.MessageReceived -= HandleMessage;
            _connection.OnDataChanged -= HandleConnectionChanged;
            _pendingRequestTracker.Clear();
        }
    }
}
