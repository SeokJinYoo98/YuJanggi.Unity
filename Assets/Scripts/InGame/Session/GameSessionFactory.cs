using System;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;
    using Controller;
    using InGame.Views;
    using Store;
    using Runtime.Input;
    using YuJanggi.Engine.JanggiEngine;

    internal static class GameSessionFactory
    {
        internal static GameSession CreateSession(
            IJanggiEngine engine,
            IInputHandler inputHandler,
            LiveView liveView, ReplayView replayView)

        {
            var options = JanggiOptionStore.Current;

            IInGameController cho = InGameControllerFactory.CreateController(
                options.PlayerCho, PlayerTeam.Cho,
                engine, engine, inputHandler);

            IInGameController han = InGameControllerFactory.CreateController(
                options.PlayerHan, PlayerTeam.Han,
                engine, engine, inputHandler);

            return new GameSession(engine, inputHandler, cho, han, liveView, replayView);
        }


        private static int ConvertTurnTime(int value)
        {
            return value switch
            {
                0 => 0,
                1 => 10,
                2 => 20,
                3 => 30,
                4 => 40,
                5 => 50,
                6 => 60,
                _ => 30
            };
        }

        private static Formation GetRandomFormation()
        {
            int count = Enum.GetValues(typeof(Formation)).Length;
            return (Formation)UnityEngine.Random.Range(0, count);
        }
    }

}


