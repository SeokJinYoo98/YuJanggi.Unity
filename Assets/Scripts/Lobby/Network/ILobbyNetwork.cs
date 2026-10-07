#nullable enable

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Engine.Domain;
using YuJanggi.Protocol.Matching;

namespace YuJanggi.Lobby.Network
{
    internal interface ILobbyNetwork
    {
        MatchingState State { get; }
        MatchInfo? Match { get; }

        bool IsFormationSubmitting { get; }
        bool IsFormationSubmitted { get; }
        bool IsGameReady { get; }

        event Action<MatchInfo>? MatchFound;
        event Action<string, Formation, Formation>? GameReadyReceived;
        event Action? OnDataChanged;

        bool TryGetReadyFormations(
            out Formation cho,
            out Formation han);

        UniTask MatchingStartRequestAsync(
            CancellationToken cancellationToken = default);
        UniTask MatchingCancelRequestAsync(
            CancellationToken cancellationToken = default);
        UniTask SubmitFormationAsync(
            Formation formation,
            CancellationToken cancellationToken = default);
    }

    internal static class LobbyProtocolMapper
    {
        internal static Formation ToCoreFormation(ProtocolFormation formation)
            => formation switch
            {
                ProtocolFormation.HEHE => Formation.HEHE,
                ProtocolFormation.EHEH => Formation.EHEH,
                ProtocolFormation.EHHE => Formation.EHHE,
                ProtocolFormation.HEEH => Formation.HEEH,
                _ => throw new InvalidOperationException($"잘못된 게임 준비 포진입니다: {formation}")
            };

        internal static PlayerTeam ToPlayerTeam(ProtocolPlayerTeam team)
            => team switch
            {
                ProtocolPlayerTeam.Cho => PlayerTeam.Cho,
                ProtocolPlayerTeam.Han => PlayerTeam.Han,
                _ => throw new InvalidOperationException($"잘못된 매칭 진영입니다: {team}")
            };

        internal static ProtocolFormation ToProtocolFormation(Formation formation)
            => formation switch
            {
                Formation.HEHE => ProtocolFormation.HEHE,
                Formation.EHEH => ProtocolFormation.EHEH,
                Formation.EHHE => ProtocolFormation.EHHE,
                Formation.HEEH => ProtocolFormation.HEEH,
                _ => throw new ArgumentOutOfRangeException(nameof(formation))
            };
    }
}
