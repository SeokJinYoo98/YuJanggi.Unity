#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;



namespace YuJanggi.InGame.Handler
{
    using Network.Handler;
    using Protocol.InGame;
    
    using Protocol.Messages;
    using System;
    using YuJanggi.Network;

    /// <summary>Protocol 생성·해석과 대기만 담당하며 게임 상태나 화면은 조작하지 않습니다.</summary>
    internal sealed class InGameNetworkHandler : NetworkHandler
    {
        private UniTaskCompletionSource? _gameStart;
        public InGameNetworkHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        { }

        public async UniTask<bool> SendInGameReadyAsync(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var previous = _gameStart;
            _gameStart = new UniTaskCompletionSource();

            previous?.TrySetCanceled();
            await SendAsync(
                ClientMessageType.GameSceneReady,
                new GameSceneReady(),
                token);

            return true;
        }
        public async UniTask WaitForGameStartEvent(
            CancellationToken token)
        {
            EnsureAvailable(token);

            var completion = _gameStart
                ?? throw new InvalidOperationException(
                    "게임 시작 대기가 준비되지 않았습니다.");

            await completion.Task.AttachExternalCancellation(token);
        }
        protected override void OnHandleMessage(ServerMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}


