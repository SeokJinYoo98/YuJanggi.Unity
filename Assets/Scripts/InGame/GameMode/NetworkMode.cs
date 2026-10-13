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
    using Protocol.InGame;
    using Store;

    internal sealed class NetworkMode
        : GameMode, IInGameNetworkReceiver
    {
        private enum GameEndState
        {
            None,
            Requested,
            Confirmed
        }

        private readonly ILocalPlayer _localPlayer;
        private readonly RemotePlayer _remotePlayer;

        private readonly ILocalInputHandler     _localInput;
        private readonly InGameNetworkHandler   _netHandler;
        private readonly NetworkManager         _network;

        private bool            _commandPending = false;

        public NetworkMode(
            IJanggiEngine        engine,
            InputHandlerFactory  inputs,
            NetworkManager       network,
            PlayerTeam           myTeam,
            IGameCommandReceiver receiver)
            : base(engine)
        {
            _localInput = inputs.Create(JanggiOptionStore.TYPE);
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

        protected override UniTask OnPassTurnAsync(
            CancellationToken token)
        {
            throw new NotImplementedException();
        }

        protected override void BeginNextTurn(
            PlayerTeam team)
        {
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            if (_localInputEnabled && team == _localPlayer.Team)
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

        protected override void OnSetLocalInputEnabled(
            bool enabled)
        {
            if (enabled && !IsEnd)
            {
                BeginNextTurn(_engine.ControllerQuery.CurrentTurn);
            }
            else
            {
                _localInput.ResetPlayer();
                _localInput.Pause();
            }
        }






        protected override UniTask<GameResultInfo> OnGameEndAsync(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token)
        {
            _commandPending = true;
            _localInputEnabled = false;
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();
            _netHandler.SetReceiver(null);
            return UniTask.FromResult(info);
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

        public void ApplyServerTurn(
            PlayerTeam team,
            ProtocolTurnAction action,
            Pos from, Pos to)
        {
            throw new NotImplementedException();
        }


        protected override async UniTask<PlayerTeam?> OnGiveUpAsync(
            CancellationToken token)
        {
            if (_commandPending)
                return null;

            // var result = await _netHandler.RequestGiveUp();
            // if (result == Success)
            // else            
            // 기권 요청을 보내고 Response를 확인합니다.
            // 실제 기권 적용은 서버 Event 수신부에서 처리합니다.

            return null;
        }
        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves)
        {
            // 서버의 메시지에 따라서 
        }

    }
}
