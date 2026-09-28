using System;

namespace YuJanggi.Controller
{
    using Engine.Domain;

    public sealed class RemoteNetworkController : IPlayerController
    {
        public bool IsLocal => false;
        public RemoteNetworkController(PlayerTeam team)
        {
            Team = team;
        }

        public event Action<Pos, Pos> OnMoveRequest;

        public PlayerTeam Team { get; }



        public void BeginTurn()
        {
        }

        public void EndTurn()
        {
        }

        public void BindEvents(IGameInputReceiver receiver)
        {
        }

        public void UnBindEvents(IGameInputReceiver receiver)
        {
        }
    }
}


