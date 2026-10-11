using System;
using YuJanggi.Engine.Domain;

namespace YuJanggi.Network
{
    internal enum NetworkMoveResult
    {
        Accepted, NotYourTurn, PieceNotFound, IllegalMove, OutOfRange, InvalidResponse
    }

    internal enum NetworkGameEndResult { Accepted, Mismatch, Failed, InvalidResponse }

    internal readonly struct NetworkGameStartData
    {
        public DateTimeOffset StartedAt { get; }
        public NetworkGameStartData(DateTimeOffset startedAt) => StartedAt = startedAt;
    }

    internal readonly struct NetworkMoveData
    {
        public PlayerTeam Team { get; }
        public Pos From { get; }
        public Pos To { get; }
        public NetworkMoveData(PlayerTeam team, Pos from, Pos to)
        {
            Team = team;
            From = from;
            To = to;
        }
    }

    internal readonly struct NetworkGameEndedData
    {
        public PlayerTeam Winner { get; }
        public int TotalMoves { get; }
        public NetworkGameEndedData(PlayerTeam winner, int totalMoves)
        {
            Winner = winner;
            TotalMoves = totalMoves;
        }
    }

    internal enum NetworkGameEventType { Move, Ended }

    // 이동과 종료 Event를 하나의 수신 순서로 소비합니다.
    internal readonly struct NetworkGameEvent
    {
        public NetworkGameEventType Type { get; }
        public NetworkMoveData Move { get; }
        public NetworkGameEndedData Ended { get; }
        public NetworkGameEvent(NetworkMoveData move)
        {
            Type = NetworkGameEventType.Move;
            Move = move;
            Ended = default;
        }
        public NetworkGameEvent(NetworkGameEndedData ended)
        {
            Type = NetworkGameEventType.Ended;
            Move = default;
            Ended = ended;
        }
    }
}
