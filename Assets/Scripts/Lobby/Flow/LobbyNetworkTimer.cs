#nullable enable
using System;
using YuJanggi.Lobby.Matching;
using YuJanggi.Network.Status;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class LobbyNetworkTimer
    {
        private const int GameStartCountdownSeconds = 10;

        private bool _visible;
        private double _startedAt;
        private int _lastDisplayedSeconds = -1;

        public MatchingState State { get; private set; } = MatchingState.Idle;
        public string? MatchId { get; private set; }

        public void Update(in NetworkStatus status, string? matchId, double now)
        {
            bool visible = (status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Connected &&
                (status.MatchingState == MatchingState.Matching ||
                 status.MatchingState == MatchingState.Matched);

            if (visible && (!_visible || State != status.MatchingState ||
                (status.MatchingState == MatchingState.Matched && MatchId != matchId)))
                _startedAt = now;

            State = status.MatchingState;
            MatchId = matchId;
            _visible = visible;
        }

        public int? GetSecondsForDisplay(double now)
        {
            int? seconds = _visible ? GetSeconds(now) : (int?)null;
            _lastDisplayedSeconds = seconds ?? -1;
            return seconds;
        }

        public bool TryGetChangedSeconds(double now, out int seconds)
        {
            seconds = default;
            if (!_visible)
                return false;

            seconds = GetSeconds(now);
            if (seconds == _lastDisplayedSeconds)
                return false;

            _lastDisplayedSeconds = seconds;
            return true;
        }

        public void Stop() => _visible = false;

        private int GetSeconds(double now)
        {
            int elapsed = (int)Math.Floor(now - _startedAt);
            return State == MatchingState.Matched
                ? Math.Max(0, GameStartCountdownSeconds - elapsed) : elapsed;
        }
    }
}
