using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace YuJanggi.Controller.AI
{
    using Core.V2.Board;
    using Core.V2.Domain;
    using Core.V2.Rule;
    public enum AIMoveStrategyType
    {
        Random,
        Greedy,
        Minimax
    }

    public readonly struct AIMove
    {
        public AIMove(Pos from, Pos to)
        {
            From = from;
            To = to;
        }

        public Pos From { get; }
        public Pos To { get; }
    }

    public interface IAIMoveStrategy
    {
        bool TrySelectMove(IBoardModel board, IJanggiRule rule, PlayerTeam team, out AIMove move);
    }

    public static class AIMoveStrategyFactory
    {
        public static IAIMoveStrategy Create(AIMoveStrategyType type)
        {
            return type switch
            {
                AIMoveStrategyType.Greedy => new GreedyAIMoveStrategy(),
                AIMoveStrategyType.Minimax => new MinimaxAIMoveStrategy(),
                _ => new RandomAIMoveStrategy()
            };
        }
    }

    public sealed class RandomAIMoveStrategy : IAIMoveStrategy
    {
        private readonly Random _random = new();

        public bool TrySelectMove(IBoardModel board, IJanggiRule rule, PlayerTeam team, out AIMove move)
        {
            var moves = AIMoveGenerator.Generate(board, rule, team);
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

        public bool TrySelectMove(IBoardModel board, IJanggiRule rule, PlayerTeam team, out AIMove move)
        {
            var moves = AIMoveGenerator.Generate(board, rule, team);
            if (moves.Count == 0)
            {
                move = default;
                return false;
            }

            int bestScore = int.MinValue;
            var bestMoves = new List<AIMove>();
            foreach (var candidate in moves)
            {
                int score = AIPieceValue.Get(board.GetPiece(candidate.To).Type);
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
        private const int MateScore = 100_000;
        private const int MaxQuiescenceDepth = 4;

        private readonly Random _random = new();
        private readonly int _maxSearchDepth;
        private readonly int _timeLimitMilliseconds;
        private readonly Dictionary<ulong, TranspositionEntry> _transpositionTable = new();
        private PlayerTeam _maximizingTeam;
        private Stopwatch _stopwatch;

        public MinimaxAIMoveStrategy(int maxSearchDepth = 4, int timeLimitMilliseconds = 350)
        {
            _maxSearchDepth = Math.Max(1, maxSearchDepth);
            _timeLimitMilliseconds = Math.Max(50, timeLimitMilliseconds);
        }

        public bool TrySelectMove(IBoardModel board, IJanggiRule rule, PlayerTeam team, out AIMove move)
        {
            var simulation = new AISimulationBoard(board);
            var moves = OrderMoves(simulation, rule, team);
            if (moves.Count == 0)
            {
                move = default;
                return false;
            }

            _maximizingTeam = team;
            _transpositionTable.Clear();
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

                        var record = simulation.DoMove(candidate.From, candidate.To);
                        int score;
                        try
                        {
                            score = Search(simulation, rule, AIPlayerTeam.Opponent(team), depth - 1, int.MinValue, int.MaxValue);
                        }
                        finally
                        {
                            simulation.UndoMove(record);
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

        private int Search(IBoardModel board, IJanggiRule rule, PlayerTeam currentTeam, int depth, int alpha, int beta)
        {
            ThrowIfTimedOut();

            ulong positionKey = CalculatePositionKey(board, currentTeam);
            if (_transpositionTable.TryGetValue(positionKey, out var cached) && cached.Depth >= depth)
                return cached.Score;

            var moves = OrderMoves(board, rule, currentTeam);
            if (moves.Count == 0)
                return EvaluateTerminal(board, rule, currentTeam);
            if (depth == 0)
                return QuiescenceSearch(board, rule, currentTeam, alpha, beta, MaxQuiescenceDepth);

            bool maximizing = currentTeam == _maximizingTeam;
            int bestScore = maximizing ? int.MinValue : int.MaxValue;
            bool wasCutOff = false;
            foreach (var candidate in moves)
            {
                var record = board.DoMove(candidate.From, candidate.To);
                int score;
                try
                {
                    score = Search(board, rule, AIPlayerTeam.Opponent(currentTeam), depth - 1, alpha, beta);
                }
                finally
                {
                    board.UndoMove(record);
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

            // Cut-off scores are bounds, not exact values, so do not reuse them
            // as a direct static score on another branch.
            if (!wasCutOff)
                _transpositionTable[positionKey] = new TranspositionEntry(depth, bestScore);

            return bestScore;
        }

        private int QuiescenceSearch(
            IBoardModel board,
            IJanggiRule rule,
            PlayerTeam currentTeam,
            int alpha,
            int beta,
            int remainingDepth)
        {
            ThrowIfTimedOut();

            bool maximizing = currentTeam == _maximizingTeam;
            bool mustEscapeCheck = IsKingInCheck(board, rule, currentTeam);
            var moves = OrderMoves(board, rule, currentTeam);
            if (moves.Count == 0)
                return EvaluateTerminal(board, rule, currentTeam);

            int standPat = Evaluate(board, rule);
            if (remainingDepth == 0)
                return standPat;

            // A checked side has no legal "stand pat" position: every legal
            // escape must be searched, even if the static score crosses a bound.
            if (!mustEscapeCheck && maximizing)
            {
                if (standPat >= beta)
                    return beta;
                alpha = Math.Max(alpha, standPat);
            }
            else if (!mustEscapeCheck)
            {
                if (standPat <= alpha)
                    return alpha;
                beta = Math.Min(beta, standPat);
            }

            foreach (var candidate in moves)
            {
                if (!mustEscapeCheck && !board.HasPiece(candidate.To))
                    continue;

                var record = board.DoMove(candidate.From, candidate.To);
                int score;
                try
                {
                    score = QuiescenceSearch(
                        board,
                        rule,
                        AIPlayerTeam.Opponent(currentTeam),
                        alpha,
                        beta,
                        remainingDepth - 1);
                }
                finally
                {
                    board.UndoMove(record);
                }

                if (maximizing)
                {
                    if (score >= beta)
                        return beta;
                    alpha = Math.Max(alpha, score);
                }
                else
                {
                    if (score <= alpha)
                        return alpha;
                    beta = Math.Min(beta, score);
                }
            }

            return maximizing ? alpha : beta;
        }

        private int EvaluateTerminal(IBoardModel board, IJanggiRule rule, PlayerTeam teamWithoutMove)
        {
            if (rule is not JanggiRule janggiRule || !janggiRule.IsKingInCheck(board, teamWithoutMove))
                return Evaluate(board, rule);

            return teamWithoutMove == _maximizingTeam ? -MateScore : MateScore;
        }

        private int Evaluate(IBoardModel board, IJanggiRule rule)
        {
            int score = 0;
            for (int x = 0; x < board.WIDTH; ++x)
            {
                for (int z = 0; z < board.HEIGHT; ++z)
                {
                    var pos = new Pos(x, z);
                    if (!board.HasPiece(pos))
                        continue;

                    var piece = board.GetPiece(pos);
                    int value = AIPieceValue.Get(piece.Type) + GetPositionalValue(board, pos, piece);
                    score += piece.Team == _maximizingTeam ? value : -value;
                }
            }

            var opponent = AIPlayerTeam.Opponent(_maximizingTeam);
            if (IsKingInCheck(board, rule, opponent))
                score += 3;
            if (IsKingInCheck(board, rule, _maximizingTeam))
                score -= 5;

            return score;
        }

        private static int GetPositionalValue(IBoardModel board, Pos pos, PieceModel piece)
        {
            int value = 0;
            if (piece.Type == PieceType.Soldier)
            {
                int progress = piece.Team == PlayerTeam.Cho ? pos.Z : board.HEIGHT - 1 - pos.Z;
                value += progress;
            }

            bool inEnemyPalace = board.IsPalace(pos) &&
                                 (piece.Team == PlayerTeam.Cho ? pos.Z >= board.HEIGHT - 3 : pos.Z <= 2);
            if (inEnemyPalace)
            {
                if (piece.Type == PieceType.Chariot)
                    value += 4;
                else if (piece.Type == PieceType.Cannon)
                    value += 2;
            }

            return value;
        }

        private static List<AIMove> OrderMoves(IBoardModel board, IJanggiRule rule, PlayerTeam team)
        {
            var moves = AIMoveGenerator.Generate(board, rule, team);
            moves.Sort((left, right) => ScoreMove(board, right).CompareTo(ScoreMove(board, left)));
            return moves;
        }

        private static int ScoreMove(IBoardModel board, AIMove move)
        {
            if (!board.HasPiece(move.To))
                return 0;

            var captured = board.GetPiece(move.To);
            var moved = board.GetPiece(move.From);
            return (AIPieceValue.Get(captured.Type) * 16) - AIPieceValue.Get(moved.Type);
        }

        private static bool IsKingInCheck(IBoardModel board, IJanggiRule rule, PlayerTeam team)
            => rule is JanggiRule janggiRule && janggiRule.IsKingInCheck(board, team);

        private void ThrowIfTimedOut()
        {
            if (_stopwatch != null && _stopwatch.ElapsedMilliseconds >= _timeLimitMilliseconds)
                throw new SearchTimeoutException();
        }

        private static ulong CalculatePositionKey(IBoardModel board, PlayerTeam currentTeam)
        {
            const ulong offsetBasis = 14_695_981_039_346_656_037UL;
            const ulong prime = 1_099_511_628_211UL;

            ulong hash = offsetBasis;
            for (int x = 0; x < board.WIDTH; ++x)
            {
                for (int z = 0; z < board.HEIGHT; ++z)
                {
                    var piece = board.GetPiece(new Pos(x, z));
                    hash ^= (ulong)((int)piece.Team + 1);
                    hash *= prime;
                    hash ^= (ulong)((int)piece.Type + 1);
                    hash *= prime;
                }
            }

            hash ^= (ulong)((int)currentTeam + 1);
            return hash;
        }

        private readonly struct TranspositionEntry
        {
            public TranspositionEntry(int depth, int score)
            {
                Depth = depth;
                Score = score;
            }

            public int Depth { get; }
            public int Score { get; }
        }

        private sealed class SearchTimeoutException : Exception
        {
        }
    }

    internal static class AIMoveGenerator
    {
        public static List<AIMove> Generate(IBoardModel board, IJanggiRule rule, PlayerTeam team)
        {
            var moves = new List<AIMove>();
            var selection = new Selection();
            for (int x = 0; x < board.WIDTH; ++x)
            {
                for (int z = 0; z < board.HEIGHT; ++z)
                {
                    var from = new Pos(x, z);
                    if (!board.HasPiece(from) || board.GetPiece(from).Team != team)
                        continue;

                    selection.Clear();
                    selection.FromPos = from;
                    rule.FindWays(board, selection);
                    foreach (var to in selection.LegalCells)
                        moves.Add(new AIMove(from, to));
                }
            }

            return moves;
        }
    }

    internal static class AIPieceValue
    {
        public static int Get(PieceType type)
        {
            return type switch
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
    }

    internal sealed class AISimulationBoard : IBoardModel
    {
        private readonly PieceModel[,] _pieces;
        private readonly bool[,] _palaces;
        private Pos _choKingPos;
        private Pos _hanKingPos;

        public AISimulationBoard(IBoardModel source)
        {
            WIDTH = source.WIDTH;
            HEIGHT = source.HEIGHT;
            _pieces = new PieceModel[WIDTH, HEIGHT];
            _palaces = new bool[WIDTH, HEIGHT];
            _choKingPos = source.GetKingPos(PlayerTeam.Cho);
            _hanKingPos = source.GetKingPos(PlayerTeam.Han);

            for (int x = 0; x < WIDTH; ++x)
            {
                for (int z = 0; z < HEIGHT; ++z)
                {
                    var pos = new Pos(x, z);
                    _palaces[x, z] = source.IsPalace(pos);
                    _pieces[x, z] = source.HasPiece(pos) ? source.GetPiece(pos) : PieceModel.None;
                }
            }
        }

        public int WIDTH { get; }
        public int HEIGHT { get; }

        public Pos GetKingPos(PlayerTeam team)
            => team == PlayerTeam.Cho ? _choKingPos : _hanKingPos;
        public bool IsInside(Pos pos)
            => 0 <= pos.X && pos.X < WIDTH && 0 <= pos.Z && pos.Z < HEIGHT;
        public bool HasPiece(Pos pos)
            => !_pieces[pos.X, pos.Z].IsNone;
        public PieceModel GetPiece(Pos pos)
            => _pieces[pos.X, pos.Z];
        public bool IsPalace(Pos pos)
            => IsInside(pos) && _palaces[pos.X, pos.Z];
        public void SetPiece(Pos pos, PieceModel piece)
            => _pieces[pos.X, pos.Z] = piece;

        public MoveRecord DoMove(Pos from, Pos to)
        {
            var moved = GetPiece(from);
            var captured = GetPiece(to);
            SetPiece(from, PieceModel.None);
            SetPiece(to, moved);
            UpdateKingPos(to, moved);
            return new MoveRecord(from, to, moved, captured);
        }

        public void UndoMove(in MoveRecord moveRecord)
        {
            SetPiece(moveRecord.From, moveRecord.MovedPiece);
            SetPiece(moveRecord.To, moveRecord.IsCapture ? moveRecord.CapturedPiece : PieceModel.None);
            UpdateKingPos(moveRecord.From, moveRecord.MovedPiece);
        }

        private void UpdateKingPos(Pos pos, PieceModel piece)
        {
            if (piece.Type != PieceType.King)
                return;

            if (piece.Team == PlayerTeam.Cho)
                _choKingPos = pos;
            else if (piece.Team == PlayerTeam.Han)
                _hanKingPos = pos;
        }
    }

    internal static class AIPlayerTeam
    {
        public static PlayerTeam Opponent(PlayerTeam team)
            => team == PlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho;
    }
}
