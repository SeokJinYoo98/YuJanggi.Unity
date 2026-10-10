using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.State
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Engine.JanggiRecord;

    using Views;
    using Mode;

    public class InGameReplayState : InGameState
    {
        private enum ReplayState { Live, Forward, Backward };
        private const float ReplayInterval = 0.7f;

        private TurnData    _currData;
        private ReplayState _currState;
        private int         _currIdx = -1;
        private bool        _moveApplied;

        private CancellationTokenSource _replayCts;

        private readonly IReplayRecord      _record;
        private readonly CancellationToken _lifecycleToken;

        private bool IsAtLatestRecord
            => _currIdx == _record.Count - 1;

        public override InGameStateType State
            => InGameStateType.Replay;

        public InGameReplayState(
            InGameView view,
            IJanggiEngine engine,
            IStateMachine stateMachine,
            CancellationToken lifecycleToken)
            : base(view, engine.References.Board, stateMachine)
        {
            _record = engine.ReplayRecord;
            _lifecycleToken = lifecycleToken;
        }

        private void CancelPlayback()
        {
            var cts = _replayCts;
            _replayCts = null;
            cts?.Cancel();
        }

        private void ResetReplay()
        {
            CancelPlayback();
            _currData       = null;
            _currState      = ReplayState.Live;
            _currIdx        = -1;
            _moveApplied    = false;
        }
        public override void HandleEnter(IGameMode mode)
        {
            if (mode.IsEnd)
                _inGameView.CloseResultView();

            mode.SetLocalInputEnabled(false);
            _lifecycleToken.ThrowIfCancellationRequested();
            _inGameView.SyncReplayUI();
            
            ResetReplay();

            _currIdx = _record.Count - 1;

            if (!_record.TryGetTurnData(_currIdx, out var data))
                return;

            _currData    = data;
            _currState   = ReplayState.Backward;
            _moveApplied = data.MovedRecord is not null;

            _record.EnterReplay();
            PlayBackwardAsync(_currData, _lifecycleToken).Forget();
        }

        public override void HandleExit(IGameMode mode)
        {
            CancelPlayback();

            if (IsAtLatestRecord
                && !_moveApplied
                && _currData?.MovedRecord is MoveRecord record)
            {
                _inGameView.ApplyMoveRecord(record);
            }

            ResetReplay();
            _record.ExitReplay();
        }

        public override void HandlePreviousButton()
        {
            if (_currIdx == 0)
                return;

            int nextIdx = _currIdx - 1;
            if (!_record.TryGetTurnData(nextIdx, out var data))
                return;

            CancelPlayback();

            PrepareNextPlayback(ReplayState.Backward, nextIdx, data);
            PlayBackwardAsync(_currData, _lifecycleToken).Forget();
        }

        public override void HandleNextButton()
        {
            if (IsAtLatestRecord)
            {
                _stateMachine.ChangeState(InGameStateType.Live);
                return;
            }
            int nextIdx = _currIdx + 1;
            if (!_record.TryGetTurnData(nextIdx, out var data))
                return;

            CancelPlayback();

            PrepareNextPlayback(ReplayState.Forward, nextIdx, data);
            PlayForwardAsync(_currData, _lifecycleToken).Forget();
        }

        private void PrepareNextPlayback(
            ReplayState nextState,
            int nextIdx,
            TurnData nextData)
        {
            _inGameView.ClearSelection();

            if (_currData?.MovedRecord is MoveRecord record)
            {
                if (nextState == ReplayState.Forward && !_moveApplied)
                    _inGameView.ApplyMoveRecord(record);
                else if (nextState == ReplayState.Backward && _moveApplied)
                    _inGameView.RevertMoveRecord(record);
            }

            _currState = nextState;
            _currIdx = nextIdx;
            _currData = nextData;
            _moveApplied = nextState == ReplayState.Backward
                && nextData.MovedRecord is not null;
        }

        private async UniTask PlayBackwardAsync(
            TurnData data,
            CancellationToken token)
        {
            if (data.MovedRecord is not MoveRecord record)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _replayCts = cts;
            token = cts.Token;

            try
            {
                while (true)
                {
                    await Revert(record, token);

                    await Select(record.MovedPiece.Id, token);

                    await Apply(record, token);
                }
            }
            finally
            {
                if (ReferenceEquals(_replayCts, cts))
                    _replayCts = null;
            }
        }

        private async UniTask PlayForwardAsync(
            TurnData data,
            CancellationToken token)
        {
            if (data.MovedRecord is not MoveRecord record)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _replayCts = cts;
            token = cts.Token;

            try
            {
                while (true)
                {
                    await Select(record.MovedPiece.Id, token);

                    await Apply(record, token);

                    await Revert(record, token);
                }
            }
            finally
            {
                if (ReferenceEquals(_replayCts, cts))
                    _replayCts = null;
            }
        }

        private async UniTask Revert(
            MoveRecord record,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _inGameView.RevertMoveRecord(record);
            _moveApplied = false;

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(ReplayInterval),
                cancellationToken: token);
        }
        private async UniTask Select(
            int pieceId,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _inGameView.SelectPiece(
                pieceId,
                null,
                null);

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(ReplayInterval),
                cancellationToken: token);
        }
        private async UniTask Apply(
            MoveRecord record,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _inGameView.ClearSelection();
            _inGameView.ApplyMoveRecord(record);
            _moveApplied = true;

            await UniTask.Delay(
                System.TimeSpan.FromSeconds(ReplayInterval),
                cancellationToken: token);
        }
    }
}
