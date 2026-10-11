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
        void ApplyServerMove(PlayerTeam team, Pos from, Pos to);
    }
    /// <summary>Protocol 생성·해석과 대기만 담당하며 게임 상태나 화면은 조작하지 않습니다.</summary>
    internal sealed class InGameNetworkHandler : NetworkHandler
    {
        private IInGameNetworkReceiver _receiver;
        private UniTaskCompletionSource _gameStart;
        private UniTaskCompletionSource<GameResultInfo> _gameEnd;
        private GameResultInfo _requestedGameResult;


        public InGameNetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        { }
        public void SetReceiver(IInGameNetworkReceiver receiver)
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
            ResetCompletion(ref _gameEnd);
            _requestedGameResult = info;

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

            if (result == GameEndResult.Accepted)
                return true;

            return false;
        }
        public async UniTask<GameResultInfo> WaitForGameEndEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _gameEnd
                ?? throw new InvalidOperationException(
                    "게임 종료 대기가 준비되지 않았습니다.");

            return await completion.Task.AttachExternalCancellation(token);
        }

        protected override void OnHandleMessage(
            ServerMessage msg)
        {
            switch (msg.Type)
            {
                case ServerMessageType.GameStartEvent:
                    _gameStart?.TrySetResult();
                    break;
                case ServerMessageType.MovePieceEvent:
                    OnServerMovePieceEvent(msg);
                    break;

                case ServerMessageType.GameEndedEvent:
                    OnServerGameEnded(msg);
                    break;

                default:
                    break;
            }
        }
        private void OnServerMovePieceEvent(
            ServerMessage msg)
        {
            var payload = msg.GetPayload<MovePieceEvent>();

            var team = ProtocolMapper.ToPlayerTeam(payload.Team);
            var from = ProtocolMapper.ToPos(payload.FromX, payload.FromZ);
            var to   = ProtocolMapper.ToPos(payload.ToX, payload.ToZ);

            _receiver?.ApplyServerMove(
                team,
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

            var info = new GameResultInfo
            {
                // GameEndedEvent에는 종료 사유가 없어 요청에 사용한 값을 유지합니다.
                Type = _requestedGameResult.Type,
                Winner = winner,
                Loser = winner switch
                {
                    PlayerTeam.Cho => PlayerTeam.Han,
                    PlayerTeam.Han => PlayerTeam.Cho,
                    _ => PlayerTeam.None
                }
            };
            _gameEnd?.TrySetResult(info);
        }
    }
}


