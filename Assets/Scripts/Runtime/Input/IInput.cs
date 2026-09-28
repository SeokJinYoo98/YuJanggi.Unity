using System;
using UnityEngine;

namespace YuJanggi.Runtime.Input
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

    public abstract class InputHandlerBehaviour : MonoBehaviour, IInputHandler
    {
        public abstract event Action<Pos> OnBoardClicked;
        public abstract event Action OnEmptyClicked;

        public abstract void Activate();
        public abstract void Deactivate();
        public abstract void RotateCamera(PlayerTeam team);
    }

    public interface IBoardClickable
    {
        public Pos BoardPos { get; }
    }
}


