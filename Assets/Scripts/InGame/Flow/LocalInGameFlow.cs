

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

        /// <summary>
        /// 로컬 게임의 인게임 시작 절차를 수행합니다.
        /// 별도의 네트워크 준비 과정 없이 즉시 게임 세션을 시작합니다.
        /// </summary>
        protected override UniTask StartAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _session.StartGame();

            return UniTask.CompletedTask;
        }
    }

}
