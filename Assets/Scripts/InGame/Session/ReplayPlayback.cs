using System.Collections;
using UnityEngine;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;
    using Engine.JanggiRecord;

    using Views;
    using Views.Board;
    using Runtime.Input;



    public enum ReplayResult
    {
        RecordIsEmpty, IdxAtEnd, IdxAtStart,
        Succeeded, Failed
    }
    public class ReplayPlayback
    {
        private Coroutine _replayRoutine;

        private readonly ICoroutineRunner _runner;
        private readonly IReadOnlyRecord _record;
        private readonly BoardView _board;
        private readonly LiveView _view;

        private bool _moveApplied;
        private int _currIdx = 0;

        private bool IsEmpty
            => _record.Count == 0;
        private bool IsAtStart
            => _currIdx == 0;
        private bool IsAtEnd
            => _currIdx == _record.Count - 1;
        public ReplayPlayback(
            BoardView board,
            IReadOnlyRecord record,
            ICoroutineRunner runner,
            LiveView view)
        {
            _board       = board;
            _record      = record;
            _runner      = runner;
            _view = view;
        }
        private enum ReplayState { Live, Forward, Backward };
        private ReplayState     _currState   = ReplayState.Live;
        private MoveContext?    _currCtx     = null;
        private const float     _replayTimer = 0.5f;

        private void StopCoroutine()
        {
            if (_replayRoutine == null) return;
            _runner.Stop(_replayRoutine);
            _replayRoutine = null;
        }
        private void StartCoroutine(MoveContext ctx)
        {
            if (_replayRoutine != null) return;
            _replayRoutine = _runner.Run(ReplayRoutine(ctx));
        }
        private void UpdateState(ReplayState nextState, in MoveContext? nextCtx, int nextIdx)
        {
            _currState = nextState;
            _currCtx   = nextCtx;
            _currIdx   = nextIdx;
        }
        private void ClearPrevState(ReplayState nextState)
        {
            _board.ClearSelection();
            StopCoroutine();
            if (_currCtx.HasValue)
            {
                if (nextState == ReplayState.Forward)
                    DoMove(_currCtx.Value, false);
                else
                    UnDoMove(_currCtx.Value);
            }

        }
        private void PrepareVisual(in MoveRecord moveRecord)
        {
            var movedPiece = moveRecord.MovedPiece;
            _board.SelectPiece(movedPiece.Id, playAudio: false);
        }
        private void PreparePlayback(MoveContext moveCtx)
        {
            if (moveCtx.IsHandicap) return;

            // Restore the move and captured piece before selecting the starting pose.
            UnDoMove(moveCtx);
            PrepareVisual(moveCtx.Record);
            StartCoroutine(moveCtx);
        }
        private void EnterState(ReplayState nextState, in MoveContext nextCtx, int nextIdx)
        {
            ClearPrevState(nextState);

            _moveApplied = nextState == ReplayState.Backward;
            PreparePlayback(nextCtx);

            UpdateState(nextState, nextCtx, nextIdx);
        }
        private void UnDoMove(MoveContext moveCtx)
        {
            if (moveCtx.IsHandicap || !_moveApplied) return;
            _board.RevertMovement(moveCtx.Record, clearSelection: false,
                restoreSelectionPose: true);
            _moveApplied = false;
        }
        private void DoMove(MoveContext moveCtx, bool playAudio)
        {
            if (moveCtx.IsHandicap) return;
            if (_moveApplied)
            {
                if (!playAudio) return;
                // Normalize an already applied move before repeating its presentation.
                UnDoMove(moveCtx);
            }
            _board.ApplyMovement(moveCtx.Record, playAudio: playAudio,
                playParticle: false, clearSelection: false, lowerSelectedPiece: true);
            _moveApplied = true;
        }
        private IEnumerator ReplayRoutine(MoveContext ctx)
        {
            yield return new WaitForSeconds(_replayTimer);
            while (!ctx.IsHandicap)
            {
                DoMove(ctx, true);
                yield return new WaitForSeconds(_replayTimer);
                UnDoMove(ctx);
                yield return new WaitForSeconds(_replayTimer);
            }
        }
        public void ResetGame()
        {
            _board.ClearSelection();
            StopCoroutine();
            UpdateState(ReplayState.Live, null, 0);
            _moveApplied = false;
        }
        public void EnterReplayView()
        {
            _view.ShowReplayMode();

            var nextState = ReplayState.Backward;
            var nextIdx   = _record.Count - 1;
            if (!_record.TryGetMoveCtx(nextIdx, out var nextCtx)) return;

            EnterState(nextState, in nextCtx, nextIdx);
        }
        public void ExitReplayView()
        {
            ClearPrevState(ReplayState.Forward);
            UpdateState(ReplayState.Live, null, _record.Count - 1);

            _view.ShowLiveMode();
        }
        public ReplayResult TryReplayBackward()
        {
            if (IsEmpty)
                return ReplayResult.RecordIsEmpty;
            if (IsAtStart)
                return ReplayResult.IdxAtStart;

            var nextState = ReplayState.Backward;
            var nextIdx   = _currIdx - 1;
            if (!_record.TryGetMoveCtx(nextIdx, out var nextCtx))
                return ReplayResult.Failed;

            EnterState(nextState, nextCtx, nextIdx);
            return ReplayResult.Succeeded;
        }
        public ReplayResult TryReplayForward()
        {
            if (IsEmpty)
                return ReplayResult.RecordIsEmpty;
            if (IsAtEnd)
                return ReplayResult.IdxAtEnd;
            var nextState = ReplayState.Forward;
            var nextIdx   = _currIdx + 1;
            if (!_record.TryGetMoveCtx(nextIdx, out var nextCtx))
                return ReplayResult.Failed;

            EnterState(nextState, in nextCtx, nextIdx);
            return ReplayResult.Succeeded;
        }
    }
}


