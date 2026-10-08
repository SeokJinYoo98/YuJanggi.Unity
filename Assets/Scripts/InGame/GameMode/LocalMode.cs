using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Views;
    internal sealed class LocalMode : GameMode
    {

        public LocalMode(
            InGameView view,
            IInputHandler localInput)
            : base(view, localInput, PlayerType.Local, PlayerType.Local)
        {
            
        }

        #region Input

        #endregion

        #region Engine Events

        #endregion


    }
}
