#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Network.V2;
    using Network.V2.Status;
    using Protocol.V2.Connection;

    /// <summary>
    /// TurnBasedNetworkService로 승격 예정
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
                throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");

            // 연결 시도의 중복 실행을 막습니다.
            if (_connectingRequestCts is not null || IsOnline)
                throw new InvalidOperationException("이미 연결 중이거나 서버에 연결되어 있습니다.");

            // 매니저 파괴 시 함께 취소되며, 메서드 종료 시 using이 CTS를 해제합니다.
            using var connectCts
                = CancellationTokenSource.CreateLinkedTokenSource(
                    _lifetimeCts.Token);

            // Disconnect에서도 취소할 수 있도록 현재 작업의 CTS를 보관합니다.
            _connectingRequestCts = connectCts;

            try
            {
                await _client.ConnectAsync(connectCts.Token);
            }
            finally
            {
                if (connectCts == _connectingRequestCts)
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

        // Matching
        public async UniTask StartMatchMakingAsync()
        {
            if (_client is null || _lifetimeCts is null)
                throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");
            if (!IsOnline)
                throw new InvalidOperationException(
                    "서버에 연결되어 있지 않습니다.");
            if (_matchingRequestCts is not null)
                throw new InvalidOperationException(
                    "이미 매칭 요청을 처리 중입니다.");

            // 매니저 수명에 연결하되, 연결 시도와는 별도의 CTS를 사용합니다.
            using var matchingCts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCts.Token);
            _matchingRequestCts = matchingCts;

            try
            {
                matchingCts.Token.ThrowIfCancellationRequested();
                await _client.MatchRequestAsync(matchingCts.Token);
            }
            finally
            {
                if (matchingCts == _matchingRequestCts)
                    _matchingRequestCts = null;
            }
        }

        public async UniTask CancelMatchMakingAsync()
        {
            if (_client is null ||
                _lifetimeCts is null)
            {
                throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");
            }

            await _client.MatchCancelRequestAsync(
                _lifetimeCts.Token);
        }
        #endregion
        #region Event Handlers
        private void HandleClientDataChanged()
        {
            if (_client is null)
                return;

            NetworkError? error = null;
            if (_client.Failure is not null)
            {
                var result = _client.HandshakeResponse?.Result
                    ?? ProtocolHandshakeResult.Success;

                NetworkError versionErrors = NetworkError.None;

                if ((result & ProtocolHandshakeResult.CoreVersionMismatch) != 0)
                    versionErrors |= NetworkError.CoreVersionMismatch;

                if ((result & ProtocolHandshakeResult.ProtocolVersionMismatch) != 0)
                    versionErrors |= NetworkError.ProtocolVersionMismatch;

                error = NetworkError.ConnectionFailed | versionErrors;
            }

            Status = new NetworkStatus(
                _client.IsOnline ? NetworkState.Online : NetworkState.Offline,
                _client.State,
                error,
                _client.Failure?.Message);

            OnNetworkChanged?.Invoke();
        }
        #endregion
        #region Private Methods

        #endregion
    }
}
