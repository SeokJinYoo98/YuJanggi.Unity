using System.Collections.Generic;
using UnityEngine;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Controller;
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
    }
    internal abstract class GameMode : IGameMode, IGameInputReceiver
    {
        protected readonly InGameView           _gameView;
        protected readonly IJanggiEngine        _engine;
        protected readonly IInputHandler        _localInput;
        protected readonly IInGameController    _playerCho;
        protected readonly IInGameController    _playerHan;

        private bool _play = false;
        protected GameMode(
            InGameView    gameView,
            IInputHandler localInput,
            PlayerType    choType,
            PlayerType    hanType)
        {
            _gameView   = gameView;

            _localInput = localInput;

            _engine = JanggiEngineFactory.CreateEngine(JanggiOptionStore.JanggiSetting);

            _playerCho = InGameControllerFactory.CreateController(
                choType,
                PlayerTeam.Cho,
                _engine,
                _engine,
                localInput);

            _playerHan = InGameControllerFactory.CreateController(
                hanType,
                PlayerTeam.Han,
                _engine,
                _engine,
                localInput);
        }
        public void Initialize()
        {
            _playerCho.Initialize(this);
            _playerHan.Initialize(this);
            _engine.InitEngine();
            _gameView.Initialize(_engine.Board);
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

            _playerCho.BindEvents();
            _playerHan.BindEvents();

            // Engine 이벤트 → Mode 처리 메서드 연결
        }
        public void UnBindEvents()
        {
            _playerCho.UnBindEvents();
            _playerHan.UnBindEvents();

            var events = _engine.GameEvents;
            events.OnPieceMoved    -= HandlePieceMoved;
            events.OnCheckReleased -= HandleCheckReleased;
            events.OnCheckOccurred -= HandleCheckOccured;
            events.OnTurnChanged   -= HandleTurnChanged;
            events.OnGameEnded     -= HandleGameEnded;
            // Engine 이벤트 → Mode 처리 메서드 연결 해제

            _gameView.UnBindEvents(_engine.GameStateEvents);
            _engine.UnBindEvents();
        }
        public void StartGame()
        {
            _play = true;
            _engine.StartEngine();
            _playerCho.BeginTurn();
            _playerHan.EndTurn();
        }
        public void Tick(float deltaTime)
        {
            if (_play)
                _engine.Tick(deltaTime);
        }

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
        protected virtual void HandleTurnChanged(PlayerTeam next)
        {
            var nextPlayer =
                next == PlayerTeam.Cho ? _playerCho : _playerHan;

            _gameView.Live.UpdateTurn(next, nextPlayer.IsLocal);

            _playerCho.EndTurn();
            _playerHan.EndTurn();
            nextPlayer.BeginTurn();
        }
        protected virtual void HandleGameEnded(GameResultInfo info)
        {
            _play = false;
            _localInput.Deactivate();
            _gameView.Board.SyncBoardState(_engine.Board);
            _playerCho.EndTurn();
            _playerHan.EndTurn();

            var loser = info.Loser == PlayerTeam.Cho ? _playerCho : _playerHan;
            _gameView.Result.ShowResult(in info, loser.IsLocal);
        }


        public virtual void RequestMove(Pos from, Pos to)
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
    }
}
