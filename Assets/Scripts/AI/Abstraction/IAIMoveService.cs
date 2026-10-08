using System.Threading;
using Cysharp.Threading.Tasks;

namespace YuJanggi.AI.Abstraction
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    internal interface IAIMoveService
    {
        UniTask<AIMove?> SelectMoveAsync(PlayerTeam team, CancellationToken cancellationToken);
    }
}
