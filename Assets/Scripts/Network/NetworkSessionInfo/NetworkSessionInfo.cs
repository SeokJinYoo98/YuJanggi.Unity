namespace YuJanggi.Network
{
    using Engine.Domain;
    public struct NetworkSessionInfo
    {
        public string MatchId { get; }
        public PlayerTeam Team { get; }
        public string OpponentId { get; }
        public string OpponentNickname { get; }

        public NetworkSessionInfo(
            string matchId,
            PlayerTeam team,
            string opponentId,
            string opponentNickname)
        {
            MatchId = matchId;
            Team = team;
            OpponentId = opponentId;
            OpponentNickname = opponentNickname;
        }
    }
}
