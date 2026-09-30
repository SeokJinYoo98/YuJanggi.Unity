#nullable enable
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Engine.Domain;
    using InGame.Handler;
    using Lobby.Matching;
    using Network;
    using Network.Status;
    using Store;
    using System.Threading;
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

        private InGameHandler? _inGameHandler;

        internal ILobbyNetwork Lobby
            => _lobbyNetworkHandler
               ?? throw new InvalidOperationException(
                   "NetworkManager가 초기화되지 않았습니다.");
        internal InGameHandler InGame
            => _inGameHandler
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
                case ServerMessageType.MovePieceEvent:
                    _inGameHandler?.HandleMessage(message);
                    break;
            }
        }











        #region Fields
        private NetworkConnection? _connection;
        private RequestDispatcher? _requests;

       
        #endregion
        #region Properties
        public MatchInfo? NetworkInfo
            => _lobbyNetworkHandler?.Match;
        public string? MatchId
        {
            get
            {
                var matchId = NetworkInfo?.MatchId;
                return string.IsNullOrEmpty(matchId) ? null : matchId;
            }
        }

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



            _lobbyNetworkHandler = new LobbyNetworkHandler(_connection, _requests);
            _inGameHandler = new InGameHandler(_connection, _requests);

            _connection.MessageReceived += HandleMessage;

            _connection.OnDataChanged += HandleClientDataChanged;
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
            }
            NetworkMatchInfoStore.Current = default;

            // 연결 종료가 Service 초기화와 pending 취소를 먼저 수행합니다.
            _connection?.Dispose();
            _lobbyNetworkHandler?.Dispose();
            _inGameHandler?.Dispose();
            _requests?.Dispose();
            _connection = null;
            _requests = null;
            _lobbyNetworkHandler = null;
            _inGameHandler = null;

        }
        #endregion
        #region Public Methods
        // Init

        // Connection
        public UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_connection is null)
                throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");
            return _connection.ConnectAsync(cancellationToken);
        }
        public void Disconnect()
        {
            _connection?.Disconnect();
        }

        // 로비 요청의 수명과 메시지 처리는 Handler에 위임합니다.
        public UniTask StartMatchMakingAsync()
        {
            return Lobby.MatchingStartRequestAsync();
        }
        public UniTask CancelMatchMakingAsync()
        {
            return Lobby.MatchingCancelRequestAsync();
        }

        /// <summary>선택한 포진을 전송합니다. 게임 준비는 서버 이벤트로 확정됩니다.</summary>
        public UniTask SubmitFormationAsync(Formation formation)
        {
            return Lobby.SubmitFormationAsync(formation);
        }

        #endregion
        #region Event Handlers
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
    }
}


