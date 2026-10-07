#nullable enable
using YuJanggi.Engine.JanggiOption;
using YuJanggi.InGame.Controller;

using MatchInfo = YuJanggi.Lobby.Network.MatchInfo;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class LobbyGameStartContext
    {
        public JanggiOptions Options { get; }
        public AIMoveStrategyType? AIStrategy { get; }
        public MatchInfo? NetworkMatch { get; }

        public LobbyGameStartContext(JanggiOptions options,
            AIMoveStrategyType? aiStrategy = null, MatchInfo? networkMatch = null)
        {
            Options = options;
            AIStrategy = aiStrategy;
            NetworkMatch = networkMatch;
        }
    }

    internal static class LobbyOptionValues
    {
        internal static int TurnTime(int dropdownValue)
            => dropdownValue switch
            {
                0 => 0, 1 => 10, 2 => 20, 3 => 30,
                4 => 40, 5 => 50, 6 => 60, _ => 30
            };
    }
}
