using System.Collections.Generic;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiBoard;
using YuJanggi.Engine.JanggiRecord;
using YuJanggi.InGame.Views;

namespace YuJanggi.Core.InGame
{
    public enum InGameStateType { Live, Replay }
    public interface IStateMachine
    {
        public void ChangeState(InGameStateType type);
    }
    public interface IInGameState
    {
        void HandleEnter();
        void HandleExit();
        void HandleTurnCompleted(TurnData data, PlayerType nextType);
        void HandleUndoCompleted(UndoData data, PlayerType currentType);
        void HandleSelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal);
        void HandlePreviousButton();
        void HandleNextButton();
        void HandleReplayButton();
    }

    public abstract class InGameState : IInGameState
    {
        protected readonly IReadOnlyBoard  _board;
        protected readonly InGameView      _inGameView;
        protected readonly IStateMachine   _stateMachine;

        protected InGameState(
            InGameView view,
            IReadOnlyBoard board,  
            IStateMachine stateMachine)
        {
            _inGameView     = view;
            _board          = board;
            _stateMachine   = stateMachine;
        }
        public abstract void HandleEnter();
        public abstract void HandleExit();

        public void HandleTurnCompleted(TurnData data, PlayerType nextType)
        {
            OnTurnCompleted(data, nextType);

            if (data.GameResult is not null)
                return;

            var nextTeam = data.ActingTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            _inGameView.ApplyLiveUI(nextTeam, nextType);
        }

        public void HandleUndoCompleted(UndoData data, PlayerType currentType)
        {
            _inGameView.ApplyLiveUI(data.CurrentTurn, currentType);

            OnUndoCompleted(data, currentType);
        }
          
        public void HandleSelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
            => OnSelectPiece(id, legal, illegal);
        public virtual void HandlePreviousButton()
        {

        }
        public virtual void HandleNextButton()
        {

        }
        public virtual void HandleReplayButton()
        {

        }

        protected virtual void OnTurnCompleted(TurnData data, PlayerType nextType) { }
        protected virtual void OnUndoCompleted(UndoData data, PlayerType currentType) { }
        protected virtual void OnSelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {  }

    }
}
