

using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Data.AI;

namespace YuJanggi.InGame.Controller
{
    using Runtime.Input;
    using YuJanggi.Controller;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.InGame.Handler;

    internal static class InGameControllerFactory
    {
        public static IInGameController CreateLocalController(
                PlayerTeam team,
                IControllerQuery query,
                IInputHandler inputHandler)
            => new InGameLocalController(team, query, inputHandler);
        public static IInGameController CreateAIController(
                PlayerTeam team,
                IControllerQuery query)
            => new InGameAIController(
                team,
                query,
                AISessionSettings.Strategy);
        public static IInGameController CreateNetworkController(
                PlayerTeam team,
                IControllerQuery query,
                IInputHandler inputHandler,
                InGameHandler handler)
            => new InGameNetworkController(
                team,
                query,
                inputHandler,
                handler);

        public static IInGameController CreateRemoteController(
                PlayerTeam team,
                IControllerQuery query,
                InGameHandler handler)
            => new InGameRemoteController(
                team,
                query,
                handler);

    }
}


