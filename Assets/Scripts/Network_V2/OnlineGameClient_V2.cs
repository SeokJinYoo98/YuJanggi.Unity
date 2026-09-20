#nullable enable
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace YuJanggi.Network_V2
{
    using Protocol.V2.Messages;
    public enum ConnectionEvent
    {
        Connecting, 
    }
    public sealed class OnlineGameClient_V2 :IDisposable
    {
        private readonly TcpGameClient_V2   _tcpClient;

        private CancellationTokenSource?    _receiveCts;
        private bool                        _disposed;

        private bool IsConnected => _tcpClient.IsConnected;
        public OnlineGameClient_V2(string host, int port)
        {
            _tcpClient = new(host, port);
        }
        /// <summary>
        /// 서버에 연결하고 온라인 세션을 시작합니다.
        /// </summary>
        public async UniTask ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (IsConnected)
                throw new InvalidOperationException(
                    "이미 서버에 연결되어 있습니다.");


            await _tcpClient.ConnectAsync(cancellationToken);

            await HandshakeAsync(cancellationToken);

            _receiveCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken);

            ReceiveLoopAsync(_receiveCts.Token).Forget();
        }

        /// <summary>
        /// 서버와 프로토콜 핸드셰이크를 수행합니다.
        /// </summary>
        private async UniTask HandshakeAsync(
            CancellationToken cancellationToken)
        {
        }

        /// <summary>
        /// 서버 메시지를 지속적으로 수신합니다.
        /// </summary>
        private async UniTask ReceiveLoopAsync(
            CancellationToken cancellationToken)
        {
        }

        /// <summary>
        /// 수신한 서버 메시지를 종류에 따라 처리합니다.
        /// </summary>
        private void HandleMessage(ServerMessage message)
        {
        }

        /// <summary>
        /// 클라이언트 메시지를 서버로 전송합니다.
        /// </summary>
        public async UniTask SendAsync(
            ClientMessage message,
            CancellationToken cancellationToken = default)
        {
        }

        /// <summary>
        /// 서버 연결 종료를 처리합니다.
        /// </summary>
        public void Disconnect()
        {
        }

        /// <summary>
        /// 연결이 끊어진 경우의 공통 처리를 수행합니다.
        /// </summary>
        private void HandleDisconnected()
        {
        }

        private void ThrowIfDisposed()
        {
        }

        public void Dispose()
        {
        }
    }
}
