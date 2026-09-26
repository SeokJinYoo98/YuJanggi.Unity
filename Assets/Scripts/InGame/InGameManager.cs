using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using BootStrap;
    using Core.V2.Board;
    using Core.V2.Domain;
    using Core.V2.Match;
    using Core.V2.Rule;
    using Cysharp.Threading.Tasks;
    using InGame.Session;
    using InGame.Views;
    using Runtime.Board;
    using Runtime.Input;
    using Runtime.Particle;
    using Runtime.UI;
    using System;
    using YuJanggi.InGame.Flow;
    using YuJanggi.InGame.Handler;

    public class InGameManager : MonoBehaviour
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수

        [Header("Views")]
        [SerializeField] private BoardView     _boardView;
        [SerializeField] private MoveGuideView _moveGuideView;
        [SerializeField] private ParticleView  _particleView;

        [Header("UIs")]
        [SerializeField] private ResultUI   _resultUI;
        [SerializeField] private MatchUI    _matchUI;
        [SerializeField] private TMP_Text   _displayModeText;


        [Header("Inputs")]
        [SerializeField] private InputHandlerBehaviour _localInput;
        [SerializeField] private CoroutineRunner       _runner;

        private GameSession     _session;
        private AudioManager    _audioManager;
        private InGameHandler   _inGameHandler;

        private IInGameFlow     _inGameFlow;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        public GameModeType GameMode
            => GameSessionStore.Current.Mode;

        #endregion

        #region Unity Lifecycle
        // Awake, OnEnable, Update, OnDestroy 등 Unity 생명주기 콜백
        private void Awake()
        {
            PrepareBootStrap();
            PrepareSessionInfo();

            CreateInGameSession();
            CreateInGameFlow();

            SetCamera();
        }
        private void OnEnable()
        {
            _session?.BindEvents();
        }
        private void Start()
        {
            _inGameFlow
             .EnterAsync(this.GetCancellationTokenOnDestroy()) // 
             .Forget();
        }
        private void Update()
        {
            _session?.Tick(Time.deltaTime);
        }
        private void OnDisable()
        {
            _session?.UnBindEvents();
        }
        private void OnDestroy()
        {
            _inGameFlow?.Exit();
        }

        #endregion

        #region Private Methods
        private void CreateInGameFlow()
        {
            switch (GameMode)
            {
                case GameModeType.Local:
                case GameModeType.AI:
                    _inGameFlow = InGameFlowFactory.CreateLocal(_session);
                    break;

            }
        }
        private void PrepareBootStrap()
        {
            _audioManager =
                YuJanggiBootStrap.Instance.AudioManager;

            _inGameHandler =
                YuJanggiBootStrap.Instance.NetworkManager.InGame;
        }
        private void PrepareSessionInfo()
        {
            var sessionInfo = GameSessionStore.Current;
   
            if (sessionInfo.Mode == GameModeType.Network)
            {
                var networkInfo = NetworkSessionStore.Current;
                Debug.Log($"MatchID: {networkInfo.MatchId}");
            }
               
            Debug.Log(
                $"GameMode: {sessionInfo.Mode}, " +
                $"Cho: {sessionInfo.Cho}, " +
                $"ChoFormation: {sessionInfo.ChoFormation}, " +
                $"Han: {sessionInfo.Han}, " +
                $"HanFormation: {sessionInfo.HanFormation}, " +
                $"TurnTime: {sessionInfo.TurnTime}");
        }
        private void CreateInGameSession()
        {
            var sessionInfo = GameSessionStore.Current;
            var matchView = CreateMatchView();
            var matchModel = CreateMatchModel(sessionInfo.TurnTime, out var record);
            var replayView = CreateReplayView(record);

            _session = GameSessionFactory.CreateSession(
                sessionInfo,
                matchView,
                matchModel,
                replayView,
                _localInput);

            _session.InitGame();
        }
        private ReplayView CreateReplayView(
            Record record)
            => new (_boardView, record, _runner, _audioManager, _displayModeText);
        private MatchModel CreateMatchModel(
            float turnTime,
            out Record record)
        {
            record         = new Record();
            var turn = new Turn(turnTime);
            var score = new Score();
            var boardModel = new BoardModel();
            var janggiRule = new JanggiRule();
            return new MatchModel(turn, record, score, boardModel, janggiRule);
        }
        private MatchView CreateMatchView()
            => new(
                _particleView, _moveGuideView, _boardView,
                _resultUI, _matchUI);
        private void SetCamera()
        {
            var sessionInfo = GameSessionStore.Current;
            if (sessionInfo.Mode == GameModeType.Local) return;
            if (sessionInfo.Cho  == PlayerType.Local) return;

            _boardView.SetDeathPosition(new Vector3(4, 0, 11));
            _localInput.RotateCamera(PlayerTeam.Han);
        }

        private async UniTask NotifyGameSceneReadyAsync()
        {
            await _inGameHandler.SendGameSceneReadyAsync();
        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        public void HandleStartGame()
            => _session.StartGame();
        public void HandleGiveUp()
        {
            _audioManager.PlayButton();
            _session.GiveUp();
        }
        public void HandleResetGame()
        {
            _audioManager.PlayButton();
            _session.ResetGame();
        }
        public void HandleHandicap()
        {
            _audioManager.PlayButton();
            _session.Handicap();
        }
        public void HandleUndo()
        {
            _audioManager.PlayButton();
            _session.UnDo();
        }
        public void HandleMainLobby()
        {
            _audioManager.PlayButton();
            _session.UnBindEvents();
            SceneManager.LoadScene("LobbyScene");
        }
        public void HandleReplayModeEnter()
        {
            _resultUI.Hide();
            HandleReplayBackward();
        }
        public void HandleReplayForward()
        {
            _audioManager.PlayButton();
            _session.StepForward();
        }
        public void HandleReplayBackward()
        {
            _audioManager.PlayButton();
            _session.StepBackward();

        }
        #endregion
    }
}
