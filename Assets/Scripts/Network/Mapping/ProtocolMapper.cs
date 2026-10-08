using System;
using System.IO;

namespace YuJanggi.Network.Mapper
{
    using Engine.Domain;
    using Protocol.Matching;

    internal static class ProtocolMapper
    {
        public static PlayerTeam ToPlayerTeam(ProtocolPlayerTeam team)
        {
            return team switch
            {
                ProtocolPlayerTeam.Cho => PlayerTeam.Cho,
                ProtocolPlayerTeam.Han => PlayerTeam.Han,
                _ => throw new InvalidDataException(
                    $"유효하지 않은 Protocol 팀입니다: {team}")
            };
        }

        public static ProtocolPlayerTeam ToProtocolPlayerTeam(PlayerTeam team)
        {
            return team switch
            {
                PlayerTeam.Cho => ProtocolPlayerTeam.Cho,
                PlayerTeam.Han => ProtocolPlayerTeam.Han,
                _ => throw new ArgumentOutOfRangeException(nameof(team), team, null)
            };
        }

        public static Formation ToFormation(ProtocolFormation formation)
        {
            return formation switch
            {
                ProtocolFormation.HEHE => Formation.HEHE,
                ProtocolFormation.EHEH => Formation.EHEH,
                ProtocolFormation.EHHE => Formation.EHHE,
                ProtocolFormation.HEEH => Formation.HEEH,
                _ => throw new InvalidDataException(
                    $"유효하지 않은 Protocol 포진입니다: {formation}")
            };
        }

        public static ProtocolFormation ToProtocolFormation(Formation formation)
        {
            return formation switch
            {
                Formation.HEHE => ProtocolFormation.HEHE,
                Formation.EHEH => ProtocolFormation.EHEH,
                Formation.EHHE => ProtocolFormation.EHHE,
                Formation.HEEH => ProtocolFormation.HEEH,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(formation), formation, null)
            };
        }
    }
}
