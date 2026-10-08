

using System.Collections.Generic;
using YuJanggi.Engine.Domain;

namespace YuJanggi.InGame.Controller
{
    public interface IGameInputReceiver
    {
        void RequestMove(Pos from, Pos to);
        void SelectPiece(int? pieceId, IReadOnlyList<Pos> legal, IReadOnlyList<Pos> illegal);
    }
    internal delegate void OnSelectPiece(
            int? pieceId,
            IReadOnlyList<Pos> legalWays,
            IReadOnlyList<Pos> illegalWays);
    internal delegate void MoveRequestHandler(Pos from, Pos to);


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
