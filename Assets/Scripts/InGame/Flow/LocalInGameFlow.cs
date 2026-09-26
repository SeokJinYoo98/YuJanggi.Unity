

using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.InGame.Session;

namespace YuJanggi.InGame.Flow
{
    public sealed class LocalInGameFlow : InGameFlow
    {
        private readonly GameSession _session;

        public LocalInGameFlow(GameSession session)
        {
            _session = session;
        }

        protected override UniTask StartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }

}
