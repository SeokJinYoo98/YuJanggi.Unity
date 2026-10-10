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

    internal sealed class LocalMode : GameMode
    {
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer _cho;
        private readonly ILocalPlayer _han;
        public LocalMode(
            IJanggiEngine       engine,
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
        public override void Initialize()
        {
            _localInput.Initialize();
        }

        protected override void OnTick(float deltaTime)
            => _engine.Tick(deltaTime);

        protected override UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (!_engine.TryProcessTurn(from, to))
                UnityEngine.Debug.LogWarning($"[LocalMode] 이동이 거절되었습니다: {from} -> {to}");

            return UniTask.CompletedTask;
        }



        protected override void OnStartGame()
        {
            _engine.StartEngine();
            _localInput.SetPlayer(_cho);
        }

        public override void BeginNextTurn(PlayerTeam team)
        {
            _localInput.ResetPlayer();
            var nextPlayer = team == PlayerTeam.Cho ?
                _cho : _han;
            _localInput.SetPlayer(nextPlayer);
        }

        public override PlayerType GetPlayerType(PlayerTeam team)
            => team == PlayerTeam.Cho ?
                _cho.Type :
                _han.Type;

        protected override void OnStopGame()
        {
            _localInput.ResetPlayer();
        }
    }
}
