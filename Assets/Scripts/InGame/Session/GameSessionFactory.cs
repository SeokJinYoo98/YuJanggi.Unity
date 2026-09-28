using System;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;

    using Controller;

    using Data.AI;

    using InGame.Views;


    public static class GameSessionFactory
    {
        public static GameSession CreateSession(
            MatchView matchView,
            ReplayView replayView,
            IInputHandler localInput)
        {
            var option = JanggiOptionStore.Current;
            IPlayerController cho = CreateController(
                option.Cho,
                PlayerTeam.Cho,
                localInput);
            IPlayerController han = CreateController(
                option.Han,
                PlayerTeam.Han,
                localInput);

            return new GameSession(
                matchView,
                replayView,
                cho,
                han,
                localInput);
        }


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


