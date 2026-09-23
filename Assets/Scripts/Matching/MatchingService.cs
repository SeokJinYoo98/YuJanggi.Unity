#nullable enable
using System;
using YuJanggi.Core.V2.Domain;

namespace YuJanggi.Matching
{
    /// <summary>매칭 상태, 참가자 정보와 포진 제출의 로컬 규칙을 관리합니다.</summary>
    public sealed class MatchingService
    {
        private bool _requestInProgress;
        private bool _cancelInProgress;

        public MatchingState State { get; private set; } = MatchingState.Idle;
        public MatchInfo? CurrentMatch { get; private set; }
        public string? MatchId => CurrentMatch?.MatchId;
        public PlayerTeam Team => CurrentMatch?.Team ?? PlayerTeam.None;
        public Formation? SelectedFormation { get; private set; }
        public Formation? SubmittedFormation { get; private set; }
        public bool IsFormationSubmitting { get; private set; }
        public event Action? OnDataChanged;

        public void BeginRequest()
        {
            EnsureOperationAllowed(MatchingState.Idle);
            _requestInProgress = true;
            ChangeState(MatchingState.Requesting);
        }

        public void MatchingAccepted()
        {
            if (State == MatchingState.Requesting)
                ChangeState(MatchingState.Matching);
        }

        public void EndRequest()
        {
            _requestInProgress = false;
            if (State == MatchingState.Requesting)
                ChangeState(MatchingState.Idle);
        }

        public void BeginCancel()
        {
            EnsureOperationAllowed(MatchingState.Matching);
            _cancelInProgress = true;
        }

        public void MatchingCancelled()
        {
            // TODO:
            // 취소 응답보다 매칭 확정이 먼저 적용되면 이미 Matched일 수 있습니다.
            // 현재는 늦은 취소 성공으로 확정된 매칭 정보를 지우지 않습니다.
            // MatchSession / GameSession에서 서버의 취소·확정 경쟁 정책에 맞춰 복구를 결정해야 합니다.
            if (State == MatchingState.Matched)
                return;
            CurrentMatch = null;
            ChangeState(MatchingState.Idle);
        }

        public void EndCancel() => _cancelInProgress = false;

        public void ApplyMatchFound(MatchInfo match)
        {
            if (CurrentMatch?.MatchId == match.MatchId)
                return;
            if (State != MatchingState.Matching)
            {
                // TODO:
                // 취소 완료 뒤 늦은 확정이나 이미 매칭된 상태에서 다른 매치가 도착할 수 있습니다.
                // 현재는 대기 중이 아니면 무시하여 기존 로컬 상태를 보존합니다.
                // MatchSession / GameSession에서 서버 상태 재조회와 확정 우선순위를 결정해야 합니다.
                return;
            }
            if (string.IsNullOrWhiteSpace(match.MatchId) ||
                match.Team is not (PlayerTeam.Cho or PlayerTeam.Han) ||
                match.OpponentTeam is not (PlayerTeam.Cho or PlayerTeam.Han) ||
                match.Team == match.OpponentTeam)
                throw new InvalidOperationException("잘못된 매칭 정보입니다.");

            CurrentMatch = match;
            ChangeState(MatchingState.Matched);
        }

        public void SelectFormation(Formation formation)
        {
            if (State != MatchingState.Matched || IsFormationSubmitting || SubmittedFormation.HasValue)
                throw new InvalidOperationException("현재는 포진을 변경할 수 없습니다.");
            if (!Enum.IsDefined(typeof(Formation), formation))
                throw new ArgumentOutOfRangeException(nameof(formation));
            SelectedFormation = formation;
        }

        public void BeginFormationSubmit(Formation formation)
        {
            SelectFormation(formation);
            IsFormationSubmitting = true;
        }

        public void FormationAccepted()
        {
            SubmittedFormation = SelectedFormation;
        }

        public void EndFormationSubmit()
        {
            IsFormationSubmitting = false;
            OnDataChanged?.Invoke();
        }

        public void Reset()
        {
            _requestInProgress = false;
            _cancelInProgress = false;
            IsFormationSubmitting = false;
            CurrentMatch = null;
            SelectedFormation = null;
            SubmittedFormation = null;
            ChangeState(MatchingState.Idle);
            // TODO:
            // 매칭 확정 직후 연결이 끊겨도 서버에는 매치가 남아 있을 수 있습니다.
            // 현재 로컬 정보는 초기화하며 재연결 시 이전 매치를 복원하지 않습니다.
            // MatchSession / GameSession에서 재접속 복원과 이탈 정책을 결정해야 합니다.
        }

        private void EnsureOperationAllowed(MatchingState requiredState)
        {
            if (State != requiredState)
                throw new InvalidOperationException($"현재 상태에서는 매칭 요청을 처리할 수 없습니다: {State}");
            if (_requestInProgress || _cancelInProgress)
                throw new InvalidOperationException("이미 매칭 신청 또는 취소 응답을 기다리고 있습니다.");
        }

        private void ChangeState(MatchingState state)
        {
            State = state;
            OnDataChanged?.Invoke();
        }
    }
}
