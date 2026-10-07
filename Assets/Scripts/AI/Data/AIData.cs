namespace YuJanggi.AI.Data
{
    public static class AISessionSettings
    {
        public static AIMoveStrategyType Strategy { get; set; } = AIMoveStrategyType.Random;
    }

    public enum AIMoveStrategyType
    {
        Random,
        Greedy,
        Minimax
    }

    internal readonly struct Entry
    {
        public Entry(int depth, int score) { Depth = depth; Score = score; }
        public int Depth { get; }
        public int Score { get; }
    }
}
