#nullable enable
using System;

namespace YuJanggi.Network.V2.Status
{
    public readonly struct NetworkStatus
    {
        public NetworkState         NetworkState { get; }
        public ConnectionState      ConnectionState { get; }
        public NetworkError?        Error { get; }
        public string?              Message { get; }

        public NetworkStatus(
            NetworkState    networkState,
            ConnectionState connectionState,
            NetworkError?   error,
            string?         message)
        {
            NetworkState    = networkState;
            ConnectionState = connectionState;
            Error           = error;
            Message         = message;
        }
    }
    public enum NetworkState
    {
        Offline, Online
    }
    public enum ConnectionState
    {
        Disconnected, Connecting, Handshaking, Connected, Matching, Matched
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

