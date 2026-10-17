using System;
using Cysharp.Threading.Tasks;
using System.Threading;



namespace YuJanggi.InGame.Handler
{
    using Engine.Domain;

    using Protocol.InGame;
    using Protocol.Messages;

    using Network;
    using Network.Mapper;
    using Network.Handler;
    internal interface IInGameNetworkReceiver
    {
        void ApplyServerGameStart(NetworkGameStartData data);
        void ApplyServerMove(NetworkMoveData data);

        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves);
    }
    /// <summary>Protocol 생성·해석과 대기만 담당하며 게임 상태나 화면은 조작하지 않습니다.</summary>
    internal sealed class InGameNetworkHandler : NetworkHandler
    {
        private IInGameNetworkReceiver _receiver;
        private UniTaskCompletionSource _gameStart;
        private UniTaskCompletionSource _rematchConfirmed;

        public InGameNetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        { }
        public void SetReceiver(IInGameNetworkReceiver
            receiver)
            => _receiver = receiver;

        public async UniTask        SendInGameReadyAsync(
            CancellationToken token)
        {
            EnsureAvailable(token);
            ResetCompletion(ref _gameStart);

            await SendAsync(
                ClientMessageType.GameSceneReady,
                new GameSceneReady(),
                token);
        }
        public async UniTask        WaitForGameStartEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _gameStart
                ?? throw new InvalidOperationException(
                    "게임 시작 대기가 준비되지 않았습니다.");

            await completion.Task.AttachExternalCancellation(token);
        }
        public async UniTask<bool>  SendMoveRequestAsync(
            PlayerTeam team,
            int turnNumber,
            Pos from,
            Pos to,
            CancellationToken token)
        {
            EnsureAvailable(token);

            var fromPos = ProtocolMapper.ToProtocolPosition(from);
            var toPos = ProtocolMapper.ToProtocolPosition(to);

            var request = new MovePieceRequest
            {
                Team = ProtocolMapper.ToProtocolPlayerTeam(team),
                TurnNumber = turnNumber,

                FromX = fromPos.X,
                FromZ = fromPos.Z,

                ToX = toPos.X,
                ToZ = toPos.Z
            };

            var response = await SendRequestAsync(
                    ClientMessageType.MovePieceRequest,
                    request,
                    ServerMessageType.MovePieceResponse,
                    token);

            var result = response.GetPayload<MovePieceResponse>().Result;
            if (result == MovePieceResult.Accepted)
                return true;

            return false;
        }
        // 외부 종료 요청용 API입니다. 정상 Engine 결과는 서버 GameEndedEvent로 확정합니다.
        // 현재 기권은 별도의 SendGiveUpRequest를 사용합니다.
        public async UniTask<bool> SendGameEndRequest(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token)
        {
            EnsureAvailable(token);

            var request = new GameEndRequest
            {
                EndReason   = ProtocolMapper.ToProtocolGameEndReason(info.Type),
                Winner      = ProtocolMapper.ToProtocolPlayerTeam(info.Winner),
                TotalMoves  = totalMoves
            };

            var response = await SendRequestAsync(
                ClientMessageType.GameEndRequest,
                request,
                ServerMessageType.GameEndResponse,
                token);

            var result = response.GetPayload<GameRequestResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }

        public async UniTask<bool> SendGiveUpRequest(
            PlayerTeam          myTeam,
            CancellationToken   token)
        {
            EnsureAvailable(token);

            var response = await SendRequestAsync(
                ClientMessageType.GiveUpRequest,
                new { Requester = ProtocolMapper.ToProtocolPlayerTeam(myTeam) },
                ServerMessageType.GiveUpResponse,
                token);

            var result = response.GetPayload<GameRequestResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }
        public async UniTask<bool> SendPassTurnRequest(
            PlayerTeam myTeam,
            int turnNumber,
            CancellationToken token)
        {
            EnsureAvailable(token);

            var response = await SendRequestAsync(
                ClientMessageType.PassTurnRequest,
                new GamePlayerRequest
                {
                    Requester = ProtocolMapper.ToProtocolPlayerTeam(myTeam),
                    TurnNumber = turnNumber
                },
                ServerMessageType.PassTurnResponse,
                token);

            var result = response.GetPayload<GameRequestResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }
        public async UniTask<bool> SendRematchRequest(
            PlayerTeam myTeam,
            CancellationToken token)
        {
            EnsureAvailable(token);
            ResetCompletion(ref _rematchConfirmed);

            var response = await SendRequestAsync(
                ClientMessageType.RematchRequest,
                new { Requester = ProtocolMapper.ToProtocolPlayerTeam(myTeam)},
                ServerMessageType.RematchResponse,
                token);

            return response.GetPayload<GameRequestResponse>().Result
                == GameRequestResult.Accepted;
        }
        public async UniTask WaitForRematchConfirmedEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _rematchConfirmed;
            await completion.Task.AttachExternalCancellation(token);
        }
        protected override void OnHandleMessage(
            ServerMessage msg)
        {
            switch (msg.Type)
            {
                case ServerMessageType.GameStartEvent:
                    OnServerGameStart(msg);
                    break;
                case ServerMessageType.TurnCompletedEvent:
                    OnServerTurnCompleted(msg);
                    break;

                case ServerMessageType.GameEndedEvent:
                    OnServerGameEnded(msg);
                    break;
                case ServerMessageType.RematchConfirmedEvent:
                    _rematchConfirmed?.TrySetResult();
                    break;

                default:
                    break;
            }
        }
        private void OnServerGameStart(ServerMessage msg)
        {
            var payload = msg.GetPayload<GameStartEvent>();
            var data = new NetworkGameStartData(
                payload.StartedAt,
                payload.TurnTimeSeconds,
                payload.TurnNumber,
                ProtocolMapper.ToPlayerTeamOrNone(payload.CurrentTeam),
                payload.TurnDeadlineAt);

            // 시작 데이터는 한 경로로 먼저 전달하고 기존 대기는 시작 신호로만 사용합니다.
            _receiver?.ApplyServerGameStart(data);
            _gameStart?.TrySetResult();
        }

        private void OnServerTurnCompleted(
            ServerMessage msg)
        {
            var payload = msg.GetPayload<TurnCompletedEvent>();

            var team = ProtocolMapper.ToPlayerTeam(payload.Team);
            var from = payload.Action == ProtocolTurnAction.Move
                ? ProtocolMapper.ToPos(payload.FromX, payload.FromZ)
                : Pos.Invalid;
            var to = payload.Action == ProtocolTurnAction.Move
                ? ProtocolMapper.ToPos(payload.ToX, payload.ToZ)
                : Pos.Invalid;

            var data = new NetworkMoveData(
                team,
                ProtocolMapper.ToTurnAction(payload.Action),
                from,
                to,
                payload.TurnNumber,
                payload.CompletedAt,
                ProtocolMapper.ToPlayerTeamOrNone(payload.NextTeam),
                payload.NextTurnDeadlineAt,
                ProtocolMapper.ToPassReason(payload.PassReason));

            _receiver?.ApplyServerMove(data);
        }
        private void OnServerGameEnded(
            ServerMessage msg)
        {
            var payload = msg.GetPayload<GameEndedEvent>();

            var winner = payload.Winner == Protocol.Matching.ProtocolPlayerTeam.None
                ? PlayerTeam.None
                : ProtocolMapper.ToPlayerTeam(payload.Winner);

            var result = new GameResultInfo
            {
                Type = ProtocolMapper.ToGameResult(payload.EndReason),
                Winner = winner,
                Loser = winner == PlayerTeam.None
                    ? PlayerTeam.None
                    : winner == PlayerTeam.Cho
                        ? PlayerTeam.Han
                        : PlayerTeam.Cho
            };

            _receiver?.ApplyServerGameEnded(result, payload.TotalMoves);
        }
    }
}


