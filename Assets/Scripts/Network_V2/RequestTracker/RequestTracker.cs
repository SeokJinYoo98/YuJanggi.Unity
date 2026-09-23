
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using YuJanggi.Protocol.V2.Messages;

namespace YuJanggi.Network.V2
{
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

        public void Add(
            string requestId,
            ServerMessageType expectedResponseType)
        {
            ValidateRequestId(requestId);

            _pendingRequests.Add(
                requestId,
                new PendingRequest(expectedResponseType));
        }

        public async UniTask<ServerMessage> WaitAsync(
            string requestId,
            CancellationToken cancellationToken = default)
        {
            ValidateRequestId(requestId);

            if (!_pendingRequests.TryGetValue(
                requestId,
                out var pendingRequest))
            {
                throw new InvalidOperationException(
                    $"대기 중인 요청이 없습니다. RequestId: {requestId}");
            }

            return await pendingRequest.CompletionSource.Task
                .AttachExternalCancellation(cancellationToken);
        }

        public bool Remove(string requestId)
        {
            ValidateRequestId(requestId);

            return _pendingRequests.Remove(requestId);
        }

        public void Clear()
        {
            _pendingRequests.Clear();
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
                throw new InvalidOperationException(
                    $"대기 중인 요청이 없습니다. RequestId: {serverMsg.RequestId}");
            }

            if (pendingRequest.ExpectedResponseType != serverMsg.Type)
            {
                throw new InvalidOperationException(
                    $"응답 타입이 일치하지 않습니다. " +
                    $"Expected: {pendingRequest.ExpectedResponseType}, " +
                    $"Actual: {serverMsg.Type}");
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
