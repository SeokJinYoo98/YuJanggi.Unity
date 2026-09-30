

using Cysharp.Threading.Tasks;
using System.Threading;
using YuJanggi.InGame.Session;
using YuJanggi.InGame.Controller;
using YuJanggi.Engine.Domain;
using System.Collections.Generic;

namespace YuJanggi.InGame.Flow
{
    public interface IInGameFlow : IGameInputReceiver
    {
        UniTask EnterAsync(CancellationToken cancellationToken);
        void Exit();
    }
    internal abstract class InGameFlow : IInGameFlow
    {
        private bool _entered;
        private CancellationToken _entryToken;
        protected GameSession Session { get; }
        protected InGameFlow(GameSession session)
        {
            Session = session;
        }
        public async UniTask EnterAsync(
            CancellationToken cancellationToken)
        {
            if (_entered)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            _entered = true;
            _entryToken = cancellationToken;

            try
            {
                Bind();
                await StartAsync(cancellationToken);
            }
            catch
            {
                if (_entered && _entryToken == cancellationToken)
                    Exit();
                throw;
            }
        }
        public void Exit()
        {
            if (!_entered)
                return;

            _entered = false;
            _entryToken = default;
            UnBind();
        }

        public abstract void RequestMove(Pos from, Pos to);

        public void ChangeSelection(
            int? pieceId,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
            => Session.ChangeSelection(pieceId, legal, illegal);

        /// <summary>
        /// 현재 게임 모드의 흐름 진행에 필요한 이벤트를 연결합니다.
        /// 별도의 이벤트 연결이 필요하지 않은 경우 재정의하지 않습니다.
        /// </summary>
        protected virtual void Bind()
        {
        }

        /// <summary>
        /// 현재 게임 모드에 맞는 인게임 시작 절차를 수행합니다.
        /// 로컬 게임은 즉시 게임을 시작하고,
        /// 네트워크 게임은 서버 준비 요청 등의 비동기 절차를 수행할 수 있습니다.
        /// </summary>
        protected abstract UniTask StartAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// 현재 게임 모드의 흐름을 위해 연결했던 이벤트를 해제합니다.
        /// 별도의 이벤트 해제가 필요하지 않은 경우 재정의하지 않습니다.
        /// </summary>
        protected virtual void UnBind()
        {
        }
    }
}


