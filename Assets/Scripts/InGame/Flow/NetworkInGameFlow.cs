

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using YuJanggi.Engine.Domain;
using YuJanggi.InGame.Handler;
using YuJanggi.InGame.Service;
using YuJanggi.InGame.Session;
using YuJanggi.Protocol.InGame;

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
            SendMoveRequestAsync(from, to, _cancellationToken).Forget();
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

        private async UniTask SendMoveRequestAsync(
            Pos from,
            Pos to,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _handler.SendMoveRequestAsync(
                    _localTeam,
                    from,
                    to,
                    cancellationToken);

                if (result.Result != MovePieceResult.Accepted)
                {
                    _movePending = false;

                    Debug.LogWarning(
                        $"이동 요청이 거절되었습니다. Result={result}");
                }
            }
            catch (OperationCanceledException)
            {
                _movePending = false;
            }
            catch (Exception exception)
            {
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
                cancellationToken != _cancellationToken || !Session.CanApplyConfirmedMove(team))
                return;

            Session.RequestMove(from, to);
            if (team == _localTeam)
                _movePending = false;
        }
    }

}


