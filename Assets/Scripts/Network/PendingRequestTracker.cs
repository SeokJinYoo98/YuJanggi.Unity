using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace YuJanggi.Network
{
    using YuJanggi.Protocol.Messages;
    public sealed class PendingRequestTracker
    {
        private sealed class PendingRequest
        {
            public ServerMessageType ExpectedResponseType { get; }
            public UniTaskCompletionSource<ServerMessage> CompletionSource { get; }

            public PendingRequest(
                ServerMessageType expectedResponseType)
            {
                ExpectedResponseType = expectedResponseType;
                CompletionSource =
                    new UniTaskCompletionSource<ServerMessage>();
            }
        }

        private readonly Dictionary<string, PendingRequest> _pendingRequests;

        public PendingRequestTracker()
        {
            _pendingRequests =
                new Dictionary<string, PendingRequest>();

            
        }

        public UniTask<ServerMessage> Add(
            string requestId,
            ServerMessageType expectedResponseType)
        {
            ValidateRequestId(requestId);

            var pendingRequest = new PendingRequest(expectedResponseType);
            _pendingRequests.Add(requestId, pendingRequest);
            return pendingRequest.CompletionSource.Task;
        }

        public bool Remove(string requestId)
        {
            ValidateRequestId(requestId);

            return _pendingRequests.Remove(requestId);
        }

        public void Clear()
        {
            // 완료 시 요청 메서드의 finally가 재진입할 수 있으므로 먼저 컬렉션을 비웁니다.
            var pendingRequests = new List<PendingRequest>(_pendingRequests.Values);
            _pendingRequests.Clear();
            foreach (var pendingRequest in pendingRequests)
                pendingRequest.CompletionSource.TrySetCanceled();
        }
        public void Complete(ServerMessage serverMsg)
        {
            if (string.IsNullOrWhiteSpace(serverMsg.RequestId))
            {
                throw new InvalidOperationException(
                    "응답 메시지에 RequestId가 없습니다.");
            }

            if (!_pendingRequests.TryGetValue(
                serverMsg.RequestId,
                out var pendingRequest))
            {
                // 취소·연결 종료 후 늦게 도착한 응답이나 중복 응답은 새 요청을 완료하지 않습니다.
                return;
            }

            if (pendingRequest.ExpectedResponseType != serverMsg.Type)
            {
                pendingRequest.CompletionSource.TrySetException(new InvalidOperationException(
                    $"응답 타입이 일치하지 않습니다. " +
                    $"Expected: {pendingRequest.ExpectedResponseType}, " +
                    $"Actual: {serverMsg.Type}"));
                return;
            }

            pendingRequest.CompletionSource
                .TrySetResult(serverMsg);
        }
        private static void ValidateRequestId(string requestId)
        {
            if (string.IsNullOrWhiteSpace(requestId))
            {
                throw new ArgumentException(
                    "RequestId는 비어 있을 수 없습니다.",
                    nameof(requestId));
            }
        }
    }
}


