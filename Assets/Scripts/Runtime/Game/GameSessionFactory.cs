using System;
using UnityEngine;
using YuJanggiCommon;


namespace YuJanggi.Runtime.Game
{
    using Core.Domain;
    using Core.Match;

    using Controller;
    using GameSession;
    public static class GameSessionFactory
    {
        public static GameSession CreateSession(
            GameSessionInfo sessionInfo,
            MatchView matchView,
            MatchModel matchModel,
            ReplayView replayView,
            IInputHandler localInput)
        {
            IPlayerController cho = CreateController(
                sessionInfo.Cho,
                PlayerTeam.Cho,
                localInput,
                matchModel);
            IPlayerController han = CreateController(
                sessionInfo.Han,
                PlayerTeam.Han,
                localInput,
                matchModel);

            return new GameSession(
                sessionInfo,
                matchView,
                matchModel,
                replayView,
                cho,
                han,
                localInput);
        }
        public static GameSessionInfo CreateClientSession(
            Formation choFormation,
            Formation hanFormation,
            int turnTimeSelection)
        {
            return new GameSessionInfo
            {
                Mode = GameModeType.Local,
                Cho = PlayerType.Local,
                Han = PlayerType.Local,
                ChoFormation = choFormation,
                HanFormation = hanFormation,
                TurnTime = ConvertTurnTime(turnTimeSelection)
            };
        }

        public static GameSessionInfo CreateAISession(
            PlayerTeam localTeam,
            Formation localFormation,
            int turnTimeSelection)
        {
            GameSessionInfo session = new()
            {
                Mode = GameModeType.AI,
                TurnTime = ConvertTurnTime(turnTimeSelection)
            };

            if (localTeam == PlayerTeam.Cho)
            {
                session.Cho = PlayerType.Local;
                session.ChoFormation = localFormation;
                session.Han = PlayerType.AI;
                session.HanFormation = GetRandomFormation();
                return session;
            }

            session.Cho = PlayerType.AI;
            session.ChoFormation = GetRandomFormation();
            session.Han = PlayerType.Local;
            session.HanFormation = localFormation;
            return session;
        }

        public static GameSessionInfo CreateNetworkSession(PlayerSide localSide)
        {
            PlayerTeam localTeam = localSide == PlayerSide.Cho
                ? PlayerTeam.Cho
                : PlayerTeam.Han;

            return new GameSessionInfo
            {
                Mode = GameModeType.Network,
                Cho = localTeam == PlayerTeam.Cho ? PlayerType.Local : PlayerType.Network,
                Han = localTeam == PlayerTeam.Han ? PlayerType.Local : PlayerType.Network,
                ChoFormation = Formation.EHHE,
                HanFormation = Formation.EHHE,
                TurnTime = 0
            };
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
                PlayerType.Network => new NetworkController(team),
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
