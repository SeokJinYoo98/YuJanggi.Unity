namespace YuJanggi.AI.Data
{
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
