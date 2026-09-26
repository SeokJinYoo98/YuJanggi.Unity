#nullable enable
using YuJanggi.Core.V2.Domain;

namespace YuJanggi.Lobby.Matching
{
    /// <summary>조회 시점의 매칭 상태를 담는 변경 불가능한 값입니다.</summary>
    public readonly struct MatchingSnapshot
    {
        public MatchingState State { get; }
        public MatchInfo? CurrentMatch { get; }
        public Formation? SelectedFormation { get; }
        public Formation? SubmittedFormation { get; }
        public Formation? ChoFormation { get; }
        public Formation? HanFormation { get; }
        public bool IsFormationSubmitting { get; }

        public string? MatchId => CurrentMatch?.MatchId;
        public PlayerTeam Team => CurrentMatch?.Team ?? PlayerTeam.None;
        // 양측 포진은 Service에서 GameReady 적용 시에만 함께 설정하고 Reset에서 제거합니다.
        public bool IsGameReady => ChoFormation.HasValue && HanFormation.HasValue;

        public MatchingSnapshot(MatchingState state, MatchInfo? currentMatch,
            Formation? selectedFormation, Formation? submittedFormation,
            Formation? choFormation, Formation? hanFormation, bool isFormationSubmitting)
        {
            State = state;
            CurrentMatch = currentMatch;
            SelectedFormation = selectedFormation;
            SubmittedFormation = submittedFormation;
            ChoFormation = choFormation;
            HanFormation = hanFormation;
            IsFormationSubmitting = isFormationSubmitting;
        }
    }
}
