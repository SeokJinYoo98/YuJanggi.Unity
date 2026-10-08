

using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


namespace YuJanggi.InGame.Flow
{
    using Engine.Domain;
    using Protocol.InGame;

    using Handler;
    using YuJanggi.BootStrap;
    using Session;
    using YuJanggi.Protocol.Matching;
    using System.Threading.Tasks;

    internal sealed class NetworkInGameFlow : InGameFlow
    {
        private readonly InGameHandler _handler;
        private readonly NetworkManager _networkManager;
        private readonly PlayerTeam _localTeam;
        private CancellationToken _cancellationToken;
        private bool _active;
        private bool _gameStarted;
        private bool _movePending;
        private GameEndedEvent _confirmedGameEnd;

        internal NetworkInGameFlow(
            GameSession session,
            InGameHandler handler,
            PlayerTeam localTeam,
            NetworkManager networkManager)
            : base(session)
        {
            _handler = handler;
            _networkManager = networkManager;
            _localTeam = localTeam;
        }

        public override void PrepareReturnToLobby()
        {
            // _networkManager.ResetMatchState();
            // _networkManager.Disconnect();
        }

        public override void RequestMove(Pos from, Pos to)
        {
            if (!_active || !_gameStarted || _movePending ||
                !Session.IsCurrentTurn(_localTeam))
                return;

            _movePending = true;
            SendMoveRequestAsync(from, to, _cancellationToken).Forget();
        }
        public override void SubmitGameResult(in GameResultInfo info)
        {
            ProtocolGameEndReason reason = ProtocolGameEndReason.CheckMate;
            switch (info.Type)
            {
                case GameResult.Draw:
                    reason = ProtocolGameEndReason.Draw;
                    break;

                case GameResult.GiveUp:
                    reason = ProtocolGameEndReason.GiveUp;
                    break;
                case GameResult.Score:
                    reason = ProtocolGameEndReason.Score;
                    break;
            }
            var winner = info.Type == GameResult.Draw ? ProtocolPlayerTeam.None
                : info.Loser == PlayerTeam.Cho ? ProtocolPlayerTeam.Han : ProtocolPlayerTeam.Cho;
            var totalMoves = info.MoveCnt;
            SendGameEndRequestAsync(
                reason,
                winner,
                totalMoves).Forget();
            TryApplyConfirmedGameEnd();

        }
        protected override void Bind()
        {
            _active = true;
            Session.RequireServerGameEndConfirmation = true;
            Session.LocalGameEnded += HandleLocalGameEnded;
            _handler.MoveConfirmed += HandleMoveConfirmed;
            _handler.GameEndConfirmed += HandleGameEndConfirmed;
        }

        protected override void UnBind()
        {
            _active = false;
            _movePending = false;
            _confirmedGameEnd = null;
            Session.RequireServerGameEndConfirmation = false;
            Session.LocalGameEnded -= HandleLocalGameEnded;
            _handler.MoveConfirmed -= HandleMoveConfirmed;
            _handler.GameEndConfirmed -= HandleGameEndConfirmed;
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

        private void HandleMoveConfirmed(
            PlayerTeam team,
            Pos from, Pos to)
            => ApplyConfirmedMoveAsync(team, from, to, _cancellationToken).Forget();

        private async UniTask ApplyConfirmedMoveAsync(
            PlayerTeam team,
            Pos from, Pos to,
            CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread();

            if (!_active || !_gameStarted || cancellationToken.IsCancellationRequested ||
                cancellationToken != _cancellationToken || !Session.CanApplyConfirmedMove(team))
                return;

            Session.RequestMove(from, to);
            if (team == _localTeam)
                _movePending = false;
        }

        private void HandleLocalGameEnded(GameResultInfo info)
            => SubmitGameResult(in info);

        private void HandleGameEndConfirmed(GameEndedEvent ended)
            => ApplyConfirmedGameEndAsync(ended, _cancellationToken).Forget();

        private async UniTask ApplyConfirmedGameEndAsync(
            GameEndedEvent ended, CancellationToken cancellationToken)
        {
            await UniTask.SwitchToMainThread();
            if (!_active || !_gameStarted || cancellationToken.IsCancellationRequested ||
                cancellationToken != _cancellationToken || Session.GameResult.HasValue)
                return;

            _confirmedGameEnd = ended;
            TryApplyConfirmedGameEnd();
        }

        private void TryApplyConfirmedGameEnd()
        {
            if (_confirmedGameEnd == null) return;
            PlayerTeam? loser = _confirmedGameEnd.Winner == ProtocolPlayerTeam.None
                ? (PlayerTeam?)null
                : _confirmedGameEnd.Winner == ProtocolPlayerTeam.Cho ? PlayerTeam.Han : PlayerTeam.Cho;
            if (Session.ApplyConfirmedGameEnd(loser, _confirmedGameEnd.TotalMoves))
            {
                _confirmedGameEnd = null;
                _movePending = false;
            }
        }

        private async UniTask SendGameEndRequestAsync(
            ProtocolGameEndReason endReason,
            ProtocolPlayerTeam winner,
            int totalMoves)
        {


            try
            {
                var receiveMsg = await _handler.SendGameEndRequestAsync(
                    endReason,
                    winner,
                    totalMoves,
                    _cancellationToken);

                if (receiveMsg.Result != GameEndResult.Accepted)
                    Debug.LogWarning($"게임 종료 요청이 거절되었습니다. Result={receiveMsg.Result}");
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

    }

}


