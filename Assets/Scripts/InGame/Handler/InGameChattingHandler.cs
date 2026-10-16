using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace YuJanggi.InGame.Handler
{
    using Engine.Domain;

    using Protocol.Chat;
    using Protocol.Messages;

    using Network;
    using Network.Handler;
    using Network.Mapper;

    internal sealed class InGameChattingHandler : NetworkHandler
    {
        private bool _sendPending;
        public event Action<PlayerTeam, string> OnChatReceived;
        public InGameChattingHandler(
            NetworkClient client,
            RequestDispatcher requests)
            : base(client, requests)
        {
        }


        protected override void OnHandleMessage(ServerMessage message)
        {
            if (message.Type != ServerMessageType.ChatEvent)
                return;

            var chat = message.GetPayload<ChatEvent>();
            var team = ProtocolMapper.ToPlayerTeam(chat.Team);

            OnChatReceived?.Invoke(team, chat.Message);
        }

        public async UniTask<bool> SendMessageAsync(
            string message,
            CancellationToken token = default)
        {
            if (_sendPending)
                return false;

            if (string.IsNullOrWhiteSpace(message)
                || message.Length > ChatRules.MaxLen)
                return false;

            _sendPending = true;

            try
            {
                var response = await SendRequestAsync(
                    ClientMessageType.ChatRequest,
                    new ChatRequest { Message = message },
                    ServerMessageType.ChatResponse,
                    token);

                var result = response.GetPayload<ChatResponse>().Result;

                if (result != ChatResult.Accepted)
                {
                    OnChatReceived?.Invoke(
                        PlayerTeam.None,
                        "전송하지 못했습니다. 잠시 후 다시 보내주세요.");

                    return false;
                }

                return true;
            }
            finally
            {
                _sendPending = false;
            }
        }
    }
}
