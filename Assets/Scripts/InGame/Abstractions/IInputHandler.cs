using System;

namespace YuJanggi.InGame.Abstractions
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
