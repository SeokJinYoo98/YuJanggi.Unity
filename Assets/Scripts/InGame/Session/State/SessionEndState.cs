using System;
using UnityEngine;


namespace YuJanggi.InGame.Session
{
    using Core.V2.Domain;
    using Core.V2.Match;

    using InGame.Views;
    public sealed class SessionEndReplayState : SessionStateBase
    {
        private readonly ReplayView _replayView;
        public SessionEndReplayState(
            ISessionTransition sessionFsm,
            IPlayerController cho, IPlayerController han,
            ILiveMatch liveMatch,
            ReplayView replayView)
                : base(sessionFsm, cho, han, liveMatch)
        {
            _replayView = replayView;
        }
        public override void Enter()
        {
            base.Enter();
            if (_liveMatch.RecordCnt == 0) 
                _transition.ToEnd();
            _replayView.EnterReplayView();
        }
        public override void Exit()
        {
            base.Exit();
            _replayView.ExitReplayView();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            var result = _replayView.TryReplayBackward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.RecordIsEmpty) _transition.ToEnd();
            if (result == ReplayResult.Failed) _transition.ToEnd();
        }
        public override void RequestStepForward()
        {
            base.RequestStepForward();
            var result = _replayView.TryReplayForward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.IdxAtEnd) _transition.ToEnd();
        }
        protected override SessionState StateName() => SessionState.EndReplayState;
    }
    public sealed class SessionEndState : SessionStateBase
    {
        private readonly IGameResultContext _resultCtx;
        private readonly MatchView          _matchView;
        public SessionEndState(
            ISessionTransition sessionFsm, 
            IGameResultContext sessionResult,
            IPlayerController cho, IPlayerController han, 
            ILiveMatch liveMatch,
            MatchView matchView) 
            : base(sessionFsm, cho, han, liveMatch)
        {
            _resultCtx  = sessionResult;
            _matchView  = matchView;
        }

        public override void Enter()
        {
            base.Enter();
            _matchView.SyncBoardState(_liveMatch);
            if (!_resultCtx.GameResult.HasValue) _transition.ToLive();
            DisableAllControllers();
            var result          = _resultCtx.GameResult.Value;
            var isLocalLose     = GetPlayer(result.Loser).IsLocal();

            _matchView.OnGameEnded(in result, isLocalLose);
            _matchView.ShowResultUI();
        }
        public override void Exit()
        {
            base.Exit();
            _matchView.HideResultUI();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            _transition.ToEndReplay();
        }
        protected override SessionState StateName() => SessionState.EndState;
    }
}
