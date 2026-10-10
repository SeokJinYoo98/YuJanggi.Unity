using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Cysharp.Threading.Tasks;
    using DG.Tweening.Core.Easing;
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Input;
    using Store;
    using System.Threading;
    using Views;
    using YuJanggi.Engine.JanggiOption;

    internal interface IGameMode
    {
        void Initialize();
        void StartGame();
        void StopGame();
        void Tick(float deltaTime);
        UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default);
        PlayerType GetPlayerType(PlayerTeam team);
        void BeginNextTurn(PlayerTeam nextTeam);
    }
    internal abstract class GameMode : IGameMode
    {
        #region Field
        protected readonly IJanggiEngine _engine;
        private bool _play = false;
        #endregion


        protected GameMode(
            IJanggiEngine engine)
        {
            _engine = engine;
        }
  
        public void StartGame()
        {
            _play = true;
            OnStartGame();
        }
        public void StopGame()
        {
            _play = false;
            OnStopGame();
        }
        public abstract void Initialize();
        public async UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await OnRequestMoveAsync(from, to, token);
            }
            catch (System.OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 요청 생명주기 종료에 따른 취소입니다.
            }
            catch (System.Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
        public void Tick(float deltaTime)
        {
            if (_play)
                OnTick(deltaTime);
        }
        public abstract void BeginNextTurn(PlayerTeam team);
        public abstract PlayerType GetPlayerType(PlayerTeam team);

        protected abstract void OnTick(float deltaTime);
        protected abstract UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token);
        protected abstract void OnStartGame();
        protected abstract void OnStopGame();
    }
}
