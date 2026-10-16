#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

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
    using Views.UI;

    internal sealed class NetworkMode
        : GameMode, IInGameNetworkReceiver
    {
        public override GameModeType ModeType => GameModeType.Network;

        private readonly ILocalPlayer _localPlayer;
        private readonly RemotePlayer _remotePlayer;

        private readonly ILocalInputHandler     _localInput;
        private readonly InGameNetworkHandler   _netHandler;
        private readonly NetworkManager         _network;

        private bool            _commandPending = false;
        private bool IsLocalTurn
            => !IsEnd && _engine.ControllerQuery.CurrentTurn == _localPlayer.Team;
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

        #region Init & Start
        protected override void OnInitialize()
        {
            _commandPending = false;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
            _commandPending = true;
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
        #endregion

        #region Move
        public async override UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (_commandPending) return;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _gameToken);
            _commandPending = true;

            await _netHandler.SendMoveRequestAsync(
                _localPlayer.Team,
                from,
                to,
                cts.Token);
        }
        #endregion

        #region EndGame
        protected override async UniTask OnClearAsync(
            CancellationToken token)
        {
            _commandPending = false;
            ClearLocalInput();

            await _network.DisconnectAsync();
        }
        protected override void HandleGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            // 로컬 Engine 결과로 종료를 요청하지 않고 서버 GameEndedEvent를 기다립니다.
            _commandPending = true;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
        }
        #endregion

        #region GameEnd UI Buttons
        public async override UniTask<bool> RequestRematchAsync(
            CancellationToken token)
        {
            bool accepted = await _netHandler.SendRematchRequest(
                _localPlayer.Team,
                token);

            if (!accepted)
                return false;

            await _netHandler.WaitForRematchConfirmedEvent(token);

            return true;
        }

        #endregion

        #region LiveGame UI Buttons

        public async override UniTask PassTurnAsync(
            CancellationToken token)
        {
            if (!IsLocalTurn || _commandPending)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _gameToken);
            _commandPending = true;

            var isAccepted = await _netHandler.SendPassTurnRequest(
              _localPlayer.Team,
              cts.Token);

            if (!isAccepted)
            {
                _commandPending = false;
                Debug.Log("넘기기 요청이 거절되었습니다.");
            }
        }
        public async override UniTask GiveUpAsync(
            CancellationToken token)
        {
            if (!IsLocalTurn || _commandPending)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _gameToken);
            _commandPending = true;

            var isAccepted = await _netHandler.SendGiveUpRequest(
                _localPlayer.Team,
                cts.Token);

            if (!isAccepted)
            {
                _commandPending = false;
                Debug.Log("기권 요청이 거절되었습니다.");
            }
        }
        #endregion

        #region Server Commands
        public void ApplyServerMove(
           PlayerTeam team,
           ProtocolTurnAction action,
           Pos from, Pos to)
        {
            if (team == _localPlayer.Team)
                _commandPending = false;

            switch (action)
            {
                case ProtocolTurnAction.Move:
                    _engine.TryProcessTurn(from, to);
                    break;

                case ProtocolTurnAction.Pass:
                    _engine.HandleHandicap();
                    break;
            }
        }
        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves)
        {
            if (IsEnd)
                return;

            if (!_engine.References.Turn.IsEnd)
                _engine.EndEngine();

            _commandPending = true;
            _localInputEnabled = false;
            ClearLocalInput();

            NotifyGameEnded(result, totalMoves);
        }
        #endregion

        #region Turn & Input
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

        private void ClearLocalInput()
        {
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();
            _netHandler.SetReceiver(null);
        }
        #endregion
    }
}
