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

        private Func<CancellationToken, UniTask> _authenticateAndConnect;
        private CancellationTokenSource          _loginCts;

        // 구글 로그인 / 서버 인증 / TCP 연결과 인증을 완료하는 작업을 연결합니다.
        public void Initialize(Func<CancellationToken, UniTask> authenticateAndConnect)
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

            var authenticateAndConnect = _authenticateAndConnect;
            if (authenticateAndConnect == null)
            {
                SetStatus("구글 로그인 기능이 아직 연결되지 않았습니다.");
                return;
            }

            using var cancellation = new CancellationTokenSource();
            _loginCts = cancellation;
            _googleLoginButton.interactable = false;
            SetStatus("로그인 중...");

            try
            {
                await authenticateAndConnect(cancellation.Token);
                await UniTask.SwitchToMainThread();
                cancellation.Token.ThrowIfCancellationRequested();

                SetStatus("로그인 완료");
                await SceneManager.LoadSceneAsync("LobbyScene");
            }
            catch (OperationCanceledException)
            {
                await UniTask.SwitchToMainThread();
                SetStatus("로그인이 취소되었습니다.");
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
