using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace YuJanggi.Login
{
    internal sealed class GoogleDesktopLogin
    {
        private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        private readonly string _clientJsonPath;

        public GoogleDesktopLogin(string clientJsonPath)
        {
            if (string.IsNullOrWhiteSpace(clientJsonPath))
                throw new ArgumentException("데스크톱 OAuth JSON 경로가 필요합니다.", nameof(clientJsonPath));

            _clientJsonPath = clientJsonPath;
        }

        public async Task<string> SignInAsync(Action<string> openBrowser, CancellationToken token)
        {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
            token.ThrowIfCancellationRequested();
            var configuration = JsonUtility.FromJson<ClientConfiguration>(File.ReadAllText(_clientJsonPath));
            InstalledClient client = configuration?.installed;
            if (client == null || string.IsNullOrWhiteSpace(client.client_id))
                throw new InvalidDataException("데스크톱 앱 유형의 OAuth JSON이 필요합니다.");

            string state = CreateRandomString();
            string verifier = CreateRandomString();
            string challenge;
            using (var sha = SHA256.Create())
                challenge = Base64Url(sha.ComputeHash(Encoding.ASCII.GetBytes(verifier)));

            using var receiver = new LoopbackCodeReceiver();
            string url = AuthorizationEndpoint
                + "?client_id=" + Uri.EscapeDataString(client.client_id)
                + "&redirect_uri=" + Uri.EscapeDataString(receiver.RedirectUri)
                + "&response_type=code&scope=openid%20profile%20email"
                + "&code_challenge_method=S256&code_challenge=" + challenge
                + "&state=" + state
                + "&prompt=select_account";

            openBrowser(url);
            string code = await receiver.ReceiveCodeAsync(state, token).ConfigureAwait(false);
            return await ExchangeCodeAsync(client, receiver.RedirectUri, code, verifier, token)
                .ConfigureAwait(false);
#else
            await Task.CompletedTask;
            throw new PlatformNotSupportedException("현재 구글 로그인은 Windows Editor / Windows 빌드만 지원합니다.");
#endif
        }

        private static async Task<string> ExchangeCodeAsync(
            InstalledClient client, string redirectUri, string code, string verifier, CancellationToken token)
        {
            var values = new Dictionary<string, string>
            {
                ["client_id"] = client.client_id,
                ["code"] = code,
                ["code_verifier"] = verifier,
                ["redirect_uri"] = redirectUri,
                ["grant_type"] = "authorization_code"
            };
            if (!string.IsNullOrWhiteSpace(client.client_secret))
                values["client_secret"] = client.client_secret;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            using var content = new FormUrlEncodedContent(values);
            using HttpResponseMessage response = await http.PostAsync(TokenEndpoint, content, token)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"구글 토큰 교환 실패 (HTTP {(int)response.StatusCode})");

            string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            TokenResponse tokens = JsonUtility.FromJson<TokenResponse>(json);
            if (tokens == null || string.IsNullOrWhiteSpace(tokens.id_token))
                throw new InvalidDataException("구글 응답에 ID 토큰이 없습니다.");

            return tokens.id_token;
        }

        private static string CreateRandomString()
        {
            var bytes = new byte[32];
            using var random = RandomNumberGenerator.Create();
            random.GetBytes(bytes);
            return Base64Url(bytes);
        }

        private static string Base64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        [Serializable]
        private sealed class ClientConfiguration
        {
            public InstalledClient installed = null;
        }

        [Serializable]
        private sealed class InstalledClient
        {
            public string client_id = string.Empty;
            public string client_secret = string.Empty;
        }

        [Serializable]
        private sealed class TokenResponse
        {
            public string id_token = string.Empty;
        }
    }
}
