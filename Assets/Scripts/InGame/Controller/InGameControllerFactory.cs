

using System;


namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using Runtime.Input;

    using AI;
    using Data.AI;
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
                    AIMoveStrategyFactory.Create(AISessionSettings.Strategy)));
        private static IInGameController CreateRemoteController(
                PlayerTeam team)
            => new InGameRemoteController(team);

    }
}


