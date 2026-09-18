using System;
using YuJanggiCommon;

namespace YuJanggi.Runtime.Network.Protocol
{
    /// <summary>
    /// Creates client-originated messages defined by the shared server contract.
    /// Network transport and Unity presentation remain outside this class.
    /// </summary>
    public static class ServerMessageFactory
    {
        public static ChatMessage CreateJoin(string playerName)
            => Create(MessageType.Join, new JoinRequest(playerName));

        public static ChatMessage CreateMatchmakingStart()
            => Create(MessageType.MatchmakingStart, new MatchmakingStartRequest());

        public static ChatMessage CreateMatchmakingCancel()
            => Create(MessageType.MatchmakingCancel, new MatchmakingCancelRequest());

        public static ChatMessage CreateLegalMovesRequest(BoardPosition from)
            => Create(MessageType.LegalMovesRequest, new LegalMovesRequest(from));

        public static ChatMessage CreateMoveRequest(BoardPosition from, BoardPosition to)
            => Create(MessageType.MoveRequest, new MoveRequest(from, to));

        public static ChatMessage CreateGameChat(string message)
            => Create(MessageType.GameChatSend, new GameChatSendRequest(message));

        private static ChatMessage Create<TPayload>(MessageType type, TPayload payload)
            => ChatMessage.Create(type, Guid.NewGuid().ToString("N"), payload);
    }
}
