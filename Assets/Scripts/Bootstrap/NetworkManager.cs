#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Network.V2;
    using Network.V2.Status;


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
        private IOnlineGameClient_V2?    _client;

        #endregion
        #region Properties
        public bool IsOnline
            => _client?.IsOnline ?? false;
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
            if (_client is not null)
                _client.OnDataChanged -= HandleClientDataChanged;
            _lifetimeCts?.Cancel();

            _client?.Dispose();
            _client = null;

            _lifetimeCts?.Dispose();
            _lifetimeCts = null;
        }
        #endregion
        #region Public Methods
        // Init
        public void Initialize(string host, int port)
        {
            if (_client is not null)
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");

            _client         = new OnlineGameClient_V2(host, port);
            _lifetimeCts    = new CancellationTokenSource();

            _client.OnDataChanged += HandleClientDataChanged;
        }
        // Connection
        public async UniTask ConnectAsync()
        {
            if (_client is null || _lifetimeCts is null)
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
                await _client.ConnectAsync(
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
            // 진행 중인 작업을 취소한 뒤 실제 연결을 종료합니다.
            _matchingRequestCts?.Cancel();
            _connectingRequestCts?.Cancel();

            _client?.Disconnect();
        }

        // Matching: 서버 응답 해석과 상태 변경은 클라이언트가 담당합니다.
        public UniTask StartMatchMakingAsync()
        {
            return RunMatchingRequestAsync((client, token) => client.MatchRequestAsync(token));
        }

        public UniTask CancelMatchMakingAsync()
        {
            return RunMatchingRequestAsync((client, token) => client.MatchCancelRequestAsync(token));
        }

        #endregion
        #region Event Handlers
        private void HandleClientDataChanged()
        {
            if (_client is null)
                return;

            Status = new NetworkStatus(
                _client.IsOnline ? NetworkState.Online : NetworkState.Offline,
                _client.State,
                _client.Error,
                _client.Failure?.Message,
                _client.CurrentMatch);

            OnNetworkChanged?.Invoke();
        }
        #endregion
        #region Private Methods
        private async UniTask RunMatchingRequestAsync(
            Func<IOnlineGameClient_V2, CancellationToken, UniTask> sendRequest)
        {
            if (_client is null || _lifetimeCts is null)
                throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");
            if (_matchingRequestCts is not null)
                throw new InvalidOperationException("이미 매칭 신청 또는 취소 요청을 처리 중입니다.");

            using var matchingCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
            _matchingRequestCts = matchingCts;
            try
            {
                matchingCts.Token.ThrowIfCancellationRequested();
                await sendRequest(_client, matchingCts.Token);
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
