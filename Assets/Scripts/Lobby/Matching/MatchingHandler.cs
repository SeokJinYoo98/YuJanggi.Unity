#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Threading;
using YuJanggi.Core.V2.Domain;
using YuJanggi.Network;
using YuJanggi.Protocol.V2.Matching;
using YuJanggi.Protocol.V2.Messages;

namespace YuJanggi.Lobby.Matching
{
    /// <summary>매칭 요청·응답과 서버 이벤트를 해석하여 로컬 매칭 상태에 전달합니다.</summary>
    public sealed class MatchingHandler : IDisposable
    {
        private readonly NetworkConnection _connection;
        private readonly RequestDispatcher _requests;
        private readonly MatchingService _service;
        private MatchingFound? _deferredMatchingFound;
        private GameReady? _deferredGameReady;
        private bool _disposed;

        private readonly Func<string?> _getMatchId;
        public MatchingState State => _service.State;
        public bool IsFormationSubmitting => _service.IsFormationSubmitting;
        public bool IsFormationSubmitted => _service.SubmittedFormation.HasValue;
        public event Action<MatchInfo>? MatchFound;
        public event Action<string, Formation, Formation>? GameReadyReceived;

        public event Action? OnDataChanged
        {
            add => _service.OnDataChanged += value;
            remove => _service.OnDataChanged -= value;
        }

        public MatchingHandler(NetworkConnection connection, RequestDispatcher requests, Func<string?> getMatchId)
        {
            _connection = connection;
            _requests = requests;
            _service = new MatchingService();
            _getMatchId = getMatchId;
            _connection.MessageReceived += HandleMessage;
            _connection.ConnectionClosed += HandleConnectionClosed;
        }

        public async UniTask MatchRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = _connection.Version;
            _service.BeginRequest();
            try
            {
                _connection.EnsureCurrentConnection(version);
                var message = await _requests.SendAsync(ClientMessageType.MatchingRequest,
                    new MatchingRequest(), ServerMessageType.MatchingResponse, cancellationToken);
                _connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingResponse>().Result != MatchingResult.Accepted)
                    return;

                _service.MatchingAccepted();
                // Accepted로 상태를 바꾼 후 미리 수신한 이벤트만 적용합니다. 이벤트를 기다리지 않습니다.
                if (version == _connection.Version && _deferredMatchingFound is not null)
                    ApplyMatchingFound(_deferredMatchingFound);
            }
            finally
            {
                if (version == _connection.Version)
                {
                    _deferredMatchingFound = null;
                    _service.EndRequest();
                }
            }
        }

        public async UniTask MatchCancelRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = _connection.Version;
            _service.BeginCancel();
            try
            {
                var message = await _requests.SendAsync(ClientMessageType.MatchingCancelRequest,
                    new MatchingCancelRequest(), ServerMessageType.MatchingCancelResponse, cancellationToken);
                _connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingCancelResponse>().Result == MatchingCancelResult.Cancelled)
                    _service.MatchingCancelled();
            }
            finally
            {
                if (version == _connection.Version)
                    _service.EndCancel();
            }
        }

        /// <summary>선택한 포진의 접수 성공 여부를 반환합니다. 게임 시작을 기다리지 않습니다.</summary>
        public async UniTask<bool> SubmitFormationAsync(Formation formation, CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            var payload = new FormationSubmitRequest { Formation = ToProtocolFormation(formation) };
            int version = _connection.Version;
            _service.BeginFormationSubmit(formation);
            try
            {
                var message = await _requests.SendAsync(ClientMessageType.FormationSubmit,
                    payload, ServerMessageType.FormationSubmitResponse, cancellationToken);
                _connection.EnsureCurrentConnection(version);
                var response = message.GetPayload<FormationSubmitResponse>();
                if (response.Result != FormationSubmitResult.Accepted)
                    return false;
                _service.FormationAccepted();
                if (_deferredGameReady is not null)
                    ApplyGameReady(_deferredGameReady);
                return true;
                // TODO:
                // 이전 제출의 응답을 놓친 뒤 재제출하면 AlreadySubmitted를 받을 수 있습니다.
                // 현재 성공으로 간주하지 않으므로 로컬 SubmittedFormation은 비어 있을 수 있습니다.
                // MatchSession에서 서버가 접수한 포진 조회 및 재시도 정책을 결정해야 합니다.
            }
            finally
            {
                if (version == _connection.Version)
                {
                    _deferredGameReady = null;
                    _service.EndFormationSubmit();
                }
            }
        }

        private void HandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
                return;
            if (message.Type == ServerMessageType.GameReady)
            {
                HandleGameReady(message);
                return;
            }
            if (message.Type != ServerMessageType.MatchingFound)
                return;
            var found = message.GetPayload<MatchingFound>();
            if (_getMatchId() == found.MatchId)
                return;
            if (_service.State == MatchingState.Requesting)
            {
                _deferredMatchingFound ??= found;
                return;
            }
            ApplyMatchingFound(found);
        }

        private void HandleGameReady(ServerMessage message)
        {
            var ready = message.GetPayload<GameReady>();
            if (_service.HasDeliveredGameReady || _service.State != MatchingState.Matched ||
                _getMatchId() != ready.MatchId)
                return;
            // enum 필드 누락을 기본 포진(HEHE)으로 취급하지 않습니다.
            if (!message.Payload!.Value.TryGetProperty(nameof(GameReady.ChoFormation), out _) ||
                !message.Payload.Value.TryGetProperty(nameof(GameReady.HanFormation), out _))
                throw new InvalidOperationException("게임 준비 포진이 누락되었습니다.");
            if (!_service.SubmittedFormation.HasValue && _service.IsFormationSubmitting)
            {
                // 응답 continuation보다 이벤트 처리가 앞서도 Accepted 확인 후 적용합니다.
                _deferredGameReady ??= ready;
                return;
            }
            if (_service.SubmittedFormation.HasValue)
                ApplyGameReady(ready);
        }

        private void ApplyGameReady(GameReady ready)
        {
            if (ready.MatchId != _getMatchId())
                return;
            var cho = ToCoreFormation(ready.ChoFormation);
            var han = ToCoreFormation(ready.HanFormation);
            if (_service.TryAcceptGameReady())
                GameReadyReceived?.Invoke(ready.MatchId, cho, han);
        }

        private static Formation ToCoreFormation(ProtocolFormation formation) => formation switch
        {
            ProtocolFormation.HEHE => Formation.HEHE,
            ProtocolFormation.EHEH => Formation.EHEH,
            ProtocolFormation.EHHE => Formation.EHHE,
            ProtocolFormation.HEEH => Formation.HEEH,
            _ => throw new InvalidOperationException($"잘못된 게임 준비 포진입니다: {formation}")
        };

        private void ApplyMatchingFound(MatchingFound found)
        {
            // TODO:
            // 취소 완료 뒤 늦은 이벤트나 이미 매칭된 상태에서 다른 MatchId가 도착할 수 있습니다.
            // 현재는 Matching 이외의 이벤트를 무시해 기존 매칭 상태를 보존합니다.
            // MatchSession / GameSession에서 서버 상태 재동기화와 확정 우선순위를 결정해야 합니다.
            if (_service.State != MatchingState.Matching)
                return;
            if (found.Opponent is null)
                throw new InvalidOperationException("매칭 상대 정보가 없습니다.");
            var match = new MatchInfo(found.MatchId, ToPlayerTeam(found.MyTeam),
                found.Opponent.PlayerId, found.Opponent.PlayerNickname, ToPlayerTeam(found.Opponent.PlayerTeam));
            if (string.IsNullOrWhiteSpace(match.MatchId) || match.Team == match.OpponentTeam)
                throw new InvalidOperationException("잘못된 매칭 정보입니다.");
            // 세션 소유자가 저장한 뒤 Matched 알림을 발생시켜 UI가 새 세션을 조회하게 합니다.
            int version = _connection.Version;
            MatchFound?.Invoke(match);
            if (version == _connection.Version)
                _service.MatchingFound();
        }

        private static PlayerTeam ToPlayerTeam(ProtocolPlayerTeam team) => team switch
        {
            ProtocolPlayerTeam.Cho => PlayerTeam.Cho,
            ProtocolPlayerTeam.Han => PlayerTeam.Han,
            _ => throw new InvalidOperationException($"잘못된 매칭 진영입니다: {team}")
        };

        private static ProtocolFormation ToProtocolFormation(Formation formation) => formation switch
        {
            Formation.HEHE => ProtocolFormation.HEHE,
            Formation.EHEH => ProtocolFormation.EHEH,
            Formation.EHHE => ProtocolFormation.EHHE,
            Formation.HEEH => ProtocolFormation.HEEH,
            _ => throw new ArgumentOutOfRangeException(nameof(formation))
        };

        private void HandleConnectionClosed()
        {
            _deferredMatchingFound = null;
            _deferredGameReady = null;
            _service.Reset();
        }

        private void EnsureConnected(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(MatchingHandler));
            cancellationToken.ThrowIfCancellationRequested();
            _connection.EnsureOnline();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _connection.MessageReceived -= HandleMessage;
            _connection.ConnectionClosed -= HandleConnectionClosed;
            HandleConnectionClosed();
        }
    }
}
