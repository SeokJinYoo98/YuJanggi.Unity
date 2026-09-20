using System;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace YuJanggi.Runtime.Controller
{
    using Core.V2.Board;
    using Core.V2.Domain;
    using Core.V2.Rule;

    public class AIController : IPlayerController, IAIController
    {
        public PlayerTeam Team { get; }
        public event Action<Pos, Pos> OnMoveRequest;
        public bool IsLocal() => false;

        private readonly IJanggiRule _rule;
        private readonly IBoardModel _boardModel;
        private readonly IAIMoveStrategy _strategy;

        private AIMove _selectedMove;
        private bool _hasSelectedMove;

        public AIController(IJanggiRule rule, IBoardModel board, PlayerTeam team, AIMoveStrategyType strategyType)
        {
            Team                = team;
            _rule               = rule;
            _boardModel         = board;
            _strategy           = AIMoveStrategyFactory.Create(strategyType);
        }
        public void BindEvents(IGameInputReceiver receiver)
        {
            OnMoveRequest += receiver.RequestMove;
        }
        public void UnBindEvents(IGameInputReceiver receiver)
        {
            OnMoveRequest -= receiver.RequestMove;
        }

        public bool TryThink()
        {
            _hasSelectedMove = TrySelectMove(_boardModel, _rule, Team, out _selectedMove);
            return _hasSelectedMove;
        }
        public bool TryGetSelectedMove()
        {
            if (!_hasSelectedMove)
                return false;

            OnMoveRequest?.Invoke(_selectedMove.From, _selectedMove.To);
            _hasSelectedMove = false;
            return true;
        }
        public void BeginTurn()
            => BeginAITurn();
        public void EndTurn()
            => CancelAITurn();



        private CancellationTokenSource _aiTurnCts;
        private void BeginAITurn()
        {
            CancelAITurn();

            _aiTurnCts = new CancellationTokenSource();
            ProcessAITurnAsync(_aiTurnCts.Token).Forget();
        }
        private void CancelAITurn()
        {
            if (_aiTurnCts == null)
                return;

            _aiTurnCts.Cancel();
            _aiTurnCts.Dispose();
            _aiTurnCts = null;
        }

        private async UniTask ProcessAITurnAsync(CancellationToken token)
        {
            try
            {
                // MatchModel is owned by the Unity main thread.  Copy its current
                // state first, then run the expensive search only on the copy.
                var boardSnapshot = new AISimulationBoard(_boardModel);
                var searchRule = new JanggiRule();
                var team = Team;
                AIMove selectedMove = default;
                bool hasSelectedMove = await UniTask.RunOnThreadPool(
                    () => TrySelectMove(boardSnapshot, searchRule, team, out selectedMove),
                    cancellationToken: token);

                token.ThrowIfCancellationRequested();
                _selectedMove = selectedMove;
                _hasSelectedMove = hasSelectedMove;
                if (!_hasSelectedMove)
                    return;

                await UniTask.Delay(500, cancellationToken: token);

                if (!TryGetSelectedMove())
                    return;
            }
            catch (OperationCanceledException)
            {
                // 정상 취소
            }
        }

        private bool TrySelectMove(IBoardModel board, IJanggiRule rule, PlayerTeam team, out AIMove move)
        {
            // A cancelled worker can take a short time to return.  Serialising
            // access prevents a replacement turn from sharing mutable strategy
            // state (transposition table and timer) with that worker.
            lock (_strategy)
                return _strategy.TrySelectMove(board, rule, team, out move);
        }
    }
}
