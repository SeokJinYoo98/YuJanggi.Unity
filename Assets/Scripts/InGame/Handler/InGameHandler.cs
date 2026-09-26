#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;



namespace YuJanggi.InGame.Handler
{
    using Protocol.V2.InGame;
    using Protocol.V2.Messages;
    using Protocol.V2.Messages.MessageFactory;
    using Network;
    using Service;

    /// <summary>인게임 서버 이벤트의 검증·해석 경계입니다. 게임 상태나 화면은 소유하지 않습니다.</summary>
    public sealed class InGameHandler : IDisposable
    {
        private readonly NetworkConnection _connection;
        private readonly RequestDispatcher _requests;
        private readonly InGameService _service;
        private bool _disposed;

        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트
        #endregion

        #region Constructors
        // 순수 C#
        public InGameHandler(
            NetworkConnection connection,
            RequestDispatcher requests)
        {
            _connection = connection;
            _requests = requests;
            _service = new InGameService();
            _connection.MessageReceived += HandleMessage;
            _connection.ConnectionClosed += HandleConnectionClosed;
        }
        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        public UniTask WaitUntilGameStartedAsync(
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            return _service.WaitUntilGameStartedAsync(cancellationToken);
        }

        public async UniTask SendGameSceneReadyAsync(
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = _connection.Version;
            using var requestCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _connection.LifetimeToken);
            // 대응 Response 계약이 없으므로 RequestDispatcher의 응답 대기를 등록하지 않습니다.
            var payload = new GameSceneReadyRequest();

            var message = ClientMessageFactory.Create(
                ClientMessageType.GameSceneReadyRequest,
                payload);

            await _connection.SendAsync(
                message,
                requestCts.Token);
            requestCts.Token.ThrowIfCancellationRequested();
            _connection.EnsureCurrentConnection(version);
        }
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _connection.MessageReceived -= HandleMessage;
            _connection.ConnectionClosed -= HandleConnectionClosed;
            _service.Reset();
        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        private void HandleMessage(ServerMessage message)
        {
            if (_disposed || message.RequestId is not null)
                return;

            switch (message.Type)
            {
                case ServerMessageType.GameStartEvent:
                    HandleGameStart(message);
                    break;
                default:
                    break;
            }

            // TODO:
            // MoveApplied / TurnChanged / GameEnded 계약이 추가되면 이곳에서 각각 분기합니다.
            // 현재는 해당 이벤트를 처리하지 않으며, 결과 반영은 InGameSession 등 인게임 계층에 연결해야 합니다.
            // 향후 요청 송신은 _requests를 사용하고 응답 대기는 RequestDispatcher에 맡깁니다.
        }
        private void HandleGameStart(ServerMessage message)
        {
            if (_service.IsGameStarted)
                return;
            if (message.Payload is not { ValueKind: JsonValueKind.Object })
                throw new InvalidDataException("GameStart Payload는 JSON 객체여야 합니다.");

            var started = message.GetPayload<GameStartEvent>();
            if (!message.Payload.Value.TryGetProperty(nameof(GameStartEvent.StartedAt), out _) ||
                started.StartedAt == default)
                throw new InvalidDataException("GameStart의 StartedAt이 없거나 잘못되었습니다.");
            _service.ApplyGameStart();
        }

        private void HandleConnectionClosed() => _service.Reset();

        private void EnsureConnected(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(InGameHandler));
            cancellationToken.ThrowIfCancellationRequested();
            _connection.EnsureOnline();
        }
        #endregion
    }
}
