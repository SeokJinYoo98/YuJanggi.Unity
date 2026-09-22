using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
namespace YuJanggi.Lobby
{
    using BootStrap;
    using Core.V2.Domain;
    using Data.AI;
    using Network.V2.Status;

    using Runtime.GameSession;
    using Runtime.Network;
    using Runtime.UI;

    using YuJanggiCommon;


    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private NetworkPanelView   _networkPanel;
        [SerializeField] private AIPanelView        _aiPanel;
        [SerializeField] private LocalPanelView     _localPanel;

        [FormerlySerializedAs("_tcpClient")]

        [SerializeField]
        private string _onlinePlayerName = "Player";


        [SerializeField]
        private TcpGameClientBehaviour _tcpClientPrefab;
        public event Action<GameStartEvent> OnOnlineGameStarted;

        private UIVisible           _curr;
        private AudioManager        _audioManager;
        private NetworkManager      _networkManager;
        private OnlineMatchService  _onlineMatchService;
        private void Awake()
        {


            // 같은 빌드를 여러 개 실행해도 기본 이름이 중복되지 않도록 합니다.


            InitializeOnlineService();

        }
        private void Start()
        {
            _audioManager = YuJanggiBootStrap.Instance.AudioManager;
        }

        private void OnDestroy()
        {
            _onlineMatchService?.Dispose();
        }
        private void OnEnable()
        {
            RegisterOnlineEvents();
            _networkManager = YuJanggiBootStrap.Instance.NetworkManager;
            _networkManager.OnNetworkChanged += HandleNetworkChanged;
        }
        private void OnDisable()
        {
            UnregisterOnlineEvents();
            if (_networkManager != null)
                _networkManager.OnNetworkChanged -= HandleNetworkChanged;
        }


        public void HandleAIPanel()
        {
            _audioManager.PlayButton();
            if (_curr != null) return;
            _curr = _aiPanel;
            _curr.Show();
        }
        public void HandleLocalPanel()
        {
            _audioManager.PlayButton();
            if (_curr != null) return;
            _curr = _localPanel;
            _curr.Show();
        }
        public void HandleCreateSession()
        {
            _audioManager.PlayButton();
            if (_curr == null) return;

            GameSessionInfo info;
            if (_curr is LocalPanelView local)
            {
                info = GameSessionFactory.CreateClientSession(
                    (Formation)local.ChoFormation,
                    (Formation)local.HanFormation,
                    local.TurnTime);
            }
            else if (_curr is AIPanelView ai)
            {
                info = GameSessionFactory.CreateAISession(
                    (PlayerTeam)ai.LocalPlayer,
                    (Formation)ai.LocalPlayerFormation,
                    ai.TurnTime);
                AISessionSettings.Strategy = ai.Strategy;
            }
            else
            {
                return;
            }

            GameSessionStore.Current = info;
            _curr = null;
            SceneManager.LoadScene("JanggiScene");
        }

        public void HandleQuitGame()
        {
            _audioManager.PlayButton();
            Application.Quit();
        }



        #region Refactoring : OnlineMatchService

        public void HandleConnectServer()
        {
            _audioManager.PlayButton();
            ChangePanel(_networkPanel);
            _onlineMatchService.StartOnlineSessionAsync().Forget();
        }

        private void InitializeOnlineService()
        {
            _onlinePlayerName = string.IsNullOrWhiteSpace(_onlinePlayerName) ||
                                 _onlinePlayerName.Trim() == "Player"
                ? $"Player-{Guid.NewGuid():N}".Substring(0, 15)
                : _onlinePlayerName.Trim();

            TcpGameClientBehaviour transport = Instantiate(_tcpClientPrefab);
            OnlineGameClient client = OnlineGameClient.Create(transport, _onlinePlayerName);

            _onlineMatchService = new OnlineMatchService(client);
        }
        private void RegisterOnlineEvents()
        {
            _onlineMatchService.OnStateChanged += HandleOnlineStateChanged;
            _onlineMatchService.StateCheck += Check;
        }
        private void UnregisterOnlineEvents()
        {
            _onlineMatchService.OnStateChanged -= HandleOnlineStateChanged;
            _onlineMatchService.StateCheck -= Check;
        }
        private void Check(string str)
            => Debug.Log(str);
        private void HandleOnlineStateChanged(OnlineMatchState state)
        {
            switch (state)
            {
                case OnlineMatchState.Local:
                    ShowHomeUI();
                    break;
                case OnlineMatchState.Connecting:
                    Debug.Log("Client: 서버에 연결중입니다.");
                    break;
                case OnlineMatchState.Connected:
                    Debug.Log("Client: 서버에 연결되었습니다.");
                    break;
                case OnlineMatchState.ConnectionFailed:
                    Debug.Log("Client: 서버 연결에 실패했습니다.");
                    break;
                case OnlineMatchState.MatchMaking:
                    Debug.Log("Client: 매칭을 찾는 중입니다.");
                    break;
                case OnlineMatchState.MatchFound:
                    Debug.Log("Client: 매칭이 완료되었습니다.");
                    break;
            }
            _networkPanel.ChangeMessage(state);
        }
        
        private void ShowHomeUI()
        {
            Debug.Log("Client: 홈 화면으로 돌아갑니다.");
            ChangePanel();
        }

        private void ChangePanel(UIVisible nextPanel=null)
        {

            if (nextPanel == null)
            {
                _curr?.Hide();
                _curr = null;
                return;
            }

            if (_curr == nextPanel)
                return;

            _curr?.Hide();
            _curr = nextPanel;
            _curr?.Show();
        }


        #endregion
        public void HandleClosePanel()
        {
            _audioManager.PlayButton();
            if (_curr == null) return;
            _curr.Hide();
            _curr = null;
        }
        #region Network_V2
        public void HandleNetworkButton()
        {
            _audioManager.PlayButton();
            ChangePanel(_networkPanel);
            ConnectNetworkAsync().Forget();
        }
        public void HandleStartMatchMaking()
        {
            _audioManager.PlayButton();
            _networkManager.StartMatchMakingAsync().Forget();
        }
        public void HandleCloseNetworkPanel()
        {
            _audioManager.PlayButton();
            _networkManager.Disconnect();
            HandleClosePanel();
        }



        private void HandleNetworkChanged()
        {
            NetworkStatus status = _networkManager.Status;

            // 실패 사유는 패널에 남기고, 정상 연결 해제일 때만 홈으로 돌아갑니다.
            if ((status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Disconnected &&
                _curr == _networkPanel)
            {
                ShowHomeUI();
            }
            _networkPanel.ChangeMessage(status);

        }


        private async UniTask ConnectNetworkAsync()
        {
            var network = YuJanggiBootStrap.Instance.NetworkManager;

            try
            {
                // 연결 중 버튼 재입력을 막아 중복 ConnectAsync 호출을 방지해야 합니다.
                // 현재는 로비 종료용 토큰과 제한 시간이 없으므로, 서버가 응답하지 않으면
                // 핸드셰이크 대기가 계속될 수 있습니다. 취소/타임아웃은 토큰으로 전달합니다.
                await network.ConnectAsync();
            }
            catch (OperationCanceledException)
            {
                // 정상적인 취소 흐름입니다. OnlineGameClient_V2에서 이미 연결을 정리하므로
                // 아래 Disconnect는 필수가 아니며 연결 해제 알림이 중복 발생할 수 있습니다.
                network.Disconnect();
            }
            catch (Exception e)
            {
                // 접속 실패(SocketException), 수신 종료/패킷 오류(IOException 계열),
                // 역직렬화 오류, 핸드셰이크 거절은 클라이언트에서 연결 정리 후 전달됩니다.
                // 사용자 안내는 OnNetworkChanged에서 Status.Error/Message를 보고 처리합니다.
                // 여기서 Disconnect를 호출하면 저장된 실패 정보가 초기화되므로 호출하지 않습니다.
                // 미초기화/중복 연결(InvalidOperationException), 폐기 후 호출(ObjectDisposedException)은
                // 상태 이벤트 없이 전달될 수 있습니다. 자동 재시도보다 호출 순서/생명주기를 수정해야 합니다.
                Debug.LogException(e);
            }
        }
        #endregion
    }

}

