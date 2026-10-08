using System.Collections.Generic;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Abstractions;
    using Views;
    using Engine.JanggiOption;
    internal sealed class LocalMode : GameMode
    {

        public LocalMode(
            InGameView view,
            IInputHandler localInput,
            JanggiOptions options = null)
            : base(view, localInput, PlayerType.Local, PlayerType.Local, options)
        {
            
        }

        #region Input

        #endregion

        #region Engine Events

        #endregion


    }
}
