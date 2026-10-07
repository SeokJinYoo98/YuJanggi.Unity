namespace YuJanggi.AI.Strategy
{
    using Abstraction;
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Search;

    public sealed class MinimaxAIMoveStrategy : IAI
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
}
