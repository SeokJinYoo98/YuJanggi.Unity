using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using YuJanggi.Network.Status;
namespace YuJanggi.Lobby
{
    using BootStrap;
    using Engine.Domain;
    using Data.AI;

    using Lobby.Matching;

    using Runtime.UI;
    using YuJanggi.Store;

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

        private string _formationFailure;
        private bool _formationAttempted;


        private void Update()
        {
            if (!_showNetworkTimer || _formationAttempted)
                return;
            var matching = _networkManager.Lobby;
            if (matching.IsFormationSubmitting || matching.IsFormationSubmitted)
                return;

            int seconds = GetNetworkTimerSeconds();
            if (seconds == _lastNetworkTimerSeconds)
                return;

            _lastNetworkTimerSeconds = seconds;
            _networkPanel.UpdateTimer(_timerState, seconds);
            if (_timerState == MatchingState.Matched && seconds == 0)
                SubmitSelectedFormationAsync().Forget();
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
            if (_networkManager != null)
                _networkManager.Lobby.GameReadyReceived -= HandleGameReady;
        }
        private void OnEnable()
        {
            _networkManager = YuJanggiBootStrap.Instance.NetworkManager;

            _networkManager.OnNetworkChanged += HandleNetworkChanged;
            _networkManager.Lobby.GameReadyReceived += HandleGameReady;

            HandleNetworkChanged();
        }

        private void OnDisable()
        {
            if (_networkManager == null)
                return;

            _networkManager.OnNetworkChanged -= HandleNetworkChanged;
            _networkManager.Lobby.GameReadyReceived -= HandleGameReady;
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
        public void HandleGameStart()
        {
            if (_isEnteringGame || !TryPrepareOptions())
                return;

            EnterGameScene();
        }

        private bool TryPrepareOptions()
        {
            if (_curr is NetworkPanelView)
            {
                if (!CanPrepareNetworkOptions() ||
                    !_networkManager.Lobby.TryGetReadyFormations(out var cho, out var han))
                    return false;

                JanggiOptionStore.SetNetworkOptions(NetworkMatchInfoStore.Current.Team, cho, han);
                return true;
            }
            // 진행 중인 매칭이 있으면 Local/AI 게임을 시작하지 않습니다.
            if (_networkManager.Lobby.State != MatchingState.Idle)
                return false;
            if (_curr is LocalPanelView local)
            {
                JanggiOptionStore.SetLocalOptions(local);
                return true;
            }
            if (_curr is AIPanelView ai)
            {
                JanggiOptionStore.SetAIOptions(ai);
                AISessionSettings.Strategy = ai.Strategy;
                return true;
            }
            return false;
        }

     



        private bool CanPrepareNetworkOptions()
        {
            var match = NetworkMatchInfoStore.Current;
            return _networkManager.IsOnline && _networkManager.Lobby.IsGameReady &&
                match != null && !string.IsNullOrWhiteSpace(match.MatchId) &&
                match.Team is PlayerTeam.Cho or PlayerTeam.Han;
        }

        private void HandleGameReady(string matchId, Formation cho, Formation han)
        {
            var match = NetworkMatchInfoStore.Current;
            if (_isEnteringGame || match == null || match.MatchId != matchId)
                return;

            if (isActiveAndEnabled)
            {
                ChangePanel(_networkPanel);
                HandleGameStart();
            }
        }

        private void EnterGameScene()
        {
            if (_isEnteringGame)
                return;
            _isEnteringGame = true;
            _showNetworkTimer = false;
            _audioManager?.PlayButton();
            _curr = null;
            SceneManager.LoadScene("JanggiScene");
        }

        public void HandleQuitGame()
        {
            _audioManager.PlayButton();
            Application.Quit();
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



        public void HandleClosePanel()
        {
            _audioManager.PlayButton();
            if (_curr == null) return;
            _curr.Hide();
            _curr = null;
        }

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
            if (_timerMatchId != NetworkMatchInfoStore.Current?.MatchId ||
                status.MatchingState != MatchingState.Matched)
            {
                _formationFailure = null;
                _formationAttempted = false;
            }
            if (CanPrepareNetworkOptions())
            {
                ChangePanel(_networkPanel);
                HandleGameStart();
                return;
            }
            UpdateNetworkTimerState(status);

            // 실패 사유는 패널에 남기고, 정상 연결 해제일 때만 홈으로 돌아갑니다.
            if ((status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Disconnected &&
                _curr == _networkPanel)
            {
                ShowHomeUI();
            }
            int? timerSeconds = _showNetworkTimer ? GetNetworkTimerSeconds() : (int?)null;
            _networkPanel.ChangeMessage(status, NetworkMatchInfoStore.Current?.Team ?? PlayerTeam.None, timerSeconds);
            _lastNetworkTimerSeconds = timerSeconds ?? -1;

            var matching = _networkManager.Lobby;
            if (status.ConnectionState == ConnectionState.Connected && matching.State == MatchingState.Matched)
            {
                if (_formationAttempted || matching.IsFormationSubmitting ||
                    matching.IsFormationSubmitted)
                {
                    _networkPanel.ShowFormationProgress(_formationFailure ??
                        (matching.IsFormationSubmitted
                            ? "포진 전송 완료 · 서버 준비 대기 중"
                            : "포진 제출 중"));
                }
            }

            if (_timerState == MatchingState.Matched && timerSeconds == 0)
                SubmitSelectedFormationAsync().Forget();

        }

        private async UniTask SubmitSelectedFormationAsync()
        {
            var matching = _networkManager.Lobby;
            string matchId = NetworkMatchInfoStore.Current?.MatchId;
            if (_isEnteringGame || !_networkManager.IsOnline || string.IsNullOrWhiteSpace(matchId) ||
                matching.State != MatchingState.Matched || _formationAttempted ||
                matching.IsFormationSubmitting || matching.IsFormationSubmitted)
                return;

            // 전송 완료 알림이 await 복귀보다 먼저 와도 자동 재전송하지 않습니다.
            _formationAttempted = true;
            _formationFailure = null;
            var formation = (Formation)_networkPanel.Selected;
            _networkPanel.ShowFormationProgress("포진 제출 중");
            try
            {
                await _networkManager.SubmitFormationAsync(formation);
                if (this == null || _isEnteringGame || NetworkMatchInfoStore.Current?.MatchId != matchId)
                    return;
            }
            catch (OperationCanceledException)
            {
                if (this == null || _isEnteringGame || NetworkMatchInfoStore.Current?.MatchId != matchId)
                    return;
                _formationFailure = "포진 제출이 취소되었습니다.";
            }
            catch (Exception exception)
            {
                if (this == null || _isEnteringGame || NetworkMatchInfoStore.Current?.MatchId != matchId)
                    return;
                _formationFailure = "포진 제출에 실패했습니다.";
                Debug.LogException(exception);
            }
            // TODO:
            // 전송 실패 시 서버 접수 여부를 알 수 없어 현재 자동 재제출이나 씬 이동을 하지 않습니다.
            // 실패 문구를 유지하며, MatchSession에서 접수 상태 재조회와 재시도 UI 정책을 정해야 합니다.
            if (this != null && isActiveAndEnabled && !_isEnteringGame &&
                NetworkMatchInfoStore.Current?.MatchId == matchId)
                HandleNetworkChanged();
        }

        private void UpdateNetworkTimerState(in NetworkStatus status)
        {
            bool showTimer = (status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Connected &&
                (status.MatchingState == MatchingState.Matching ||
                 status.MatchingState == MatchingState.Matched);
            string matchId = NetworkMatchInfoStore.Current?.MatchId;

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

    }

}



