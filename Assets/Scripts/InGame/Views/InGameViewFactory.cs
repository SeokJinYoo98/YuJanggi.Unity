

using TMPro;
using YuJanggi.Engine.JanggiRecord;
using YuJanggi.Runtime.Board;
using YuJanggi.Runtime.Input;
using YuJanggi.Runtime.Particle;
using YuJanggi.Runtime.UI;

namespace YuJanggi.InGame.Views
{
    internal static class InGameViewFactory
    {
        internal static LiveView CreateLiveView(
            ParticleView particleView,
            MoveGuideView moveGuideView,
            BoardView boardView,
            ResultUI resultUI,
            MatchUI matchUI)
                => new(particleView, moveGuideView, boardView, resultUI, matchUI);

        internal static ReplayView CreateReplayView(
            IReplayBoardRenderer board,
            IReadOnlyRecord record,
            ICoroutineRunner runner,
            TMP_Text displayMode)
                => new(board, record, runner, displayMode);
    }
}
