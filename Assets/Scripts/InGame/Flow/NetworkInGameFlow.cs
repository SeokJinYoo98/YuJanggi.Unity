

using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.InGame.Handler;
using YuJanggi.InGame.Service;
using YuJanggi.InGame.Session;

namespace YuJanggi.InGame.Flow
{
    public sealed class NetworkInGameFlow : InGameFlow
    {
        private readonly InGameHandler _handler;
        public NetworkInGameFlow(GameSession session, InGameHandler handler)
            : base(session)
        {
            _handler = handler;
        }

        protected override async UniTask StartAsync(CancellationToken cancellationToken)
        {
            await _handler.SendGameSceneReadyAsync(
                cancellationToken);

            await _handler.WaitUntilGameStartedAsync(
                cancellationToken);

            Session.StartGame();
        }
    }

}
