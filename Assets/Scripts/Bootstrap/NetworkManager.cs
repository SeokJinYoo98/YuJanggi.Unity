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

    /// <summary>클라이언트의 생명주기를 관리하고 해석된 데이터로 네트워크 상태를 생성합니다.</summary>
    public sealed class NetworkManager : MonoBehaviour
    {
        private CancellationTokenSource?    _lifetimeCts;
        private IOnlineGameClient_V2?       _client;
        public bool IsConnected
            => _client?.IsConnected ?? false;

        public NetworkStatus Status { get; private set; }
            = new NetworkStatus(
                NetworkState.Offline,
                ConnectionState.Disconnected,
                null, null);

        public event Action? OnNetworkChanged;

        public void Initialize(string host, int port)
        {
            if (_client is not null)
                throw new InvalidOperationException("NetworkManager가 이미 초기화되었습니다.");

            _client         = new OnlineGameClient_V2(host, port);
            _lifetimeCts    = new CancellationTokenSource();

            _client.OnDataChanged += HandleClientDataChanged;
        }

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (_client is null || _lifetimeCts is null)
                throw new InvalidOperationException("NetworkManager가 초기화되지 않았습니다.");

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifetimeCts.Token, cancellationToken);

            await _client.ConnectAsync(connectCts.Token);
        }

        public void Disconnect()
        {
            _client?.Disconnect();
        }

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
                _client.IsConnected ? NetworkState.Online : NetworkState.Offline,
                _client.State,
                error,
                _client.Failure?.Message);

            OnNetworkChanged?.Invoke();
        }

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
    }
}

