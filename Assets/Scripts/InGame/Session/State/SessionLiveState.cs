using System.Collections.Generic;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;

    using InGame.Views;
    using InGame.Views.Board;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.InGame.Controller;

    internal sealed class SessionLiveState : SessionStateBase
    {
        private readonly LiveView  _liveView;
        private readonly BoardView _boardView;
        internal SessionLiveState(
            ISessionTransition sessionFsm, 
            ISessionEngine     engine, 
            IInGameController cho, IInGameController han, 
            LiveView liveView, BoardView boardView)
            : base(sessionFsm, cho, han, engine)
        {
            _liveView = liveView;
            _boardView = boardView;
        }
        // 라이브가 필요한걸 준비
        public override  void Enter() 
        {
            base.Enter();
            _engine.ToLiveRecord();
            _boardView.ClearSelection();
            _boardView.SyncBoardState(_engine.Board);
        }
        // 라이브를 정리한다.
        public override  void Exit() 
        {
            base.Exit();
            DisableAllControllers();
            _boardView.ClearSelection();
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
            _boardView.ClearSelection();
            if (moveCtx.IsHandicap) return;

            _boardView.ApplyMovement(moveCtx.Record);
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
            _boardView.ClearSelection();
            if (!pieceId.HasValue) return;
            _boardView.SelectPiece(pieceId.Value);
            _boardView.ShowMoveGuides(legals, illegals);
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
                _boardView.RevertMovement(moveCtx.Record);
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
            _boardView.ClearSelection();
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


