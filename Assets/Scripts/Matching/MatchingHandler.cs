#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Core.V2.Domain;
using YuJanggi.Network;
using YuJanggi.Protocol.V2.Matching;
using YuJanggi.Protocol.V2.Messages;

namespace YuJanggi.Matching
{
    /// <summary>매칭 요청·응답과 서버 이벤트를 해석하여 로컬 매칭 상태에 전달합니다.</summary>
    public sealed class MatchingHandler : IDisposable
    {
        private readonly NetworkConnection _connection;
        private readonly RequestDispatcher _requests;
        private readonly MatchingService _service;
        private MatchingFound? _deferredMatchingFound;
        private bool _disposed;

        public MatchingHandler(NetworkConnection connection, RequestDispatcher requests, MatchingService service)
        {
            _connection = connection;
            _requests = requests;
            _service = service;
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
            var payload = new FormationSubmit { Formation = ToProtocolFormation(formation) };
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
                return true;
                // TODO:
                // 이전 제출의 응답을 놓친 뒤 재제출하면 AlreadySubmitted를 받을 수 있습니다.
                // 현재 성공으로 간주하지 않으므로 로컬 SubmittedFormation은 비어 있을 수 있습니다.
                // MatchSession에서 서버가 접수한 포진 조회 및 재시도 정책을 결정해야 합니다.
                // 또한 RoomCreated는 양쪽에게 전달되는 시작 이벤트가 아니므로 여기서 게임을 시작하지 않습니다.
                // GameSession / GameScene은 이후 서버 준비·시작 이벤트로 실제 대국 진입을 확정해야 합니다.
            }
            finally
            {
                if (version == _connection.Version)
                    _service.EndFormationSubmit();
            }
        }

        private void HandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null || message.Type != ServerMessageType.MatchingFound)
                return;
            var found = message.GetPayload<MatchingFound>();
            if (_service.MatchId == found.MatchId)
                return;
            if (_service.State == MatchingState.Requesting)
            {
                _deferredMatchingFound ??= found;
                return;
            }
            ApplyMatchingFound(found);
        }

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
            _service.ApplyMatchFound(new MatchInfo(found.MatchId, ToPlayerTeam(found.MyTeam),
                found.Opponent.PlayerId, found.Opponent.PlayerNickname, ToPlayerTeam(found.Opponent.PlayerTeam)));
        }

        private static PlayerTeam ToPlayerTeam(ProtocolPlayerTeam team) => team switch
        {
            ProtocolPlayerTeam.Cho => PlayerTeam.Cho,
            ProtocolPlayerTeam.Han => PlayerTeam.Han,
            _ => throw new InvalidOperationException($"잘못된 매칭 진영입니다: {team}")
        };

        private static MatchingFormation ToProtocolFormation(Formation formation) => formation switch
        {
            Formation.HEHE => MatchingFormation.HEHE,
            Formation.EHEH => MatchingFormation.EHEH,
            Formation.EHHE => MatchingFormation.EHHE,
            Formation.HEEH => MatchingFormation.HEEH,
            _ => throw new ArgumentOutOfRangeException(nameof(formation))
        };

        private void HandleConnectionClosed()
        {
            _deferredMatchingFound = null;
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
