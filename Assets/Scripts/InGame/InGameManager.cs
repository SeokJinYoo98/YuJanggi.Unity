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
        }
        private void BindEvents()
        {
            _engine.BindEvents();
            _engine.GameEvents.OnTurnCompleted += HandleTurnCompleted;
            _inGameView.BindEvents(_engine.GameStateEvents);
        }
        private void UnBindEvents()
        {
            _engine.GameEvents.OnTurnCompleted -= HandleTurnCompleted;
            _inGameView.UnBindEvents(_engine.GameStateEvents);
            _engine.UnBindEvents();
        }

        #region Engine Handle
        private void HandleTurnCompleted(TurnData data)
        {
            var nextTeam = data.ActingTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            if (data.MovedRecord is MoveRecord record)
                _inGameView.ApplyMoveRecord(record);

            if (data.GameResult is GameResultInfo result)
            {
                _inGameMode.StopGame();

                _inGameView.OnGameEnded(
                    result,
                    _inGameMode.GetPlayerType(result.Winner) == PlayerType.Local,
                    data.MoveCount);

                return;
            }

            var nextType = _inGameMode.GetPlayerType(nextTeam);

            _inGameMode.BeginNextTurn(nextTeam);

            _inGameView.ApplyLiveUI(
                nextTeam,
                nextType,
                data.Score,
                data.TotalTurn);

            if (data.CheckedTeam is PlayerTeam checkedTeam)
                _inGameView.PlayJanggunEffect(checkedTeam);

            if (data.CheckReleasedTeam is PlayerTeam releasedTeam)
                _inGameView.PlayMeonggunEffect(releasedTeam);

        }
        #endregion

        #region Input Handle
        public void RequestMove(Pos from, Pos to)
            => _inGameMode.RequestMoveAsync(
                    from,
                    to,
                    this.GetCancellationTokenOnDestroy()
                ).Forget();
        public void SelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
            => _inGameView.SelectPiece(id, legal, illegal);

        public void HandleTakebackButton() { }
        public void HandlePassTurnButton() { }
        public void HandleGiveUpButton() { }
        public void HandlePreviousButton() { }
        public void HandleNextButton() { }
        public void HandleRematchButton() { }
        public void HandleReplayButton() { }
        #endregion
    }
}


