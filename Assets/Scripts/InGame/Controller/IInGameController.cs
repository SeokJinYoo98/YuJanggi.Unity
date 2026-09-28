

using System.Collections.Generic;
using YuJanggi.Engine.Domain;

namespace YuJanggi.InGame.Controller
{
    internal delegate void SelectionChangedHandler(
            int? pieceId,
            IReadOnlyList<Pos> legalWays,
            IReadOnlyList<Pos> illegalWays);
    internal delegate void MoveRequestHandler(Pos from, Pos to);

    internal interface IInGameLocalController
    {
        event SelectionChangedHandler OnSelectionChanged;
    }
    internal interface IInGameController
    {
        event MoveRequestHandler OnMoveRequest;

        PlayerTeam Team { get; }
        bool IsLocal { get; }

        void BeginTurn();
        void EndTurn();

        void BindEvents(IGameInputReceiver receiver);
        void UnBindEvents(IGameInputReceiver receiver);
    }
}
