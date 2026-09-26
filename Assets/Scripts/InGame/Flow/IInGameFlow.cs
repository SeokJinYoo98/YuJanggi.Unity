

using Cysharp.Threading.Tasks;
using System.Threading;

namespace YuJanggi.InGame.Flow
{
    public interface IInGameFlow
    {
        /// <summary>
        /// 인게임 흐름에 진입합니다.
        /// 중복 진입을 방지하고 필요한 이벤트를 연결한 뒤,
        /// 현재 게임 모드에 맞는 진입 절차를 수행합니다.
        /// </summary>
        UniTask EnterAsync(CancellationToken cancellationToken);
        /// <summary>
        /// 현재 인게임 흐름을 종료합니다.
        /// 중복 종료를 방지하고 진입 시 연결한 이벤트를 해제합니다.
        /// </summary>
        void Exit();
    }
    public abstract class InGameFlow : IInGameFlow
    {
        private bool _entered;


        public async UniTask EnterAsync(
            CancellationToken cancellationToken)
        {
            if (_entered)
                return;

            cancellationToken.ThrowIfCancellationRequested();

            _entered = true;
            Bind();

            try
            {
                await StartAsync(cancellationToken);
            }
            catch
            {
                Exit();
                throw;
            }
        }


        public void Exit()
        {
            if (!_entered)
                return;

            _entered = false;
            UnBind();
        }

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
