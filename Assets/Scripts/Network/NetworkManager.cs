#nullable disable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace YuJanggi.Network
{
    using YuJanggi.InGame.Handler;
    using YuJanggi.Lobby.Handler;
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
        // 통신 응답과 승인 후 Event 대기의 제한이며 게임 턴 제한시간과는 무관합니다.
        [SerializeField, Min(1f)] private float _requestTimeoutSeconds = 10f;
        internal TimeSpan RequestTimeout { get; private set; }
        internal CancellationToken ConnectionToken
            => _receiveCts?.Token ?? new CancellationToken(canceled: true);

        private readonly SemaphoreSlim _connectLock = new(1, 1);

        private NetworkClient _client;

        private RequestDispatcher       _requests;
        private LobbyNetworkHandler     _lobbyNetworkHandler;
        private InGameNetworkHandler    _inGameHandler;
        private InGameChattingHandler   _inGameChat;

        private CancellationTokenSource _receiveCts;
        private UniTaskCompletionSource _receiveCompletion;

        internal LobbyNetworkHandler    Lobby
            => _lobbyNetworkHandler;
        internal InGameNetworkHandler   InGame
            => _inGameHandler;
        internal InGameChattingHandler InGameChat
            => _inGameChat;
        

        internal void Initialize(
            string host,
            int port)
        {
            if (_client is not null)
                throw new InvalidOperationException(
                    "NetworkManager가 이미 초기화되었습니다.");

            RequestTimeout = TimeSpan.FromSeconds(_requestTimeoutSeconds);
            var client   = new NetworkClient(host, port);
            var requests = new RequestDispatcher(client, RequestTimeout);

            LobbyNetworkHandler   lobby      = null;
            InGameNetworkHandler  inGame     = null;
            InGameChattingHandler inGameChat = null;
            try
            {
                lobby      = new LobbyNetworkHandler(client, requests);
                inGame     = new InGameNetworkHandler(client, requests);
                inGameChat = new InGameChattingHandler(client, requests);

                _client                 = client;
                _requests               = requests;

                _lobbyNetworkHandler    = lobby;
                _inGameHandler          = inGame;
                _inGameChat             = inGameChat;
            }
            catch
            {
                inGame?.Dispose();
                lobby?.Dispose();
                inGameChat?.Dispose();

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

            int typeNum = (int)message.Type;

            if (200 <= typeNum && typeNum <= 299)
                Lobby.HandleMessage(message);
            else if (300 <= typeNum && typeNum <= 399)
                InGame.HandleMessage(message);
            else if (400 <= typeNum && typeNum <= 499)
                InGameChat.HandleMessage(message);
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
            _requests.BeginConnection(_receiveCts.Token);

            ReceiveLoopAsync(
                _client,
                _receiveCts,
                _receiveCompletion)
                .Forget();
        }

        // TODO: 로그인 씬 구현 시 연결 종료 상태 표시와 씬 전환 정책을 연결합니다.
        public async UniTask StopReceiveLoopAsync()
        {
            var cts = _receiveCts;
            var completion = _receiveCompletion;
            if (cts is null || completion is null)
                return;

            cts.Cancel();
            _client?.Disconnect();
            await completion.Task;
        }

        private async UniTask ReceiveLoopAsync(
            NetworkClient client,
            CancellationTokenSource cts,
            UniTaskCompletionSource completion)
        {
            var token = cts.Token;
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
                try
                {
                    if (ReferenceEquals(_receiveCts, cts))
                    {
                        // 토큰부터 종료해 새 요청을 막고 전송/응답/Event 대기를 함께 끝냅니다.
                        try
                        {
                            cts.Cancel();
                        }
                        finally
                        {
                            try
                            {
                                _requests?.Clear();
                            }
                            finally
                            {
                                try
                                {
                                    _inGameHandler?.CancelPendingEvents();
                                }
                                finally
                                {
                                    _receiveCts = null;
                                    _receiveCompletion = null;
                                    client.Disconnect();
                                }
                            }
                        }
                    }
                }
                finally
                {
                    completion.TrySetResult();
                    cts.Dispose();
                }
            }
        }

        private void OnApplicationQuit()
        {
            _receiveCts?.Cancel();
            _client?.Disconnect();
        }

        private void OnDestroy()
        {
            _receiveCts?.Cancel();
            _client?.Disconnect();
            _requests?.Dispose();

            _lobbyNetworkHandler?.Dispose();
            _inGameHandler?.Dispose();
            _inGameChat?.Dispose();

            _client?.Dispose();
        }
    }
}
