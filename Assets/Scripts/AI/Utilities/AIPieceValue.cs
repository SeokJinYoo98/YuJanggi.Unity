namespace YuJanggi.AI.Utilities
{
    using Engine.Domain;

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
}
