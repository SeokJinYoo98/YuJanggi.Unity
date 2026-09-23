#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Network;
    using Matching;
    using Core.V2.Domain;
    using Network.Status;


    /// <summary>
    /// Unity 네트워크 요청의 수명과 UI에 전달할 상태를 관리합니다.
    /// Unity쪽 네트워크 진입점
    /// 상태를 조립해서 이벤트로 전달
    /// </summary>
    public sealed class NetworkManager : MonoBehaviour
    {
        #region Fields
        private CancellationTokenSource? _lifetimeCts;
        private CancellationTokenSource? _connectingRequestCts;
        private CancellationTokenSource? _matchingRequestCts;
        private NetworkConnection? _connection;
        private RequestDispatcher? _requests;
        private MatchingService? _matchingService;
        private MatchingHandler? _matchingHandler;

        #endregion
        #region Properties
        public NetworkConnection Connection => _connection
            ?? throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");
        public MatchingHandler Matching => _matchingHandler
            ?? throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");
        public MatchingService MatchingService => _matchingService
            ?? throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");
        public bool IsMatched
            => Status.MatchingState == MatchingState.Matched;
        public bool IsOnline
            => _connection?.IsOnline ?? false;
        public NetworkStatus Status { get; private set; }
            = new NetworkStatus(
                NetworkState.Offline,
                ConnectionState.Disconnected,
                null, null);


        #endregion
        #region Events
        public event Action? OnNetworkChanged;
        #endregion
        #region Unity Lifecycle
        private void OnDestroy()
        {
            if (_connection is not null)
                _connection.OnDataChanged -= HandleClientDataChanged;
            if (_matchingService is not null)
                _matchingService.OnDataChanged -= HandleClientDataChanged;

            // 연결 종료가 Service 초기화와 pending 취소를 먼저 수행합니다.
            _connection?.Dispose();
            _lifetimeCts?.Cancel();
            _matchingHandler?.Dispose();
            _requests?.Dispose();
            _connection = null;
            _requests = null;
            _matchingHandler = null;
            _matchingService = null;

            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }
        #endregion
        #region Public Methods
        // Init
        public void Initialize(string host, int port)
        {
            if (_connection is not null)
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");

            _connection = new NetworkConnection(host, port);
            _requests = new RequestDispatcher(_connection);
            _matchingService = new MatchingService();
            _matchingHandler = new MatchingHandler(_connection, _requests, _matchingService);
            _lifetimeCts = new CancellationTokenSource();

            _connection.OnDataChanged += HandleClientDataChanged;
            _matchingService.OnDataChanged += HandleClientDataChanged;
        }
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
            return RunMatchingRequestAsync((handler, token) => handler.MatchRequestAsync(token));
        }

        public UniTask CancelMatchMakingAsync()
        {
            return RunMatchingRequestAsync((handler, token) => handler.MatchCancelRequestAsync(token));
        }

        /// <summary>선택한 포진을 제출합니다. 접수 성공은 게임 시작을 의미하지 않습니다.</summary>
        public async UniTask<bool> SubmitFormationAsync(Formation formation)
        {
            bool accepted = false;
            await RunMatchingRequestAsync(async (handler, token) =>
            {
                accepted = await handler.SubmitFormationAsync(formation, token);
            });
            return accepted;
        }

        #endregion
        #region Event Handlers
        private void HandleClientDataChanged()
        {
            if (_connection is null || _matchingService is null)
                return;

            Status = new NetworkStatus(
                _connection.IsOnline ? NetworkState.Online : NetworkState.Offline,
                _connection.State,
                _connection.Error,
                _connection.Failure?.Message,
                _matchingService.CurrentMatch,
                _matchingService.Team,
                _matchingService.State);

            OnNetworkChanged?.Invoke();
        }
        #endregion
        #region Private Methods
        private async UniTask RunMatchingRequestAsync(
            Func<MatchingHandler, CancellationToken, UniTask> sendRequest)
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
                await sendRequest(Matching, matchingCts.Token);
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
