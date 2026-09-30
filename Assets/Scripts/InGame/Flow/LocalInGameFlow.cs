

using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.InGame.Session;
using YuJanggi.Engine.Domain;

namespace YuJanggi.InGame.Flow
{
    internal sealed class LocalInGameFlow : InGameFlow
    {
        public LocalInGameFlow(GameSession session)
            : base(session)
        {
        }

        public override void RequestMove(Pos from, Pos to)
            => Session.RequestMove(from, to);

        /// <summary>
        /// 로컬 게임의 인게임 시작 절차를 수행합니다.
        /// 별도의 네트워크 준비 과정 없이 즉시 게임 세션을 시작합니다.
        /// </summary>
        protected override UniTask StartAsync(
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Session.IsStarted)
                Session.StartGame();

            return UniTask.CompletedTask;
        }
    }

}


