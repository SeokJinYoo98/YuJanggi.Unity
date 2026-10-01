using TMPro;

namespace YuJanggi.InGame.Views
{
    using Runtime.UI;

    internal static class InGameViewFactory
    {
        internal static LiveView CreateLiveView(ResultUI resultUI, MatchUI matchUI, TMP_Text displayMode)
            => new(resultUI, matchUI, displayMode);
    }
}
