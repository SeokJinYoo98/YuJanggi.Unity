namespace YuJanggi.InGame.Controller
{
    using YuJanggi.Engine.JanggiEngine;
    using Engine.Domain;
    using YuJanggi.Controller.AI;

    public enum AIMoveStrategyType
    {
        Random,
        Greedy,
        Minimax
    }

    internal class InGameAIController : IInGameController
    {
        private readonly IControllerQuery   _query;
        private readonly IAIMoveService _moves;
        public PlayerTeam Team { get; }

        public event MoveRequestHandler OnMoveRequest;
        public InGameAIController(
            PlayerTeam team,
            IControllerQuery query,
            IAIMoveService moves)
        {
            Team = team;
            _query = query;
            _moves = moves;
        }


        public bool IsLocal => false;



        public void BeginTurn()
        {
            if (_query.CurrentTurn != Team)
                return;
            if (_moves.TrySelectMove(Team, out var move))
                OnMoveRequest?.Invoke(move.From, move.To);
        }

        public void BindEvents(IGameInputReceiver receiver)
        {
            OnMoveRequest += receiver.RequestMove;
        }

        public void EndTurn()
        {
        }

        public void UnBindEvents(IGameInputReceiver receiver)
        {
            OnMoveRequest -= receiver.RequestMove;
        }
    }
}


