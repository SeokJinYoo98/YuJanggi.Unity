#nullable enable
using YuJanggi.Engine.JanggiOption;
using YuJanggi.InGame.Controller;
using YuJanggi.Lobby.Matching;

namespace YuJanggi.Lobby.Flow
{
    // Flow가 확정한 시작 데이터. Store 저장과 씬 이동은 LobbyManager가 수행한다.
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
