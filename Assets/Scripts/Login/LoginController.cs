using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace YuJanggi.Login
{
    public sealed class LoginController : MonoBehaviour
    {
        [SerializeField] private Button   _googleLoginButton;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private string _googleClientJsonPath;

        private Func<string, CancellationToken, UniTask> _authenticateAndConnect;
        private CancellationTokenSource          _loginCts;

        // 받은 ID 토큰으로 서버 인증 / TCP 연결과 인증을 완료하는 작업을 연결합니다.
        public void Initialize(Func<string, CancellationToken, UniTask> authenticateAndConnect)
        {
            _authenticateAndConnect = authenticateAndConnect
                ?? throw new ArgumentNullException(nameof(authenticateAndConnect));
        }

        private void OnDisable()
        {
            _loginCts?.Cancel();
        }

        public void OnGoogleLoginClicked()
             => LoginAsync().Forget();
        


        private async UniTask LoginAsync()
        {
            if (_loginCts != null)
                return;

            if (_googleLoginButton == null)
            {
                SetStatus("Google Login Button을 연결해 주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_googleClientJsonPath))
            {
                SetStatus("Google Client Json Path에 데스크톱 OAuth JSON 경로를 입력해 주세요.");
                return;
            }

            using var cancellation = new CancellationTokenSource();
            cancellation.CancelAfter(TimeSpan.FromMinutes(5));
            _loginCts = cancellation;
            _googleLoginButton.interactable = false;
            SetStatus("브라우저에서 구글 로그인해 주세요.");

            try
            {
                var googleLogin = new GoogleDesktopLogin(_googleClientJsonPath);
                string idToken = await googleLogin.SignInAsync(Application.OpenURL, cancellation.Token);
                await UniTask.SwitchToMainThread();
                cancellation.Token.ThrowIfCancellationRequested();

                var authenticateAndConnect = _authenticateAndConnect;
                if (authenticateAndConnect == null)
                {
                    SetStatus("구글 ID 토큰 수신 완료 / 서버 인증은 아직 연결되지 않았습니다.");
                    return;
                }

                SetStatus("서버 인증 중...");
                await authenticateAndConnect(idToken, cancellation.Token);
                await UniTask.SwitchToMainThread();
                cancellation.Token.ThrowIfCancellationRequested();
                SetStatus("로그인 완료");
                await SceneManager.LoadSceneAsync("LobbyScene");
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                SetStatus("로그인이 취소되었거나 대기 시간이 지났습니다.");
            }
            catch (PlatformNotSupportedException)
            {
                await UniTask.SwitchToMainThread();
                SetStatus("현재는 Windows에서만 구글 로그인을 확인할 수 있습니다.");
            }
            catch (System.IO.FileNotFoundException)
            {
                await UniTask.SwitchToMainThread();
                SetStatus("OAuth JSON 파일을 찾을 수 없습니다. 파일 경로를 확인해 주세요.");
            }
            catch (Exception exception)
            {
                await UniTask.SwitchToMainThread();
                SetStatus("로그인에 실패했습니다. 다시 시도해 주세요.");
                Debug.LogError($"로그인 실패 ({exception.GetType().Name})", this);
            }
            finally
            {
                _loginCts = null;
                if (this != null && _googleLoginButton != null)
                    _googleLoginButton.interactable = true;
            }
        }

        public void SetStatus(string message)
        {
            if (this != null && _statusText != null)
                _statusText.SetText(message);
        }
    }
}
