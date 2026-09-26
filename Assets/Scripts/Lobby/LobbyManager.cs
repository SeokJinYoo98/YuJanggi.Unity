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
    using Lobby.Matching;

    using InGame.Session;
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
        private string _formationSubmissionMatchId;
        private string _formationFailure;
        private GameSessionInfo? _preparedGameSession;

        private void Update()
        {
            if (!_showNetworkTimer || _formationSubmissionMatchId != null)
                return;
            var matching = _networkManager.Matching;
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
                _networkManager.Matching.GameReadyReceived -= HandleGameReady;
        }
        private void OnEnable()
        {
            _networkManager = YuJanggiBootStrap.Instance.NetworkManager;

            _networkManager.OnNetworkChanged += HandleNetworkChanged;
            _networkManager.Matching.GameReadyReceived += HandleGameReady;

            HandleNetworkChanged();
        }

        private void OnDisable()
        {
            if (_networkManager == null)
                return;

            _networkManager.OnNetworkChanged -= HandleNetworkChanged;
            _networkManager.Matching.GameReadyReceived -= HandleGameReady;
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
            if (_networkManager.IsMatched)
            {
                if (!TryGetPreparedGameSession(out info))
                    return;
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

            EnterGameScene(info);
        }

        private void HandleGameReady(string matchId, Formation cho, Formation han)
        {
            var session = _networkManager.NetworkInfo;
            if (_isEnteringGame || !_networkManager.IsOnline || session.MatchId != matchId ||
                session.Team is not (PlayerTeam.Cho or PlayerTeam.Han))
                return;

            // 확정 포진은 게임 구성에만 저장하고 Matching 계층에는 남기지 않습니다.
            _formationSubmissionMatchId = matchId;
            _preparedGameSession = GameSessionFactory.CreateNetworkSession(session.Team, cho, han);
            if (isActiveAndEnabled)
                HandleCreateSession();
        }

        private bool TryGetPreparedGameSession(out GameSessionInfo info)
        {
            info = default;
            if (!_networkManager.IsOnline || !_networkManager.IsMatched || !_preparedGameSession.HasValue)
                return false;
            info = _preparedGameSession.Value;
            return true;
        }

        private void EnterGameScene(GameSessionInfo info)
        {
            if (_isEnteringGame)
                return;
            _isEnteringGame = true;
            _showNetworkTimer = false;
            _audioManager?.PlayButton();
            GameSessionStore.Current = info;
            _preparedGameSession = null;
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
            if (_formationSubmissionMatchId != _networkManager.MatchId)
            {
                _formationSubmissionMatchId = null;
                _formationFailure = null;
                _preparedGameSession = null;
            }
            if (TryGetPreparedGameSession(out var sessionInfo))
            {
                EnterGameScene(sessionInfo);
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
            _networkPanel.ChangeMessage(status, _networkManager.NetworkInfo.Team, timerSeconds);
            _lastNetworkTimerSeconds = timerSeconds ?? -1;

            var matching = _networkManager.Matching;
            if (status.ConnectionState == ConnectionState.Connected && matching.State == MatchingState.Matched)
            {
                if (_formationSubmissionMatchId != null || matching.IsFormationSubmitting ||
                    matching.IsFormationSubmitted)
                {
                    _networkPanel.ShowFormationProgress(_formationFailure ??
                        (matching.IsFormationSubmitted
                            ? "포진 접수 완료 · 서버 준비 대기 중"
                            : "포진 제출 중"));
                }
            }

            if (_timerState == MatchingState.Matched && timerSeconds == 0)
                SubmitSelectedFormationAsync().Forget();

        }

        private async UniTask SubmitSelectedFormationAsync()
        {
            var matching = _networkManager.Matching;
            string matchId = _networkManager.MatchId;
            if (_isEnteringGame || !_networkManager.IsOnline || string.IsNullOrWhiteSpace(matchId) ||
                matching.State != MatchingState.Matched || _formationSubmissionMatchId == matchId ||
                matching.IsFormationSubmitting || matching.IsFormationSubmitted)
                return;

            _formationSubmissionMatchId = matchId;
            _formationFailure = null;
            var formation = (Formation)_networkPanel.Selected;
            _networkPanel.ShowFormationProgress("포진 제출 중");
            try
            {
                bool accepted = await _networkManager.SubmitFormationAsync(formation);
                if (this == null || _isEnteringGame || _networkManager.MatchId != matchId)
                    return;
                if (!accepted)
                    _formationFailure = "포진 접수가 거절되었습니다.";
            }
            catch (OperationCanceledException)
            {
                if (this == null || _isEnteringGame || _networkManager.MatchId != matchId)
                    return;
                _formationFailure = "포진 제출이 취소되었습니다.";
            }
            catch (Exception exception)
            {
                if (this == null || _isEnteringGame || _networkManager.MatchId != matchId)
                    return;
                _formationFailure = "포진 제출에 실패했습니다.";
                Debug.LogException(exception);
            }
            // TODO:
            // 전송 실패 시 서버 접수 여부를 알 수 없어 현재 자동 재제출이나 씬 이동을 하지 않습니다.
            // 실패 문구를 유지하며, MatchSession에서 접수 상태 재조회와 재시도 UI 정책을 정해야 합니다.
            if (this != null && isActiveAndEnabled && !_isEnteringGame &&
                _networkManager.MatchId == matchId)
                HandleNetworkChanged();
        }

        private void UpdateNetworkTimerState(in NetworkStatus status)
        {
            bool showTimer = (status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Connected &&
                (status.MatchingState == MatchingState.Matching ||
                 status.MatchingState == MatchingState.Matched);
            string matchId = _networkManager.MatchId;

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

