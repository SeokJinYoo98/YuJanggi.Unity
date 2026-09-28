


namespace YuJanggi.Store
{
    using Engine.Domain;
    using Engine.JanggiOption;
    using System;
    using YuJanggi.Lobby.Matching;
    using YuJanggi.Runtime.UI;

    public struct NetworkSessionInfo
    {
        public string MatchId { get; }
        public PlayerTeam Team { get; }
        public string OpponentId { get; }
        public string OpponentNickname { get; }

        public NetworkSessionInfo(
            string matchId,
            PlayerTeam team,
            string opponentId,
            string opponentNickname)
        {
            MatchId = matchId;
            Team = team;
            OpponentId = opponentId;
            OpponentNickname = opponentNickname;
        }
    }
    public static class NetworkMatchInfoStore
    {
        public static MatchInfo Current;
    }
    public static class JanggiOptionStore
    {
        public static JanggiOptions Current { get; private set; }

        public static void SetLocalOptions(LocalPanelView local)
        {
            Current = new JanggiOptions
            {
                GameMode = GameModeType.Local,
                PlayerCho = PlayerType.Local,
                PlayerHan = PlayerType.Local,
                ChoFormation = (Formation)local.ChoFormation,
                HanFormation = (Formation)local.HanFormation,
                TurnTime = ConvertTurnTime(local.TurnTime)
            };
        }
        public static void SetAIOptions(AIPanelView ai)
        {
            bool localIsCho = (PlayerTeam)ai.LocalPlayer == PlayerTeam.Cho;
            var localFormation = (Formation)ai.LocalPlayerFormation;
            var aiFormation = (Formation)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Formation)).Length);
            Current =  new JanggiOptions
            {
                GameMode = GameModeType.AI,
                PlayerCho = localIsCho ? PlayerType.Local : PlayerType.AI,
                PlayerHan = localIsCho ? PlayerType.AI : PlayerType.Local,
                ChoFormation = localIsCho ? localFormation : aiFormation,
                HanFormation = localIsCho ? aiFormation : localFormation,
                TurnTime = ConvertTurnTime(ai.TurnTime)
            };
        }
        public static void SetNetworkOptions(PlayerTeam localTeam, Formation cho, Formation han)
        {
            if (localTeam is not (PlayerTeam.Cho or PlayerTeam.Han))
                throw new ArgumentOutOfRangeException(nameof(localTeam));

            bool localIsCho = localTeam == PlayerTeam.Cho;
            Current = new JanggiOptions
            {
                GameMode = GameModeType.Network,
                PlayerCho = localIsCho ? PlayerType.Network : PlayerType.Remote,
                PlayerHan = localIsCho ? PlayerType.Remote : PlayerType.Network,
                ChoFormation = cho,
                HanFormation = han,
                // 서버 TurnTime 계약이 생기기 전까지 기존 기본값을 유지합니다.
                TurnTime = 30
            };
        }

        public static void ClearNetworkOptions()
        {
            if (Current?.GameMode == GameModeType.Network)
                Current = null;
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
    }

    #region Fields
    // 내부 상태와 참조를 저장하는 변수
    #endregion

    #region Properties
    // 상태를 조회하거나 변경하는 접근 속성
    #endregion

    #region Events
    // 상태 변화나 특정 동작을 외부에 알리는 이벤트
    #endregion

    #region Unity Lifecycle
    // Awake, OnEnable, Update, OnDestroy 등 Unity 생명주기 콜백
    #endregion

    #region Public Methods
    // 외부에서 호출하는 기능
    #endregion

    #region Event Handlers
    // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
    #endregion

    #region Private Methods
    // 클래스 내부에서 사용하는 보조 로직
    #endregion

    //////////////

    #region Fields
    // 내부 상태와 참조를 저장하는 변수
    #endregion

    #region Properties
    // 상태를 조회하거나 변경하는 접근 속성
    #endregion

    #region Events
    // 상태 변화나 특정 동작을 외부에 알리는 이벤트
    #endregion

    #region Constructors
    // 순수 C#
    #endregion

    #region Public Methods
    // 외부에서 호출하는 기능
    #endregion

    #region Event Handlers
    // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
    #endregion

    #region Private Methods
    // 클래스 내부에서 사용하는 보조 로직
    #endregion
}



