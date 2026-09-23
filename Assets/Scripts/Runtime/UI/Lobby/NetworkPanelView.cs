using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace YuJanggi.Runtime.UI
{
    using Network;
    using TMPro;
    using YuJanggi.Network.V2.Status;

    public class NetworkPanelView : UIVisible
    {
 
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _statusDetailText;
        private void OnDestroy()
        {
            
        }
        private void ClearText()
        {
            _statusText.SetText(string.Empty);
            _statusDetailText.SetText(string.Empty);
        }
        public void ChangeMessage(in NetworkStatus status)
        {

            ClearText();
            NetworkError error = status.Error ?? NetworkError.None;
            bool failed = error != NetworkError.None;
            string title = !failed && status.NetworkState == NetworkState.Online
                ? "Online" : "Offline";
            string detail = failed
                ? GetFailureReason(error)
                : GetConnectionStage(status.ConnectionState);

            _statusText.SetText(title);
            _statusDetailText.SetText(detail);

            if (failed)
                Debug.LogWarning($"Client: {title} - {detail} ({error}) {status.Message}");
            else
                Debug.Log($"Client: {title} - {detail}");
        }

        private static string GetConnectionStage(ConnectionState state)
        {
            return state switch
            {
                ConnectionState.Disconnected => "연결 해제",
                ConnectionState.Connecting => "연결 중",
                ConnectionState.Handshaking => "버전 확인 중",
                ConnectionState.Connected => "연결 완료",
                ConnectionState.Matching => "매칭 중",
                ConnectionState.Matched => "매칭 완료",
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
