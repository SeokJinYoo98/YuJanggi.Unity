using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using Core.InGame;
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Input;
    using Mode;
    using State;
    using Store;
    using Views;
    using YuJanggi.InGame.Views.UI;

    public class InGameManager : MonoBehaviour, IGameCommandReceiver, IStateMachine
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;

        [Header("View")]
        [SerializeField] private InGameView  _inGameView;

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
                new InGameLiveState(
                    _inGameView,
                    EngineReferences,
                    this));

            _states.Add(
                InGameStateType.Replay,
                new InGameReplayState(
                    _inGameView,
                    _engine,
                    this,
                    _lifecycleToken));

            _currState = _states[InGameStateType.Live];

            _inGameMode = GameModeFactory.Create(
                JanggiOptionStore.JanggiSetting.GameMode,
                _engine,
                new InputHandlerFactory(_inputs, transform),
                this,
                JanggiOptionStore.JanggiSetting.GameMode == GameModeType.Network
                    ? BootStrap.YuJanggiBootStrap.Instance.NetworkManager.InGame
                    : null);
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
            var token = _lifecycleToken;
            _inGameMode.Initialize(token);

            _inGameView.Initialize(
                EngineReferences.Board,
                _inGameMode.ModeType == GameModeType.Network);

            await _inGameMode.StartGameAsync(_lifecycleToken);

            _inGameView.StartGame(
                PlayerTeam.Cho,
                _inGameMode.GetPlayerType(PlayerTeam.Cho));
        }

        private void BindEvents()
        {
            UnBindEvents();
            _engine.BindEvents();

            var engine = _engine.References;
            _inGameView.BindEvents(engine);

            _inGameMode.OnTurnCompleted += HandleTurnCompleted;
            _inGameMode.OnUndoCompleted += HandleUndoCompleted;
            _inGameMode.OnGameEnded     += HandleGameEnded;
            _inGameMode.BindEvents();
        }
        private void UnBindEvents()
        {
            _inGameMode.UnBindEvents();
            _inGameMode.OnTurnCompleted -= HandleTurnCompleted;
            _inGameMode.OnUndoCompleted -= HandleUndoCompleted;
            _inGameMode.OnGameEnded     -= HandleGameEnded;

            var engine = _engine.References;
            _inGameView.UnBindEvents(engine);

            _engine.UnBindEvents();
        }

        #region Engine Handle
        private void HandleGameEnded(
            GameResultInfo result,
            bool isLocalWin,
            int moveCount)
        {
            ChangeState(InGameStateType.Live);

            _inGameView.OnGameEnded(
                result,
                isLocalWin,
                moveCount);

            _inGameView.OpenResultView();
        }
        private void HandleUndoCompleted(
            UndoData data,
            PlayerType currentType)
        {
            _currState.HandleUndoCompleted(
                data,
                currentType);
        }
        private void HandleTurnCompleted(
            TurnData data,
            PlayerType nextType)
        {
            _currState.HandleTurnCompleted(
                data,
                nextType);
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
        public void HandleModeActionButton()
        {
            if (_inGameMode.ModeType == GameModeType.Network)
            {
                // 채팅창 열기
                return;
            }

            if (!IsLive)
                return;

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
            await _inGameMode.ClearAsync(token);
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

            _inGameMode.Initialize(token);

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


