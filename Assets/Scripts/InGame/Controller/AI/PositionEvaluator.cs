using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;

namespace YuJanggi.Controller.AI
{
    internal sealed class PositionEvaluator
    {
        private const int MateScore = 100_000;
        private readonly PlayerTeam _maximizingTeam;

        public PositionEvaluator(PlayerTeam maximizingTeam)
            => _maximizingTeam = maximizingTeam;

        public int EvaluateTerminal(IAIPosition position, PlayerTeam teamWithoutMove)
        {
            if (!position.IsKingInCheck(teamWithoutMove))
                return Evaluate(position);
            return teamWithoutMove == _maximizingTeam ? -MateScore : MateScore;
        }

        public int Evaluate(IAIPosition position)
        {
            int score = 0;
            for (int x = 0; x < position.Width; ++x)
            {
                for (int z = 0; z < position.Height; ++z)
                {
                    var pos = new Pos(x, z);
                    if (!position.HasPiece(pos))
                        continue;

                    var piece = position.GetPiece(pos);
                    int value = AIPieceValue.Get(piece.Type) + GetPositionalValue(position, pos, piece);
                    score += piece.Team == _maximizingTeam ? value : -value;
                }
            }

            var opponent = AIPlayerTeam.Opponent(_maximizingTeam);
            if (position.IsKingInCheck(opponent)) score += 3;
            if (position.IsKingInCheck(_maximizingTeam)) score -= 5;
            return score;
        }

        private static int GetPositionalValue(IAIPosition position, Pos pos, YuJanggi.Engine.JanggiBoard.PieceModel piece)
        {
            int value = 0;
            if (piece.Type == PieceType.Soldier)
                value += piece.Team == PlayerTeam.Cho ? pos.Z : position.Height - 1 - pos.Z;

            bool inEnemyPalace = position.IsPalace(pos) &&
                (piece.Team == PlayerTeam.Cho ? pos.Z >= position.Height - 3 : pos.Z <= 2);
            if (inEnemyPalace)
            {
                if (piece.Type == PieceType.Chariot) value += 4;
                else if (piece.Type == PieceType.Cannon) value += 2;
            }
            return value;
        }
    }
}
