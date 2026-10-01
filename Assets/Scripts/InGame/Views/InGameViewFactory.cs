using TMPro;

namespace YuJanggi.InGame.Views
{
    using Engine.JanggiRecord;
    using Board;
    using Runtime.UI;
    using Runtime.Input;

    internal static class InGameViewFactory
    {
        internal static LiveView CreateLiveView(ResultUI resultUI, MatchUI matchUI)
            => new(resultUI, matchUI);

        internal static ReplayView CreateReplayView(
            IReplayBoardView board,
            IReadOnlyRecord record,
            ICoroutineRunner runner,
            TMP_Text displayMode)
            => new(board, record, runner, displayMode);
    }
}
