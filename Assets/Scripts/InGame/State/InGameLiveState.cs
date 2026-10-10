

using System.Collections.Generic;
using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiBoard;
using YuJanggi.Engine.JanggiEngine;
using YuJanggi.Engine.JanggiRecord;
using YuJanggi.InGame.Views;

namespace YuJanggi.InGame.State
{
    public class InGameLiveState : InGameState
    {
        private readonly IReadOnlyRecord _record;
        public InGameLiveState(
            InGameView inGameView,
            IReadOnlyEngine engine,
            IStateMachine stateMachine)
            :base(inGameView, engine.Board, stateMachine)
        {
            _record = engine.ReadOnlyRecord;
        }   
        public override void HandlePreviousButton()
        {

        }
        public override void HandleEnter()
        {
            _inGameView.ClearSelection();
            _inGameView.SyncBoardState(_board);
        }

        public override void HandleExit()
        {
            throw new System.NotImplementedException();
        }
        protected override void OnTurnCompleted(TurnData data, PlayerType nextType)
        {
            _inGameView.ClearSelection();

            if (data.MovedRecord is MoveRecord record)
                _inGameView.ApplyMoveRecord(record);

            if (data.CheckedTeam is PlayerTeam checkedTeam)
                _inGameView.PlayJanggunEffect(checkedTeam);

            if (data.CheckReleasedTeam is PlayerTeam releasedTeam)
                _inGameView.PlayMeonggunEffect(releasedTeam);
        }

        protected override void OnUndoCompleted(UndoData data, PlayerType currentType)
        {
            _inGameView.ClearSelection();

            if (data.UndoneMove is MoveRecord record)
                _inGameView.RevertMoveRecord(record);

            if (data.CheckReleasedTeam is PlayerTeam releasedTeam)
                _inGameView.PlayMeonggunEffect(releasedTeam);

            if (data.CheckedTeam is PlayerTeam checkedTeam)
                _inGameView.PlayJanggunEffect(checkedTeam);
        }

        protected override void OnSelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {
            if (id is null)
            {
                _inGameView.ClearSelection();
                return;
            }

            _inGameView.SelectPiece(id.Value, legal, illegal);
        }


    }
}
