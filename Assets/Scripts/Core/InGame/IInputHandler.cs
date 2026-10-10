using System;

namespace YuJanggi.Core.InGame
{
    using Engine.Domain;
    public enum GameInputType { PC, Record }
    public interface IInputHandler
    {
        public void Initialize();
    }

    public interface ILocalInputHandler : IInputHandler
    {
        public void ResetPlayer();
        public void SetPlayer(IBoardInputReceiver receiver);
        public void Pause();
        public void Resume();
    }
}
