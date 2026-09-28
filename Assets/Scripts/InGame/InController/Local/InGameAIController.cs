using System;

namespace YuJanggi.InGame.Controller
{
    using YuJanggi.Engine.JanggiEngine;
    using Engine.Domain;

    public enum AIMoveStrategyType
    {
        Random,
        Greedy,
        Minimax
    }

    internal class InGameAIController : IInGameController
    {
        private readonly IControllerQuery _query;
        private readonly AIMoveStrategyType _aiStrategy;
        public InGameAIController(
            PlayerTeam team,
            IControllerQuery query,
            AIMoveStrategyType strategy)
        {
            Team = team;
            _query = query;
            _aiStrategy = strategy;
        }
        public PlayerTeam Team { get; }


        public bool IsLocal => throw new NotImplementedException();

        public event MoveRequestHandler OnMoveRequest;

        public void BeginTurn()
        {
            throw new NotImplementedException();
        }

        public void BindEvents(IGameInputReceiver receiver)
        {
            throw new NotImplementedException();
        }

        public void EndTurn()
        {
            throw new NotImplementedException();
        }

        public void UnBindEvents(IGameInputReceiver receiver)
        {
            throw new NotImplementedException();
        }
    }
}


