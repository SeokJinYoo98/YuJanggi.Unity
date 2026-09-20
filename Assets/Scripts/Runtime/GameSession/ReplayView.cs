using System;
using System.Collections;
using UnityEngine;
using TMPro;
namespace Yujanggi.Runtime.GameSession
{
    using BootStrap;

    using Core.Match;


    using Core.Domain;
    using Audio;
    using Board;
    using Runtime.Input;
    public enum ReplayResult
    {
        RecordIsEmpty, IdxAtEnd, IdxAtStart,
        Succeeded, Failed
    }
    public class ReplayView
    {
        private Coroutine                     _replayRoutine;

        private readonly ICoroutineRunner     _runner;
        private readonly IReplayBoardRenderer _board;
        private readonly Record               _record;
        private readonly AudioManager         _audio;
        private readonly TMP_Text             _displayModeText;
        private int                           _currIdx = 0;

        private bool IsEmpty          => _record.Count == 0;
        private bool IsAtStart        => _currIdx == 0;
        private bool IsAtEnd          => _currIdx == _record.Count - 1;
        public ReplayView(IReplayBoardRenderer board, Record record, ICoroutineRunner runner, AudioManager audio, TMP_Text displayMode)
        {
            _board       = board;
            _record      = record;
            _runner      = runner;
            _audio       = audio;
            _displayModeText = displayMode;
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
            _board.UnHighlight();
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
            _board.HighlightOnlyPiece(movedPiece.Id);
        }
        private void EnterState(ReplayState nextState, in MoveContext nextCtx, int nextIdx)
        {
            ClearPrevState(nextState);

            if (!nextCtx.IsHandicap)
            {
                PrepareVisual(nextCtx.Record);
                StartCoroutine(nextCtx);
            }

            UpdateState(nextState, nextCtx, nextIdx);
        }
        private void UnDoMove(MoveContext moveCtx)
        {
            if (moveCtx.IsHandicap) return;
            var record = moveCtx.Record;
            var movedPiece   = record.MovedPiece;
            var movedToPos   = record.From;
            _board.MovePiece(movedPiece.Id, movedToPos);

            if (!record.IsCapture)
                return;
            
            var capturedId    = record.CapturedPiece.Id;
            var cpaturedTeam  = record.CapturedPiece.Team;
            var cpaturedToPos = record.To;
            _board.RestoreCapturedPiece(capturedId, cpaturedTeam, cpaturedToPos);
        }
        private void DoMove(MoveContext moveCtx, bool playAudio)
        {
            if (moveCtx.IsHandicap) return;

            if (playAudio)
                _audio.PlaySfxOneShot(JanggiSfx.Move);

            var record = moveCtx.Record;
            var movedPiece = record.MovedPiece;
            var movedToPos = record.To;
            _board.MovePiece(movedPiece.Id, movedToPos);

            if (!record.IsCapture)
                return;
            
            if (playAudio) 
                _audio.PlaySfxOneShot(JanggiSfx.Capture);
            var capturedId   = record.CapturedPiece.Id;
            var capturedTeam = record.CapturedPiece.Team;
            _board.PlaceCapturedPiece(capturedId, capturedTeam);
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
            _currState = ReplayState.Live;
        }
        public void EnterReplayView()
        {
            _record.EnterReplay();
            _displayModeText.SetText("기보 보기");

            var nextState = ReplayState.Backward;
            var nextIdx   = _record.Count - 1;
            if (!_record.TryGetMoveCtx(nextIdx, out var nextCtx)) return;

            EnterState(nextState, in nextCtx, nextIdx);
        }
        public void ExitReplayView()
        {
            _record.ExitReplay();
            ClearPrevState(ReplayState.Forward);
            UpdateState(ReplayState.Live, null, _record.Count - 1);

            _displayModeText.SetText("라이브 보기");
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
