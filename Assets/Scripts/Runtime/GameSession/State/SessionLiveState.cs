using System.Collections.Generic;
using UnityEngine;


namespace YuJanggi.Runtime.GameSession
{
    using Core.Domain;
    using Core.Match;
    using Piece;

    public sealed class SessionLiveState : SessionStateBase
    {
        private readonly MatchView  _matchView;
        public SessionLiveState(
            ISessionTransition sessionFsm, 
            ILiveMatch         matchModel, 
            IPlayerController cho, IPlayerController han, 
            MatchView matchView)
            : base(sessionFsm, cho, han, matchModel)
        {
            _matchView = matchView;
        }
        // 라이브가 필요한걸 준비
        public override  void Enter() 
        {
            base.Enter();
            _matchView.UnHighlight();
            _matchView.SyncBoardState(_liveMatch);
            
        }
        // 라이브를 정리한다.
        public override  void Exit() 
        {
            base.Exit();
            _matchView.UnHighlight();
        }
        public override void OnTurnChanged(PlayerTeam next)
        {
            base.OnTurnChanged(next);
            var nextPlayer = GetPlayer(next);
            _matchView.OnTurnChanged(nextPlayer.IsLocal());
            BeginNextTurn(next);
        }

        public override void OnPieceMoved(in MoveContext moveCtx)
        {
            base.OnPieceMoved(moveCtx);
            _matchView.UnHighlight();
            if (moveCtx.IsHandicap) return;

            _matchView.ApplyMovement(moveCtx.Record);
        }

        public override void OnGameEnded(in GameResultInfo info)
        {
            base.OnGameEnded(info); 
            _transition.ToEnd();
        }

        public override void OnCheckOccurred(PlayerTeam team)
        {
            base.OnCheckOccurred(team);
            _matchView.CheckOccured(team);
        }
        public override void OnCheckReleased()
        {
            base.OnCheckReleased(); 
            _matchView.CheckReleased();
        }
        //
        public override void OnSelectionChanged(int? pieceId, IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals)
        {
            base.OnSelectionChanged(pieceId, legals, illegals);
            _matchView.UnHighlight();
            if (!pieceId.HasValue) return;
            _matchView.HighlightPiece(pieceId.Value);
            _matchView.HighlightWays(legals, illegals);
        }

        //
        public override void RequestMove(Pos from, Pos to)
        {
            base.RequestMove(from, to);
            _liveMatch.TryMove(from, to);
        }
        public override void RequestUndo()
        {
            base.RequestUndo();
            // 이거 뷰를 직접 조작하는게 좀 이상함 *************************
            if (!_liveMatch.TryUnDo(out var moveCtx))
                return;

            if (!moveCtx.IsHandicap)
                _matchView.RevertMovement(moveCtx.Record);
        }
        public override void RequestGiveUp()
        {
            base.RequestGiveUp();
            _liveMatch.GiveUp();
        }
        public override void RequestHandicap()
        {
            base.RequestHandicap();
            _liveMatch.Handicap();
            _matchView.UnHighlight();
        }
        public override void RequestStepBackward()
        {
            base.RequestStepBackward();
            if (_liveMatch.RecordCnt == 0) return;
            _transition.ToReplay();
        }
        protected override SessionState StateName() => SessionState.LiveState;
    }
}
