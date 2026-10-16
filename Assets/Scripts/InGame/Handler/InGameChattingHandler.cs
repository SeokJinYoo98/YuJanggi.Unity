



namespace YuJanggi.InGame.Handler
{
    using Protocol.Messages;

    using Network.Handler;
    using YuJanggi.Network;

    internal sealed class InGameChattingHandler : NetworkHandler
    {
        public InGameChattingHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        {
        }

        protected override void OnHandleMessage(ServerMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
