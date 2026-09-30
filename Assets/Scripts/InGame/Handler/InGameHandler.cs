#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;



namespace YuJanggi.InGame.Handler
{
    using Network;
    using Protocol.InGame;
    using Protocol.Matching;
    using Protocol.Messages;
    using Service;
    using YuJanggi.Engine.Domain;
    using YuJanggi.Network.Handler;

    /// <summary>인게임 서버 이벤트의 검증·해석 경계입니다. 게임 상태나 화면은 소유하지 않습니다.</summary>
    internal sealed class InGameHandler : NetworkHandler
    {
        #region Fields
        private readonly InGameService _service;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트
        public event Action<PlayerTeam, Pos, Pos>? MoveConfirmed;
        #endregion

        #region Constructors
        // 순수 C#
        public InGameHandler(
            NetworkConnection connection,
            RequestDispatcher requests)
            : base(connection, requests)
        {
            _service = new InGameService();
            Connection.ConnectionClosed += HandleConnectionClosed;
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

        public UniTask SendGameSceneReadyAsync(
            CancellationToken cancellationToken = default)
        {
            // 대응 Response 계약이 없으므로 RequestDispatcher의 응답 대기를 등록하지 않습니다.
            return SendAsync(
                ClientMessageType.GameSceneReady,
                new GameSceneReady(),
                cancellationToken);
        }

        public UniTask SendMoveAsync(
            PlayerTeam team, Pos from, Pos to,
            CancellationToken cancellationToken = default)
        {
            if (!IsBoardPosition(from) || !IsBoardPosition(to))
                throw new ArgumentOutOfRangeException(nameof(from), "잘못된 이동 좌표입니다.");

            return SendAsync(
                ClientMessageType.MovePieceRequest,
                new MovePieceRequest
                {
                    Team = ToProtocolTeam(team),
                    FromX = (byte)from.X,
                    FromZ = (byte)from.Z,
                    ToX = (byte)to.X,
                    ToZ = (byte)to.Z
                },
                cancellationToken);
        }
        #endregion

        #region Protected Methods
        protected override void OnHandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
                return;

            switch (message.Type)
            {
                case ServerMessageType.GameStartEvent:
                    HandleGameStart(message);
                    break;
                case ServerMessageType.MovePieceEvent:
                    HandleMovePiece(message);
                    break;
            }
        }

        protected override void OnDispose()
        {
            Connection.ConnectionClosed -= HandleConnectionClosed;
            _service.Reset();
        }
        #endregion

        #region Event Handlers
        private void HandleConnectionClosed() => _service.Reset();
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직


        private static bool IsBoardPosition(Pos pos)
            => pos.X is >= 0 and <= 8 && pos.Z is >= 0 and <= 9;

        private static ProtocolPlayerTeam ToProtocolTeam(PlayerTeam team)
            => team switch
            {
                PlayerTeam.Cho => ProtocolPlayerTeam.Cho,
                PlayerTeam.Han => ProtocolPlayerTeam.Han,
                _ => throw new ArgumentOutOfRangeException(nameof(team))
            };

        private static PlayerTeam ToPlayerTeam(ProtocolPlayerTeam team)
            => team switch
            {
                ProtocolPlayerTeam.Cho => PlayerTeam.Cho,
                ProtocolPlayerTeam.Han => PlayerTeam.Han,
                _ => throw new InvalidDataException("잘못된 이동 진영입니다.")
            };

        private void HandleMovePiece(ServerMessage message)
        {
            if (message.Payload is not { ValueKind: JsonValueKind.Object })
                throw new InvalidDataException("MovePieceEvent Payload가 올바르지 않습니다.");

            var payload = message.Payload.Value;
            if (
                !payload.TryGetProperty(nameof(MovePieceEvent.Team), out _) ||
                !payload.TryGetProperty(nameof(MovePieceEvent.FromX), out _) ||
                !payload.TryGetProperty(nameof(MovePieceEvent.FromZ), out _) ||
                !payload.TryGetProperty(nameof(MovePieceEvent.ToX), out _) ||
                !payload.TryGetProperty(nameof(MovePieceEvent.ToZ), out _))
                throw new InvalidDataException("MovePieceEvent Payload가 올바르지 않습니다.");

            var move = message.GetPayload<MovePieceEvent>();
            var team = ToPlayerTeam(move.Team);
            var from = new Pos(move.FromX, move.FromZ);
            var to = new Pos(move.ToX, move.ToZ);
            if (!IsBoardPosition(from) || !IsBoardPosition(to))
                throw new InvalidDataException("MovePieceEvent 좌표가 올바르지 않습니다.");

            MoveConfirmed?.Invoke(team, from, to);
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

        #endregion
    }
}


