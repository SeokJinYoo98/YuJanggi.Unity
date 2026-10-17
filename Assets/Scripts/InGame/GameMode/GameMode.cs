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
        GameModeType ModeType { get; }
        event Action<TurnData, PlayerType> OnTurnCompleted;
        event Action<UndoData, PlayerType> OnUndoCompleted;
        event Action<GameResultInfo, bool, int> OnGameEnded;
        void BindEvents();
        void UnBindEvents();
        public bool IsEnd { get; }
        void Tick(float deltaTime);
        void SetLocalInputEnabled(bool enabled);
        PlayerType GetPlayerType(PlayerTeam team);

        public void Initialize(CancellationToken token);
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
        public void TakeBack();
    }
    public abstract class GameMode : IGameMode
    {
        #region Field
        protected readonly IJanggiEngine _engine;

        public event Action<TurnData, PlayerType>       OnTurnCompleted;
        public event Action<UndoData, PlayerType>       OnUndoCompleted;
        public event Action<GameResultInfo, bool, int>  OnGameEnded;

        private CancellationTokenSource _gameCts;
        protected CancellationToken     _gameToken;

        private bool   _play = false;
        protected bool _localInputEnabled = true;
        public bool IsEnd => !_play;
        public abstract GameModeType ModeType { get; }
        protected virtual bool ShouldTickEngine => true;
        #endregion
        #region Constructor
        protected GameMode(
            IJanggiEngine engine)
        {
            _engine = engine;
        }

        #endregion

        #region Update
        public void Tick(float deltaTime)
        {
            if (_play && ShouldTickEngine) _engine.Tick(deltaTime);
        }
        #endregion

        #region Turn & Input
        protected abstract void     BeginNextTurn(PlayerTeam team);
        public abstract PlayerType  GetPlayerType(PlayerTeam team);
        public void                 SetLocalInputEnabled(bool enabled)
        {
            _localInputEnabled = enabled;
            OnSetLocalInputEnabled(enabled);
        }
        protected abstract void     OnSetLocalInputEnabled(bool enabled);
        #endregion

        #region Engine Events
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
        protected virtual void HandleTurnCompleted(
            TurnData data)
        {
            var nextTeam = data.ActingTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            if (data.GameResult is null)
                BeginNextTurn(nextTeam);

            OnTurnCompleted?.Invoke(data, GetPlayerType(nextTeam));

            if (data.GameResult is GameResultInfo result)
                HandleGameEnded(result, data.MoveCount);
        }
        protected abstract void HandleGameEnded(
            GameResultInfo result,
            int moveCount);
        protected virtual void HandleUndoCompleted(
            UndoData data)
        {
            BeginNextTurn(data.CurrentTurn);
            OnUndoCompleted?.Invoke(data, GetPlayerType(data.CurrentTurn));
        }
        #endregion

        #region Game Start
        public void Initialize(CancellationToken token)
        {
            CancelGameTasks();
            _gameCts    = CancellationTokenSource.CreateLinkedTokenSource(token);
            _gameToken  = _gameCts.Token;

            _play              = false;
            _localInputEnabled = true;

            BindEvents();

            _engine.InitEngine();
            OnInitialize();
        }
        protected abstract void OnInitialize();

        public virtual async UniTask StartGameAsync(
            CancellationToken token = default)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _gameToken);
            token = cts.Token;
            token.ThrowIfCancellationRequested();
            _play = false;

            await OnGameStartAsync(token);

            token.ThrowIfCancellationRequested();
            _engine.StartEngine();
            _play = true;

            BeginNextTurn(_engine.ControllerQuery.CurrentTurn);

        }
        protected abstract UniTask OnGameStartAsync(
            CancellationToken token);
        #endregion

        #region Move Requests
        public abstract UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token = default);
        #endregion

        #region LiveGame UI Buttons
        public abstract UniTask PassTurnAsync(
            CancellationToken token = default);
        public virtual void TakeBack() { }
        public abstract UniTask GiveUpAsync(
            CancellationToken token = default);
        #endregion

        #region GameEnd UI Buttons
        public virtual UniTask<bool> RequestRematchAsync(
            CancellationToken token = default)
            => UniTask.FromResult(true);
        #endregion

        #region Game End
 

        protected void NotifyGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            _play = false;
            UnBindEvents();

            CancelGameTasks();

            OnGameEnded?.Invoke(
                result,
                GetPlayerType(result.Winner) == PlayerType.Local,
                moveCount);
        }
        #endregion

        #region Clear
        public async UniTask ClearAsync(
            CancellationToken token = default)
        {
            _play = false;
            UnBindEvents();
            CancelGameTasks();
 
            await OnClearAsync(token);
        }
        protected abstract UniTask OnClearAsync(
            CancellationToken token);

        private void CancelGameTasks()
        {
            var cts = _gameCts;
            _gameCts = null;

            if (cts == null)
                return;

            try
            {
                cts.Cancel();
            }
            finally
            {
                cts.Dispose();
            }
        }
        #endregion
    }
}
