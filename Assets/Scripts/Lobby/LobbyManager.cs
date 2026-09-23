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
    using Runtime.UI;

    using YuJanggiCommon;


    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private NetworkPanelView   _networkPanel;
        [SerializeField] private AIPanelView        _aiPanel;
        [SerializeField] private LocalPanelView     _localPanel;

        private UIVisible           _curr;
        private AudioManager        _audioManager;
        private NetworkManager      _networkManager;

        private void Awake()
        {

        }
        private void Start()
        {
            _audioManager = YuJanggiBootStrap.Instance.AudioManager;
        }

        private void OnDestroy()
        {
           
        }
        private void OnEnable()
        {
            _networkManager = YuJanggiBootStrap.Instance.NetworkManager;
            _networkManager.OnNetworkChanged += HandleNetworkChanged;
        }
        private void OnDisable()
        {
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




        private void Check(string str)
            => Debug.Log(str);
   
        
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
            if (_networkManager.IsOnline)
                return;

            ChangePanel(_networkPanel);
            _networkManager.
                ConnectAsync().
                Forget();
        }
        public void HandleMatchMakingButton()
        {
            _audioManager.PlayButton();

            var state =
                _networkManager.Status.ConnectionState;

            if (state == ConnectionState.Connected)
                _networkManager
                    .StartMatchMakingAsync()
                    .Forget();
            
            else if (state == ConnectionState.Matching)
                _networkManager
                    .CancelMatchMakingAsync()
                    .Forget();
       
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
        #endregion
    }

}

