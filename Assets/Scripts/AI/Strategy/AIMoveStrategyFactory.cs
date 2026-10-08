
using YuJanggi.Core.AI;

namespace YuJanggi.AI.Strategy
{
    using Data;

    public static class AIMoveStrategyFactory
    {
        public static IAI Create(AIMoveStrategyType type)
            => type switch
            {
                AIMoveStrategyType.Greedy => new GreedyAIMoveStrategy(),
                AIMoveStrategyType.Minimax => new MinimaxAIMoveStrategy(),
                _ => new RandomAIMoveStrategy()
            };
    }

}
