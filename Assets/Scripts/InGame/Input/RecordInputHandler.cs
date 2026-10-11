
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

using YuJanggi.Core.InGame;
using YuJanggi.Store;

namespace YuJanggi.InGame.Input
{
    using BootStrap;
    using YuJanggi.Engine.Domain;

    public sealed class RecordInputHandler : ILocalInputHandler
    {
        private readonly HistroyManager _history;
        private readonly CancellationToken _lifecycleToken;
        private CancellationTokenSource _playbackCts;
        private Camera _camera;

        private IBoardInputReceiver _currReceiver;
        private bool _pause = false;
        public RecordInputHandler(HistroyManager history, CancellationToken lifecycleToken)
        {
            _history = history;
            _lifecycleToken = lifecycleToken;
        }
        public void Initialize()
        {
            _camera = Camera.main;

            if (_camera == null)
                return;

            RotateCamera();
        }

        public void Pause()
            => _pause = true;

        public void ResetPlayer()
        {
            CancelPlayback();
            _currReceiver = null;
        }

        public void Resume()
            => _pause = false;

        public void SetPlayer(IBoardInputReceiver receiver)
        {
            CancelPlayback();
            _currReceiver = receiver;
            var cts = CancellationTokenSource.CreateLinkedTokenSource(_lifecycleToken);
            _playbackCts = cts;
            PlayRecordAsync(receiver, cts).Forget();
        }

        private void CancelPlayback()
        {
            var cts = _playbackCts;
            _playbackCts = null;
            cts?.Cancel();
        }

        private async UniTask PlayRecordAsync(
            IBoardInputReceiver receiver,
            CancellationTokenSource cts)
        {
            using (cts)
            {
                var token = cts.Token;
                try
                {
                    if (_history.Moves.Count == 0)
                        return;

                    // 이동을 입력하기 전까지 큐에 남겨, 중간에 취소돼도 같은 수를 재생합니다.
                    var move = _history.Moves.Peek();

                    await UniTask.Delay(2000, cancellationToken: token);
                    await UniTask.WaitUntil(() => !_pause, cancellationToken: token);
                    token.ThrowIfCancellationRequested();
                    receiver.HandleValidClick(move.From);

                    await UniTask.Delay(2000, cancellationToken: token);
                    await UniTask.WaitUntil(() => !_pause, cancellationToken: token);
                    token.ThrowIfCancellationRequested();
                    _history.Moves.Dequeue();
                    receiver.HandleValidClick(move.To);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Player 교체 또는 씬 종료에 따른 재생 취소입니다.
                }
                finally
                {
                    if (ReferenceEquals(_playbackCts, cts))
                        _playbackCts = null;
                }
            }
        }

        private void RotateCamera()
        {
            var options = JanggiOptionStore.JanggiSetting;

            var localTeam = options.GameMode == GameModeType.Network
                ? NetworkSessionStore.MyTeam
                : options.PlayerCho == PlayerType.Local
                    ? PlayerTeam.Cho
                    : PlayerTeam.Han;
            if (localTeam == PlayerTeam.Han)
            {
                _camera.transform.position = new Vector3(4, 9, 6);
                _camera.transform.eulerAngles = new Vector3(90, 0, 180);
            }
            else
            {
                _camera.transform.position = new Vector3(4, 9, 3);
                _camera.transform.eulerAngles = new Vector3(90, 0, 0);
            }
        }
    }
}
