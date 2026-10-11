#nullable enable
using Cysharp.Threading.Tasks;
using System;

using System.Threading;

using YuJanggi.BootStrap;
using YuJanggi.Core.InGame;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiEngine;
using YuJanggi.InGame.Handler;
using YuJanggi.InGame.Input;
using YuJanggi.InGame.Player;



namespace YuJanggi.InGame.Mode
{
    using Network;

    internal sealed class NetworkMode : GameMode
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly RemotePlayer _remotePlayer;

        private readonly ILocalInputHandler     _localInput;
        private readonly InGameNetworkHandler   _netHandler;
        private readonly NetworkManager         _network;
        public NetworkMode(
            IGameEngine          engine,
            InputHandlerFactory  inputs,
            NetworkManager       network,
            PlayerTeam           myTeam,
            IGameCommandReceiver receiver)
            : base(engine)
        {
            _localInput = inputs.Create(GameInputType.Pointer);
            _network = network;
            _netHandler = network.InGame;

            var localTeam = myTeam;

            _localPlayer = new LocalPlayer(
                localTeam,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);

            var remoteTeam = localTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            _remotePlayer = new RemotePlayer(
                remoteTeam,
                receiver);
        }
        protected override UniTask OnInitializeAsync(
            CancellationToken token)
        {
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            return UniTask.CompletedTask;
        }
        protected override async UniTask OnGameStartAsync(
            CancellationToken token)
        {
            try
            {
                await _netHandler.SendInGameReadyAsync(token);
                await _netHandler.WaitForGameStartEvent(token);
            }
            catch
            {
                _localInput.ResetPlayer();
                _localInput.Pause();
                throw;
            }
        }
        public override void BeginNextTurn(PlayerTeam team)
        {
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            if (team == _localPlayer.Team)
            {
                _localInput.Resume();
                _localInput.SetPlayer(_localPlayer);
            }
            else
            {
                _localInput.Pause();
            }
        }

        public override PlayerType GetPlayerType(PlayerTeam team)
        {
            throw new NotImplementedException();
        }

        public override void SetLocalInputEnabled(bool enabled)
        {
            if (enabled) _localInput.Resume();
            else _localInput.Pause();
        }

        protected override UniTask OnGameEndAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

 

        protected override UniTask OnGiveUpAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }



        protected override UniTask OnPassTurnAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override UniTask OnRequestMoveAsync(Pos from, Pos to, CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override UniTask<bool> OnRequestRematchAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override UniTask OnTakeBackAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override void OnTick(float deltaTime)
        {
            throw new NotImplementedException();
        }

        private sealed class PendingMove
        {
            public Pos From { get; }
            public Pos To { get; }
            public UniTaskCompletionSource<NetworkMoveResult> Response { get; } = new();
            public PendingMove(Pos from, Pos to) { From = from; To = to; }
        }

  
    }
}
