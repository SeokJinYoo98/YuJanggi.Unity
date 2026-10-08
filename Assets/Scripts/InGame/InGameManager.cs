using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using BootStrap;
    using Audio;

    using Views.Particle;
    using Views.Board;

    using Controller.Input;

    using UI;
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
        [SerializeField] private MoveGuideManager _moveGuideView;
        [SerializeField] private ParticleManager  _particleView;

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
        private CancellationTokenSource _flowCancellation;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        public GameModeType GameMode
            => JanggiOptionStore.JanggiSetting.GameMode;

        #endregion

        #region Unity Lifecycle
        // Awake, OnEnable, Update, OnDestroy 등 Unity 생명주기 콜백
        private void Awake()
        {
            PrepareBootStrap();

            CreateInGameSession();
            CreateInGameFlow();

            InitInGameSession();

            SetCamera();
        }
        private void OnEnable()
        {
            _session?.BindEvents(_inGameFlow);
            _flowCancellation = new CancellationTokenSource();
            _inGameFlow?.EnterAsync(_flowCancellation.Token).Forget();
        }
        private void Update()
        {
            _session?.Tick(Time.deltaTime);
        }
        private void OnDisable()
        {
            _flowCancellation?.Cancel();
            _inGameFlow?.Exit();
            _session?.UnBindEvents(_inGameFlow);
            _flowCancellation?.Dispose();
            _flowCancellation = null;
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
                    _inGameFlow = InGameFlowFactory.CreateNetwork(
                        _session,
                        _inGameHandler,
                        OnlineMatchInfo.MyTeam,
                        YuJanggiBootStrap.Instance.NetworkManager);
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

        private void CreateInGameSession()
        {
            var options =
                JanggiOptionStore.JanggiSetting;

            var engine =
                JanggiEngineFactory.CreateEngine(
                    options);

            var liveView =
                InGameViewFactory.CreateLiveView(
                    _resultUI,
                    _matchUI,
                    _displayModeText);

            var replayPlayback = new ReplayPlayback(
                _boardView, engine.Record, _runner, liveView);

            _session = GameSessionFactory.CreateSession(
                engine,
                _localInput,
                liveView,
                replayPlayback,
                _boardView);
        }

        private void InitInGameSession()
        {
            _session.InitGame();
        }

        private void SetCamera()
        {
            var option = JanggiOptionStore.JanggiSetting;

            if (option.GameMode == GameModeType.Local)
                return;

            var localTeam =
                option.PlayerCho is PlayerType.Local or PlayerType.Network
                    ? PlayerTeam.Cho
                    : PlayerTeam.Han;

            if (localTeam == PlayerTeam.Cho)
                return;

            _boardView.SetDeathPosition(new Vector3(4, 0, 11));
            _localInput.RotateCamera(PlayerTeam.Han);
        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        public void HandleStartGame()
            => _session.StartGame();
        public void HandleGiveUp()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.GiveUp();
        }
        public void HandleResetGame()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.ResetGame();
        }
        public void HandleHandicap()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.Handicap();
        }
        public void HandleUndo()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.UnDo();
        }
        public void HandleMainLobby()
        {
            _audioManager.PlayUI(UISfx.Button);
            if (GameMode == GameModeType.Network)
            {
                var networkManager = YuJanggiBootStrap.Instance.NetworkManager;
                //networkManager.ResetMatchState();
                //networkManager.Disconnect();
            }
            SceneManager.LoadScene("LobbyScene");
        }
        public void HandleReplayModeEnter()
        {
            _resultUI.Close();
            HandleReplayBackward();
        }
        public void HandleReplayForward()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.StepForward();
        }
        public void HandleReplayBackward()
        {
            _audioManager.PlayUI(UISfx.Button);
            _session.StepBackward();

        }
        #endregion
    }
}


