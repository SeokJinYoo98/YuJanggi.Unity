using Cysharp.Threading.Tasks;
using System.Threading;

namespace YuJanggi.InGame.Service
{
    internal sealed class InGameService
    {
        private UniTaskCompletionSource _gameStarted = new();

        public bool IsGameStarted { get; private set; }

        public UniTask WaitUntilGameStartedAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return IsGameStarted
                ? UniTask.CompletedTask
                : _gameStarted.Task.AttachExternalCancellation(cancellationToken);
        }

        public void ApplyGameStart()
        {
            if (IsGameStarted)
                return;
            IsGameStarted = true;
            _gameStarted.TrySetResult();
        }

        public void Reset()
        {
            var previous = _gameStarted;
            IsGameStarted = false;
            _gameStarted = new UniTaskCompletionSource();
            previous.TrySetCanceled();
        }
    }
}
