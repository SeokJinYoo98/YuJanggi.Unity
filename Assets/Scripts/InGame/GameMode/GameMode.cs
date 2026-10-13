using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    public interface IGameMode
    {
        event Action<TurnData, PlayerType> OnTurnCompleted;
        event Action<UndoData, PlayerType> OnUndoCompleted;
        event Action<GameResultInfo, bool, int> OnGameEnded;
        void BindEvents();
        void UnBindEvents();
        public bool IsEnd { get; }
        void Tick(float deltaTime);
        void SetLocalInputEnabled(bool enabled);
        PlayerType GetPlayerType(PlayerTeam team);

        UniTask InitializeAsync(
            CancellationToken token = default);
        UniTask StartGameAsync(
            CancellationToken token = default);
        UniTask ClearAsync(
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
        protected readonly IJanggiEngine _engine;

        public event Action<TurnData, PlayerType>       OnTurnCompleted;
        public event Action<UndoData, PlayerType>       OnUndoCompleted;
        public event Action<GameResultInfo, bool, int>  OnGameEnded;
        private CancellationToken _lifecycleToken;
        private bool   _play = false;
        protected bool _localInputEnabled = true;
        public bool IsEnd => !_play;
        #endregion
        protected GameMode(
            IJanggiEngine engine)
        {
            _engine = engine;
        }

        public void Tick(float deltaTime)
        {
            if (_play) _engine.Tick(deltaTime);
        }
        protected abstract void     BeginNextTurn(PlayerTeam team);
        public abstract PlayerType  GetPlayerType(PlayerTeam team);
        public void SetLocalInputEnabled(bool enabled)
        {
            _localInputEnabled = enabled;
            OnSetLocalInputEnabled(enabled);
        }

        protected abstract void OnSetLocalInputEnabled(bool enabled);

        public void BindEvents()
        {
            UnBindEvents();
            var events = _engine.References.GameEvents;
            events.OnTurnCompleted += HandleTurnCompleted;
            events.OnUndoCompleted += HandleUndoCompleted;
        }

        public void UnBindEvents()
        {
            var events = _engine.References.GameEvents;
            events.OnTurnCompleted -= HandleTurnCompleted;
            events.OnUndoCompleted -= HandleUndoCompleted;
        }

        protected virtual void HandleTurnCompleted(TurnData data)
        {
            var nextTeam = data.ActingTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            if (data.GameResult is null)
                BeginNextTurn(nextTeam);

            // 종료 결과도 TurnData.GameResult에 포함된 상태로 기존 처리에 전달합니다.
            OnTurnCompleted?.Invoke(data, GetPlayerType(nextTeam));

            if (data.GameResult is GameResultInfo result)
                HandleGameEndedAsync(
                    result,
                    data.MoveCount).Forget();
              
        }
        protected virtual void HandleUndoCompleted(UndoData data)
        {
            BeginNextTurn(data.CurrentTurn);
            OnUndoCompleted?.Invoke(data, GetPlayerType(data.CurrentTurn));
        }


        public async UniTask InitializeAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _lifecycleToken = token;
            _play = false;
            _localInputEnabled = true;
            BindEvents();
            _engine.InitEngine();
            await OnInitializeAsync(token);
        }
        protected abstract UniTask OnInitializeAsync(
            CancellationToken token);

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
        protected abstract UniTask OnGameStartAsync(
            CancellationToken token);

        public async UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnRequestMoveAsync(from, to, token);
        }
        protected abstract UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token);

        public async UniTask PassTurnAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnPassTurnAsync(token);
        }
        protected abstract UniTask OnPassTurnAsync(
            CancellationToken token);

        public async UniTask TakeBackAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await OnTakeBackAsync(token);
        }
        protected abstract UniTask OnTakeBackAsync(
            CancellationToken token);


        public async UniTask<bool> RequestRematchAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await OnRequestRematchAsync(token);
        }

        protected abstract UniTask<bool> OnRequestRematchAsync(
            CancellationToken token);

        public async UniTask GiveUpAsync(
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            var loser = await OnGiveUpAsync(token);

            if (loser != null)
                _engine.GiveUp(loser.Value);
        }
        protected abstract UniTask<PlayerTeam?> OnGiveUpAsync(
            CancellationToken token);

        protected async UniTask HandleGameEndedAsync(
            GameResultInfo result,
            int moveCount)
        {
            _play = false;
            UnBindEvents();

            result = await OnGameEndAsync(
                result,
                _engine.References.ReadOnlyRecord.Count,
                _lifecycleToken);
            // 결과가 무엇인지?

            OnGameEnded?.Invoke(
                result,
                GetPlayerType(result.Winner) == PlayerType.Local,
                moveCount);
        }
        protected abstract UniTask<GameResultInfo> OnGameEndAsync(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token);

        #region 게임 종료



        protected UniTask HandleGiveUpAsync(PlayerTeam team)
        {
            if (IsEnd)
                return UniTask.CompletedTask;

            var result = new GameResultInfo
            {
                Type   = GameResult.GiveUp,
                Loser  = team,
                Winner = team == PlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho
            };

            _engine.EndEngine();

            return HandleGameEndedAsync(
                result,
                _engine.References.ReadOnlyRecord.Count);
        }
        public async UniTask ClearAsync(CancellationToken token = default)
        {
            _play = false;
            UnBindEvents();
            //
            // 종료메서드
            //
            await OnClearAsync(token);
            //
        }





        protected abstract UniTask OnClearAsync(
            CancellationToken token);
        #endregion
    }
}
