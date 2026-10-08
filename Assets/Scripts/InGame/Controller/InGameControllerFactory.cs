using System;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using YuJanggi.AI.Data;
    using YuJanggi.AI.Service;
    using YuJanggi.AI.Strategy;
    using YuJanggi.Engine.JanggiOption;

    using YuJanggi.Store;

    internal static class InGameControllerFactory
    {
        internal static IInGameController CreateController(
            PlayerType type,
            PlayerTeam team,
            IControllerQuery query,
            IAIPositionSource aiPositions,
            IInputHandler inputHandler)
              => type switch
              {
                  PlayerType.Local
                  => CreateLocalController(
                      team, query, inputHandler),

                  PlayerType.AI
                  => CreateAIController(
                      team, query, aiPositions),

                  PlayerType.Network
                  => CreateLocalController(
                      team, query, inputHandler),

                  PlayerType.Remote
                  => CreateRemoteController(
                      team),

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
                IControllerQuery query,
                IAIPositionSource aiPositions)
            => new InGameAIController(
                team,
                query,
                new AIMoveService(aiPositions,
                    AIMoveStrategyFactory.Create(JanggiOptionStore.AISetting.Value)));
        private static IInGameController CreateRemoteController(
                PlayerTeam team)
            => new InGameRemoteController(team);

    }
}


