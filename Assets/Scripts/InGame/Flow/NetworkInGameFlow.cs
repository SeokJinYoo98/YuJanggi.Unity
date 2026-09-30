

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using YuJanggi.Engine.Domain;
using YuJanggi.InGame.Handler;
using YuJanggi.InGame.Service;
using YuJanggi.InGame.Session;

namespace YuJanggi.InGame.Flow
{
    internal sealed class NetworkInGameFlow : InGameFlow
    {
        private readonly InGameHandler _handler;
        private readonly PlayerTeam _localTeam;
        private CancellationToken _cancellationToken;
        private bool _active;
        private bool _gameStarted;
        private bool _movePending;

        internal NetworkInGameFlow(
            GameSession session,
            InGameHandler handler,
            PlayerTeam localTeam)
            : base(session)
        {
            _handler = handler;
            _localTeam = localTeam;
        }

        public override void RequestMove(Pos from, Pos to)
        {
            if (!_active || !_gameStarted || _movePending ||
                !Session.IsCurrentTurn(_localTeam))
                return;

            _movePending = true;
            SendMoveAsync(from, to, _cancellationToken).Forget();
        }

        protected override void Bind()
        {
            _active = true;
            _handler.MoveConfirmed += HandleMoveConfirmed;
        }

        protected override void UnBind()
        {
            _active = false;
            _movePending = false;
            _handler.MoveConfirmed -= HandleMoveConfirmed;
        }

        protected override async UniTask StartAsync(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            if (_gameStarted)
                return;

            await _handler.SendGameSceneReadyAsync(
                cancellationToken);

            await _handler.WaitUntilGameStartedAsync(
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            if (!_active)
                return;

            Session.StartGame();
            _gameStarted = true;
        }

        private async UniTask SendMoveAsync(Pos from, Pos to, CancellationToken cancellationToken)
        {
            try
            {
                await _handler.SendMoveAsync(_localTeam, from, to, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken == _cancellationToken)
                    _movePending = false;
            }
            catch (Exception exception)
            {
                if (cancellationToken == _cancellationToken)
                    _movePending = false;
                Debug.LogException(exception);
            }
        }

        private void HandleMoveConfirmed(PlayerTeam team, Pos from, Pos to)
            => ApplyConfirmedMoveAsync(team, from, to, _cancellationToken).Forget();

        private async UniTask ApplyConfirmedMoveAsync(
            PlayerTeam team, Pos from, Pos to, CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread();

            if (!_active || !_gameStarted || cancellationToken.IsCancellationRequested ||
                cancellationToken != _cancellationToken || !Session.IsCurrentTurn(team))
                return;

            Session.RequestMove(from, to);
            if (team == _localTeam)
                _movePending = false;
        }
    }

}


