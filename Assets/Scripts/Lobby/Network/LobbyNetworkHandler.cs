#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using YuJanggi.Network;
using YuJanggi.Network.Handler;
using YuJanggi.Protocol.Connection;
using YuJanggi.Protocol.Matching;
using YuJanggi.Protocol.Messages;

namespace YuJanggi.Lobby.Network
{
    internal sealed class LobbyNetworkHandler : NetworkHandler
    {
        public LobbyNetworkHandler(NetworkClient client, RequestDispatcher requests)
            : base(client, requests)
        {
        }
        public async UniTask<bool> Panel_HandshakeAsync(
            CancellationToken token)
        {
            try
            {
                var msgType = ClientMessageType.HandshakeRequest;
                var payload = new ProtocolHandshakeRequest
                {
                    YuJanggiProtocolVersion = Protocol.Version.Version.Current,
                    YuJanggiCoreVersion = Engine.Version.Version.Current
                };

                var response = await SendRequestAsync(
                    msgType,
                    payload,
                    ServerMessageType.HandshakeResponse,
                    token);

                var handshake =
                    response.GetPayload<ProtocolHandshakeResponse>();

                return handshake.Result ==
                       ProtocolHandshakeResult.Success;
            }
            catch (OperationCanceledException)
            {
                throw; // 핸드셰이크 취소
            }
            catch
            {
                return false; // 핸드셰이크 실패
            }
        }
        public async UniTask Panel_MatchRequestAsync(
            CancellationToken token)
        {
            try
            {
                await SendRequestAsync(
                    ClientMessageType.MatchingStartRequest,
                    new MatchingStartRequest(),
                    ServerMessageType.MatchingStartResponse,
                    token);

            }
            finally
            {

            }
        }

        protected override void OnHandleMessage(ServerMessage message)
        {
        }
    }
}
