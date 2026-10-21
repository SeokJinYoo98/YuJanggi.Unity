using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YuJanggi.Login
{
    internal sealed class LoopbackCodeReceiver : IDisposable
    {
        private const string CallbackPath = "/oauth2/callback/";
        private const int MaxHeaderSize = 8192;
        private readonly TcpListener _listener;

        public string RedirectUri { get; }

        public LoopbackCodeReceiver()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            RedirectUri = $"http://127.0.0.1:{port}{CallbackPath}";
        }

        public async Task<string> ReceiveCodeAsync(string expectedState, CancellationToken token)
        {
            using var stopListener = token.Register(() => _listener.Stop());
            try
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    using TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    using var requestTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                    requestTimeout.CancelAfter(TimeSpan.FromSeconds(10));
                    using var closeClient = requestTimeout.Token.Register(() => client.Close());
                    try
                    {
                        string code = await ReadCodeAsync(client.GetStream(), expectedState, requestTimeout.Token)
                            .ConfigureAwait(false);
                        if (code != null)
                            return code;
                    }
                    catch (Exception) when (requestTimeout.IsCancellationRequested && !token.IsCancellationRequested)
                    {
                        // 응답하지 않는 연결을 닫고 구글 콜백을 계속 기다립니다.
                    }
                    catch (Exception exception) when (
                        exception is IOException || exception is InvalidDataException || exception is SocketException)
                    {
                        // 닫혔거나 잘못된 로컬 요청은 무시합니다.
                    }
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
        }

        private async Task<string> ReadCodeAsync(NetworkStream stream, string expectedState, CancellationToken token)
        {
            string header = await ReadHeaderAsync(stream, token).ConfigureAwait(false);
            string[] requestLine = header.Split(new[] { "\r\n" }, StringSplitOptions.None)[0].Split(' ');
            if (requestLine.Length != 3 || requestLine[0] != "GET"
                || !requestLine[1].StartsWith(CallbackPath + "?", StringComparison.Ordinal)
                || !Uri.TryCreate(new Uri(RedirectUri), requestLine[1], out Uri callback))
            {
                await ReplyAsync(stream, "400 Bad Request", "Invalid login callback.", token).ConfigureAwait(false);
                return null;
            }

            Dictionary<string, string> query = ParseQuery(callback.Query);
            if (!query.TryGetValue("state", out string state) || state != expectedState)
            {
                await ReplyAsync(stream, "400 Bad Request", "Invalid login state.", token).ConfigureAwait(false);
                return null;
            }

            if (query.ContainsKey("error"))
            {
                await ReplyAsync(stream, "200 OK", "Login was canceled. You may return to the game.", token)
                    .ConfigureAwait(false);
                throw new OperationCanceledException(token);
            }

            if (!query.TryGetValue("code", out string code) || string.IsNullOrWhiteSpace(code))
            {
                await ReplyAsync(stream, "400 Bad Request", "Missing authorization code.", token).ConfigureAwait(false);
                return null;
            }

            await ReplyAsync(stream, "200 OK", "Authorization code received. You may close this window and return to the game.", token)
                .ConfigureAwait(false);
            return code;
        }

        private static async Task<string> ReadHeaderAsync(NetworkStream stream, CancellationToken token)
        {
            var buffer = new byte[MaxHeaderSize];
            int count = 0;
            while (count < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, count, buffer.Length - count, token).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException("로그인 콜백 연결이 끊겼습니다.");

                count += read;
                string header = Encoding.ASCII.GetString(buffer, 0, count);
                if (header.IndexOf("\r\n\r\n", StringComparison.Ordinal) >= 0)
                    return header;
            }

            throw new InvalidDataException("로그인 콜백 헤더가 너무 큽니다.");
        }

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string pair in query.TrimStart('?').Split('&'))
            {
                int separator = pair.IndexOf('=');
                if (separator <= 0)
                    continue;

                string key = Uri.UnescapeDataString(pair.Substring(0, separator).Replace('+', ' '));
                string value = Uri.UnescapeDataString(pair.Substring(separator + 1).Replace('+', ' '));
                if (result.ContainsKey(key))
                    throw new InvalidDataException("로그인 콜백 매개변수가 중복되었습니다.");

                result.Add(key, value);
            }
            return result;
        }

        private static async Task ReplyAsync(NetworkStream stream, string status, string message, CancellationToken token)
        {
            byte[] body = Encoding.UTF8.GetBytes(message);
            byte[] header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status}\r\nContent-Type: text/plain; charset=utf-8\r\n"
                + $"Content-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(header, 0, header.Length, token).ConfigureAwait(false);
            await stream.WriteAsync(body, 0, body.Length, token).ConfigureAwait(false);
        }

        public void Dispose() => _listener.Stop();
    }
}
