using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.Serialization;
using YuJanggiCommon;
using UnityEngine;
using Yujanggi.Core.Domain;
using Yujanggi.Runtime.UI;

namespace Yujanggi.Runtime.Game
{
    using Audio;
    using GameSession;
    using UnityEngine.SceneManagement;
    using Yujanggi.Runtime.Network;
    using Yujanggi.Runtime.Network.Protocol;

    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private AIPanelView            _aiPanel;
        [SerializeField] private LocalPanelView         _localPanel;
        [FormerlySerializedAs("_tcpClient")]
        [SerializeField] private TcpGameClientBehaviour _tcpClientPrefab;
        [SerializeField] private string                 _onlinePlayerName = "Player";
        private TcpGameClientBehaviour _tcpClient;
        private bool _isTcpClientBound;
        private bool _isConnecting;
        private CancellationTokenSource _serverConnectionCancellation;
        private bool _isJoinPending;
        private bool _isMatchmakingPending;
        private bool _isJoined;
        private bool _isMatched;
        private string _sessionPlayerName;

        public event Action<GameStartEvent> OnOnlineGameStarted;

        UIVisible _curr;

        private AudioManager _audio;
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            // 같은 빌드를 여러 개 실행해도 기본 이름이 중복되지 않도록 합니다.
            _sessionPlayerName = string.IsNullOrWhiteSpace(_onlinePlayerName) ||
                                 _onlinePlayerName.Trim() == "Player"
                ? $"Player-{Guid.NewGuid():N}".Substring(0, 15)
                : _onlinePlayerName.Trim();
        }
        private void OnEnable()
        {
            BindTcpClientEvents();
        }
        private void OnDisable()
        {
            UnbindTcpClientEvents();
        }
        private void OnDestroy()
        {
            _serverConnectionCancellation?.Cancel();
        }
        private void Start()
        {
            _audio = AudioManager.Instance;
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
        public void HandleServerButton()
        {
            if (_serverConnectionCancellation != null ||
                (_tcpClient != null && _tcpClient.IsConnected))
            {
                HandleDisconnectServer();
            }
            else
            {
                HandleConnectServer();
            }
        }

        private async void HandleConnectServer()
        {
            _audio?.PlayButton();
            if (_isConnecting || _serverConnectionCancellation != null ||
                (_tcpClient != null && _tcpClient.IsConnected))
            {
                return;
            }

            if (!TryCreateTcpClient())
            {
                return;
            }
            Debug.Log("서버에 연결중입니다.");
            _isConnecting = true;

            var cancellation = new CancellationTokenSource();
            _serverConnectionCancellation = cancellation;
            try
            {
                await _tcpClient.ConnectAsync(cancellation.Token);
                cancellation.Token.ThrowIfCancellationRequested();
                if (_tcpClient != null && _tcpClient.IsConnected)
                {
                    Debug.Log("서버에 연결되었습니다.");
                    _isJoinPending = true;
                    await _tcpClient.SendAsync(
                        ServerMessageFactory.CreateJoin(_sessionPlayerName), cancellation.Token);
                }
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
                if (_tcpClient != null)
                    _tcpClient.Disconnect();
            }
            finally
            {
                _serverConnectionCancellation = null;
                cancellation.Dispose();
                _isConnecting = false;
            }
        }

        private void HandleDisconnectServer()
        {
            _audio?.PlayButton();
            CancelServerConnection();
        }

        private void CancelServerConnection()
        {
            _serverConnectionCancellation?.Cancel();
            if (_tcpClient != null)
                _tcpClient.Disconnect();

            ResetConnectionState();
            Debug.Log("서버 연결을 취소했습니다.");
        }

        public void HandleStartMatchmaking()
        {
            if (_isConnecting || _serverConnectionCancellation != null ||
                _isJoinPending || _isMatchmakingPending || _isMatched)
            {
                return;
            }

            if (_tcpClient != null && _tcpClient.IsConnected)
            {
                if (_isJoined)
                    StartMatchmakingAsync().Forget();
                return;
            }

            HandleConnectServer();
        }

        public async void HandleCancelMatchmaking()
        {
            _audio.PlayButton();
            if (_tcpClient == null || !_tcpClient.IsConnected || _isConnecting)
            {
                return;
            }

            if (!_isMatchmakingPending)
            {
                _tcpClient.Disconnect();
                return;
            }

            _isMatchmakingPending = false;
            await _tcpClient.SendAsync(ServerMessageFactory.CreateMatchmakingCancel());
        }
        public void HandleQuitGame()
        {
            _audio.PlayButton(); 
            Application.Quit();
        }
        private bool TryCreateTcpClient()
        {
            if (_tcpClient != null)
            {
                return true;
            }

            if (_tcpClientPrefab == null)
            {
                Debug.LogError("LobbyManager에 TcpGameClientBehaviour 프리팹을 연결하세요.");
                return false;
            }

            _tcpClient = Instantiate(_tcpClientPrefab);
            BindTcpClientEvents();
            return true;
        }

        private void BindTcpClientEvents()
        {
            if (_tcpClient == null || _isTcpClientBound)
            {
                return;
            }

            _tcpClient.OnMessageReceived += HandleServerMessage;
            _tcpClient.OnErrorOccurred += HandleConnectionError;
            _tcpClient.OnDisconnected += HandleDisconnected;
            _isTcpClientBound = true;
        }

        private void UnbindTcpClientEvents()
        {
            if (_tcpClient == null || !_isTcpClientBound)
            {
                return;
            }

            _tcpClient.OnMessageReceived -= HandleServerMessage;
            _tcpClient.OnErrorOccurred -= HandleConnectionError;
            _tcpClient.OnDisconnected -= HandleDisconnected;
            _isTcpClientBound = false;
        }

        private void HandleServerMessage(ChatMessage message)
        {
            try
            {
                switch (message.Type)
                {
                    case MessageType.Join:
                        if (!_isJoinPending)
                        {
                            return;
                        }

                        _isJoinPending = false;
                        _isJoined = true;
                        StartMatchmakingAsync().Forget();
                        break;

                    case MessageType.MatchmakingStatus:
                        MatchmakingStatusResponse status = message.GetPayload<MatchmakingStatusResponse>();
                        _isMatchmakingPending = status.State == MatchmakingState.Waiting;
                        if (_isMatchmakingPending)
                            Debug.Log("매칭 상대를 기다리고 있습니다.");
                        break;

                    case MessageType.MatchFound:
                        MatchFoundResponse match = message.GetPayload<MatchFoundResponse>();
                        _isMatchmakingPending = false;
                        _isMatched = true;
                        Debug.Log(string.IsNullOrEmpty(match.Message)
                            ? $"{match.Opponent.PlayerName} 님과 매칭되었습니다."
                            : match.Message);
                        break;

                    case MessageType.GameStart:
                        GameStartEvent gameStart = message.GetPayload<GameStartEvent>();
                        _isMatchmakingPending = false;
                        GameSessionStore.Current = GameSessionFactory.CreateNetworkSession(gameStart.Side);
                        DontDestroyOnLoad(_tcpClient.gameObject);
                        OnOnlineGameStarted?.Invoke(gameStart);
                        break;

                    case MessageType.Error:
                        ErrorResponse error = message.GetPayload<ErrorResponse>();
                        ResetConnectionState();
                        Debug.LogWarning($"서버 오류 ({error.Code}): {error.Message}");
                        break;
                }
            }
            catch (Exception exception)
            {
                ResetConnectionState();
                Debug.LogWarning($"서버 메시지를 처리하지 못했습니다: {exception.Message}");
            }
        }

        private async UniTask StartMatchmakingAsync()
        {
            if (_tcpClient == null || !_tcpClient.IsConnected)
            {
                return;
            }

            _isMatchmakingPending = true;
            await _tcpClient.SendAsync(ServerMessageFactory.CreateMatchmakingStart());
        }

        private void HandleConnectionError(string message)
        {
            ResetConnectionState();
            Debug.LogWarning($"TCP 연결 오류: {message}");
        }

        private void HandleDisconnected()
        {
            ResetConnectionState();
            Debug.Log("TCP 서버 연결이 종료되었습니다.");
        }

        private void ResetConnectionState()
        {
            _isConnecting = false;
            _isJoinPending = false;
            _isMatchmakingPending = false;
            _isJoined = false;
            _isMatched = false;
        }
    }

}
