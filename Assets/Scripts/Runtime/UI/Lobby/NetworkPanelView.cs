using UnityEngine;

namespace YuJanggi.Runtime.UI
{
    using TMPro;
    using Core.V2.Domain;
    using Network.Status;
    using Lobby.Matching;

    public class NetworkPanelView : UIVisible
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _statusDetailText;
        [SerializeField] private TMP_Dropdown _formationDropDown;

        public int Selected
            => _formationDropDown.value;
        private void ClearText()
        {
            _statusText.SetText(string.Empty);
            _statusDetailText.SetText(string.Empty);
        }
        public void ChangeMessage(in NetworkStatus status, int? timerSeconds = null)
        {

            ClearText();
            NetworkError error = status.Error ?? NetworkError.None;
            bool failed = error != NetworkError.None;
            _formationDropDown.interactable = !failed &&
                status.NetworkState == NetworkState.Online && status.MatchingState == MatchingState.Matched;

            string title = !failed && status.NetworkState == NetworkState.Online
                ? "Online" : "Offline";
            if (!failed && status.NetworkState == NetworkState.Online &&
                status.MatchingState == MatchingState.Matched)
                title = status.Team is PlayerTeam.Cho or PlayerTeam.Han
                    ? $"선택된 진영: {status.Team}"
                    : "선택된 진영: 확인 불가";
            string detail = failed
                ? GetFailureReason(error)
                : timerSeconds.HasValue
                    ? GetTimerText(status.MatchingState, timerSeconds.Value)
                    : GetConnectionStage(status);

            _statusText.SetText(title);
            _statusDetailText.SetText(detail);

            if (failed)
                Debug.LogWarning($"Client: {title} - {detail} ({error}) {status.Message}");
            else
                Debug.Log($"Client: {title} - {detail}");
        }

        /// <summary>로비가 계산한 매칭 대기 시간 또는 게임 진입 카운트다운을 표시합니다.</summary>
        public void UpdateTimer(MatchingState state, int seconds)
        {
            _statusDetailText.SetText(GetTimerText(state, seconds));
        }

        /// <summary>로비에서 전달한 포진 제출·서버 준비 상태를 표시합니다.</summary>
        public void ShowFormationProgress(string message)
        {
            _formationDropDown.interactable = false;
            _statusDetailText.SetText(message);
        }

        private static string GetTimerText(MatchingState state, int seconds)
        {
            if (state == MatchingState.Matching)
                return $"매칭: {seconds / 60:00}분{seconds % 60:00}초";

            return $"남은 시간: {seconds}초";
        }
        private static string GetConnectionStage(in NetworkStatus status)
        {
            if (status.ConnectionState == ConnectionState.Connected)
            {
                return status.MatchingState switch
                {
                    MatchingState.Requesting => "매칭 신청 중",
                    MatchingState.Matching => "매칭 중",
                    MatchingState.Matched => "매칭 완료",
                    _ => "연결 완료"
                };
            }
            return status.ConnectionState switch
            {
                ConnectionState.Disconnected => "연결 해제",
                ConnectionState.Connecting => "연결 중",
                ConnectionState.Handshaking => "버전 확인 중",
                ConnectionState.Connected => "연결 완료",
                _ => "상태 확인 불가"
            };
        }

        private static string GetFailureReason(NetworkError error)
        {
            bool coreMismatch = (error & NetworkError.CoreVersionMismatch) != 0;
            bool protocolMismatch = (error & NetworkError.ProtocolVersionMismatch) != 0;
            if (coreMismatch && protocolMismatch)
                return "게임·통신 버전 불일치";
            if (coreMismatch)
                return "게임 버전 불일치";
            if (protocolMismatch)
                return "통신 버전 불일치";

            if ((error & NetworkError.AuthenticationFailed) != 0)
                return "인증 실패";
            if ((error & NetworkError.ServerError) != 0)
                return "서버 오류";
            if ((error & NetworkError.ConnectionLost) != 0)
                return "연결 끊김";
            return "서버 연결 실패";
        }
    }
}
