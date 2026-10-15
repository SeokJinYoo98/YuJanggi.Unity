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
    public interface IInGameNetworkReceiver
    {
        void ApplyServerMove(
            PlayerTeam team,
            ProtocolTurnAction action,
            Pos from, Pos to);

        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves);
    }
    /// <summary>Protocol 생성·해석과 대기만 담당하며 게임 상태나 화면은 조작하지 않습니다.</summary>
    internal sealed class InGameNetworkHandler : NetworkHandler
    {
        private IInGameNetworkReceiver _receiver;
        private UniTaskCompletionSource _gameStart;


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

            var result = response.GetPayload<GameEndResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }

        public async UniTask<bool> SendGiveUpRequest(
            PlayerTeam          myTeam,
            CancellationToken   token)
        {
            EnsureAvailable(token);

            var request = new GiveUpRequest
            {
                Requester = ProtocolMapper.ToProtocolPlayerTeam(myTeam)
            };

            var response = await SendRequestAsync(
                ClientMessageType.GiveUpRequest,
                request,
                ServerMessageType.GiveUpResponse,
                token);

            var result = response.GetPayload<GiveUpResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }
        public async UniTask<bool> SendPassTurnRequest(
            PlayerTeam myTeam,
            CancellationToken token)
        {
            EnsureAvailable(token);

            var request = new PassTurnRequest
            {
                Requester = ProtocolMapper.ToProtocolPlayerTeam(myTeam)
            };

            var response = await SendRequestAsync(
                ClientMessageType.PassTurnRequest,
                request,
                ServerMessageType.PassTurnResponse,
                token);

            var result = response.GetPayload<GiveUpResponse>().Result;

            if (result == GameRequestResult.Accepted)
                return true;

            return false;
        }
        protected override void OnHandleMessage(
            ServerMessage msg)
        {
            switch (msg.Type)
            {
                case ServerMessageType.GameStartEvent:
                    _gameStart?.TrySetResult();
                    break;
                case ServerMessageType.TurnCompletedEvent:
                    OnServerTurnCompleted(msg);
                    break;

                case ServerMessageType.GameEndedEvent:
                    OnServerGameEnded(msg);
                    break;

                default:
                    break;
            }
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

            _receiver?.ApplyServerMove(
                team,
                payload.Action,
                from,
                to);
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


