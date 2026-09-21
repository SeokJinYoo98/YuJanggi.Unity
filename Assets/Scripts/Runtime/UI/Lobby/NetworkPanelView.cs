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
            StopMatchingText();
        }
        private static readonly string[] StatusTexts =
        {
            "알 수 없는 상태",
            "서버 연결 중...",
            "서버 연결 완료",
            "서버 연결 실패",
            "매칭 완료",
        };
        private readonly string[] MatchingTexts =
        {
            "매칭을 찾는 중.",
            "매칭을 찾는 중..",
            "매칭을 찾는 중..."
        };
        private void ClearText()
        {
            _statusText.SetText(string.Empty);
            _statusDetailText.SetText(string.Empty);
        }
        public void ChangeMessage(in NetworkStatus status)
        {
            // 이전 표시 루프가 새 상태 문구를 덮어쓰지 않도록 종료합니다.
            StopMatchingText();
            StopMatchFoundCountdown();

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

        public void ChangeMessage(OnlineMatchState type)
        {
            ClearText();
            switch (type)
            {
                case OnlineMatchState.Connecting:
                case OnlineMatchState.Connected:
                case OnlineMatchState.ConnectionFailed:
                    _statusText.SetText(StatusTexts[(int)type]);
                    break;

                case OnlineMatchState.MatchMaking:
                    StartMatchingText();
                    break;
                case OnlineMatchState.MatchFound:
                    StartMatchFoundCountdown();
                    break;
                default:
                    ClearText();
                    _statusText.SetText(StatusTexts[(int)type]);
                    break;
            }
        }

        #region Matching
        private CancellationTokenSource _matchingTextCts;
        private double                  _matchMakingStartTime;
        private void            StartMatchingText()
        {
            StopMatchingText();
            _matchMakingStartTime = Time.realtimeSinceStartupAsDouble;
            _matchingTextCts = new CancellationTokenSource();
            PlayMatchingTextAsync(_matchingTextCts.Token).Forget();
        }
        private async UniTask   PlayMatchingTextAsync(CancellationToken cancellationToken)
        {
            int index = 0;

            try
            {
                while (true)
                {
                    double elapsed = Time.realtimeSinceStartupAsDouble - _matchMakingStartTime;

                    int minutes = (int)(elapsed / 60);
                    int seconds = (int)(elapsed % 60);

                   
                    _statusDetailText.SetText("매칭 시간 {0:00}:{1:00}", minutes, seconds);
                    _statusText.SetText(MatchingTexts[index]);

                    index = ++index % MatchingTexts.Length;

                    await UniTask.Delay(
                        500,
                        cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 정상적인 애니메이션 종료
            }
        }
        private void            StopMatchingText()
        {
            _matchingTextCts?.Cancel();
            _matchingTextCts?.Dispose();
            _matchingTextCts = null;
        }
        #endregion

        #region MatchFound
        private CancellationTokenSource _matchFoundCts;
        private double                  _matchFoundEndTime;
        private void            StartMatchFoundCountdown()
        {
            StopMatchingText();
            StopMatchFoundCountdown();

            _matchFoundEndTime = Time.realtimeSinceStartupAsDouble + 10.0;

            _matchFoundCts = new CancellationTokenSource();
            PlayMatchFoundCountdownAsync(_matchFoundCts.Token).Forget();
        }
        private async UniTask   PlayMatchFoundCountdownAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    double remaining =
                        _matchFoundEndTime - Time.realtimeSinceStartupAsDouble;

                    int seconds = Mathf.Max(
                        0,
                        Mathf.CeilToInt((float)remaining));

                    _statusText.SetText("매칭 완료");
                    _statusDetailText.SetText(
                        "게임 시작까지 {0}초",
                        seconds);

                    if (remaining <= 0)
                    {
                        _statusDetailText.SetText("게임 시작 대기 중...");
                        break;
                    }

                    await UniTask.Delay(
                        100,
                        ignoreTimeScale: true,
                        cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 정상 종료
            }
        }
        private void            StopMatchFoundCountdown()
        {
            _matchFoundCts?.Cancel();
            _matchFoundCts?.Dispose();
            _matchFoundCts = null;
        }
        #endregion


    }
}
