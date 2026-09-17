using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using Yujanggi.Core.Domain;
using Yujanggi.Runtime.UI;
using YuJanggiCommon;

namespace Yujanggi.Runtime.Game
{
    using Audio;
    using GameSession;
    using UnityEngine.SceneManagement;
    using Yujanggi.Runtime.Network;


    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private NetworkPanelView _networkPanel;
        [SerializeField] private AIPanelView _aiPanel;
        [SerializeField] private LocalPanelView _localPanel;

        [FormerlySerializedAs("_tcpClient")]

        [SerializeField]
        private string _onlinePlayerName = "Player";


        [SerializeField]
        private TcpGameClientBehaviour _tcpClientPrefab;
        public event Action<GameStartEvent> OnOnlineGameStarted;

        private UIVisible _curr;
        private AudioManager _audio;
        private OnlineMatchService _onlineMatchService;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            // 같은 빌드를 여러 개 실행해도 기본 이름이 중복되지 않도록 합니다.


            InitializeOnlineService();

        }
        private void Start()
        {
            _audio = AudioManager.Instance;
        }

        private void OnDestroy()
        {
            _onlineMatchService?.Dispose();
        }
        private void OnEnable()
        {
            RegisterOnlineEvents();
        }
        private void OnDisable()
        {
            UnregisterOnlineEvents();
        }

        public void HandleClosePanel()
        {
            _audio.PlayButton();
            if (_curr == null) return;
            _curr.Hide();
            _curr = null;
        }
        public void HandleAIPanel()
        {
            _audio.PlayButton();
            if (_curr != null) return;
            _curr = _aiPanel;
            _curr.Show();
        }
        public void HandleLocalPanel()
        {
            _audio.PlayButton();
            if (_curr != null) return;
            _curr = _localPanel;
            _curr.Show();
        }
        public void HandleCreateSession()
        {
            _audio.PlayButton();
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
            _audio.PlayButton();
            Application.Quit();
        }






        private void HandleServerMessage(ChatMessage message)
        {
            //try
            //{
            //    switch (message.Type)
            //    {
            //        case MessageType.Join:
            //            if (!_isJoinPending)
            //            {
            //                return;
            //            }

            //            _isJoinPending = false;
            //            _isJoined = true;
            //            StartMatchmakingAsync().Forget();
            //            break;

            //        case MessageType.MatchmakingStatus:
            //            MatchmakingStatusResponse status = message.GetPayload<MatchmakingStatusResponse>();
            //            _isMatchmakingPending = status.State == MatchmakingState.Waiting;
            //            if (status.State == MatchmakingState.Cancelled)
            //                _onlineClient.ClearMatch();
            //            if (_isMatchmakingPending)
            //                Debug.Log("매칭 상대를 기다리고 있습니다.");
            //            break;

            //        case MessageType.MatchFound:
            //            MatchFoundResponse match = message.GetPayload<MatchFoundResponse>();
            //            _isMatchmakingPending = false;
            //            _isMatched = true;
            //            _onlineClient.BeginMatch(match);
            //            Debug.Log(string.IsNullOrEmpty(match.Message)
            //                ? $"{match.Opponent.PlayerName} 님과 매칭되었습니다."
            //                : match.Message);
            //            break;

            //        case MessageType.FormationSelected:
            //            FormationSelectedResponse formation =
            //                message.GetPayload<FormationSelectedResponse>();
            //            _onlineClient.ApplyFormationSelected(formation);
            //            break;

            //        case MessageType.GameStart:
            //            GameStartEvent gameStart = message.GetPayload<GameStartEvent>();
            //            _isMatchmakingPending = false;
            //            _onlineClient.ApplyGameStart(gameStart);
            //            GameSessionStore.Current = GameSessionFactory.CreateNetworkSession(gameStart.Side);
            //            OnOnlineGameStarted?.Invoke(gameStart);
            //            break;

            //        case MessageType.Error:
            //            ErrorResponse error = message.GetPayload<ErrorResponse>();
            //            ResetConnectionState();
            //            Debug.LogWarning($"서버 오류 ({error.Code}): {error.Message}");
            //            break;
            //    }
            //}
            //catch (Exception exception)
            //{
            //    ResetConnectionState();
            //    Debug.LogWarning($"서버 메시지를 처리하지 못했습니다: {exception.Message}");
            //}
        }



        #region Refactoring : OnlineMatchService
        public void HandleCloseNetworkPanel()
        {
            _audio.PlayButton();
            _onlineMatchService.DisconnectAsync().Forget();

        }
        public void HandleConnectServer()
        {
            _audio.PlayButton();
            ChangePanel(_networkPanel);
            _onlineMatchService.StartOnlineSessionAsync().Forget();
        }
        public void HandleStartMatchMaking()
        {
            _audio.PlayButton();
            _onlineMatchService.StartMatchMakingAsync().Forget();
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
    }

}
