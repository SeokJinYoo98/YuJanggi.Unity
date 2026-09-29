using System;
using System.Collections.Generic;
using System.Diagnostics;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;

namespace YuJanggi.Controller.AI
{
    internal sealed class MinimaxSearch
    {
        private const int MaxQuiescenceDepth = 4;
        private readonly Random _random = new();
        private readonly int _maxSearchDepth;
        private readonly int _timeLimitMilliseconds;
        private readonly TranspositionTable _transpositionTable = new();
        private readonly PlayerTeam _maximizingTeam;
        private readonly PositionEvaluator _evaluator;
        private Stopwatch _stopwatch;

        public MinimaxSearch(PlayerTeam team, int maxSearchDepth, int timeLimitMilliseconds)
        {
            _maximizingTeam = team;
            _maxSearchDepth = Math.Max(1, maxSearchDepth);
            _timeLimitMilliseconds = Math.Max(50, timeLimitMilliseconds);
            _evaluator = new PositionEvaluator(team);
        }

        public bool TrySelectMove(IAIPosition position, out AIMove move)
        {
            var moves = MoveOrdering.GetOrderedMoves(position, _maximizingTeam);
            if (moves.Count == 0)
            {
                move = default;
                return false;
            }

            _stopwatch = Stopwatch.StartNew();
            var bestMove = moves[0];
            for (int depth = 1; depth <= _maxSearchDepth; ++depth)
            {
                try
                {
                    int bestScore = int.MinValue;
                    var bestMoves = new List<AIMove>();
                    foreach (var candidate in moves)
                    {
                        ThrowIfTimedOut();
                        var record = position.DoMove(candidate.From, candidate.To);
                        int score;
                        try
                        {
                            score = Search(position, AIPlayerTeam.Opponent(_maximizingTeam), depth - 1, int.MinValue, int.MaxValue);
                        }
                        finally
                        {
                            position.UndoMove(record);
                        }

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

                    bestMove = bestMoves[_random.Next(bestMoves.Count)];
                    moves.Remove(bestMove);
                    moves.Insert(0, bestMove);
                }
                catch (SearchTimeoutException)
                {
                    break;
                }
            }

            move = bestMove;
            return true;
        }

        private int Search(IAIPosition position, PlayerTeam currentTeam, int depth, int alpha, int beta)
        {
            ThrowIfTimedOut();
            if (_transpositionTable.TryGet(position, currentTeam, depth, out int cached))
                return cached;

            var moves = MoveOrdering.GetOrderedMoves(position, currentTeam);
            if (moves.Count == 0)
                return _evaluator.EvaluateTerminal(position, currentTeam);
            if (depth == 0)
                return QuiescenceSearch(position, currentTeam, alpha, beta, MaxQuiescenceDepth);

            bool maximizing = currentTeam == _maximizingTeam;
            int bestScore = maximizing ? int.MinValue : int.MaxValue;
            bool wasCutOff = false;
            foreach (var candidate in moves)
            {
                var record = position.DoMove(candidate.From, candidate.To);
                int score;
                try
                {
                    score = Search(position, AIPlayerTeam.Opponent(currentTeam), depth - 1, alpha, beta);
                }
                finally
                {
                    position.UndoMove(record);
                }

                if (maximizing)
                {
                    bestScore = Math.Max(bestScore, score);
                    alpha = Math.Max(alpha, bestScore);
                }
                else
                {
                    bestScore = Math.Min(bestScore, score);
                    beta = Math.Min(beta, bestScore);
                }
                if (beta <= alpha)
                {
                    wasCutOff = true;
                    break;
                }
            }

            // A cut-off gives a bound, not an exact score.
            if (!wasCutOff)
                _transpositionTable.Store(position, currentTeam, depth, bestScore);
            return bestScore;
        }

        private int QuiescenceSearch(IAIPosition position, PlayerTeam currentTeam, int alpha, int beta, int remainingDepth)
        {
            ThrowIfTimedOut();
            bool maximizing = currentTeam == _maximizingTeam;
            bool mustEscapeCheck = position.IsKingInCheck(currentTeam);
            var moves = MoveOrdering.GetOrderedMoves(position, currentTeam);
            if (moves.Count == 0)
                return _evaluator.EvaluateTerminal(position, currentTeam);

            int standPat = _evaluator.Evaluate(position);
            if (remainingDepth == 0)
                return standPat;

            if (!mustEscapeCheck && maximizing)
            {
                if (standPat >= beta) return beta;
                alpha = Math.Max(alpha, standPat);
            }
            else if (!mustEscapeCheck)
            {
                if (standPat <= alpha) return alpha;
                beta = Math.Min(beta, standPat);
            }

            foreach (var candidate in moves)
            {
                if (!mustEscapeCheck && !position.HasPiece(candidate.To))
                    continue;

                var record = position.DoMove(candidate.From, candidate.To);
                int score;
                try
                {
                    score = QuiescenceSearch(position, AIPlayerTeam.Opponent(currentTeam), alpha, beta, remainingDepth - 1);
                }
                finally
                {
                    position.UndoMove(record);
                }

                if (maximizing)
                {
                    if (score >= beta) return beta;
                    alpha = Math.Max(alpha, score);
                }
                else
                {
                    if (score <= alpha) return alpha;
                    beta = Math.Min(beta, score);
                }
            }
            return maximizing ? alpha : beta;
        }

        private void ThrowIfTimedOut()
        {
            if (_stopwatch != null && _stopwatch.ElapsedMilliseconds >= _timeLimitMilliseconds)
                throw new SearchTimeoutException();
        }

        private sealed class SearchTimeoutException : Exception { }
    }
}
