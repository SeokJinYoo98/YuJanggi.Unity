

using System.Collections.Generic;
using YuJanggi.Engine.Domain;
namespace YuJanggi.Core.InGame
{
   public interface IGameCommandReceiver
    {
        void RequestMove(Pos from, Pos to);
        void SelectPiece(int? id, IReadOnlyList<Pos> legal, IReadOnlyList<Pos> illegal);
    }
   public interface IBoardInputReceiver
    {
        void HandleValidClick(Pos pos);
        void HandleInvalidClick();
    }
    public interface IInGamePlayer
    {
        public PlayerTeam Team { get; }
        public PlayerType Type { get; }

    }
    public interface ILocalPlayer : IInGamePlayer, IBoardInputReceiver
    {

    }
    public abstract class InGamePlayer
    {
        public PlayerTeam Team { get; }
        public PlayerType Type { get; }
        protected IGameCommandReceiver Receiver { get; }
        protected InGamePlayer(
            PlayerTeam team,
            PlayerType type,
            IGameCommandReceiver receiver)
        {
            Team = team;
            Type = type;
            Receiver = receiver;
        }
    }
}
