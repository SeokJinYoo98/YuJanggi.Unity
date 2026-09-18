using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using YuJanggiCommon;

namespace YuJanggi.Runtime.Network
{
    /// <summary>
    /// Thread-safe TCP transport for the server's length-prefixed JSON protocol.
    /// Received events are queued so callers can decide where to dispatch them.
    /// </summary>
    public sealed class TcpGameClient : IDisposable
    {
        private readonly ConcurrentQueue<TcpGameClientEvent> _receivedEvents = new();
        private readonly SemaphoreSlim _sendLock = new(1, 1);
        private readonly object _connectionLock = new();

        private Connection _connection;

        public bool IsConnected
        {
            get
            {
                lock (_connectionLock)
                    return _connection != null && !_connection.Cancellation.IsCancellationRequested;
            }
        }

        public async Task ConnectAsync(string host, int port, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("서버 호스트가 필요합니다.", nameof(host));
            if (port is < 1 or > 65535)
                throw new ArgumentOutOfRangeException(nameof(port));

            lock (_connectionLock)
            {
                if (_connection != null)
                    throw new InvalidOperationException("이미 서버에 연결되어 있습니다.");
            }

            var client = new TcpClient();
            try
            {
                using var registration = cancellationToken.Register(client.Dispose);
                await client.ConnectAsync(host, port);
                cancellationToken.ThrowIfCancellationRequested();

                var connection = new Connection(client);
                lock (_connectionLock)
                {
                    if (_connection != null)
                        throw new InvalidOperationException("이미 서버에 연결되어 있습니다.");

                    _connection = connection;
                }

                _ = Task.Run(() => ReceiveLoopAsync(connection));
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        public async Task SendAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            Connection connection = GetConnection();
            byte[] packet = MessageProtocol.Encode(message);

            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                await connection.Stream.WriteAsync(packet, 0, packet.Length, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public bool TryDequeueEvent(out TcpGameClientEvent clientEvent)
            => _receivedEvents.TryDequeue(out clientEvent);

        public void Disconnect()
        {
            Connection connection;
            lock (_connectionLock)
            {
                connection = _connection;
                _connection = null;
            }

            connection?.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
            _sendLock.Dispose();
        }

        private Connection GetConnection()
        {
            lock (_connectionLock)
            {
                if (_connection == null || _connection.Cancellation.IsCancellationRequested)
                    throw new InvalidOperationException("서버에 연결되어 있지 않습니다.");

                return _connection;
            }
        }

        private async Task ReceiveLoopAsync(Connection connection)
        {
            bool disconnectedByClient = false;
            try
            {
                while (!connection.Cancellation.IsCancellationRequested)
                {
                    byte[] header = new byte[MessageProtocol.HeaderSize];
                    await ReadExactlyAsync(connection.Stream, header, connection.Cancellation.Token);

                    int bodyLength = MessageProtocol.DecodeBodyLength(header);
                    byte[] body = new byte[bodyLength];
                    await ReadExactlyAsync(connection.Stream, body, connection.Cancellation.Token);

                    _receivedEvents.Enqueue(TcpGameClientEvent.MessageReceived(
                        MessageProtocol.DecodeBody(body)
                    ));
                }
            }
            catch (OperationCanceledException) when (connection.Cancellation.IsCancellationRequested)
            {
                disconnectedByClient = true;
            }
            catch (ObjectDisposedException) when (connection.Cancellation.IsCancellationRequested)
            {
                disconnectedByClient = true;
            }
            catch (Exception exception)
            {
                _receivedEvents.Enqueue(TcpGameClientEvent.ErrorOccurred(exception.Message));
            }
            finally
            {
                lock (_connectionLock)
                {
                    if (ReferenceEquals(_connection, connection))
                        _connection = null;
                }

                connection.Dispose();

                if (!disconnectedByClient)
                    _receivedEvents.Enqueue(TcpGameClientEvent.Disconnected());
            }
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(
                    buffer,
                    offset,
                    buffer.Length - offset,
                    cancellationToken
                );

                if (read == 0)
                    throw new EndOfStreamException("서버 연결이 종료되었습니다.");

                offset += read;
            }
        }

        private sealed class Connection : IDisposable
        {
            private int _disposed;

            public Connection(TcpClient client)
            {
                Client = client;
                Stream = client.GetStream();
                Cancellation = new CancellationTokenSource();
            }

            public TcpClient Client { get; }
            public NetworkStream Stream { get; }
            public CancellationTokenSource Cancellation { get; }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                    return;

                Cancellation.Cancel();
                Stream.Dispose();
                Client.Dispose();
            }
        }
    }

    public enum TcpGameClientEventType
    {
        MessageReceived,
        Disconnected,
        ErrorOccurred
    }

    public readonly struct TcpGameClientEvent
    {
        private TcpGameClientEvent(TcpGameClientEventType type, ChatMessage message, string errorMessage)
        {
            Type = type;
            Message = message;
            ErrorMessage = errorMessage;
        }

        public TcpGameClientEventType Type { get; }
        public ChatMessage Message { get; }
        public string ErrorMessage { get; }

        public static TcpGameClientEvent MessageReceived(ChatMessage message)
            => new(TcpGameClientEventType.MessageReceived, message, null);

        public static TcpGameClientEvent Disconnected()
            => new(TcpGameClientEventType.Disconnected, null, null);

        public static TcpGameClientEvent ErrorOccurred(string errorMessage)
            => new(TcpGameClientEventType.ErrorOccurred, null, errorMessage);
    }
}
