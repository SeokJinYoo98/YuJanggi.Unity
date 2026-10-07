using System;
using UnityEngine;

namespace YuJanggi.InGame.Controller.Input
{
    using Engine.Domain;
    using YuJanggi.InGame.Abstractions;

    public abstract class InputHandlerBehaviour : MonoBehaviour, IInputHandler
    {
        public abstract event Action<Pos> OnBoardClicked;
        public abstract event Action OnEmptyClicked;

        public abstract void Activate();
        public abstract void Deactivate();
        public abstract void RotateCamera(PlayerTeam team);
    }

}


