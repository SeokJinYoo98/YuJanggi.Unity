using System;
using UnityEngine;


namespace YuJanggi.InGame.Session
{
    using Engine.Domain;
    using InGame.Views;
    using InGame.Views.Board;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.InGame.Controller;

    internal sealed class SessionEndReplayState : SessionStateBase
    {
        private readonly ReplayPlayback _replayPlayback;
        public SessionEndReplayState(
            ISessionTransition sessionFsm,
            IInGameController cho, IInGameController han,
            ISessionEngine engine,
            ReplayPlayback replayPlayback)
                : base(sessionFsm, cho, han, engine)
        {
            _replayPlayback = replayPlayback;
        }
        public override void Enter()
        {
            base.Enter();
            if (_engine.Record.Count == 0) 
                _transition.ToEnd();
            _replayPlayback.EnterReplayView();
        }
        public override void Exit()
        {
            base.Exit();
            _replayPlayback.ExitReplayView();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            var result = _replayPlayback.TryReplayBackward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.RecordIsEmpty) _transition.ToEnd();
            if (result == ReplayResult.Failed) _transition.ToEnd();
        }
        public override void RequestStepForward()
        {
            base.RequestStepForward();
            var result = _replayPlayback.TryReplayForward();
            if (_debug)
                Debug.Log($"{result}");
            if (result == ReplayResult.Succeeded) return;
            if (result == ReplayResult.IdxAtEnd) _transition.ToEnd();
        }
        protected override SessionState StateName() => SessionState.EndReplayState;
    }
    internal sealed class SessionEndState : SessionStateBase
    {
        private readonly IGameResultContext _resultCtx;
        private readonly LiveView          _liveView;
        private readonly BoardView         _boardView;
        public SessionEndState(
            ISessionTransition sessionFsm, 
            IGameResultContext sessionResult,
            IInGameController cho, IInGameController han, 
            ISessionEngine engine,
            LiveView liveView, BoardView boardView)
            : base(sessionFsm, cho, han, engine)
        {
            _resultCtx  = sessionResult;
            _liveView  = liveView;
            _boardView = boardView;
        }

        public override void Enter()
        {
            base.Enter();
            _boardView.SyncBoardState(_engine.Board);
            if (!_resultCtx.GameResult.HasValue) _transition.ToLive();
            DisableAllControllers();
            var result          = _resultCtx.GameResult.Value;
            var isLocalLose     = GetPlayer(result.Loser).IsLocal;

            _liveView.OnGameEnded(in result, isLocalLose);
            _liveView.ShowResultUI();
        }
        public override void Exit()
        {
            base.Exit();
            _liveView.HideResultUI();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            _transition.ToEndReplay();
        }
        protected override SessionState StateName() => SessionState.EndState;
    }
}


