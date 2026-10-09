using System;
using UnityEngine;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Input
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    public abstract class InputHandler : MonoBehaviour, IInputHandler
    {
        public event Action<Pos> OnBoardClicked;
        public event Action      OnEmptyClicked;

        protected void RaiseBoardClicked(Pos position)
            => OnBoardClicked?.Invoke(position);
        protected void RaiseEmptyClicked()
            => OnEmptyClicked?.Invoke();

        public virtual bool Initialize()
            => true;
        public abstract void Activate();
        public abstract void Deactivate();
        public virtual void RotateCamera(PlayerTeam team) { }
        public virtual void Release() => Deactivate();
        protected virtual void OnDestroy() => Release();
    }

}


