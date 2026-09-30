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
using YuJanggi.Store;

namespace YuJanggi.Lobby.Network
{
    internal interface ILobbyNetwork
    {
        MatchingState State { get; }
        bool IsFormationSubmitting { get; }
        bool IsFormationSubmitted { get; }
        bool IsGameReady { get; }
        event Action<MatchInfo>? MatchFound;
        event Action<string, Formation, Formation>? GameReadyReceived;
        event Action? OnDataChanged;
        bool TryGetReadyFormations(out Formation cho, out Formation han);
        UniTask MatchingStartRequestAsync(CancellationToken cancellationToken = default);
        UniTask MatchingCancelRequestAsync(CancellationToken cancellationToken = default);
        UniTask SubmitFormationAsync(Formation formation, CancellationToken cancellationToken = default);
    }

    internal sealed class LobbyNetworkHandler : NetworkHandler, ILobbyNetwork
    {
        private readonly LobbyNetworkService _service = new();
        private readonly Func<string?> _getMatchId;
        private MatchingFoundEvent? _deferredMatchingFound;
        private GameReadyEvent? _deferredGameReady;
        private (Formation Cho, Formation Han)? _readyFormations;

        public MatchingState State => _service.State;
        public bool IsFormationSubmitting => _service.IsFormationSubmitting;
        public bool IsFormationSubmitted => _service.SubmittedFormation.HasValue;
        public bool IsGameReady => _service.HasDeliveredGameReady;
        public event Action<MatchInfo>? MatchFound;
        public event Action<string, Formation, Formation>? GameReadyReceived;
        public event Action? OnDataChanged
        {
            add => _service.OnDataChanged += value;
            remove => _service.OnDataChanged -= value;
        }

        public LobbyNetworkHandler(NetworkConnection connection, RequestDispatcher requests,
            Func<string?> getMatchId) : base(connection, requests)
        {
            _getMatchId = getMatchId;
            Connection.ConnectionClosed += HandleConnectionClosed;
        }

        public async UniTask MatchingStartRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = Connection.Version;
            _service.BeginRequest();
            try
            {
                Connection.EnsureCurrentConnection(version);
                var message = await SendRequestAsync(ClientMessageType.MatchingStartRequest,
                    new MatchingStartRequest(), ServerMessageType.MatchingStartResponse, cancellationToken);
                Connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingStartResponse>().Result != MatchingResult.Accepted)
                    return;
                _service.MatchingAccepted();
                if (version == Connection.Version && _deferredMatchingFound is not null)
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

        public async UniTask MatchingCancelRequestAsync(CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            int version = Connection.Version;
            _service.BeginCancel();
            try
            {
                var message = await SendRequestAsync(ClientMessageType.MatchingCancelRequest,
                    new MatchingCancelRequest(), ServerMessageType.MatchingCancelResponse, cancellationToken);
                Connection.EnsureCurrentConnection(version);
                if (message.GetPayload<MatchingCancelResponse>().Result == MatchingCancelResult.Cancelled)
                    _service.MatchingCancelled();
            }
            finally
            {
                if (version == Connection.Version)
                    _service.EndCancel();
            }
        }

        public async UniTask SubmitFormationAsync(Formation formation,
            CancellationToken cancellationToken = default)
        {
            EnsureConnected(cancellationToken);
            string? matchId = _getMatchId();
            if (string.IsNullOrWhiteSpace(matchId))
                throw new InvalidOperationException("포진을 제출할 매칭 정보가 없습니다.");
            var payload = new FormationSubmit
            {
                MatchId = matchId,
                Formation = ToProtocolFormation(formation)
            };
            int version = Connection.Version;
            _service.BeginFormationSubmit(formation);
            try
            {
                await SendAsync(ClientMessageType.FormationSubmit, payload, cancellationToken);
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

        protected override void OnHandleMessage(ServerMessage message)
        {
            if (message.RequestId is not null)
                return;
            switch (message.Type)
            {
                case ServerMessageType.MatchingFoundEvent:
                    var found = message.GetPayload<MatchingFoundEvent>();
                    if (_getMatchId() == found.MatchId)
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

        private void HandleGameReady(ServerMessage message)
        {
            var ready = message.GetPayload<GameReadyEvent>();
            if (_service.HasDeliveredGameReady || _service.State != MatchingState.Matched ||
                _getMatchId() != ready.MatchId)
                return;
            if (!message.Payload!.Value.TryGetProperty(nameof(GameReadyEvent.ChoFormation), out _) ||
                !message.Payload.Value.TryGetProperty(nameof(GameReadyEvent.HanFormation), out _))
                throw new InvalidOperationException("게임 준비 포진이 누락되었습니다.");
            if (!_service.SubmittedFormation.HasValue && _service.IsFormationSubmitting)
            {
                _deferredGameReady ??= ready;
                return;
            }
            if (_service.SubmittedFormation.HasValue)
                ApplyGameReady(ready);
        }

        private void ApplyGameReady(GameReadyEvent ready)
        {
            if (ready.MatchId != _getMatchId())
                return;
            var cho = ToCoreFormation(ready.ChoFormation);
            var han = ToCoreFormation(ready.HanFormation);
            var match = NetworkMatchInfoStore.Current;
            if (match is null || match.MatchId != ready.MatchId ||
                match.Team is not (PlayerTeam.Cho or PlayerTeam.Han))
                return;
            if (!_service.TryAcceptGameReady())
                return;
            _readyFormations = (cho, han);
            GameReadyReceived?.Invoke(ready.MatchId, cho, han);
        }

        public bool TryGetReadyFormations(out Formation cho, out Formation han)
        {
            cho = default;
            han = default;
            if (!IsGameReady || !_readyFormations.HasValue)
                return false;
            (cho, han) = _readyFormations.Value;
            return true;
        }

        private void ApplyMatchingFound(MatchingFoundEvent found)
        {
            if (_service.State != MatchingState.Matching)
                return;
            if (found.Opponent is null)
                throw new InvalidOperationException("매칭 상대 정보가 없습니다.");
            var match = new MatchInfo(found.MatchId, ToPlayerTeam(found.MyTeam),
                found.Opponent.PlayerId, found.Opponent.PlayerNickname,
                ToPlayerTeam(found.Opponent.PlayerTeam));
            if (string.IsNullOrWhiteSpace(match.MatchId) || match.Team == match.OpponentTeam)
                throw new InvalidOperationException("잘못된 매칭 정보입니다.");
            int version = Connection.Version;
            _readyFormations = null;
            JanggiOptionStore.ClearNetworkOptions();
            MatchFound?.Invoke(match);
            if (version == Connection.Version)
                _service.MatchingFound();
        }

        private static Formation ToCoreFormation(ProtocolFormation formation) => formation switch
        {
            ProtocolFormation.HEHE => Formation.HEHE,
            ProtocolFormation.EHEH => Formation.EHEH,
            ProtocolFormation.EHHE => Formation.EHHE,
            ProtocolFormation.HEEH => Formation.HEEH,
            _ => throw new InvalidOperationException($"잘못된 게임 준비 포진입니다: {formation}")
        };

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
            _readyFormations = null;
            JanggiOptionStore.ClearNetworkOptions();
            _service.Reset();
        }

        protected override void OnDispose()
        {
            Connection.ConnectionClosed -= HandleConnectionClosed;
            HandleConnectionClosed();
        }
    }
}
