
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Profiling;

using YuJanggi.Core.AI;
namespace YuJanggi.AI.Service
{
    using Strategy;
    using Engine.Domain;
    using Engine.JanggiEngine;

    internal sealed class AIMoveService : IAIMoveService
    {
        private static readonly ProfilerMarker SnapshotMarker =
            new("AI.CreateSnapshot");

        private readonly IAIPositionSource _positions;
        private readonly IAI _strategy;

        public AIMoveService(IAIPositionSource positions, IAI strategy)
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
