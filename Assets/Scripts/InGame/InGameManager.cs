using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace YuJanggi.InGame
{
    using Input;
    using Mode;
    using UnityEngine.SocialPlatforms.Impl;
    using Views;
    using YuJanggi.Core.InGame;
    using YuJanggi.Engine.Domain;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.InGame.Views.Board;
    using YuJanggi.Store;

    public class InGameManager : MonoBehaviour, IGameCommandReceiver
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;

        [Header("View")]
        [SerializeField] private InGameView   _inGameView;

        private IGameMode     _inGameMode;
        private IJanggiEngine _engine;

        #endregion

        private void Awake()
        {
            _engine = JanggiEngineFactory.CreateEngine(
                JanggiOptionStore.JanggiSetting);

            _inGameMode = GameModeFactory.Create(
                JanggiOptionStore.JanggiSetting.GameMode,
                _engine,
                new InputHandlerFactory(_inputs, transform),
                this);
        }
        private void OnEnable()
            => BindEvents();
        private void OnDisable()
            => UnBindEvents();
        private void Start()
        {
            Initialize();
            StartGame();
        }
        private void Update()
            => _inGameMode?.Tick(Time.deltaTime);
        private void Initialize()
        {
            _engine.InitEngine();
            _inGameMode.Initialize();
            _inGameView.Initialize(_engine.Board);
        }
        private void StartGame()
        {
            _engine.StartEngine();
            _inGameMode.StartGame();
            _inGameView.StartGame(
                PlayerTeam.Cho,
                _inGameMode.GetPlayerType(PlayerTeam.Cho));
        }
        private void BindEvents()
        {
            _engine.BindEvents();
            _inGameView.BindEvents(_engine.GameStateEvents);
            _engine.GameEvents.OnTurnCompleted += HandleTurnCompleted;
            _engine.GameEvents.OnUndoCompleted += HandleUndoCompleted;
        }
        private void UnBindEvents()
        {
            _engine.GameEvents.OnTurnCompleted -= HandleTurnCompleted;
            _engine.GameEvents.OnUndoCompleted -= HandleUndoCompleted;
            _inGameView.UnBindEvents(_engine.GameStateEvents);
            _engine.UnBindEvents();
        }

        #region Engine Handle
        private void HandleUndoCompleted(UndoData data)
        {
            _inGameView.ClearSelection();

            if (data.UndoneMove is MoveRecord record)
                _inGameView.RevertMoveRecord(record);

            var currentTeam = data.CurrentTurn;
            var currentType = _inGameMode.GetPlayerType(currentTeam);

            _inGameView.ApplyScore(data.Score);
            _inGameView.ApplyLiveUI(currentTeam, currentType);

            if (data.CheckReleasedTeam is PlayerTeam releasedTeam)
                _inGameView.PlayMeonggunEffect(releasedTeam);

            if (data.CheckedTeam is PlayerTeam checkedTeam)
                _inGameView.PlayJanggunEffect(checkedTeam);

            _inGameMode.BeginNextTurn(currentTeam);
        }
        private void HandleTurnCompleted(TurnData data)
        {
            _inGameView.ClearSelection();

            var nextTeam = data.ActingTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            if (data.MovedRecord is MoveRecord record)
                _inGameView.ApplyMoveRecord(record);

            _inGameView.ApplyScore(data.Score);

            if (data.GameResult is GameResultInfo result)
            {
                _inGameMode.EndGame();

                _inGameView.OnGameEnded(
                    result,
                    _inGameMode.GetPlayerType(result.Winner) == PlayerType.Local,
                    data.MoveCount);

                return;
            }

            var nextType = _inGameMode.GetPlayerType(nextTeam);

            _inGameView.ApplyLiveUI(
                nextTeam,
                nextType);

            if (data.CheckedTeam is PlayerTeam checkedTeam)
                _inGameView.PlayJanggunEffect(checkedTeam);

            if (data.CheckReleasedTeam is PlayerTeam releasedTeam)
                _inGameView.PlayMeonggunEffect(releasedTeam);

            _inGameMode.BeginNextTurn(nextTeam);
        }
        #endregion

        #region Input Handle

        public void SelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
        {
            if (id is null)
            {
                _inGameView.ClearSelection();
                return;
            }
            _inGameView.SelectPiece(
                id.Value,
                legal,
                illegal);
        }
        public void HandleRequestMove(Pos from, Pos to)
            => _inGameMode.RequestMoveAsync(
                    from,
                    to,
                    this.GetCancellationTokenOnDestroy()
                ).Forget();
        public void HandleTakebackButton()
            => _inGameMode.TakeBackAsync().Forget();
        public void HandlePassTurnButton()
            => _inGameMode.PassTurnAsync().Forget();

        public void HandleGiveUpButton()
        {

        }
        public void HandlePreviousButton() { }
        public void HandleNextButton() { }


        public void HandleRematchButton() { }
        public void HandleReplayButton() { }
        public void HandleLobbyButton() { }
        #endregion
    }
}


