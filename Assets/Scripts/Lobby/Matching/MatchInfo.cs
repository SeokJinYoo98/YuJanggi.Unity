#nullable enable
using YuJanggi.Core.V2.Domain;

namespace YuJanggi.Lobby.Matching
{
    public enum MatchingState { Idle, Requesting, Matching, Matched }

    /// <summary>프로토콜 DTO와 분리된 현재 매칭 정보입니다.</summary>
    public sealed class MatchInfo
    {
        public string MatchId { get; }
        public PlayerTeam Team { get; }
        public string OpponentPlayerId { get; }
        public string OpponentNickname { get; }
        public PlayerTeam OpponentTeam { get; }

        public MatchInfo(string matchId, PlayerTeam team, string opponentPlayerId,
            string opponentNickname, PlayerTeam opponentTeam)
        {
            MatchId = matchId;
            Team = team;
            OpponentPlayerId = opponentPlayerId;
            OpponentNickname = opponentNickname;
            OpponentTeam = opponentTeam;
        }
    }
}
