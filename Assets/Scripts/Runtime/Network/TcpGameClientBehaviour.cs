using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YuJanggiCommon;

namespace Yujanggi.Runtime.Network
{
    /// <summary>
    /// Unity-facing bridge that dispatches queued TCP events from Update.
    /// All subscribers therefore run on Unity's main thread.
    /// </summary>
    public sealed class TcpGameClientBehaviour : MonoBehaviour
    {
        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port = 7777;

        private TcpGameClient _client;

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<string> OnErrorOccurred;
        public event Action OnDisconnected;

        public bool IsConnected => _client != null && _client.IsConnected;

        private void Awake()
        {
            _client = new TcpGameClient();
        }

        private void Update()
        {
            while (_client.TryDequeueEvent(out TcpGameClientEvent clientEvent))
            {
                switch (clientEvent.Type)
                {
                    case TcpGameClientEventType.MessageReceived:
                        OnMessageReceived?.Invoke(clientEvent.Message);
                        break;
                    case TcpGameClientEventType.ErrorOccurred:
                        OnErrorOccurred?.Invoke(clientEvent.ErrorMessage);
                        break;
                    case TcpGameClientEventType.Disconnected:
                        OnDisconnected?.Invoke();
                        break;
                }
            }
        }

        private void OnDestroy()
        {
            _client?.Dispose();
            _client = null;
        }

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.ConnectAsync(_host, _port, cancellationToken);
            }
            catch (Exception) when (cancellationToken.IsCancellationRequested)
            {
                // 취소로 소켓이 닫히면 SocketException 등으로 완료될 수도 있습니다.
                throw new OperationCanceledException(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                OnErrorOccurred?.Invoke(exception.Message);
            }
        }

        public async UniTask SendAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            try
            {
                await _client.SendAsync(message, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                OnErrorOccurred?.Invoke(exception.Message);
            }
        }

        public void Disconnect()
            => _client?.Disconnect();
    }
}
