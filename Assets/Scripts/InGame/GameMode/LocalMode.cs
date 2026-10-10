using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using Engine.JanggiEngine;
    using Input;
    using Views;
    using YuJanggi.InGame.Player;
    using NUnit.Framework.Constraints;

    internal sealed class LocalMode : GameMode
    {
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer _cho;
        private readonly ILocalPlayer _han;
        public LocalMode(
            IGameEngine engine,
            InputHandlerFactory inputs,
            IGameCommandReceiver receiver)
            : base(engine)
        {
            _localInput = inputs.Create(GameInputType.PC);

            _cho = new LocalPlayer(
                PlayerTeam.Cho,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);

            _han = new LocalPlayer(
                PlayerTeam.Han,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);
        }

        public override void BeginNextTurn(PlayerTeam team)
        {
            _localInput.ResetPlayer();

            _cho.ResetSelection();
            _han.ResetSelection();

            var nextPlayer = team == PlayerTeam.Cho ? _cho : _han;
            _localInput.SetPlayer(nextPlayer);
        }
        public override PlayerType GetPlayerType(PlayerTeam team)
            => team == PlayerTeam.Cho ?
                _cho.Type :
                _han.Type;

        protected override void OnTick(float deltaTime)
            => _engine.Tick(deltaTime);

        protected override UniTask OnGameStartAsync(
            CancellationToken token)
        {
            _localInput.SetPlayer(_cho);
            return UniTask.CompletedTask;
        }
        protected override UniTask OnGameEndAsync(
            CancellationToken token)
        {
            _localInput.ResetPlayer();
            return UniTask.CompletedTask;
        }
        protected override UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (!_engine.TryProcessTurn(from, to))
                UnityEngine.Debug.LogWarning(
                    $"[LocalMode] 이동이 거절되었습니다: {from} -> {to}");

            return UniTask.CompletedTask;
        }
        protected override UniTask OnPassTurnAsync(
            CancellationToken token)
        {
            _engine.HandleHandicap();
            return UniTask.CompletedTask;
        }
        protected override UniTask OnGiveUpAsync(
            CancellationToken token)
        {
            _engine.GiveUp();
            return UniTask.CompletedTask;
        }

        protected override UniTask<bool> OnRequestRematchAsync(
            CancellationToken token)
            => UniTask.FromResult(true);

        protected override UniTask OnTakeBackAsync(
            CancellationToken token)
        {
            _engine.Undo();
            return UniTask.CompletedTask;
        }
        protected override UniTask OnInitializeAsync(
            CancellationToken token)
        {
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _cho.ResetSelection();
            _han.ResetSelection();
            return UniTask.CompletedTask;
        }
    }
}
