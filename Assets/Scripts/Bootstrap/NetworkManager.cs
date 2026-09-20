#nullable enable

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using Network_V2;

    /// <summary>
    /// 온라인 네트워크 클라이언트의 생성과 생명주기를 관리합니다.
    /// </summary>
    public sealed class NetworkManager : MonoBehaviour
    {
        private CancellationTokenSource? _lifetimeCts;
        private OnlineGameClient_V2? _client;

        public IOnlineGameClient_V2 Client
            => _client ?? throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");

        public bool IsConnected
            => _client?.IsConnected ?? false;

        /// <summary>
        /// 네트워크 클라이언트를 초기화합니다.
        /// </summary>
        public void Initialize(
            string  host,
            int     port)
        {
            if (_client is not null)
            {
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");
            }

            _lifetimeCts = new CancellationTokenSource();

            _client = new OnlineGameClient_V2(
                host,
                port);

            _client.OnStateChanged      += HandleStateChanged;
            _client.OnConnectionFailed  += HandleConnectionFailed;
        }

        /// <summary>
        /// 서버 연결과 프로토콜 핸드셰이크를 시작합니다.
        /// </summary>
        public async UniTask ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            if (_client is null ||
                _lifetimeCts is null)
            {
                throw new InvalidOperationException(
                    "NetworkManager가 초기화되지 않았습니다.");
            }

            using CancellationTokenSource linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(
                    _lifetimeCts.Token,
                    cancellationToken);

            await _client.ConnectAsync(
                linkedCts.Token);
        }

        /// <summary>
        /// 서버 연결을 종료합니다.
        /// </summary>
        public void Disconnect()
        {
            _client?.Disconnect();
        }

        private void OnDestroy()
        {
            if (_client is not null)
            {
                _client.OnStateChanged -= HandleStateChanged;
                _client.OnConnectionFailed -= HandleConnectionFailed;

                _client.Dispose();
            }

            _lifetimeCts?.Cancel();
            _lifetimeCts?.Dispose();
        }

        private void HandleStateChanged(
            OnlineConnectionState state)
        {
            Debug.Log($"Network State: {state}");
        }

        private void HandleConnectionFailed(
            string reason)
        {
            Debug.LogError($"Connection Failed: {reason}");
        }
    }
}

