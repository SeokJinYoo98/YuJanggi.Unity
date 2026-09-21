#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.Network.V2
{
    using Core.V2;
    using Protocol.V2;
    using Protocol.V2.Connection;
    using Protocol.V2.Messages;
    using Protocol.V2.Messages.MessageFactory;
    using Status;

    public interface IOnlineGameClient_V2 : IDisposable
    {
        bool                        IsConnected { get; }
        ConnectionState             State { get; }
        ProtocolHandshakeResponse?  HandshakeResponse { get; }
        Exception?                  Failure { get; }
        event Action?               OnDataChanged;
        UniTask ConnectAsync(CancellationToken cancellationToken = default);
        void Disconnect();
    }

    /// <summary>TCP 연결과 핸드셰이크 프로토콜 해석을 담당합니다.</summary>
    public sealed class OnlineGameClient_V2 : IOnlineGameClient_V2
    {
        private readonly TcpGameClient_V2 _tcpClient;
        private CancellationTokenSource? _connectCts;
        private bool _disposed;

        // TCP 연결만으로는 온라인 세션이 준비된 것이 아닙니다.
        public bool IsConnected
            => State == ConnectionState.Connected &&
            _tcpClient.IsConnected;

        public ConnectionState State { get; private set; }
            = ConnectionState.Disconnected;
        public ProtocolHandshakeResponse? HandshakeResponse { get; private set; }
        public Exception? Failure { get; private set; }
        public event Action? OnDataChanged;

        public OnlineGameClient_V2(string host, int port)
        {
            _tcpClient = new TcpGameClient_V2(host, port);
        }

        public async UniTask ConnectAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (_connectCts is not null || State != ConnectionState.Disconnected)
                throw new InvalidOperationException("이미 연결 중이거나 서버에 연결되어 있습니다.");

            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _connectCts = connectCts;
            var token = connectCts.Token;
            HandshakeResponse = null;
            Failure = null;

            try
            {
                token.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Connecting);

                await _tcpClient.ConnectAsync(token);
                token.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Handshaking);

                await HandshakeAsync(token);
                token.ThrowIfCancellationRequested();
                ChangeState(ConnectionState.Connected);
                // 현재 범위는 핸드셰이크까지이며 이후 메시지 수신은 시작하지 않습니다.
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                _tcpClient.Disconnect();
                HandshakeResponse = null;
                Failure = null;
                ChangeState(ConnectionState.Disconnected);
                throw new OperationCanceledException(token);
            }
            catch (Exception exception)
            {
                _tcpClient.Disconnect();
                Failure = exception;
                ChangeState(ConnectionState.Disconnected);
                throw;
            }
            finally
            {
                _connectCts = null;
            }
        }

        private async UniTask HandshakeAsync(CancellationToken cancellationToken)
        {
            var request = new ProtocolHandshakeRequest
            {
                YuJanggiProtocolVersion = ProtocolVersion.Current,
                YuJanggiCoreVersion = CoreVersion.Current
            };
            ClientMessage message = ClientMessageFactory.Create(
                ClientMessageType.ProtocolHandshake, request);
            await _tcpClient.SendAsync(message, cancellationToken);


            ServerMessage response = await _tcpClient.ReceiveAsync(cancellationToken);

            if (response.Type != ServerMessageType.ProtocolHandshake)
                throw new InvalidOperationException($"잘못된 핸드셰이크 응답입니다: {response.Type}");

            if (response.RequestId != message.RequestId)
                throw new InvalidOperationException("핸드셰이크 RequestId가 일치하지 않습니다.");

            HandshakeResponse = response.GetPayload<ProtocolHandshakeResponse>();

            if (HandshakeResponse.Result != ProtocolHandshakeResult.Success)
                throw new InvalidOperationException($"핸드셰이크 실패: {HandshakeResponse.Result}");
        }

        public void Disconnect()
        {
            _connectCts?.Cancel();
            _tcpClient.Disconnect();
            HandshakeResponse = null;
            Failure = null;
            ChangeState(ConnectionState.Disconnected);
        }

        private void ChangeState(ConnectionState state)
        {
            State = state;
            OnDataChanged?.Invoke();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(OnlineGameClient_V2));
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            Disconnect();
            _tcpClient.Dispose();
        }
    }
}
