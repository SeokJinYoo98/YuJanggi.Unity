using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;

namespace YuJanggi.Controller.AI
{
    internal interface IAIMoveService
    {
        bool TrySelectMove(PlayerTeam team, out AIMove move);
    }

    internal sealed class AIMoveService : IAIMoveService
    {
        private readonly IAIPositionSource _positions;
        private readonly IAIMoveStrategy _strategy;

        public AIMoveService(IAIPositionSource positions, IAIMoveStrategy strategy)
        {
            _positions = positions;
            _strategy = strategy;
        }

        public bool TrySelectMove(PlayerTeam team, out AIMove move)
            => _strategy.TrySelectMove(_positions.CreateAIPosition(), team, out move);
    }
}
