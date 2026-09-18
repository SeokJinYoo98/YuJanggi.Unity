using System;

namespace YuJanggi.Runtime.Controller
{
    using Core.Domain;

    public sealed class NetworkController : IPlayerController
    {
        public NetworkController(PlayerTeam team)
        {
            Team = team;
        }

        public event Action<Pos, Pos> OnMoveRequest;

        public PlayerTeam Team { get; }

        public bool IsLocal()
        {
            return false;
        }

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
