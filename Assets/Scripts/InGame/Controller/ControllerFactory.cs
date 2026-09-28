

using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Data.AI;

namespace YuJanggi.Controller
{
    public sealed class ControllerFactory
    {
        private static IPlayerController CreateController(
            PlayerType type,
            PlayerTeam team,
            IInputHandler input,
            MatchModel match)
        {
            return type switch
            {
                PlayerType.Local => new LocalController(match.Rule, match.Board, team, input),
                PlayerType.AI => new AIController(match.Rule, match.Board, team, AISessionSettings.Strategy),
                PlayerType.Network => new RemoteNetworkController(team),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static IPlayerController CreateLocalController(
            PlayerTeam team,
            IInputHandler input,
            MatchModel match)
            => new LocalController(
                match.Rule, match.Board,
                team,
                input);
        public static IPlayerController CreateAIController(
            MatchModel match,
            PlayerTeam team)
            => new AIController(
                match.Rule, match.Board,
                team,
                AISessionSettings.Strategy);

    }
}


