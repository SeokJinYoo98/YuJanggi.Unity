using System;
using YuJanggi.Core.InGame;

namespace YuJanggi.Store
{
    using Engine.Domain;
    using Engine.JanggiOption;

    using AI.Data;
    using Network;

    public static class OnlineMatchInfoStore
    {

        private struct NetworkSessionInfo
        {
            public PlayerTeam MyTeam;
            public string     MatchId;
            public string     OpponentPlayerId;
            public string     OpponentNickname;
        }
        private static NetworkSessionInfo? _current;

        private static NetworkSessionInfo Current
            => _current ?? throw new InvalidOperationException(
                "매칭 정보가 저장되지 않았습니다.");
  
        public static bool   HasData
            => _current.HasValue;
        public static PlayerTeam MyTeam
            => Current.MyTeam;
        public static string MatchId
            => Current.MatchId;
        public static string OpponentPlayerId
            => Current.OpponentPlayerId;
        public static string OpponentNickname
            => Current.OpponentNickname;
        public static void SaveData(
            in NetworkMatchingData data)
        {
            _current = new NetworkSessionInfo
            {
                MyTeam           = data.MyTeam,
                MatchId          = data.MatchId,
                OpponentPlayerId = data.OpponentPlayerId,
                OpponentNickname = data.OpponentNickname
            };
        }

        public static void Clear()
            => _current = null;
    }

    public static class JanggiOptionStore
    {
        public static GameInputType TYPE => GameInputType.Record;
        public static JanggiOptions         JanggiSetting { get; private set; }
        public static AIMoveStrategyType?   AISetting { get; private set; }
        public static void SaveOptions(
            JanggiOptions options,
            AIMoveStrategyType? aiStrategy = null)
        {
            JanggiSetting = options
                ?? throw new ArgumentNullException(nameof(options));

            AISetting = aiStrategy;
        }

        public static void Clear()
        {
            JanggiSetting = default;
            AISetting = null;
        }
    }
    public static class JanggiOptionFactory
    {
        public static JanggiOptions CreateLocal(
            int choFormation,
            int hanFormation,
            int turnTime)
        {
            return new JanggiOptions
            {
                GameMode        = GameModeType.Local,

                PlayerCho       = PlayerType.Local,
                ChoFormation    = ToFormation(choFormation),

                PlayerHan       = PlayerType.Local,
                HanFormation    = ToFormation(hanFormation),

                TurnTime        = ToTurnTime(turnTime)
            };
        }
        public static JanggiOptions CreateAI(
            int playerTeam,
            int playerFormation,
            int turnTime)
        {
            bool isPlayerCho = playerTeam == (int)PlayerTeam.Cho;

            var cho = isPlayerCho ? PlayerType.Local : PlayerType.AI;
            var choForm = isPlayerCho ? ToFormation(playerFormation) : ToFormation(GenerateAIFormation());

            var han = isPlayerCho ? PlayerType.AI : PlayerType.Local;
            var hanForm = isPlayerCho ? ToFormation(GenerateAIFormation()) : ToFormation(playerFormation);

            return new JanggiOptions
            {
                GameMode     = GameModeType.AI,

                PlayerCho    = cho,
                ChoFormation = choForm,

                PlayerHan    = han,
                HanFormation = hanForm,

                TurnTime     = ToTurnTime(turnTime)
            };
        }
        public static JanggiOptions CreateNetwork(
            in NetworkFormationData data,
            PlayerTeam myTeam)
        {
            var isPlayerCho = myTeam == PlayerTeam.Cho;

            return new JanggiOptions
            {
                GameMode     = GameModeType.Network,

                PlayerCho    = isPlayerCho ? PlayerType.Network : PlayerType.Remote,
                ChoFormation = data.Cho,

                PlayerHan    = isPlayerCho ? PlayerType.Remote : PlayerType.Network,
                HanFormation = data.Han,

                TurnTime     = 60f
            };
        }
        private static int                  ToTurnTime(int value)
            => value switch
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
        private static Formation            ToFormation(int value)
            => (Formation)value;
        private static int                  GenerateAIFormation()
        {
            return UnityEngine.Random.Range(
                0, Enum.GetValues(typeof(Formation)).Length);
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



/*
 * OperationCanceledException
 * - Cancel()은 신호를 보내고, 
 * - Token을 확인하거나 사용 중인 비동기 작업이 OperationCanceledException으로 취소를 전파합니다.
 * 
 * - Level3 → OperationCanceledException
 * - Level2 → await에서 예외 발생 → 이후 코드 실행 안 함 → catch 없으므로 Level1로 전파
 * - Level1 → catch에서 처리
 * 
 * 
 * [Handler]
 * JsonException
 * - 데이터는 받았는데 JSON을 내가 기대한 타입으로 해석할 수 없다
 * 
 * ObjectDisposedException
 * - Dispose()된 객체를 다시 사용하려고 할 때 발생하는 예외입니다.
 *
 *
 *
 
 */
