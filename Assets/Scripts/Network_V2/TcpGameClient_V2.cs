#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace YuJanggi.Network.V2
{
    using Protocol.V2.Messages;
    using Protocol.V2.Framing;
    using Protocol.V2.Serialization;
    /// <summary>
    /// TCP 연결을 관리하고
    /// 프로토콜 메시지의 송수신을 처리하는 전송 계층 클라이언트입니다.
    /// </summary>
    public sealed class TcpGameClient_V2 : IDisposable
    {
        private TcpClient?              _client;
        private NetworkStream?          _stream;
        private readonly SemaphoreSlim  _sendLock;

        private readonly string _host;
        private readonly int    _port;
        private bool            _disposed;

        /// <summary>
        /// 현재 TCP 연결 상태를 반환합니다.
        /// </summary>
        public bool IsConnected =>
            _client is not null &&
            _stream is not null &&
            _client.Connected;

        public TcpGameClient_V2(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("서버 호스트가 필요합니다.", nameof(host));
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));

            _disposed = false;
            _host = host; _port = port;
            _sendLock    = new SemaphoreSlim(1, 1);
        }
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _stream?.Dispose();
            _client?.Dispose();

            _sendLock.Dispose();
        }
        public void Disconnect()
        {
            _stream?.Dispose();
            _stream = null;

            _client?.Dispose();
            _client = null;
        }

        /// <summary>
        /// 이미 Dispose된 객체를 다시 쓰려고 하면 예외 발생
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(TcpGameClient_V2));
            }
        }

        /// <summary>
        /// 서버에 TCP 연결을 시도합니다.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async UniTask ConnectAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (_client is not null)
            {
                throw new InvalidOperationException(
                    "이미 TCP 클라이언트가 생성되어 있습니다.");
            }

            var client = new TcpClient();

            try
            {
                using (cancellationToken.Register(client.Dispose))
                {
                    await client.ConnectAsync(_host, _port);
                }

                cancellationToken.ThrowIfCancellationRequested();

                _client = client;
                _stream = client.GetStream();
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 클라이언트 메시지를 서버로 전송합니다.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async UniTask SendAsync(
            ClientMessage message,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var stream = GetConnectedStream();

            byte[] body =
                MessageSerializer.Serialize(message);
            byte[] packet =
                MessageFramer.Encode(body);

            await _sendLock.WaitAsync(cancellationToken);

            try
            {
                await stream.WriteAsync(
                    packet,
                    0,
                    packet.Length,
                    cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        /// <summary>
        /// 서버에서 메시지를 수신합니다.
        /// </summary>
        public async UniTask<ServerMessage>ReceiveAsync(
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            try
            {
                var stream = GetConnectedStream();

                byte[] header =
                    new byte[MessageFramer.HeaderSize];
                await ReadExactlyAsync(
                    stream,
                    header,
                    cancellationToken);

                byte[] body =
                     new byte[MessageFramer.DecodeBodyLength(header)];
                await ReadExactlyAsync(
                    stream,
                    body,
                    cancellationToken);

                return MessageSerializer.Deserialize
                    <ServerMessage>(body);
            }
            finally
            {
                
            }

        }

        private NetworkStream GetConnectedStream()
        {
            ThrowIfDisposed();

            if (_stream is null)
            {
                throw new InvalidOperationException(
                    "서버에 연결되어 있지 않습니다.");
            }

            return _stream;
        }

        private async UniTask ReadExactlyAsync(
            NetworkStream       stream,
            byte[]              buffer,
            CancellationToken   cancellationToken)
        {
            int offset = 0;

            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(
                    buffer,
                    offset,
                    buffer.Length - offset,
                    cancellationToken);

                if (read == 0)
                {
                    throw new IOException(
                        "서버와의 연결이 종료되었습니다.");
                }

                offset += read;
            }
        }

    }
}
