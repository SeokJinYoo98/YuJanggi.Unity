#nullable enable
using System;

namespace YuJanggi.Network.Status
{
    using Lobby.Matching;
    public readonly struct NetworkStatus
    {
        public ConnectionState      ConnectionState { get; }
        public NetworkError?        Error { get; }
        public string?              Message { get; }
        public MatchingState        MatchingState { get; }

        public NetworkStatus(
            ConnectionState connectionState,
            NetworkError?   error,
            string?         message,
            MatchingState   matchingState = MatchingState.Idle)
        {
            ConnectionState = connectionState;
            Error           = error;
            Message         = message;
            MatchingState   = matchingState;
        }
    }
    public enum ConnectionState
    {
        Disconnected, Connecting, Handshaking, Connected,
    }

    [Flags]
    public enum NetworkError
    {
        None = 0,
        ConnectionFailed = 1 << 0,
        ConnectionLost = 1 << 1,
        CoreVersionMismatch = 1 << 2,
        ProtocolVersionMismatch = 1 << 3,
        AuthenticationFailed = 1 << 4,
        ServerError = 1 << 5
    }
}

