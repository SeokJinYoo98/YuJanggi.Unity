using System;

namespace YuJanggi.InGame.Session
{
    using Core.V2.Domain;
    using Core.V2.Match;

    using Runtime.Controller;

    using Data.AI;

    using InGame.Views;
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

        /// <summary>서버가 배정한 내 진영과 로비에서 선택한 포진으로 세션 정보를 구성합니다.</summary>
        public static GameSessionInfo  CreateNetworkSession(PlayerTeam localTeam, Formation localFormation)
        {
            if (localTeam is not (PlayerTeam.Cho or PlayerTeam.Han))
                throw new ArgumentOutOfRangeException(nameof(localTeam));
            if (!Enum.IsDefined(typeof(Formation), localFormation))
                throw new ArgumentOutOfRangeException(nameof(localFormation));

            // TODO:
            // MatchingFound에는 상대 포진이 없어 현재는 상대 포진을 EHHE로 임시 구성합니다.
            // 그대로 대국을 시작하면 상대 및 서버의 실제 보드와 다를 수 있습니다.
            // 게임 시작 프로토콜에서 양쪽 포진을 받아 이 정보를 확정한 뒤 보드를 초기화해야 합니다.
            return CreateNetworkSession(
                localTeam,
                localTeam == PlayerTeam.Cho ? localFormation : Formation.EHHE,
                localTeam == PlayerTeam.Han ? localFormation : Formation.EHHE);
        }

        public static GameSessionInfo CreateNetworkSession(
            PlayerTeam localTeam,
            Formation cho,
            Formation han)
        {
            if (localTeam is not (PlayerTeam.Cho or PlayerTeam.Han))
                throw new ArgumentOutOfRangeException(nameof(localTeam));
            if (!Enum.IsDefined(typeof(Formation), cho))
                throw new ArgumentOutOfRangeException(nameof(cho));
            if (!Enum.IsDefined(typeof(Formation), han))
                throw new ArgumentOutOfRangeException(nameof(han));

            return new GameSessionInfo
            {
                Mode = GameModeType.Network,
                Cho = localTeam == PlayerTeam.Cho ? PlayerType.Local : PlayerType.Network,
                Han = localTeam == PlayerTeam.Han ? PlayerType.Local : PlayerType.Network,
                ChoFormation = cho,
                HanFormation = han,
                TurnTime = 30
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
