using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;

namespace YuJanggi.InGame.Player
{
    // 상대는 보드 입력을 받지 않으며 서버 확정 이동만 NetworkMode가 적용합니다.
    internal sealed class RemotePlayer : InGamePlayer, IInGamePlayer
    {
        public RemotePlayer(PlayerTeam team, IGameCommandReceiver receiver)
            : base(team, PlayerType.Remote, receiver) { }
    }
}
