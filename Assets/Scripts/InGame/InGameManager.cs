using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using BootStrap;


    using Runtime.Board;
    using Runtime.Input;
    using Runtime.Particle;
    using Runtime.UI;
    using Store;


    using Flow;
    using Handler;
    using Session;
    using Views;

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
            => JanggiOptionStore.Current.GameMode;

        #endregion

        #region Unity Lifecycle
        // Awake, OnEnable, Update, OnDestroy 등 Unity 생명주기 콜백
        private void Awake()
        {
            PrepareBootStrap();
            ShowJanggiOption();

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
                case GameModeType.Network:
                    _inGameFlow = InGameFlowFactory.CreateNetwork(_session, _inGameHandler);
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
        private void ShowJanggiOption()
        {
            var janggiOptions = JanggiOptionStore.Current;
   
            if (janggiOptions.GameMode == GameModeType.Network)
            {
                var networkInfo = NetworkSessionStore.Current;
                Debug.Log($"MatchID: {networkInfo.MatchId}");
            }
               
            Debug.Log(
                $"GameMode: {janggiOptions.GameMode}, " +
                $"Cho: {janggiOptions.PlayerCho}, " +
                $"ChoFormation: {janggiOptions.ChoFormation}, " +
                $"Han: {janggiOptions.PlayerHan}, " +
                $"HanFormation: {janggiOptions.HanFormation}, " +
                $"TurnTime: {janggiOptions.TurnTime}");
        }
        private void CreateInGameSession()
        {
            var janggiOptions = JanggiOptionStore.Current;

            var janggiEngine = JanggiEngineFactory.CreateEngine(janggiOptions);

            var matchView = CreateLiveView();
            var matchModel = CreateMatchModel(janggiOptions.TurnTime, out var record);
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

        private LiveView CreateLiveView()
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


