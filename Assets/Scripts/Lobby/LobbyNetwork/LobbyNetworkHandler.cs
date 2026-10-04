#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Engine.Domain;
using YuJanggi.Lobby.Matching;
using YuJanggi.Network;
using YuJanggi.Network.Handler;
using YuJanggi.Protocol.Matching;
using YuJanggi.Protocol.Messages;

namespace YuJanggi.Lobby.Network
{

    internal sealed class LobbyNetworkHandler : NetworkHandler, ILobbyNetwork
    {
        #region Fields
        private readonly LobbyNetworkService _service = new();
        private readonly object        _requestSync = new();

        private CancellationTokenSource? _matchingRequestCts;
        private CancellationTokenSource? _deferredRequestDispose;

        private bool _cancelingRequest;

        private MatchingFoundEvent? _deferredMatchingFound;
        private GameReadyEvent?     _deferredGameReady;

        #endregion

        #region Properties
        public MatchingState State
            => _service.State;
        public MatchInfo? Match => _service.Match;
        public bool IsFormationSubmitting
            => _service.IsFormationSubmitting;
        public bool IsFormationSubmitted
            => _service.SubmittedFormation.HasValue;
        public bool IsGameReady
            => _service.HasDeliveredGameReady;
        #endregion

        #region Events
        public event Action<MatchInfo>? MatchFound;
        public event Action<string, Formation, Formation>? GameReadyReceived;
        public event Action? OnDataChanged
        {
            add => _service.OnDataChanged += value;
            remove => _service.OnDataChanged -= value;
        }
        #endregion

        #region Constructors
        public LobbyNetworkHandler(
            NetworkConnection connection,
            RequestDispatcher requests)
            : base(connection, requests)
        {
            Connection.ConnectionClosed += HandleConnectionClosed;
        }
        #endregion

        #region Public Methods
        public void ResetMatchState()
        {
            _deferredMatchingFound = null;
            _deferredGameReady = null;
            _service.ResetMatchState();
        }

        public UniTask MatchingStartRequestAsync(
            CancellationToken cancellationToken = default)
            => RunMatchingRequestAsync(MatchingStartCoreAsync, cancellationToken);

        public UniTask MatchingCancelRequestAsync(
            CancellationToken cancellationToken = default)
            => RunMatchingRequestAsync(MatchingCancelCoreAsync, cancellationToken);

        public UniTask SubmitFormationAsync(
            Formation formation,
            CancellationToken cancellationToken = default)
            => RunMatchingRequestAsync(
                token => SubmitFormationCoreAsync(formation, token),
                cancellationToken);

        public bool TryGetReadyFormations(
            out Formation cho,
            out Formation han)
            => _service.TryGetReadyFormations(out cho, out han);
        #endregion

        #region Private Methods
        private async UniTask MatchingStartCoreAsync(
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = Connection.Version;
            _service.BeginRequest();
            try
            {
                Connection.EnsureCurrentConnection(version);

                var message = await SendRequestAsync(
                    ClientMessageType.MatchingStartRequest,
                    new MatchingStartRequest(),
                    ServerMessageType.MatchingStartResponse,
                    cancellationToken);

                Connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingStartResponse>().Result
                    != MatchingResult.Accepted)
                    return;

                _service.MatchingAccepted();

                if (version == Connection.Version &&
                    _deferredMatchingFound is not null)
                    ApplyMatchingFound(_deferredMatchingFound);
            }
            finally
            {
                if (version == Connection.Version)
                {
                    _deferredMatchingFound = null;
                    _service.EndRequest();
                }
            }
        }

        private async UniTask MatchingCancelCoreAsync(
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = Connection.Version;
            _service.BeginCancel();
            try
            {
                var message = await SendRequestAsync(
                    ClientMessageType.MatchingCancelRequest,
                    new MatchingCancelRequest(),
                    ServerMessageType.MatchingCancelResponse,
                    cancellationToken);

                Connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingCancelResponse>().Result ==
                    MatchingCancelResult.Cancelled)
                    _service.MatchingCancelled();
            }
            finally
            {
                if (version == Connection.Version)
                    _service.EndCancel();
            }
        }

        private async UniTask SubmitFormationCoreAsync(Formation formation,
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            string? matchId = Match?.MatchId;

            if (string.IsNullOrWhiteSpace(matchId))
                throw new InvalidOperationException("포진을 제출할 매칭 정보가 없습니다.");

            var payload = new FormationSubmit
            {
                MatchId = matchId,
                Formation = LobbyProtocolMapper.ToProtocolFormation(formation)
            };

            int version = Connection.Version;

            _service.BeginFormationSubmit(formation);

            try
            {
                await SendAsync(
                    ClientMessageType.FormationSubmit,
                    payload,
                    cancellationToken);

                Connection.EnsureCurrentConnection(version);

                _service.FormationSent();

                if (_deferredGameReady is not null)
                    ApplyGameReady(_deferredGameReady);
            }
            finally
            {
                if (version == Connection.Version)
                {
                    _deferredGameReady = null;
                    _service.EndFormationSubmit();
                }
            }
        }

        #endregion

        #region Protected Methods
        protected override void OnHandleMessage(
            ServerMessage message)
        {
            if (message.RequestId is not null)
                return;
            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                    var found = message.GetPayload<MatchingFoundEvent>();
                    if (Match?.MatchId == found.MatchId)
                        return;

                    if (_service.State == MatchingState.Requesting)
                    {
                        _deferredMatchingFound ??= found;
                        return;
                    }

                    ApplyMatchingFound(found);
                    break;
                case ServerMessageType.GameReadyEvent:
                    HandleGameReady(message);
                    break;
            }
        }

        protected override void OnDispose()
        {
            Connection.ConnectionClosed -= HandleConnectionClosed;
            CancellationTokenSource? requestCts;
            lock (_requestSync)
            {
                requestCts = _matchingRequestCts;
                _cancelingRequest = requestCts is not null;
            }
            try
            {
                requestCts?.Cancel();
            }
            finally
            {
                CancellationTokenSource? deferred;
                lock (_requestSync)
                {
                    _cancelingRequest = false;
                    deferred = _deferredRequestDispose;
                    _deferredRequestDispose = null;
                }
                deferred?.Dispose();
            }
            HandleConnectionClosed();
        }
        #endregion

        #region Private Methods
        private async UniTask RunMatchingRequestAsync(
            Func<CancellationToken, UniTask> operation,
            CancellationToken cancellationToken)
        {
            EnsureConnected(cancellationToken);
            var requestCts = CancellationTokenSource.CreateLinkedTokenSource(
                Connection.LifetimeToken, cancellationToken);

            lock (_requestSync)
            {
                if (_matchingRequestCts is not null)
                {
                    requestCts.Dispose();
                    throw new InvalidOperationException("이미 매칭 신청 또는 취소 요청을 처리 중입니다.");
                }
                _matchingRequestCts = requestCts;
            }

            try
            {
                await operation(requestCts.Token);
            }
            finally
            {
                bool disposeNow;
                lock (_requestSync)
                {
                    if (ReferenceEquals(_matchingRequestCts, requestCts))
                        _matchingRequestCts = null;
                    disposeNow = !_cancelingRequest;
                    if (!disposeNow)
                        _deferredRequestDispose = requestCts;
                }
                if (disposeNow)
                    requestCts.Dispose();
            }
        }

        private void HandleGameReady(ServerMessage message)
        {
            var ready = message.GetPayload<GameReadyEvent>();
            if (_service.HasDeliveredGameReady ||
                _service.State != MatchingState.Matched ||
                Match?.MatchId != ready.MatchId)
                return;

            if (!message.Payload!.Value.TryGetProperty(
                nameof(GameReadyEvent.ChoFormation), out _) ||
                !message.Payload.Value.TryGetProperty(
                    nameof(GameReadyEvent.HanFormation), out _))
                throw new InvalidOperationException("게임 준비 포진이 누락되었습니다.");

            if (!_service.SubmittedFormation.HasValue &&
                _service.IsFormationSubmitting)
            {
                _deferredGameReady ??= ready;
                return;
            }

            if (_service.SubmittedFormation.HasValue)
                ApplyGameReady(ready);
        }

        private void ApplyGameReady(GameReadyEvent ready)
        {
            if (ready.MatchId != Match?.MatchId)
                return;
            var cho = LobbyProtocolMapper.ToCoreFormation(ready.ChoFormation);
            var han = LobbyProtocolMapper.ToCoreFormation(ready.HanFormation);
            var match = Match;

            if (match is null ||
                match.MatchId != ready.MatchId ||
                match.Team is not (PlayerTeam.Cho or PlayerTeam.Han))
                return;

            if (!_service.TryAcceptGameReady(cho, han))
                return;

            GameReadyReceived?.Invoke(ready.MatchId, cho, han);
        }

        private void ApplyMatchingFound(MatchingFoundEvent found)
        {
            if (_service.State != MatchingState.Matching)
                return;

            if (found.Opponent is null)
                throw new InvalidOperationException("매칭 상대 정보가 없습니다.");

            var match = new MatchInfo(
                found.MatchId,
                LobbyProtocolMapper.ToPlayerTeam(found.MyTeam),
                found.Opponent.PlayerId,
                found.Opponent.PlayerNickname,
                LobbyProtocolMapper.ToPlayerTeam(found.Opponent.PlayerTeam));

            if (string.IsNullOrWhiteSpace(match.MatchId) || match.Team == match.OpponentTeam)
                throw new InvalidOperationException("잘못된 매칭 정보입니다.");

            int version = Connection.Version;
            _service.MatchingFound(match);
            if (version == Connection.Version)
                MatchFound?.Invoke(match);
        }


        #endregion

        #region Event Handlers
        private void HandleConnectionClosed()
        {
            ResetMatchState();
        }
        #endregion
    }
}
