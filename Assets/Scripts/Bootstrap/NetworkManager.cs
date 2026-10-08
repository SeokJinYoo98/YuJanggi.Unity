#nullable disable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.BootStrap
{
    using YuJanggi.InGame.Handler;
    using YuJanggi.Lobby.Handler;
    using YuJanggi.Network;
    using YuJanggi.Protocol.Messages;

    public enum ConnectingResult
    {
        Success,
        Failed,
        AlreadyConnecting,
        AlreadyConnected
    }
    /// <summary>연결 시작과 종료, 수신 루프, Response/Event 라우팅을 담당합니다.</summary>
    public sealed class NetworkManager : MonoBehaviour
    {
        private readonly SemaphoreSlim _connectLock = new(1, 1);

        private NetworkClient _client;

        private RequestDispatcher       _requests;
        private LobbyNetworkHandler     _lobbyNetworkHandler;
        private InGameHandler           _inGameHandler;

        private CancellationTokenSource _receiveCts;
        private UniTaskCompletionSource _receiveCompletion;

        internal LobbyNetworkHandler    Lobby
            => _lobbyNetworkHandler;
        internal InGameHandler          InGame
            => _inGameHandler;

 

        internal void Initialize(
            string host,
            int port)
        {
            if (_client is not null)
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");

            var client   = new NetworkClient(host, port);
            var requests = new RequestDispatcher(client);

            LobbyNetworkHandler lobby   = null;
            InGameHandler       inGame  = null;

            try
            {
                lobby   = new LobbyNetworkHandler(client, requests);
                inGame  = new InGameHandler(client, requests);

                _client                 = client;
                _requests               = requests;
                _lobbyNetworkHandler    = lobby;
                _inGameHandler          = inGame;
            }
            catch
            {
                inGame?.Dispose();
                lobby?.Dispose();
                requests.Dispose();
                client.Dispose();
                throw;
            }
        }

        internal void HandleMessage(ServerMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (message.RequestId is not null)
            {
                _requests.HandleMessage(message);
                return;
            }

            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                case ServerMessageType.GameReadyEvent:
                    Lobby.HandleMessage(message);
                    break;
                case ServerMessageType.GameStartEvent:
                case ServerMessageType.MovePieceEvent:
                case ServerMessageType.GameEndedEvent:
                    InGame.HandleMessage(message);
                    break;
            }
        }

        public async UniTask<ConnectingResult> ConnectAsync(
            CancellationToken token)
        {
            if (!await _connectLock.WaitAsync(0, token))
                return ConnectingResult.AlreadyConnecting;

            try
            {
                if (_receiveCts is not null)
                    return ConnectingResult.AlreadyConnected;

                try
                {
                    await _client.ConnectAsync(token);
                    token.ThrowIfCancellationRequested();
                    StartReceiveLoop();
                    return ConnectingResult.Success;
                }
                catch (OperationCanceledException)
                {
                    await DisconnectCoreAsync();
                    throw;
                }
                catch (Exception exception)
                {
                    await DisconnectCoreAsync();
                    Debug.LogException(exception);
                    return ConnectingResult.Failed;
                }
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async UniTask DisconnectAsync()
        {
            await _connectLock.WaitAsync();
            try
            {
                await DisconnectCoreAsync();
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private async UniTask DisconnectCoreAsync()
        {
            _receiveCts?.Cancel();
            try
            {
                _client?.Disconnect();
            }
            finally
            {
                try
                {
                    _requests?.Clear();
                }
                finally
                {
                    await StopReceiveLoopAsync();
                }
            }
        }

        // TCP 연결 성공 직후 시작해 Handshake Response도 라우팅합니다.
        public void StartReceiveLoop()
        {
 
            if (_receiveCts is not null)
                throw new InvalidOperationException(
                    "이전 ReceiveLoop를 먼저 종료해야 합니다.");

            _receiveCts         = new CancellationTokenSource();
            _receiveCompletion  = new UniTaskCompletionSource();

            ReceiveLoopAsync(
                _client,
                _receiveCts.Token,
                _receiveCompletion)
                .Forget();
        }

        // TODO: 로그인 씬 구현 시 ReceiveLoop 종료 예외 처리 정리
        // - Pending Request 종료
        // - 연결 상태 Offline 처리
        // - Disconnect
        // - 재접속 또는 로그인 씬 전환 정책 적용
        public async UniTask StopReceiveLoopAsync()
        {
            var cts = _receiveCts;
            var completion = _receiveCompletion;
            if (cts is null || completion is null)
                return;

            cts.Cancel();
            await completion.Task;
            if (ReferenceEquals(_receiveCts, cts))
            {
                _receiveCts = null;
                _receiveCompletion = null;
                cts.Dispose();
            }
        }

        private async UniTask ReceiveLoopAsync(
            NetworkClient client,
            CancellationToken token,
            UniTaskCompletionSource completion)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var message = await client.ReceiveAsync(token);
                    token.ThrowIfCancellationRequested();
                    HandleMessage(message);
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                // 수신 루프 종료에 따른 정상 취소입니다.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                completion.TrySetResult();
            }
        }

        private void OnDestroy()
        {
            var cts = _receiveCts;
            _receiveCts = null;
            _receiveCompletion = null;
            try
            {
                cts?.Cancel();
            }
            finally
            {
                cts?.Dispose();
            }

            _client?.Disconnect();
            _requests?.Dispose();

            _lobbyNetworkHandler?.Dispose();
            _inGameHandler?.Dispose();
            _client?.Dispose();
        }
    }
}
