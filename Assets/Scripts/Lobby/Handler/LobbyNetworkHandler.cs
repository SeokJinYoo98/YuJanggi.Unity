#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;


namespace YuJanggi.Lobby.Handler
{
    using Engine.Domain;

    using Network;
    using Network.Mapper;
    using Network.Handler;

    using Protocol.Connection;
    using Protocol.Matching;
    using Protocol.Messages;
    public enum MatchRequestResult
        { Success, Failed, InvalidResponse }

    internal sealed class LobbyNetworkHandler : NetworkHandler
    {
        private UniTaskCompletionSource<NetworkMatchingData>  _matchFound = new();
        private UniTaskCompletionSource<NetworkFormationData> _gameReady = new();
        public LobbyNetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        { }

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

            var prev = _matchFound;
            _matchFound = new UniTaskCompletionSource<NetworkMatchingData>();
            prev.TrySetCanceled();

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
        public async UniTask<NetworkMatchingData> WaitForMatchFoundEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _matchFound;
            return await completion.Task.AttachExternalCancellation(token);
        }
        public async UniTask SubmitFormation(
            string matchId,
            Formation formation,
            CancellationToken token)
        {
            EnsureAvailable(token);

            var prev = _gameReady;
            _gameReady = new UniTaskCompletionSource<NetworkFormationData>();
            prev.TrySetCanceled();

            await SendAsync(
                ClientMessageType.FormationSubmit,
                new FormationSubmit
                {
                    MatchId   = matchId,
                    Formation = ProtocolMapper.ToProtocolFormation(formation)
                },
                token);
        }
        public async UniTask<NetworkFormationData> WaitForGameReadyEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _gameReady;
            return await completion.Task.AttachExternalCancellation(token);
        }
        protected override void OnHandleMessage(
            ServerMessage message)
        {
            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                    MatchFounded(message);
                    break;
                case ServerMessageType.GameReadyEvent:
                    GameReadyEvent(message);
                    break;
                default:
                    break;
            }
        }
        private void GameReadyEvent(ServerMessage msg)
        {
            var payload = msg.GetPayload<GameReadyEvent>();

            _gameReady.TrySetResult(new NetworkFormationData
            {
                MatchId = payload.MatchId,
                Cho = ProtocolMapper.ToFormation(payload.ChoFormation),
                Han = ProtocolMapper.ToFormation(payload.HanFormation)
            });
        }
        private void MatchFounded(ServerMessage msg)
        {
            var payload = msg.GetPayload<MatchingFoundEvent>();

            _matchFound.TrySetResult(new NetworkMatchingData
            {
                MatchId          = payload.MatchId,
                MyTeam             = ProtocolMapper.ToPlayerTeam(payload.MyTeam),
                OpponentPlayerId = payload.Opponent.PlayerId,
                OpponentNickname = payload.Opponent.PlayerNickname,
                OpponentTeam     = ProtocolMapper.ToPlayerTeam(payload.Opponent.PlayerTeam)
            });
        }
        protected override void OnDispose()
        {
            _matchFound.TrySetCanceled();
        }
        private static MatchRequestResult ToMatchRequestResult(MatchingResult result)
        {
            return result switch
            {
                MatchingResult.Accepted => MatchRequestResult.Success,

                MatchingResult.AlreadyMatching
                    or MatchingResult.AlreadyMatched
                    or MatchingResult.HandshakeRequired
                    or MatchingResult.ServerError => MatchRequestResult.Failed,

                _ => MatchRequestResult.InvalidResponse
            };
        }
    }
}
