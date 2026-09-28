

using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Data.AI;

namespace YuJanggi.InGame.Controller
{
    using Runtime.Input;
    using YuJanggi.BootStrap;
    using YuJanggi.Controller;
    using YuJanggi.Engine.JanggiEngine;
    using YuJanggi.Engine.JanggiOption;
    using YuJanggi.InGame.Handler;

    internal static class InGameControllerFactory
    {
        internal static IInGameController CreateController(
            PlayerType type,
            PlayerTeam team,
            IControllerQuery query,
            IInputHandler inputHandler)
              => type switch
              {
                  PlayerType.Local
                  => CreateLocalController(
                      team, query, inputHandler),

                  PlayerType.AI => CreateAIController(
                      team, query),

                  PlayerType.Network => CreateNetworkController(
                      team, query, inputHandler,
                      YuJanggiBootStrap.Instance.NetworkManager.InGame),

                  PlayerType.Remote => CreateRemoteController(
                      team, query,
                      YuJanggiBootStrap.Instance.NetworkManager.InGame),

                  _ => throw new ArgumentOutOfRangeException(
                      nameof(type), type, null)
              };

        private static IInGameController CreateLocalController(
                PlayerTeam team,
                IControllerQuery query,
                IInputHandler inputHandler)
            => new InGameLocalController(team, query, inputHandler);
        private static IInGameController CreateAIController(
                PlayerTeam team,
                IControllerQuery query)
            => new InGameAIController(
                team,
                query,
                AISessionSettings.Strategy);
        private static IInGameController CreateNetworkController(
                PlayerTeam team,
                IControllerQuery query,
                IInputHandler inputHandler,
                InGameHandler networkHandler)
            => new InGameNetworkController(
                team,
                query,
                inputHandler,
                networkHandler);

        private static IInGameController CreateRemoteController(
                PlayerTeam team,
                IControllerQuery query,
                InGameHandler networkHandler)
            => new InGameRemoteController(
                team,
                query,
                networkHandler);

    }
}


