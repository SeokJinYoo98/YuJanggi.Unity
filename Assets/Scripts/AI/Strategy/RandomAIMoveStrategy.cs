using System;
using System.Collections.Generic;
namespace YuJanggi.AI.Strategy
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using Abstraction;
    public sealed class RandomAIMoveStrategy : IAI
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
            move = moves[_random.Next(moves.Count)];
            return true;
        }
    }
}
