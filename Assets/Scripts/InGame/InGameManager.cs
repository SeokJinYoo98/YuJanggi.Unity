using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using Input;
    using Mode;

    using Views;
    using Core.InGame;
    using Engine.Domain;
    using Engine.JanggiEngine;
 
    using Store;

    public class InGameManager : MonoBehaviour, IGameCommandReceiver
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;

        [Header("View")]
        [SerializeField] private InGameView   _inGameView;

        private IGameMode     _inGameMode;
        private IJanggiEngine _engine;
        private CancellationTokenSource _lifecycleCts;
        private CancellationToken _lifecycleToken;

        #endregion

        private void Awake()
        {
            _lifecycleCts = new CancellationTokenSource();
            _lifecycleToken = _lifecycleCts.Token;

            _engine = JanggiEngineFactory.CreateEngine(
                JanggiOptionStore.JanggiSetting);

            _inGameMode = GameModeFactory.Create(
                JanggiOptionStore.JanggiSetting.GameMode,
                _engine,
                new InputHandlerFactory(_inputs, transform),
                this);
        }
        private void Update()
            => _inGameMode?.Tick(Time.deltaTime);
        private void OnEnable()
            => BindEvents();
        private void OnDisable()
            => UnBindEvents();
        private void Start()
            => InitializeAndStartAsync().Forget();
        private void OnDestroy()
        {
            try
            {
                _lifecycleCts.Cancel();
            }
            finally
            {
                _lifecycleCts.Dispose();
            }
        }

        private async UniTask InitializeAndStartAsync()
        {
            await InitializeAsync();
            await StartGameAsync();
        }

        private async UniTask InitializeAsync()
        {
            var token = _lifecycleToken;
            await _inGameMode.InitializeAsync(token);
            token.ThrowIfCancellationRequested();
            _inGameView.Initialize(_engine.Board);
        }
        private async UniTask StartGameAsync()
        {
            await _inGameMode.StartGameAsync(_lifecycleToken);
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
        private async UniTask EndGameAsync(
            GameResultInfo result,
            int moveCount)
        {
            var token = _lifecycleToken;
            await _inGameMode.EndGameAsync(token);
            token.ThrowIfCancellationRequested();

            _inGameView.OnGameEnded(
                result,
                _inGameMode.GetPlayerType(result.Winner) == PlayerType.Local,
                moveCount);
        }
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
                EndGameAsync(result, data.MoveCount).Forget();

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
                    _lifecycleToken
                ).Forget();
        public void HandleTakebackButton()
            => _inGameMode.TakeBackAsync(_lifecycleToken).Forget();
        public void HandlePassTurnButton()
            => _inGameMode.PassTurnAsync(_lifecycleToken).Forget();
        public void HandleGiveUpButton()
            => _inGameMode.GiveUpAsync(_lifecycleToken).Forget();
        public void HandlePreviousButton() { }
        public void HandleNextButton() { }

        public void HandleLobbyButton()
            => ReturnToLobbyAsync().Forget();

        private async UniTask ReturnToLobbyAsync()
        {
            var token = _lifecycleToken;
            await _inGameMode.EndGameAsync(token);
            token.ThrowIfCancellationRequested();
            SceneManager.LoadScene("LobbyScene");
        }
        public void HandleRematchButton()
            => RematchAsync().Forget();

        private async UniTask RematchAsync()
        {
            var token = _lifecycleToken;

            // 재대결이 승인된 경우에만 기존 게임을 초기화합니다.
            if (!await _inGameMode.RequestRematchAsync(token))
                return;

            await _inGameMode.InitializeAsync(token);
            token.ThrowIfCancellationRequested();

            _inGameView.SyncLiveView(
                _engine.Board,
                PlayerTeam.Cho,
                _inGameMode.GetPlayerType(PlayerTeam.Cho));

            await _inGameMode.StartGameAsync(token);
        }
        #endregion
    }
}


