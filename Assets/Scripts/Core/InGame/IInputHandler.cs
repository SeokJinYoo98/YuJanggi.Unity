using System;

namespace YuJanggi.Core.InGame
{
    using Engine.Domain;

    public interface IInputHandler
    {
        public event Action<Pos> OnBoardClicked;
        public event Action OnEmptyClicked;
        public void RotateCamera(PlayerTeam team);
        public void Activate();
        public void Deactivate();
    }
}
