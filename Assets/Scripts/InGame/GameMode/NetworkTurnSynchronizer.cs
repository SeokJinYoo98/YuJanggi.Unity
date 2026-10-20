#nullable enable
using System;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Network;

    internal enum NetworkTurnApplyStatus { Ignored, Applied, Desynced }

    internal sealed class NetworkTurnSynchronizer
    {
        private readonly IJanggiEngine _engine;
        private TurnData? _appliedTurn;

        internal NetworkGameStartData? ServerGameStart { get; private set; }
        internal NetworkMoveData? LastServerTurn { get; private set; }

        internal int? ServerTurnNumber => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTeam == PlayerTeam.None
                ? (int?)null
                : LastServerTurn.Value.TurnNumber + 1
            : ServerGameStart?.TurnNumber;
        internal PlayerTeam ServerTeam => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTeam
            : ServerGameStart?.CurrentTeam ?? PlayerTeam.None;
        internal DateTimeOffset? ServerDeadlineAt => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTurnDeadlineAt
            : ServerGameStart?.TurnDeadlineAt;

        internal NetworkTurnSynchronizer(IJanggiEngine engine)
            => _engine = engine;

        internal void Reset()
        {
            ServerGameStart = null;
            LastServerTurn = null;
            _appliedTurn = null;
        }

        internal void ApplyGameStart(NetworkGameStartData data)
        {
            ServerGameStart = data;
            LastServerTurn = null;
        }

        internal (int cho, int han) GetServerTimerDisplay()
        {
            var team = ServerTeam;
            if (ServerDeadlineAt is not { } deadline || team == PlayerTeam.None)
                return (0, 0);

            var remaining = Math.Max(0,
                (int)Math.Ceiling((deadline - DateTimeOffset.UtcNow).TotalSeconds));
            // 대기 중인 팀은 기존 Timer UI처럼 턴 제한시간을 표시합니다.
            var turnTime = Math.Max(0, (int)Math.Ceiling(ServerGameStart?.TurnTimeSeconds ?? 0f));
            return team == PlayerTeam.Cho ? (remaining, turnTime) : (turnTime, remaining);
        }

        internal void CaptureTurnCompleted(TurnData data)
            => _appliedTurn = data;

        internal NetworkTurnApplyStatus ApplyTurn(
            NetworkMoveData data,
            bool isEnd,
            out TurnData? appliedTurn,
            out string? message)
        {
            appliedTurn = null;

            if (ServerTurnNumber is not { } expectedTurn)
            {
                message = "적용할 서버 턴이 없습니다.";
                return NetworkTurnApplyStatus.Desynced;
            }

            if (data.TurnNumber < expectedTurn)
            {
                message = $"과거 턴 무시: received={data.TurnNumber}, expected={expectedTurn}";
                return NetworkTurnApplyStatus.Ignored;
            }

            if (data.TurnNumber > expectedTurn)
            {
                message = "TurnNumber gap이 발생했습니다.";
                return NetworkTurnApplyStatus.Desynced;
            }

            message = ValidateBeforeApply(data, isEnd, expectedTurn, out var recordCount);
            if (message is not null)
                return NetworkTurnApplyStatus.Desynced;

            var turn = ApplyToEngine(data, out message);
            if (message is not null)
                return NetworkTurnApplyStatus.Desynced;

            message = ValidateAfterApply(data, turn, recordCount);
            if (message is not null)
                return NetworkTurnApplyStatus.Desynced;

            LastServerTurn = data;
            appliedTurn = turn;
            return NetworkTurnApplyStatus.Applied;
        }

        private string? ValidateBeforeApply(
            NetworkMoveData data,
            bool isEnd,
            int expectedTurn,
            out int recordCount)
        {
            recordCount = 0;
            // IsEnd는 StartEngine 완료 전에도 true입니다. 준비 전 이벤트는 적용하거나 보관하지 않습니다.
            if (isEnd || _engine.References.Turn.IsEnd)
                return "Engine이 시작되지 않았거나 이미 종료되었습니다.";

            recordCount = _engine.References.ReadOnlyRecord.Count;
            if (recordCount + 1 != expectedTurn
                || data.Team != ServerTeam
                || data.Team != _engine.ControllerQuery.CurrentTurn)
                return "적용 전 Engine 턴/기록과 서버 상태가 일치하지 않습니다.";

            return null;
        }

        private TurnData? ApplyToEngine(NetworkMoveData data, out string? error)
        {
            _appliedTurn = null;
            error = null;
            try
            {
                switch (data.Action)
                {
                    case NetworkTurnAction.Move:
                        if (!_engine.TryProcessTurn(data.From, data.To))
                        {
                            error = "Engine.TryProcessTurn이 실패했습니다.";
                            return null;
                        }
                        break;

                    case NetworkTurnAction.Pass:
                        _engine.HandleHandicap();
                        break;

                    default:
                        error = "지원하지 않는 턴 동작입니다.";
                        return null;
                }

                return _appliedTurn;
            }
            catch (Exception exception)
            {
                error = $"Engine 적용 중 예외가 발생했습니다: {exception}";
                return null;
            }
            finally
            {
                _appliedTurn = null;
            }
        }

        private string? ValidateAfterApply(NetworkMoveData data, TurnData? turn, int recordCount)
        {
            if (turn is null
                || _engine.References.ReadOnlyRecord.Count != recordCount + 1
                || turn.MoveCount != data.TurnNumber
                || turn.ActingTeam != data.Team
                || (turn.MovedRecord is not null) != (data.Action == NetworkTurnAction.Move))
                return "Engine의 턴 완료 결과/기록을 확인할 수 없습니다.";

            if (data.NextTeam == PlayerTeam.None)
            {
                // 정상 종료 시 Engine은 마지막 행동 팀을 유지하므로 CurrentTurn과 None을 비교하지 않습니다.
                if (!_engine.References.Turn.IsEnd || !turn.GameResult.HasValue)
                    return "서버는 종료 턴이지만 Engine의 GameResult/종료 상태가 없습니다.";
            }
            else if (_engine.References.Turn.IsEnd
                || turn.GameResult.HasValue
                || data.NextTeam != _engine.ControllerQuery.CurrentTurn)
            {
                return "서버 NextTeam과 Engine의 다음 턴/종료 상태가 일치하지 않습니다.";
            }

            return null;
        }
    }
}
