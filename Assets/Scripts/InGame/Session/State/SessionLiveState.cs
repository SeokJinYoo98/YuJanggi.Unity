using System.Collections.Generic;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;

    using InGame.Views;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.InGame.Controller;

    internal sealed class SessionLiveState : SessionStateBase
    {
        private readonly LiveView  _liveView;
        internal SessionLiveState(
            ISessionTransition sessionFsm, 
            ISessionEngine     engine, 
            IInGameController cho, IInGameController han, 
            LiveView liveView)
            : base(sessionFsm, cho, han, engine)
        {
            _liveView = liveView;
        }
        // 라이브가 필요한걸 준비
        public override  void Enter() 
        {
            base.Enter();
            _engine.ToLiveRecord();
            _liveView.UnHighlight();
            _liveView.SyncBoardState(_engine.Board);
        }
        // 라이브를 정리한다.
        public override  void Exit() 
        {
            base.Exit();
            DisableAllControllers();
            _liveView.UnHighlight();
        }
        public override void OnTurnChanged(PlayerTeam next)
        {
            base.OnTurnChanged(next);
            var nextPlayer = GetPlayer(next);
            _liveView.UpdateTurnInfo(next, nextPlayer.IsLocal);
            BeginNextTurn(next);
        }

        public override void OnPieceMoved(in MoveContext moveCtx)
        {
            base.OnPieceMoved(moveCtx);
            _liveView.UnHighlight();
            if (moveCtx.IsHandicap) return;

            _liveView.ApplyMovement(moveCtx.Record);
        }

        public override void OnGameEnded(in GameResultInfo info)
        {
            base.OnGameEnded(info); 
            _transition.ToEnd();
        }

        public override void OnCheckOccurred(PlayerTeam team)
        {
            base.OnCheckOccurred(team);
            _liveView.CheckOccured(team);
        }
        public override void OnCheckReleased()
        {
            base.OnCheckReleased();
            _liveView.CheckReleased();
        }
        //
        public override void OnSelectionChanged(int? pieceId, IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals)
        {
            base.OnSelectionChanged(pieceId, legals, illegals);
            _liveView.UnHighlight();
            if (!pieceId.HasValue) return;
            _liveView.HighlightPiece(pieceId.Value);
            _liveView.HighlightWays(legals, illegals);
        }

        //
        public override void RequestMove(Pos from, Pos to)
        {
            base.RequestMove(from, to);
            _engine.TryMove(from, to);
        }
        public override void RequestUndo()
        {
            base.RequestUndo();
            // 이거 뷰를 직접 조작하는게 좀 이상함 *************************
            if (!_engine.TryUnDo(out var moveCtx))
                return;

            if (!moveCtx.IsHandicap)
                _liveView.RevertMovement(moveCtx.Record);
        }
        public override void RequestGiveUp()
        {
            base.RequestGiveUp();
            _engine.GiveUp();
        }
        public override void RequestHandicap()
        {
            base.RequestHandicap();
            _engine.Handicap();
            _liveView.UnHighlight();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            if (_engine.Record.Count == 0) return;
            _transition.ToReplay();
        }
        protected override SessionState StateName() => SessionState.LiveState;
    }
}


