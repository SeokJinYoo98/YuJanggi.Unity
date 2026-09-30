#nullable enable
using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;
using YuJanggi.Runtime.UI;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class AILobbyFlow : LocalFlow
    {
        private readonly AIPanelView _view;
        private readonly Func<bool> _canStart;

        internal AILobbyFlow(AIPanelView view, Func<bool> canStart)
        {
            _view = view;
            _canStart = canStart;
        }

        protected override void OnBindEvents()
        {
            _view.StartRequested += HandleStartRequested;
        }

        protected override void OnUnBindEvents()
        {
            _view.StartRequested -= HandleStartRequested;
        }

        private void HandleStartRequested()
        {
            if (!IsBound || !_canStart()) return;
            bool localIsCho = (PlayerTeam)_view.LocalPlayer == PlayerTeam.Cho;
            var localFormation = (Formation)_view.LocalPlayerFormation;
            var aiFormation = (Formation)UnityEngine.Random.Range(
                0, Enum.GetValues(typeof(Formation)).Length);
            var options = new JanggiOptions
            {
                GameMode = GameModeType.AI,
                PlayerCho = localIsCho ? PlayerType.Local : PlayerType.AI,
                PlayerHan = localIsCho ? PlayerType.AI : PlayerType.Local,
                ChoFormation = localIsCho ? localFormation : aiFormation,
                HanFormation = localIsCho ? aiFormation : localFormation,
                TurnTime = LobbyOptionValues.TurnTime(_view.TurnTime)
            };
            RaiseGameStartReady(new LobbyGameStartContext(options, _view.Strategy));
        }
    }
}
