#nullable enable
using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;

namespace YuJanggi.Lobby.Flow
{
    using UI;
    internal sealed class LocalLobbyFlow : LocalFlow
    {
        private readonly LocalPanel _view;
        private readonly Func<bool> _canStart;

        internal LocalLobbyFlow(LocalPanel view, Func<bool> canStart)
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
            var options = new JanggiOptions
            {
                GameMode = GameModeType.Local,
                PlayerCho = PlayerType.Local,
                PlayerHan = PlayerType.Local,
                ChoFormation = (Formation)_view.ChoFormation,
                HanFormation = (Formation)_view.HanFormation,
                TurnTime = LobbyOptionValues.TurnTime(_view.TurnTime)
            };
            RaiseGameStartReady(new LobbyGameStartContext(options));
        }
    }
}
