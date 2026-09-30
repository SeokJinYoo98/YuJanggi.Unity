#nullable enable
using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;
using YuJanggi.Runtime.UI;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class AILobbyFlow : ILobbyFlow
    {
        private readonly AIPanelView _view;
        private readonly Func<bool> _canStart;
        private bool _bound;

        public event Action<LobbyGameStartContext>? GameStartReady;

        internal AILobbyFlow(AIPanelView view, Func<bool> canStart)
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
            GameStartReady?.Invoke(new LobbyGameStartContext(options, _view.Strategy));
        }
    }
}
