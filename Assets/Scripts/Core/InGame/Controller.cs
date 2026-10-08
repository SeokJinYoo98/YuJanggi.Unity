

using System.Collections.Generic;
using YuJanggi.Engine.Domain;

namespace YuJanggi.Core.InGame
{
    public interface IGameInputReceiver
    {
        void RequestMove(Pos from, Pos to);
        void SelectPiece(int? pieceId, IReadOnlyList<Pos> legal, IReadOnlyList<Pos> illegal);
    }
    internal interface IInGameController
    {
        PlayerTeam Team { get; }
        bool IsLocal { get; }

        void BeginTurn();
        void EndTurn();
        void Initialize(IGameInputReceiver receiver);
        void BindEvents();
        void UnBindEvents();
    }
}
