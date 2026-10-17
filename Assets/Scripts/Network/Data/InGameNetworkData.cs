using System;
using YuJanggi.Engine.Domain;

namespace YuJanggi.Network
{
    internal enum NetworkMoveResult
    {
        Accepted, NotYourTurn, PieceNotFound, IllegalMove, OutOfRange, InvalidResponse
    }

    internal enum NetworkGameEndResult { Accepted, Mismatch, Failed, InvalidResponse }

    internal enum NetworkTurnAction { Move, Pass }
    internal enum NetworkPassReason { None, UserRequest, Timeout }

    internal readonly struct NetworkGameStartData
    {
        public DateTimeOffset StartedAt { get; }
        public float TurnTimeSeconds { get; }
        public int TurnNumber { get; }
        public PlayerTeam CurrentTeam { get; }
        public DateTimeOffset? TurnDeadlineAt { get; }

        public NetworkGameStartData(
            DateTimeOffset startedAt,
            float turnTimeSeconds,
            int turnNumber,
            PlayerTeam currentTeam,
            DateTimeOffset? turnDeadlineAt)
        {
            StartedAt = startedAt;
            TurnTimeSeconds = turnTimeSeconds;
            TurnNumber = turnNumber;
            CurrentTeam = currentTeam;
            TurnDeadlineAt = turnDeadlineAt;
        }
    }

    internal readonly struct NetworkMoveData
    {
        public PlayerTeam Team { get; }
        public NetworkTurnAction Action { get; }
        public Pos From { get; }
        public Pos To { get; }
        public int TurnNumber { get; }
        public DateTimeOffset CompletedAt { get; }
        public PlayerTeam NextTeam { get; }
        public DateTimeOffset? NextTurnDeadlineAt { get; }
        public NetworkPassReason PassReason { get; }

        public NetworkMoveData(
            PlayerTeam team,
            NetworkTurnAction action,
            Pos from,
            Pos to,
            int turnNumber,
            DateTimeOffset completedAt,
            PlayerTeam nextTeam,
            DateTimeOffset? nextTurnDeadlineAt,
            NetworkPassReason passReason)
        {
            Team = team;
            Action = action;
            From = from;
            To = to;
            TurnNumber = turnNumber;
            CompletedAt = completedAt;
            NextTeam = nextTeam;
            NextTurnDeadlineAt = nextTurnDeadlineAt;
            PassReason = passReason;
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
