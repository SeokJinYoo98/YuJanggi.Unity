#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using Handler;
    using Input;
    using Network;
    using Player;


    internal sealed class NetworkMode
        : GameMode, IInGameNetworkReceiver
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly RemotePlayer _remotePlayer;

        private readonly ILocalInputHandler     _localInput;
        private readonly InGameNetworkHandler   _netHandler;
        private readonly NetworkManager         _network;
 
        private bool _commandPending = false;
        public NetworkMode(
            IJanggiEngine        engine,
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
            _commandPending = false;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
            _commandPending = true;
            return UniTask.CompletedTask;
        }
        protected override async UniTask OnGameStartAsync(
            CancellationToken token)
        {
            _commandPending = true;
            _netHandler.SetReceiver(this);

            await _netHandler.SendInGameReadyAsync(token);
            await _netHandler.WaitForGameStartEvent(token);

            _commandPending = false;
        }
        protected override async UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (_commandPending) return;
            _commandPending = true;

            await _netHandler.SendMoveRequestAsync(
                _localPlayer.Team,
                from,
                to,
                token);


        }
        protected override async UniTask<GameResultInfo> OnGameEndAsync(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token)
        {
            _commandPending = true;
   

            await _netHandler.SendGameEndRequest(
                info,
                totalMoves,
                token);

            var result = await _netHandler.WaitForGameEndEvent(
                token);

            _commandPending = false;
            return result;
        }


        protected override UniTask OnClearAsync(
            CancellationToken token)
        {
            _netHandler.SetReceiver(null);
            _commandPending = false;
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();
            return UniTask.CompletedTask;
        }



        protected override UniTask OnTakeBackAsync(
            CancellationToken token)
        {
            throw new NotImplementedException();
        }
        protected override UniTask<bool> OnRequestRematchAsync(
            CancellationToken token)
        {
            throw new NotImplementedException();
        }
        protected override UniTask OnGiveUpAsync(
            CancellationToken token)
        {
            throw new NotImplementedException();
        }
        protected override UniTask OnPassTurnAsync(
            CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public override void BeginNextTurn(
            PlayerTeam team)
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

        public override PlayerType GetPlayerType(
            PlayerTeam team)
            => team == _localPlayer.Team ? PlayerType.Local : PlayerType.Remote;

        public override void SetLocalInputEnabled(
            bool enabled)
        {
            if (enabled) _localInput.Resume();
            else _localInput.Pause();
        }
        public void ApplyServerMove(
            PlayerTeam team,
            Pos from,
            Pos to)
        {
            _engine.TryProcessTurn(from, to);

            if (team == _localPlayer.Team)
                _commandPending = false;
        }
    }
}
