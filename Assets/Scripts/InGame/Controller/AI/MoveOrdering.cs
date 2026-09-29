using System.Collections.Generic;
using YuJanggi.Engine.JanggiEngine;

namespace YuJanggi.Controller.AI
{
    internal static class MoveOrdering
    {
        public static List<AIMove> GetOrderedMoves(IAIPosition position, YuJanggi.Engine.Domain.PlayerTeam team)
        {
            var moves = new List<AIMove>();
            position.GetLegalMoves(team, moves);
            moves.Sort((left, right) => Score(position, right).CompareTo(Score(position, left)));
            return moves;
        }

        private static int Score(IAIPosition position, AIMove move)
        {
            if (!position.HasPiece(move.To)) return 0;
            var captured = position.GetPiece(move.To);
            var moved = position.GetPiece(move.From);
            return AIPieceValue.Get(captured.Type) * 16 - AIPieceValue.Get(moved.Type);
        }
    }
}
