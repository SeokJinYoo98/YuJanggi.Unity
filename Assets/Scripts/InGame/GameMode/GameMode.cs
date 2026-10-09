using System.Collections.Generic;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Input;
    using Store;
    using Views;
    using YuJanggi.Engine.JanggiOption;

    internal interface IGameMode
    {
        public void Initialize();
        public void BindEvents();
        public void UnBindEvents();
        public void StartGame();
        public void Tick(float deltaTime);

        public void HandleTakebackButton();
        public void HandlePassTurnButton();
        public void HandleGiveUpButton();
        public void HandlePreviousButton();
        public void HandleNextButton();
        public void HandleRematchButton();
        public void HandleReplayButton();
    }
    internal abstract class GameMode : IGameMode, IGameCommandReceiver
    {
        #region Field
        protected readonly InGameView           _gameView;
        protected readonly IJanggiEngine        _engine;

        private bool _play = false;
        #endregion

        #region Constructors
        protected GameMode(
            InGameView gameView)
        {
            _gameView = gameView;
            _engine = JanggiEngineFactory.CreateEngine(
                JanggiOptionStore.JanggiSetting);
        }
        #endregion

        #region InGameUses
        public void Initialize()
        {
            _engine.InitEngine();
            _gameView.Initialize(_engine.Board);
            OnInit();
        }
        public void BindEvents()
        {
            _engine.BindEvents();

            var stateEvents = _engine.GameStateEvents;
            _gameView.BindEvents(stateEvents);

            var events = _engine.GameEvents;
            events.OnPieceMoved    += HandlePieceMoved;
            events.OnCheckReleased += HandleCheckReleased;
            events.OnCheckOccurred += HandleCheckOccured;
            events.OnTurnChanged   += HandleTurnChanged;
            events.OnGameEnded     += HandleGameEnded;

        }
        public void UnBindEvents()
        {
            var events = _engine.GameEvents;
            events.OnPieceMoved    -= HandlePieceMoved;
            events.OnCheckReleased -= HandleCheckReleased;
            events.OnCheckOccurred -= HandleCheckOccured;
            events.OnTurnChanged   -= HandleTurnChanged;
            events.OnGameEnded     -= HandleGameEnded;

            _gameView.UnBindEvents(_engine.GameStateEvents);
            _engine.UnBindEvents();
        }
        public void StartGame()
        {
            _play = true;
            _engine.StartEngine();

        }
        public void Tick(float deltaTime)
        {
            if (_play)
                _engine.Tick(deltaTime);
        }

        protected virtual void OnInit()
        {

        }
        #endregion

        #region Engine Events
        protected virtual void HandlePieceMoved(MoveContext moveCtx)
        {
            var board = _gameView.Board;
            board.ClearSelection();
            if (moveCtx.IsHandicap)
                return;

            board.ApplyMovement(moveCtx.Record);
        }
        protected virtual void HandleCheckReleased()
            => _gameView.CheckEffect.PlayMeonggun();
        protected virtual void HandleCheckOccured(PlayerTeam team)
            => _gameView.CheckEffect.PlayJanggun(team);
        protected virtual void HandleGameEnded(GameResultInfo info)
        {
            //_play = false;
            //_choInput.Deactivate();
            //_gameView.Board.SyncBoardState(_engine.Board);
            //_playerCho.EndTurn();
            //_playerHan.EndTurn();

            //var loser = info.Loser == PlayerTeam.Cho ? _playerCho : _playerHan;
            //_gameView.Result.ShowResult(in info, loser.IsLocal);
        }
        protected abstract void HandleTurnChanged(PlayerTeam next);
        #endregion

        #region Input Events
        public virtual void RequestMove(
            Pos from,
            Pos to)
                => _engine.TryMove(from, to);
        public virtual void SelectPiece(
            int? pieceId,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {
            var board = _gameView.Board;
            board.ClearSelection();
            if (pieceId is null)
                return;
            board.SelectPiece(pieceId.Value);
            board.ShowMoveGuides(legal, illegal);
        }
        #endregion

        #region UI Events
        public abstract void HandleTakebackButton();
        public abstract void HandlePassTurnButton();
        public abstract void HandleGiveUpButton();
        public abstract void HandlePreviousButton();
        public abstract void HandleNextButton();
        public abstract void HandleRematchButton();
        public abstract void HandleReplayButton();
        #endregion
    }
}
