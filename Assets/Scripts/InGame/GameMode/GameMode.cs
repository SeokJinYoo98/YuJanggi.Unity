using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    public interface IGameMode
    {
        public bool IsEnd { get; }
        void Tick(float deltaTime);
        void SetLocalInputEnabled(bool enabled);
        PlayerType GetPlayerType(PlayerTeam team);
        void BeginNextTurn(PlayerTeam nextTeam);

        UniTask InitializeAsync(
            CancellationToken token = default);
        UniTask StartGameAsync(
            CancellationToken token = default);
        UniTask ClearAsync(
            CancellationToken token = default);
        UniTask<GameResultInfo> EndGameAsync(
            GameResultInfo info,
            int totalMoves,
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
    public abstract class GameMode : IGameMode
    {
        #region Field
        protected readonly IGameEngine _engine;
        private bool _play = false;
        public bool IsEnd => !_play;
        #endregion
        protected GameMode(
            IGameEngine engine)
        {
            _engine = engine;
        }

        public void Tick(float deltaTime)
        {
            if (_play) _engine.Tick(deltaTime);
        }
        public abstract void        BeginNextTurn(PlayerTeam team);
        public abstract PlayerType  GetPlayerType(PlayerTeam team);
        public abstract void        SetLocalInputEnabled(bool enabled);

        public async UniTask InitializeAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _play = false;
            _engine.InitEngine();
            await OnInitializeAsync(token);
        }
        public virtual async UniTask StartGameAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _play = false;

            await OnGameStartAsync(token);

            _engine.StartEngine();
            _play = true;
            Debug.Log("StartGameAsync");
            BeginNextTurn(_engine.ControllerQuery.CurrentTurn);

            // TODO: 시작 대기 중 취소 시 Engine이 시작되는지 확인하고, 필요하면 대기 후 검사를 추가합니다.
        }
        public async UniTask ClearAsync(CancellationToken token = default)
        {
            _play = false;
            await OnClearAsync(token);
        }

        public async UniTask<GameResultInfo> EndGameAsync(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token = default)
        {
            _play = false;
            return await OnGameEndAsync(info, totalMoves, token);
        }




        public async UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnRequestMoveAsync(from, to, token);
        }
        public async UniTask PassTurnAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnPassTurnAsync(token);
        }
        public async UniTask GiveUpAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnGiveUpAsync(token);
        }
        public async UniTask TakeBackAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnTakeBackAsync(token);
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
        protected abstract UniTask OnClearAsync(
            CancellationToken token);
        protected abstract UniTask<GameResultInfo> OnGameEndAsync(
            GameResultInfo info,
            int totalMoves,
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
