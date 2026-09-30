#nullable enable
using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;
using YuJanggi.Runtime.UI;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class LocalLobbyFlow : ILobbyFlow
    {
        private readonly LocalPanelView _view;
        private readonly Func<bool> _canStart;
        private bool _bound;

        public event Action<LobbyGameStartContext>? GameStartReady;

        internal LocalLobbyFlow(LocalPanelView view, Func<bool> canStart)
        {
            _view = view;
            _canStart = canStart;
        }

        public void BindEvents()
        {
            if (_bound) return;
            _bound = true;
            _view.StartRequested += HandleStartRequested;
        }

        public void UnBindEvents()
        {
            if (!_bound) return;
            _bound = false;
            _view.StartRequested -= HandleStartRequested;
        }

        private void HandleStartRequested()
        {
            if (!_bound || !_canStart()) return;
            var options = new JanggiOptions
            {
                GameMode = GameModeType.Local,
                PlayerCho = PlayerType.Local,
                PlayerHan = PlayerType.Local,
                ChoFormation = (Formation)_view.ChoFormation,
                HanFormation = (Formation)_view.HanFormation,
                TurnTime = LobbyOptionValues.TurnTime(_view.TurnTime)
            };
            GameStartReady?.Invoke(new LobbyGameStartContext(options));
        }
    }
}
