#nullable enable
using System;

namespace YuJanggi.Lobby.Flow
{
    internal abstract class LocalFlow : ILobbyFlow
    {
        private bool _bound;

        protected bool IsBound => _bound;

        public event Action<LobbyGameStartContext>? GameStartReady;

        public void BindEvents()
        {
            if (_bound)
                return;

            _bound = true;
            OnBindEvents();
        }

        public void UnBindEvents()
        {
            if (!_bound)
                return;

            _bound = false;
            OnUnBindEvents();
        }

        protected void RaiseGameStartReady(LobbyGameStartContext context)
            => GameStartReady?.Invoke(context);

        protected abstract void OnBindEvents();
        protected abstract void OnUnBindEvents();
    }
}
