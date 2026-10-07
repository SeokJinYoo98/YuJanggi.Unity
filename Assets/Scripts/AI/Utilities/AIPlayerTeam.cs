namespace YuJanggi.AI.Utilities
{
    using Engine.Domain;

    internal static class AIPlayerTeam
    {
        public static PlayerTeam Opponent(PlayerTeam team)
            => team == PlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho;
    }
}
