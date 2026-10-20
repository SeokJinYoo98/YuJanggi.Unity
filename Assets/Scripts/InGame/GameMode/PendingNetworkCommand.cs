#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.InGame.Mode
{
    using Network;

    internal sealed class PendingNetworkCommand
    {
        private sealed class Command
        {
            internal readonly UniTaskCompletionSource Completion = new();
            internal readonly bool WaitForGameEnd;
            internal bool IsPending = true;

            internal Command(bool waitForGameEnd)
                => WaitForGameEnd = waitForGameEnd;
        }

        private readonly NetworkManager _network;
        private Command? _current;

        internal bool IsPending => _current?.IsPending == true;

        internal PendingNetworkCommand(NetworkManager network)
            => _network = network;

        internal async UniTask ExecuteAsync(
            Func<CancellationToken, UniTask<bool>> sendRequest,
            CancellationToken token,
            CancellationToken gameToken,
            Action<Exception> onFailure,
            bool waitForGameEnd = false)
        {
            if (IsPending || gameToken.IsCancellationRequested)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                token, gameToken, _network.ConnectionToken);
            var command = new Command(waitForGameEnd);
            _current = command;

            try
            {
                if (!await sendRequest(cts.Token))
                    return;

                await WaitForEventAsync(command, cts.Token);
            }
            catch (OperationCanceledException) when (gameToken.IsCancellationRequested)
            {
                // 게임/씬 종료에 따른 취소는 서버 결과 실패로 처리하지 않습니다.
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(_current, command)
                    && command.Completion.Task.Status != UniTaskStatus.Succeeded)
                    onFailure(exception);
            }
            finally
            {
                // 이전 명령의 늦은 완료가 새 명령의 대기를 해제하지 않습니다.
                if (ReferenceEquals(_current, command))
                    _current = null;
            }
        }

        internal void CompleteTurn(bool isLocalTurn, Action notifyTurnCompleted)
        {
            var command = isLocalTurn && _current is { WaitForGameEnd: false }
                ? _current
                : null;
            if (command is not null)
                command.IsPending = false;

            notifyTurnCompleted();
            command?.Completion.TrySetResult();
        }

        internal void CompleteGame(Action notifyGameEnded)
        {
            var command = Detach();
            try
            {
                notifyGameEnded();
            }
            finally
            {
                command?.Completion.TrySetResult();
            }
        }

        internal void Cancel()
            => Detach()?.Completion.TrySetCanceled();

        private Command? Detach()
        {
            var command = _current;
            _current = null;
            return command;
        }

        private async UniTask WaitForEventAsync(Command command, CancellationToken token)
        {
            using var timeoutCts = new CancellationTokenSource();
            using var timeout = timeoutCts.CancelAfterSlim(_network.RequestTimeout, DelayType.Realtime);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
            try
            {
                await command.Completion.Task.AttachExternalCancellation(cts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                var eventName = command.WaitForGameEnd ? "GameEndedEvent" : "TurnCompletedEvent";
                throw new TimeoutException(
                    $"승인 후 {eventName} 대기 제한시간({_network.RequestTimeout.TotalSeconds}초)을 초과했습니다.");
            }
        }
    }
}
