using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.InGame.Session
{
    using Core.V2.Domain;
    using Core.V2.Match;

    using InGame.Views;
    public sealed class SessionReplayState : SessionStateBase
    {
        private readonly MatchView  _matchView;
        private readonly ReplayView _replayView;
        public SessionReplayState(
            ISessionTransition      sessionFsm, 
            ILiveMatch              matchModel, 
            IPlayerController cho, IPlayerController han, 
            ReplayView replayView, 
            MatchView  matchView)
            : base(sessionFsm, cho, han, matchModel)
        {
            _matchView   = matchView;
            _replayView  = replayView;
        }
        // 리플레이 준비
        public override void Enter()
        {
            base.Enter();
            _replayView.EnterReplayView();
        }
        // 리플레이 정리
        public override void Exit()
        {
            base.Exit();
            _replayView.ExitReplayView();
        }
        public override void OnTurnChanged(PlayerTeam next)
        {
            base.OnTurnChanged(next);
            var nextPlayer = GetPlayer(next);
            _matchView.OnTurnChanged(nextPlayer.IsLocal());
            BeginNextTurn(next);
        }
        // public override void OnPieceMoved(in MoveContext moveCtx) { }
        public override void OnGameEnded(in GameResultInfo info)
        {
            base.OnGameEnded(info);
            _transition.ToEnd();
        }
        // public override void OnCheckOccurred(PlayerTeam team) { }
        // public override void OnCheckReleased() { }

        // 입력 (Controller → State)
        // public override void OnSelectionChanged(int? pieceId, IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals) { }
        public override void RequestMove(Pos from, Pos to)
        {
            base.RequestMove(from, to); 
            _liveMatch.TryMove(from, to);
        }
        // public override void RequestUndo() { }
        // public override void RequestGiveUp() { }
        // public override void RequestHandicap() { }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            var result = _replayView.TryReplayBackward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.RecordIsEmpty) _transition.ToLive();
            if (result == ReplayResult.Failed) _transition.ToLive();
        }
        public override void RequestStepForward()
        {
            base.RequestStepForward();
            var result = _replayView.TryReplayForward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.IdxAtEnd) _transition.ToLive();
        }
        protected override SessionState StateName() => SessionState.ReplayState;
    }
}
