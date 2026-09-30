namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;

    // 상대 이동은 서버 확정 이벤트가 Flow를 거쳐 Session에 적용한다.
    // 이 객체는 Session의 양측 플레이어/턴 표시 계약만 채운다.
    internal sealed class InGameRemoteController : IInGameController
    {
        public PlayerTeam Team { get; }
        public bool IsLocal => false;

        internal InGameRemoteController(PlayerTeam team)
        {
            Team = team;
        }

        public void BeginTurn() { }
        public void EndTurn() { }
        public void BindEvents(IGameInputReceiver receiver) { }
        public void UnBindEvents(IGameInputReceiver receiver) { }
    }
}
