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
    using State;

    using Views;
    using Core.InGame;
    using Engine.Domain;
    using Engine.JanggiEngine;

    using Store;

    public class InGameManager : MonoBehaviour, IGameCommandReceiver, IStateMachine
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;

        [Header("View")]
        [SerializeField] private InGameView _inGameView;

        private IGameMode _inGameMode;
        private IJanggiEngine _engine;
        private readonly Dictionary<InGameStateType, IInGameState> _states = new();

        private CancellationTokenSource _lifecycleCts;
        private CancellationToken _lifecycleToken;

        private IInGameState _currState;
        #endregion

        private IReadOnlyEngine EngineReferences
            => _engine.References;
        private bool IsLive => _currState.State == InGameStateType.Live;
        private void Awake()
        {
            _lifecycleCts = new CancellationTokenSource();
            _lifecycleToken = _lifecycleCts.Token;

            _engine = JanggiEngineFactory.CreateEngine(
                JanggiOptionStore.JanggiSetting);

            _states.Add(
                InGameStateType.Live,
                new InGameLiveState(_inGameView, EngineReferences, this));
            _states.Add(
                InGameStateType.Replay,
                new InGameReplayState(_inGameView, _engine, this, _lifecycleToken));


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
            _inGameView.Initialize(EngineReferences.Board);
        }
        private async UniTask StartGameAsync()
        {
            await _inGameMode.StartGameAsync(_lifecycleToken);

            _inGameView.StartGame(
                PlayerTeam.Cho,
                _inGameMode.GetPlayerType(PlayerTeam.Cho));

            _currState = _states[InGameStateType.Live];
        }


        private void BindEvents()
        {
            _engine.BindEvents();
            var engine = _engine.References;
            _inGameView.BindEvents(engine);

            var events = engine.GameEvents;
            events.OnTurnCompleted += HandleTurnCompleted;
            events.OnUndoCompleted += HandleUndoCompleted;
        }
        private void UnBindEvents()
        {
            var engine = _engine.References;
            var events = engine.GameEvents;
            events.OnTurnCompleted -= HandleTurnCompleted;
            events.OnUndoCompleted -= HandleUndoCompleted;
            _inGameView.UnBindEvents(engine);
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

            ChangeState(InGameStateType.Live);

            _inGameView.OnGameEnded(
                result,
                _inGameMode.GetPlayerType(result.Winner) == PlayerType.Local,
                moveCount);

            _inGameView.OpenResultView();
        }
        private void HandleUndoCompleted(UndoData data)
        {
            var currentTeam = data.CurrentTurn;
            var currentType = _inGameMode.GetPlayerType(currentTeam);

            _currState.HandleUndoCompleted(
                data,
                currentType);

            _inGameMode.BeginNextTurn(currentTeam);
        }
        private void HandleTurnCompleted(TurnData data)
        {
            var nextTeam = data.ActingTeam ==
                PlayerTeam.Cho ?
                    PlayerTeam.Han :
                    PlayerTeam.Cho;

            var nextType = _inGameMode.GetPlayerType(nextTeam);
            _currState.HandleTurnCompleted(data, nextType);

            if (data.GameResult is GameResultInfo result)
            {
                EndGameAsync(result, data.MoveCount).Forget();
                return;
            }

            _inGameMode.BeginNextTurn(nextTeam);
        }
        #endregion

        #region Input Handle

        public void SelectPiece(
            int? id,
            IReadOnlyList<Pos> legal,
            IReadOnlyList<Pos> illegal)
            => _currState.HandleSelectPiece(id, legal, illegal);
        public void HandleRequestMove(Pos from, Pos to)
            => _inGameMode.RequestMoveAsync(
                    from,
                    to,
                    _lifecycleToken
                ).Forget();
        public void HandleTakebackButton()
        {
            if (!IsLive) return;
            _inGameMode.TakeBackAsync(_lifecycleToken).Forget();
        }
        public void HandlePassTurnButton()
        {
            if (!IsLive) return;
            _inGameMode.PassTurnAsync(_lifecycleToken).Forget();
        }
        public void HandleGiveUpButton()
        {
            if (!IsLive) return;
            _inGameMode.GiveUpAsync(_lifecycleToken).Forget();
        }
        public void HandlePreviousButton()
            => _currState.HandlePreviousButton();
        public void HandleNextButton()
            => _currState.HandleNextButton();

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

            _inGameView.PrepareRematchView(
                EngineReferences.Board,
                PlayerTeam.Cho,
                _inGameMode.GetPlayerType(PlayerTeam.Cho));

            await _inGameMode.StartGameAsync(token);
        }
        #endregion
        public void ChangeState(InGameStateType type)
        {
            var nextState = _states[type];
            if (ReferenceEquals(_currState, nextState))
                return;

            _currState?.HandleExit(_inGameMode);
            _currState = nextState;
            _currState.HandleEnter(_inGameMode);
        }
    }
}


