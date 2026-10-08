#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;


namespace YuJanggi.Lobby.Handler
{
    using Protocol.Connection;
    using Protocol.Matching;
    using Protocol.Messages;

    using Network;
    using Network.Handler;

    // Protocol 요청을 생성하고 전송한다.
    // 응답을 해석해 Panel이 판단할 결과를 반환한다.
    // 예상 가능한 통신·응답 오류를 실패 결과로 변환한다.

    public enum MatchRequestResult
        { Success, Failed, InvalidResponse }

    internal sealed class LobbyNetworkHandler : NetworkHandler
    {
        private UniTaskCompletionSource _matchFound = new();

        public LobbyNetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        {
        }

        public async UniTask<bool> HandshakeAsync(
            CancellationToken token)
        {
            try
            {
                var msgType = ClientMessageType.HandshakeRequest;
                var payload = new ProtocolHandshakeRequest
                {
                    YuJanggiProtocolVersion = Protocol.Version.Version.Current,
                    YuJanggiCoreVersion = Engine.Version.Version.Current
                };

                var response = await SendRequestAsync(
                    msgType,
                    payload,
                    ServerMessageType.HandshakeResponse,
                    token);

                var handshake =
                    response.GetPayload<ProtocolHandshakeResponse>();

                return handshake.Result ==
                       ProtocolHandshakeResult.Success;
            }
            catch (Exception exception) when (
                exception is InvalidDataException or JsonException)
            {
                // Json 역직렬화 실패
                // 
                return false;
            }
        }
        public async UniTask<MatchRequestResult> MatchRequestAsync(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var previous = _matchFound;
            _matchFound = new UniTaskCompletionSource();
            previous.TrySetCanceled();
 
            try
            {
                var response = await SendRequestAsync(
                    ClientMessageType.MatchingStartRequest,
                    new MatchingStartRequest(),
                    ServerMessageType.MatchingStartResponse,
                    token);

                var result = response.GetPayload<MatchingStartResponse>().Result;

                return ToMatchRequestResult(result);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or JsonException)
            {
                // 응답 Payload가 없거나 올바르게 해석할 수 없습니다.
                return MatchRequestResult.InvalidResponse;
            }
        }
        public async UniTask WaitForMatchFoundEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _matchFound;

            await completion.Task
                .AttachExternalCancellation(token);
        }
        public async UniTask SubmitFormation(
            CancellationToken token)
        {

        }
        public async UniTask WaitForGameReadyEvent(
            CancellationToken token)
        {

        }
        protected override void OnHandleMessage(
            ServerMessage message)
        {
            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                    _matchFound.TrySetResult();
                    break;

                default:
                    break;
            }
        }

        protected override void OnDispose()
        {
            _matchFound.TrySetCanceled();
        }

        private static MatchRequestResult ToMatchRequestResult(MatchingResult result)
        {
            return result switch
            {
                MatchingResult.Accepted
                    => MatchRequestResult.Success,

                MatchingResult.AlreadyMatching
                    => MatchRequestResult.Failed,

                MatchingResult.AlreadyMatched
                    => MatchRequestResult.Failed,

                MatchingResult.HandshakeRequired
                    => MatchRequestResult.Failed,

                _ => MatchRequestResult.InvalidResponse
            };
        }
    }
}
