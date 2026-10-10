using System.Collections.Generic;
using UnityEngine;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.State
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Engine.JanggiRecord;

    using Views;
    using YuJanggi.InGame.Mode;

    public class InGameLiveState : InGameState
    {
        private readonly IReadOnlyRecord _record;

        public override InGameStateType State
            => InGameStateType.Live;

        public InGameLiveState(
            InGameView inGameView,
            IReadOnlyEngine engine,
            IStateMachine stateMachine)
            :base(inGameView, engine.Board, stateMachine)
        {
            _record = engine.ReadOnlyRecord;
        }   

        public override void HandleEnter(IGameMode mode)
        {
            if (mode.IsEnd) _inGameView.OpenResultView();
            mode.SetLocalInputEnabled(true);
            _inGameView.ClearSelection();
            _inGameView.SyncBoardState(_board);
            _inGameView.SyncLiveUI();
        }

        public override void HandleExit(IGameMode mode)
        {
            _inGameView.ClearSelection();
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
        public override void HandlePreviousButton()
        {
            if (_record.Count <= 0)
                return;

            _stateMachine.ChangeState(InGameStateType.Replay);
        }

    }
}
