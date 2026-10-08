#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace YuJanggi.Network
{
    using Protocol.Messages;
    using Protocol.Framing;
    using Protocol.Serialization;
    /// <summary>
    /// TCP 연결을 관리하고
    /// 프로토콜 메시지의 송수신을 처리하는 전송 계층 클라이언트입니다.
    /// </summary>

    public sealed class NetworkClient : IDisposable
    {
        private TcpClient?              _client;
        private NetworkStream?          _stream;
        private readonly SemaphoreSlim  _sendLock;

        private readonly string _host;
        private readonly int    _port;
        private bool            _disposed;

        public NetworkClient(string host, int port)
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


        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(
                    nameof(NetworkClient));
            }
        }
        private void ThrowIfConnected()
        {
            if (_client is not null)
            {
                throw new InvalidOperationException(
                    "이미 TCP 클라이언트가 생성되어 있습니다.");
            }
        }

        public async UniTask ConnectAsync(
            CancellationToken token = default)
        {
            ThrowIfDisposed();
            ThrowIfConnected();

            token.ThrowIfCancellationRequested();

            TcpClient? client = new();
            CancellationTokenRegistration registration = default;

            try
            {
                registration =
                    token.Register(client.Dispose);

                await client.ConnectAsync(_host, _port);

                token.ThrowIfCancellationRequested();

                var stream = client.GetStream();

                _client = client;
                _stream = stream;

                client = null;
            }
            catch (ObjectDisposedException)
                when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            finally
            {
                registration.Dispose();
                client?.Dispose();
            }
        }

        public async UniTask SendAsync(
            ClientMessage message,
            CancellationToken token = default)
        {
            ThrowIfDisposed();

            var stream = GetConnectedStream();

            token.ThrowIfCancellationRequested();

            byte[] body =
                MessageSerializer.Serialize(message);

            byte[] packet =
                MessageFramer.Encode(body);

            await _sendLock.WaitAsync(token);

            try
            {
                await stream.WriteAsync(
                    packet,
                    0,
                    packet.Length,
                    token);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public async UniTask<ServerMessage>ReceiveAsync(
            CancellationToken token = default)
        {
            ThrowIfDisposed();

            var stream = GetConnectedStream();

            byte[] header =
                new byte[MessageFramer.HeaderSize];

            await ReadExactlyAsync(
                stream,
                header,
                token);

            byte[] body =
                new byte[MessageFramer.DecodeBodyLength(header)];

            await ReadExactlyAsync(
                stream,
                body,
                token);

            return MessageSerializer.Deserialize<ServerMessage>(body);
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
