#nullable enable
using System;
using YuJanggi.Engine.Domain;
using YuJanggi.Lobby.Matching;
using YuJanggi.Protocol.Matching;

namespace YuJanggi.Lobby.Network
{
    /// <summary>Protocol 메시지와 무관한 로비 매칭 상태 전이입니다.</summary>
    internal sealed class LobbyNetworkService
    {
        private bool _requestInProgress;
        private bool _cancelInProgress;
        private bool _gameReadyDelivered;
        private (Formation Cho, Formation Han)? _readyFormations;

        public MatchingState State { get; private set; } = MatchingState.Idle;
        public MatchInfo? Match { get; private set; }
        public Formation? SelectedFormation { get; private set; }
        public Formation? SubmittedFormation { get; private set; }
        public bool IsFormationSubmitting { get; private set; }
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
            // 매칭 확정이 취소 응답보다 먼저 도착한 경우 확정 상태를 유지합니다.
            if (State == MatchingState.Matched)
                return;
            ChangeState(MatchingState.Idle);
        }

        public void EndCancel() => _cancelInProgress = false;

        public void MatchingFound(MatchInfo match)
        {
            if (State == MatchingState.Matching)
            {
                Match = match;
                ChangeState(MatchingState.Matched);
            }
        }

        public void BeginFormationSubmit(Formation formation)
        {
            if (State != MatchingState.Matched || IsFormationSubmitting || SubmittedFormation.HasValue)
                throw new InvalidOperationException("현재는 포진을 변경할 수 없습니다.");
            if (!Enum.IsDefined(typeof(Formation), formation))
                throw new ArgumentOutOfRangeException(nameof(formation));
            SelectedFormation = formation;
            IsFormationSubmitting = true;
        }

        public void FormationSent()
        {
            SubmittedFormation = SelectedFormation;
        }

        public void EndFormationSubmit()
        {
            IsFormationSubmitting = false;
            OnDataChanged?.Invoke();
        }

        public bool TryAcceptGameReady(Formation cho, Formation han)
        {
            if (State != MatchingState.Matched || !SubmittedFormation.HasValue || _gameReadyDelivered)
                return false;
            _gameReadyDelivered = true;
            _readyFormations = (cho, han);
            return true;
        }

        public bool TryGetReadyFormations(out Formation cho, out Formation han)
        {
            cho = default;
            han = default;
            if (!_gameReadyDelivered || !_readyFormations.HasValue)
                return false;
            (cho, han) = _readyFormations.Value;
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
            Match = null;
            _readyFormations = null;
            ChangeState(MatchingState.Idle);
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
