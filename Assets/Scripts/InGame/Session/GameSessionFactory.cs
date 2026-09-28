using System;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;
    using Controller;
    using InGame.Views;
    using Store;
    using Runtime.Input;
    public static class GameSessionFactory
    {
      
        public static GameSession CreateSession(
            LiveView liveView,
            ReplayView replayView,
            IInGameController cho,
            IInGameController han,
            IInputHandler localInput)
        {
            var option = JanggiOptionStore.Current;
            var choOption = option.PlayerCho;


            return new GameSession(
                liveView,
                replayView,
                cho,
                han,
                localInput);
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


