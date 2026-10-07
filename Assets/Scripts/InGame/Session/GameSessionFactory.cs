namespace YuJanggi.InGame.Session
{
    using Engine.JanggiEngine;
    using Engine.Domain;

    using Core.Abstractions;

    using Controller;
    using Views;
    using Views.Board;
    using Store;



    internal static class GameSessionFactory
    {
        internal static GameSession CreateSession(
            IJanggiEngine engine,
            IInputHandler inputHandler,
            LiveView liveView, ReplayPlayback replayPlayback, BoardView boardView)

        {
            var options = JanggiOptionStore.Current;

            IInGameController cho = InGameControllerFactory.CreateController(
                options.PlayerCho, PlayerTeam.Cho,
                engine, engine, inputHandler);

            IInGameController han = InGameControllerFactory.CreateController(
                options.PlayerHan, PlayerTeam.Han,
                engine, engine, inputHandler);

            return new GameSession(engine, inputHandler, cho, han, liveView, replayPlayback, boardView);
        }


    }

}


