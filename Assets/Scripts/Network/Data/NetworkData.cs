namespace YuJanggi.Network
{
    using Engine.Domain;
    public struct NetworkMatchingData
    {
        public string       MatchId;
        public PlayerTeam   MyTeam;
        public string       OpponentPlayerId;
        public string       OpponentNickname;
        public PlayerTeam   OpponentTeam;
    }
    public struct NetworkFormationData
    {
        public string    MatchId;
        public Formation Cho;
        public Formation Han;
    }
}
