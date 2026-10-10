using System.Collections.Generic;
using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiBoard;
using YuJanggi.Engine.JanggiEngine;
using YuJanggi.Engine.JanggiRecord;
using YuJanggi.InGame.Views;

namespace YuJanggi.InGame.State
{
    public class InGameReplayState : InGameState
    {
        private IReplayRecord _record;
        public InGameReplayState(
            InGameView view,
            IJanggiEngine engine,
            IStateMachine stateMachine)
            : base(view, engine.References.Board, stateMachine)
        {
            _record = engine.ReplayRecord;
        }

        public override void HandleEnter()
        {
            throw new System.NotImplementedException();
        }

        public override void HandleExit()
        {
            throw new System.NotImplementedException();
        }




        protected override void OnTurnCompleted(TurnData data, PlayerType nextType)
        {
            throw new System.NotImplementedException();
        }

        protected override void OnUndoCompleted(UndoData data, PlayerType currentType)
        {
            throw new System.NotImplementedException();
        }

 
    }
}
