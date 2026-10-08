using TMPro;

namespace YuJanggi.InGame.Views
{
    using UI;

    internal static class InGameViewFactory
    {
        internal static GameView CreateLiveView(
            ResultView resultUI,
            LiveView matchUI,
            TMP_Text displayMode)

            => new(resultUI,
                matchUI,
                displayMode);
    }
}
