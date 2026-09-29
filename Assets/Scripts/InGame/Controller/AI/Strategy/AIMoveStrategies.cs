using System;
using System.Collections.Generic;


namespace YuJanggi.InGame.Controller.AI
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    public interface IAIMoveStrategy
    {
        bool TrySelectMove(IAIPosition position, PlayerTeam team, out AIMove move);
    }

    public static class AIMoveStrategyFactory
    {
        public static IAIMoveStrategy Create(AIMoveStrategyType type)
            => type switch
            {
                AIMoveStrategyType.Greedy => new GreedyAIMoveStrategy(),
                AIMoveStrategyType.Minimax => new MinimaxAIMoveStrategy(),
                _ => new RandomAIMoveStrategy()
            };
    }

    public sealed class RandomAIMoveStrategy : IAIMoveStrategy
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

    public sealed class GreedyAIMoveStrategy : IAIMoveStrategy
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

    public sealed class MinimaxAIMoveStrategy : IAIMoveStrategy
    {
        private readonly int _maxSearchDepth;
        private readonly int _timeLimitMilliseconds;

        public MinimaxAIMoveStrategy(int maxSearchDepth = 4, int timeLimitMilliseconds = 350)
        {
            _maxSearchDepth = maxSearchDepth;
            _timeLimitMilliseconds = timeLimitMilliseconds;
        }

        public bool TrySelectMove(IAIPosition position, PlayerTeam team, out AIMove move)
            => new MinimaxSearch(team, _maxSearchDepth, _timeLimitMilliseconds)
                .TrySelectMove(position, out move);
    }

    internal static class AIPieceValue
    {
        public static int Get(PieceType type)
            => type switch
            {
                PieceType.King => 10_000,
                PieceType.Chariot => 13,
                PieceType.Cannon => 7,
                PieceType.Horse => 5,
                PieceType.Elephant => 3,
                PieceType.Guard => 3,
                PieceType.Soldier => 2,
                _ => 0
            };
    }

    internal static class AIPlayerTeam
    {
        public static PlayerTeam Opponent(PlayerTeam team)
            => team == PlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho;
    }
}
