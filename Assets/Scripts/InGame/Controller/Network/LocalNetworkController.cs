using System;

namespace YuJanggi.Controller
{
    using Engine.JanggiBoard;
    using Engine.Domain;
    using Engine.Rule;

    using BootStrap;
    using Network;
    using InGame.Handler;

    public sealed class LocalNetworkController : IPlayerController
    {

        private readonly InGameHandler _inGameHandler;
        public PlayerTeam Team { get; }
        public bool IsLocal => true;
 
        private bool                    _isTurn;
        private readonly IInputHandler  _input;
        private readonly IReadOnlyBoard    _board;
        private readonly IJanggiRule    _rule;

        private readonly Selection      _selection;
        public LocalNetworkController(IJanggiRule rule, IReadOnlyBoard board, PlayerTeam team, IInputHandler input)
        {
            Team        = team;
            _board      = board;
            _rule       = rule;
            _input      = input;

            _selection = new Selection();

            _isTurn = false;

            _inGameHandler = YuJanggiBootStrap.Instance.NetworkManager.InGame;
        }
        public event Action<Pos, Pos> OnMoveRequest;

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


