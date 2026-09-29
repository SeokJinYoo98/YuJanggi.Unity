
using System.Threading;
using Cysharp.Threading.Tasks;

namespace YuJanggi.InGame.Controller.AI
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Unity.Profiling;

    internal interface IAIMoveService
    {
        UniTask<AIMove?> SelectMoveAsync(PlayerTeam team, CancellationToken cancellationToken);
    }

    internal sealed class AIMoveService : IAIMoveService
    {
        private static readonly ProfilerMarker SnapshotMarker =
            new("AI.CreateSnapshot");

        private readonly IAIPositionSource _positions;
        private readonly IAIMoveStrategy _strategy;

        public AIMoveService(IAIPositionSource positions, IAIMoveStrategy strategy)
        {
            _positions = positions;
            _strategy = strategy;
        }

        public async UniTask<AIMove?> SelectMoveAsync(PlayerTeam team, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IAIPosition position;
            using (SnapshotMarker.Auto())
                position = _positions.CreateAIPosition();

            cancellationToken.ThrowIfCancellationRequested();
            if (_strategy is MinimaxAIMoveStrategy)
            {
                var strategy = _strategy;
                var result = await UniTask.RunOnThreadPool(() =>
                {
                    bool found = strategy.TrySelectMove(position, team, out var selected);
                    return (found, selected);
                }, configureAwait: true, cancellationToken: cancellationToken);

                return result.Item1 ? result.Item2 : (AIMove?)null;
            }

            return _strategy.TrySelectMove(position, team, out var move)
                ? move
                : (AIMove?)null;
        }
    }
}
