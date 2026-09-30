#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Engine.Domain;
    using InGame.Handler;
    using Lobby.Matching;
    using Network;
    using Network.Status;
    using Store;
    using YuJanggi.Lobby.Network;
    using YuJanggi.Protocol.Messages;


    /// <summary>
    /// Unity 네트워크 요청의 수명과 UI에 전달할 상태를 관리합니다.
    /// Unity쪽 네트워크 진입점
    /// 상태를 조립해서 이벤트로 전달
    /// </summary>
    public sealed class NetworkManager : MonoBehaviour
    {
     
        private LobbyNetworkHandler? _lobbyNetworkHandler;
        internal ILobbyNetwork Lobby
            => _lobbyNetworkHandler
               ?? throw new InvalidOperationException(
                   "NetworkManager가 초기화되지 않았습니다.");
        internal void HandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
            {
                _requests?.HandleMessage(message);
                return;
            }
            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                case ServerMessageType.GameReadyEvent:
                    _lobbyNetworkHandler?.HandleMessage(message);
                    break;
                case ServerMessageType.GameStartEvent:
                    _inGameHandler?.HandleMessage(message);
                    break;
            }
        }











        #region Fields
        private CancellationTokenSource? _lifetimeCts;
        private CancellationTokenSource? _connectingRequestCts;
        private CancellationTokenSource? _matchingRequestCts;

        private NetworkConnection? _connection;
        private RequestDispatcher? _requests;

       
        private InGameHandler? _inGameHandler;

        #endregion
        #region Properties
        public MatchInfo? NetworkInfo
            => NetworkMatchInfoStore.Current;
        public string? MatchId
        {
            get
            {
                var matchId = NetworkInfo?.MatchId;
                return string.IsNullOrEmpty(matchId) ? null : matchId;
            }
        }

        public InGameHandler InGame
            => _inGameHandler ?? throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");

        public bool IsMatched
            => Status.MatchingState == MatchingState.Matched;
        public bool IsOnline
            => _connection?.IsOnline ?? false;
        public NetworkStatus Status { get; private set; }
            = new NetworkStatus(
                ConnectionState.Disconnected,
                null, null);
        #endregion
        public void Initialize(string host, int port)
        {
            if (_connection is not null)
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");

            NetworkMatchInfoStore.Current = default;
            _connection = new NetworkConnection(host, port);
            _requests = new RequestDispatcher(_connection);



            _lobbyNetworkHandler = new LobbyNetworkHandler(_connection, _requests, () => MatchId);
            _inGameHandler = new InGameHandler(_connection, _requests);
            _lifetimeCts = new CancellationTokenSource();

            _connection.MessageReceived += HandleMessage;

            _connection.OnDataChanged += HandleClientDataChanged;
            _lobbyNetworkHandler.MatchFound += HandleMatchFound;
            _lobbyNetworkHandler.OnDataChanged += HandleClientDataChanged;
        }
        #region Events
        public event Action? OnNetworkChanged;
        #endregion

        #region Unity Lifecycle
        private void OnDestroy()
        {
            if (_connection is not null)
            {
                _connection.MessageReceived -= HandleMessage;
                _connection.OnDataChanged -= HandleClientDataChanged;

            }
                
            if (_lobbyNetworkHandler is not null)
            {
                _lobbyNetworkHandler.OnDataChanged -= HandleClientDataChanged;
                _lobbyNetworkHandler.MatchFound -= HandleMatchFound;
            }
            NetworkMatchInfoStore.Current = default;

            // 연결 종료가 Service 초기화와 pending 취소를 먼저 수행합니다.
            _connection?.Dispose();
            _lifetimeCts?.Cancel();
            _lobbyNetworkHandler?.Dispose();
            _inGameHandler?.Dispose();
            _requests?.Dispose();
            _connection = null;
            _requests = null;
            _lobbyNetworkHandler = null;
            _inGameHandler = null;

            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }
        #endregion
        #region Public Methods
        // Init

        // Connection
        public async UniTask ConnectAsync()
        {
            if (_connection is null || _lifetimeCts is null)
                throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");

            if (_connectingRequestCts is not null || IsOnline)
                throw new InvalidOperationException(
                    "이미 연결 중이거나 서버에 연결되어 있습니다.");

            using var connectCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    _lifetimeCts.Token);

            _connectingRequestCts = connectCts;

            try
            {
                await _connection.ConnectAsync(
                    connectCts.Token);
            }
            finally
            {
                if (_connectingRequestCts == connectCts)
                    _connectingRequestCts = null;
            }
        }
        public void Disconnect()
        {
            // 이전 연결을 무효화하고 상태를 초기화한 뒤 요청 취소를 완료합니다.
            _connection?.Disconnect();
            _matchingRequestCts?.Cancel();
            _connectingRequestCts?.Cancel();
        }

        // Matching: Unity 요청 수명만 관리하고 메시지 해석은 Handler에 위임합니다.
        public UniTask StartMatchMakingAsync()
        {
            return RunMatchingRequestAsync((handler, token) => handler.MatchingStartRequestAsync(token));
        }
        public UniTask CancelMatchMakingAsync()
        {
            return RunMatchingRequestAsync((handler, token) => handler.MatchingCancelRequestAsync(token));
        }

        /// <summary>선택한 포진을 전송합니다. 게임 준비는 서버 이벤트로 확정됩니다.</summary>
        public UniTask SubmitFormationAsync(Formation formation)
        {
            return RunMatchingRequestAsync((handler, token) => handler.SubmitFormationAsync(formation, token));
        }

        #endregion
        #region Event Handlers
        private void HandleMatchFound(MatchInfo match)
        {
            NetworkMatchInfoStore.Current = match;
        }

        private void HandleClientDataChanged()
        {
            if (_connection is null || _lobbyNetworkHandler is null)
                return;

            if (_connection.State == ConnectionState.Disconnected)
                NetworkMatchInfoStore.Current = default;
            Status = new NetworkStatus(
                _connection.State,
                _connection.Error,
                _connection.Failure?.Message,
                _lobbyNetworkHandler.State);

            OnNetworkChanged?.Invoke();
        }

        #endregion
        #region Private Methods
        private async UniTask RunMatchingRequestAsync(
            Func<ILobbyNetwork, CancellationToken, UniTask> sendRequest)
        {
            if (_connection is null || _lifetimeCts is null)
                throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");
            if (_matchingRequestCts is not null)
                throw new InvalidOperationException("이미 매칭 신청 또는 취소 요청을 처리 중입니다.");

            using var matchingCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
            _matchingRequestCts = matchingCts;
            try
            {
                matchingCts.Token.ThrowIfCancellationRequested();
                await sendRequest(Lobby, matchingCts.Token);
            }
            finally
            {
                if (_matchingRequestCts == matchingCts)
                    _matchingRequestCts = null;
            }
        }

        #endregion
    }
}


