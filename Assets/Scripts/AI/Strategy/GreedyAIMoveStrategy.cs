using System;
using System.Collections.Generic;
namespace YuJanggi.AI.Strategy
{
    using Abstraction;
    using Utilities;
  
    using Engine.Domain;
    using Engine.JanggiEngine;

    public sealed class GreedyAIMoveStrategy : IAI
    {
        private readonly Random _random = new();

        public bool TrySelectMove(IAIPosition position, PlayerTeam team, out AIMove move)
        {
            var moves = new List<AIMove>();
            position.GetLegalMoves(team, moves);
            if (moves.Count == 0)
            {
                move = default;
                return false;
            }

            int bestScore = int.MinValue;
            var bestMoves = new List<AIMove>();
            foreach (var candidate in moves)
            {
                int score = AIPieceValue.Get(position.GetPiece(candidate.To).Type);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(candidate);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(candidate);
                }
            }
            move = bestMoves[_random.Next(bestMoves.Count)];
            return true;
        }
    }
}
