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
    using Network.Status;
    using Matching;

    using Runtime.GameSession;
    using Runtime.UI;

    public class LobbyManager : MonoBehaviour
    {
        private const int GameStartCountdownSeconds = 10;

        [SerializeField] private NetworkPanelView   _networkPanel;
        [SerializeField] private AIPanelView        _aiPanel;
        [SerializeField] private LocalPanelView     _localPanel;

        private UIVisible           _curr;
        private AudioManager        _audioManager;
        private NetworkManager      _networkManager;

        private MatchingState _timerState = MatchingState.Idle;
        private bool _showNetworkTimer;
        private double _networkTimerStartedAt;
        private int _lastNetworkTimerSeconds = -1;
        private string _timerMatchId;
        private bool _isEnteringGame;

        private void Update()
        {
            if (!_showNetworkTimer)
                return;

            int seconds = GetNetworkTimerSeconds();
            if (seconds == _lastNetworkTimerSeconds)
                return;

            _lastNetworkTimerSeconds = seconds;
            _networkPanel.UpdateTimer(_timerState, seconds);
            if (_timerState == MatchingState.Matched && seconds == 0)
                HandleCreateSession();
        }

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
            HandleNetworkChanged();
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
            if (_isEnteringGame)
                return;

            GameSessionInfo info;
            if (_timerState == MatchingState.Matched)
            {
                NetworkStatus status = _networkManager.Status;
                if (!_showNetworkTimer || GetNetworkTimerSeconds() != 0 ||
                    status.NetworkState != NetworkState.Online ||
                    status.MatchingState != MatchingState.Matched ||
                    (status.Error ?? NetworkError.None) != NetworkError.None ||
                    status.CurrentMatch is null ||
                    string.IsNullOrWhiteSpace(status.CurrentMatch.MatchId) ||
                    status.CurrentMatch.MatchId != _timerMatchId ||
                    status.Team is not (PlayerTeam.Cho or PlayerTeam.Han))
                    return;

                // TODO:
                // 기존 로비는 카운트다운 종료 시 포진을 서버에 제출하지 않고 씬을 이동합니다.
                // 현재 로컬 세션은 생성되지만 서버 GameRoom 생성·준비 완료를 보장하지 않습니다.
                // GameSession / GameScene 준비 흐름에서 SubmitFormationAsync 호출과 서버 시작 이벤트 대기를
                // 연결하고, 포진 거절·연결 종료 시 씬 진입을 어떻게 취소할지 결정해야 합니다.
                info = GameSessionFactory.CreateNetworkSession(
                    status.Team, (Formation)_networkPanel.Selected);
            }
            else if (_curr is LocalPanelView local)
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

            _isEnteringGame = true;
            _showNetworkTimer = false;
            _audioManager?.PlayButton();
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
        #region Network
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
                _networkManager.Status.MatchingState;

            if (_networkManager.IsOnline && state == MatchingState.Idle)
                _networkManager
                    .StartMatchMakingAsync()
                    .Forget();
            
            else if (state == MatchingState.Matching)
                _networkManager
                    .CancelMatchMakingAsync()
                    .Forget();
       
        }
        public void HandleCloseNetworkPanel()
        {
            _audioManager.PlayButton();
            if (_networkManager.IsMatched)
            {
                Debug.Log("이미 매치가 성사되어 취소하지 못합니다.");
                return;
            }
            _networkManager.Disconnect();
            HandleClosePanel();
        }



        private void HandleNetworkChanged()
        {
            if (_isEnteringGame)
                return;

            NetworkStatus status = _networkManager.Status;
            UpdateNetworkTimerState(status);

            // 실패 사유는 패널에 남기고, 정상 연결 해제일 때만 홈으로 돌아갑니다.
            if ((status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Disconnected &&
                _curr == _networkPanel)
            {
                ShowHomeUI();
            }
            int? timerSeconds = _showNetworkTimer ? GetNetworkTimerSeconds() : (int?)null;
            _networkPanel.ChangeMessage(status, timerSeconds);
            _lastNetworkTimerSeconds = timerSeconds ?? -1;

            if (_timerState == MatchingState.Matched && timerSeconds == 0)
                HandleCreateSession();

        }

        private void UpdateNetworkTimerState(in NetworkStatus status)
        {
            bool showTimer = (status.Error ?? NetworkError.None) == NetworkError.None &&
                status.NetworkState == NetworkState.Online &&
                (status.MatchingState == MatchingState.Matching ||
                 status.MatchingState == MatchingState.Matched);
            string matchId = status.CurrentMatch?.MatchId;

            // 같은 상태/매칭의 재알림이나 패널 재표시는 카운트다운을 초기화하지 않습니다.
            if (showTimer && (!_showNetworkTimer || _timerState != status.MatchingState ||
                (status.MatchingState == MatchingState.Matched && _timerMatchId != matchId)))
            {
                _networkTimerStartedAt = Time.realtimeSinceStartupAsDouble;
            }

            _timerState = status.MatchingState;
            _timerMatchId = matchId;
            _showNetworkTimer = showTimer;
        }

        private int GetNetworkTimerSeconds()
        {
            int elapsedSeconds = (int)Math.Floor(
                Time.realtimeSinceStartupAsDouble - _networkTimerStartedAt);
            return _timerState == MatchingState.Matched
                ? Math.Max(0, GameStartCountdownSeconds - elapsedSeconds)
                : elapsedSeconds;
        }
        #endregion
    }

}

