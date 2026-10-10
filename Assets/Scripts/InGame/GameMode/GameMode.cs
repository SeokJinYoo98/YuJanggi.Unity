using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    internal interface IGameMode
    {

        void Tick(float deltaTime);

        PlayerType GetPlayerType(PlayerTeam team);
        void BeginNextTurn(PlayerTeam nextTeam);

        UniTask InitializeAsync(
            CancellationToken token = default);
        UniTask StartGameAsync(
            CancellationToken token = default);
        UniTask EndGameAsync(
            CancellationToken token = default);
        UniTask<bool> RequestRematchAsync(
            CancellationToken token = default);
        UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default);
        public UniTask PassTurnAsync(
            CancellationToken token = default);
        public UniTask GiveUpAsync(
            CancellationToken token = default);
        public UniTask TakeBackAsync(
            CancellationToken token = default);
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

        public void Tick(float deltaTime)
        {
            if (_play)
                OnTick(deltaTime);
        }
        public abstract void BeginNextTurn(PlayerTeam team);
        public abstract PlayerType GetPlayerType(PlayerTeam team);
 
        protected abstract void OnTick(float deltaTime);


        public async UniTask EndGameAsync(
            CancellationToken token = default)
        {
            _play = false;
            token.ThrowIfCancellationRequested();
            await OnGameEndAsync(token);
        }
        public async UniTask InitializeAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _play = false;
            _engine.InitEngine();
            await OnInitializeAsync(token);
        }
        public async UniTask StartGameAsync(
            CancellationToken token = default)
        {
            _play = false;
            token.ThrowIfCancellationRequested();
            _engine.StartEngine();
            await OnGameStartAsync(token);
            token.ThrowIfCancellationRequested();
            _play = true;
        }
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
        public async UniTask PassTurnAsync(
            CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await OnPassTurnAsync(token);
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                // 요청 취소
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
        public async UniTask GiveUpAsync(
            CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await OnGiveUpAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 요청 생명주기 종료에 따른 취소입니다.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
        public async UniTask TakeBackAsync(
            CancellationToken token = default)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                await OnTakeBackAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 요청 생명주기 종료에 따른 취소입니다.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

        }

        public async UniTask<bool> RequestRematchAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await OnRequestRematchAsync(token);
        }

        protected abstract UniTask<bool> OnRequestRematchAsync(
            CancellationToken token);
        protected abstract UniTask OnInitializeAsync(
            CancellationToken token);
        protected abstract UniTask OnGameEndAsync(
            CancellationToken token);
        protected abstract UniTask OnGameStartAsync(
            CancellationToken token);
        protected abstract UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token);
        protected abstract UniTask OnPassTurnAsync(
            CancellationToken token);
        protected abstract UniTask OnGiveUpAsync(
            CancellationToken token);
        protected abstract UniTask OnTakeBackAsync(
            CancellationToken token);
    }
}
