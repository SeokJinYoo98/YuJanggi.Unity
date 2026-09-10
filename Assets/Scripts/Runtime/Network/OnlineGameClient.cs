using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YuJanggiCommon;

namespace Yujanggi.Runtime.Network
{
    public interface IOnlineGameClient
    {
        event Action<ChatMessage> OnMessageReceived;
        event Action<string> OnErrorOccurred;
        event Action OnDisconnected;

        bool IsConnected { get; }
        bool IsConnecting { get; }
        OnlineMatchContext CurrentMatch { get; }

        UniTask<bool> ConnectAsync(CancellationToken cancellationToken = default);
        UniTask SendAsync(ChatMessage message, CancellationToken cancellationToken = default);
        OnlineMatchContext BeginMatch(MatchFoundResponse match);
        void ApplyFormationSelected(FormationSelectedResponse response);
        void ApplyGameStart(GameStartEvent gameStart);
        void ClearMatch();
        void Disconnect();
    }

    /// <summary>
    /// Owns the online connection independently from a scene and relays transport events.
    /// Lobby and match controllers can subscribe only while their scene is active.
    /// </summary>
    public sealed class OnlineGameClient : MonoBehaviour, IOnlineGameClient
    {
        private TcpGameClientBehaviour _transport;
        private CancellationTokenSource _connectionCancellation;
        private bool _isConnecting;
        private bool _isInitialized;

        public static OnlineGameClient Instance { get; private set; }

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<string> OnErrorOccurred;
        public event Action OnDisconnected;

        public bool IsConnected => _transport != null && _transport.IsConnected;
        public bool IsConnecting => _isConnecting;
        public OnlineMatchContext CurrentMatch { get; private set; }

        public static OnlineGameClient Create(TcpGameClientBehaviour transport)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));

            if (Instance != null)
            {
                if (transport.gameObject == Instance.gameObject)
                {
                    Instance.Initialize(transport);
                    return Instance;
                }

                Destroy(transport.gameObject);
                return Instance;
            }

            OnlineGameClient client = transport.GetComponent<OnlineGameClient>();
            if (client == null)
                client = transport.gameObject.AddComponent<OnlineGameClient>();

            client.Initialize(transport);
            return client;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_isInitialized)
                UnbindTransportEvents();

            _connectionCancellation?.Cancel();
            _connectionCancellation?.Dispose();
            _connectionCancellation = null;
            if (_transport != null)
                _transport.Disconnect();

            if (Instance == this)
                Instance = null;
        }

        public async UniTask<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            EnsureInitialized();

            if (IsConnected)
                return true;
            if (_isConnecting)
                return false;

            _isConnecting = true;
            var connectionCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _connectionCancellation = connectionCancellation;

            try
            {
                await _transport.ConnectAsync(connectionCancellation.Token);
                return IsConnected;
            }
            finally
            {
                if (ReferenceEquals(_connectionCancellation, connectionCancellation))
                    _connectionCancellation = null;

                connectionCancellation.Dispose();
                _isConnecting = false;
            }
        }

        public UniTask SendAsync(
            ChatMessage message,
            CancellationToken cancellationToken = default)
        {
            EnsureInitialized();
            return _transport.SendAsync(message, cancellationToken);
        }

        public OnlineMatchContext BeginMatch(MatchFoundResponse match)
        {
            CurrentMatch = new OnlineMatchContext(match);
            return CurrentMatch;
        }

        public void ApplyFormationSelected(FormationSelectedResponse response)
        {
            EnsureCurrentMatch();
            CurrentMatch.ApplyFormationSelected(response);
        }

        public void ApplyGameStart(GameStartEvent gameStart)
        {
            EnsureCurrentMatch();
            CurrentMatch.ApplyGameStart(gameStart);
        }

        public void ClearMatch()
            => CurrentMatch = null;

        public void Disconnect()
        {
            _connectionCancellation?.Cancel();
            _transport?.Disconnect();
            ClearMatch();
        }

        private void Initialize(TcpGameClientBehaviour transport)
        {
            if (_isInitialized)
                return;

            _transport = transport;
            BindTransportEvents();
            _isInitialized = true;
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized || _transport == null)
                throw new InvalidOperationException("온라인 게임 클라이언트가 초기화되지 않았습니다.");
        }

        private void EnsureCurrentMatch()
        {
            if (CurrentMatch == null)
                throw new InvalidOperationException("진행 중인 온라인 매칭 정보가 없습니다.");
        }

        private void BindTransportEvents()
        {
            _transport.OnMessageReceived += HandleMessageReceived;
            _transport.OnErrorOccurred += HandleErrorOccurred;
            _transport.OnDisconnected += HandleDisconnected;
        }

        private void UnbindTransportEvents()
        {
            if (_transport == null)
                return;

            _transport.OnMessageReceived -= HandleMessageReceived;
            _transport.OnErrorOccurred -= HandleErrorOccurred;
            _transport.OnDisconnected -= HandleDisconnected;
        }

        private void HandleMessageReceived(ChatMessage message)
            => OnMessageReceived?.Invoke(message);

        private void HandleErrorOccurred(string message)
            => OnErrorOccurred?.Invoke(message);

        private void HandleDisconnected()
        {
            ClearMatch();
            OnDisconnected?.Invoke();
        }
    }
}
