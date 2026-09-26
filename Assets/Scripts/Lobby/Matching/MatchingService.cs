#nullable enable
using System;
using YuJanggi.Core.V2.Domain;

namespace YuJanggi.Lobby.Matching
{
    /// <summary>매칭 진행 상태와 포진 제출의 로컬 규칙을 관리합니다.</summary>
    internal sealed class MatchingService
    {
        private bool _requestInProgress;
        private bool _cancelInProgress;

        public MatchingState State { get; private set; } = MatchingState.Idle;

        public Formation? SelectedFormation { get; private set; }
        public Formation? SubmittedFormation { get; private set; }
        public bool IsFormationSubmitting { get; private set; }


        private bool _gameReadyDelivered;
        public bool HasDeliveredGameReady => _gameReadyDelivered;

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
            ChangeState(MatchingState.Idle);
        }

        public void EndCancel() => _cancelInProgress = false;

        public void MatchingFound()
        {
            if (State == MatchingState.Matching)
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

        // 세션 데이터는 보관하지 않고 같은 준비 이벤트의 중복 전달만 방지합니다.
        public bool TryAcceptGameReady()
        {
            if (State != MatchingState.Matched || !SubmittedFormation.HasValue || _gameReadyDelivered)
                return false;
            _gameReadyDelivered = true;
            return true;
        }

        public void Reset()
        {
            _requestInProgress = false;
            _cancelInProgress = false;
            IsFormationSubmitting = false;
            SelectedFormation = null;
            SubmittedFormation = null;
            _gameReadyDelivered = false;
            ChangeState(MatchingState.Idle);
            // TODO:
            // 매칭 확정 직후 연결이 끊겨도 서버에는 매치가 남아 있을 수 있습니다.
            // 현재 매칭 진행 상태는 초기화하며 재연결 시 이전 매치를 복원하지 않습니다.
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
