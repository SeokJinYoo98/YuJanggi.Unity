using MatchInfo = YuJanggi.Lobby.Network.MatchInfo;

namespace YuJanggi.Store
{
    using Engine.Domain;
    using Engine.JanggiOption;
    using System;

    public static class NetworkMatchInfoStore
    {
        public static MatchInfo Current;
    }
    public static class JanggiOptionStore
    {
        public static JanggiOptions Current { get; private set; }

        public static void SetOptions(JanggiOptions options)
            => Current = options ?? throw new ArgumentNullException(nameof(options));

        public static void ClearNetworkOptions()
        {
            if (Current?.GameMode == GameModeType.Network)
                Current = null;
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



