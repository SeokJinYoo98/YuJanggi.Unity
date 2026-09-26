#nullable enable
using System;
using YuJanggi.Core.V2.Domain;

namespace YuJanggi.Network.Status
{
    using Lobby.Matching;
    public readonly struct NetworkStatus
    {
        public NetworkState         NetworkState { get; }
        public ConnectionState      ConnectionState { get; }
        public NetworkError?        Error { get; }
        public string?              Message { get; }
        public MatchInfo?           CurrentMatch { get; }
        public PlayerTeam           Team { get; }
        public MatchingState        MatchingState { get; }

        public NetworkStatus(
            NetworkState    networkState,
            ConnectionState connectionState,
            NetworkError?   error,
            string?         message,
            MatchInfo?      currentMatch = null,
            PlayerTeam      team = PlayerTeam.None,
            MatchingState   matchingState = MatchingState.Idle)
        {
            NetworkState    = networkState;
            ConnectionState = connectionState;
            Error           = error;
            Message         = message;
            CurrentMatch    = currentMatch;
            Team            = team;
            MatchingState   = matchingState;
        }
    }
    public enum NetworkState
    {
        Offline, Online
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

