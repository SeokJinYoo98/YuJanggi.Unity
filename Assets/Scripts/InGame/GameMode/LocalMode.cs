using UnityEngine;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Views;
    using YuJanggi.InGame.Player;

    internal sealed class LocalMode : GameMode
    {
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer _cho;
        private readonly ILocalPlayer _han;
        public LocalMode(
            InGameView          view,
            ILocalInputHandler  localInput)
            : base(view)
        {
            _localInput = localInput;

            _cho = new LocalPlayer(
                PlayerTeam.Cho,
                PlayerType.Local,
                this,
                _engine);

            _han = new LocalPlayer(
                PlayerTeam.Han,
                PlayerType.Local,
                this,
                _engine);
        }
        protected override void OnInit()
        {
            _localInput.Initialize();
        }
        protected override void HandleTurnChanged(PlayerTeam next)
        {
            _gameView.Live.UpdateTurn(next, PlayerType.Local);
            var nextPlayer = next == PlayerTeam.Cho ? _cho : _han;

            _localInput.ResetPlayer();
            _localInput.SetPlayer(nextPlayer);
        }




        public override void HandleGiveUpButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandleNextButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandlePassTurnButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandlePreviousButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandleRematchButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandleReplayButton()
        {
            throw new System.NotImplementedException();
        }

        public override void HandleTakebackButton()
        {
            throw new System.NotImplementedException();
        }


    }
}
