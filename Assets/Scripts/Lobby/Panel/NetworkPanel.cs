using System;
using TMPro;
using UnityEngine;

namespace YuJanggi.Lobby.Panel
{
    using Cysharp.Threading.Tasks;
    using System.Threading;
    using YuJanggi.BootStrap;
    using YuJanggi.Lobby.Network;

    public class NetworkPanel : Panel, IGameStartPanel
    {
        [SerializeField] private TMP_Text       _statusText;
        [SerializeField] private TMP_Text       _statusDetailText;
        [SerializeField] private TMP_Dropdown   _formationDropDown;

        private NetworkManager           _network;
        private LobbyNetworkHandler      _handler;
        private CancellationTokenSource  _panelCts;


        protected override void OnOpen()
        {
            _network = YuJanggiBootStrap.Instance.NetworkManager;
            _handler = _network.Lobby;
            HandleOpenPanel();
            ConnectAsync().Forget();
        }
        protected override void OnClose()
        {
            _panelCts?.Cancel();
            _panelCts?.Dispose();
            _panelCts = null;

            _handler = null;
            _network = null;
        }
        public UniTask<bool>    PrepareGameAsync()
        {
            // StartMatchMaking
            throw new NotImplementedException();
        }
        private async UniTask ConnectAsync()
        {
            var network = _network;
            var handler = _handler;

            _panelCts?.Cancel();
            _panelCts?.Dispose();

            var cts = new CancellationTokenSource();
            var token = cts.Token;

            _panelCts = cts;

            try
            {
                if (!await ConnectNetworkAsync(network, token))
                    return;

                await HandshakeAsync(network, handler, token);
            }
            catch (OperationCanceledException)
            {
                // 패널 작업 취소입니다.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (ReferenceEquals(_panelCts, cts))
                    HandleHandshakeResult(false);
            }
            finally
            {
                if (ReferenceEquals(_panelCts, cts))
                    _panelCts = null;
                cts.Dispose();
            }
        }

        private async UniTask<bool> ConnectNetworkAsync(
            NetworkManager network,
            CancellationToken token)
        {
            var result = await network.Panel_ConnectAsync(token);
            HandleConnectResult(result);

            if (HandleConnectResult(result))
                return true;

            token.ThrowIfCancellationRequested();
            return false;
        }

        private async UniTask<bool> HandshakeAsync(
            NetworkManager network, 
            LobbyNetworkHandler handler,
            CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();
                var result = await handler.Panel_HandshakeAsync(token);
                token.ThrowIfCancellationRequested();
                if (HandleHandshakeResult(result))
                    return true;
            }
            catch
            {
                await network.Panel_DisconnectAsync();
                throw;
            }

            await network.Panel_DisconnectAsync();
            return false;
        }

        private void HandleOpenPanel()
        {
            _statusText.SetText("Offline");
            _statusDetailText.SetText("Connecting In Progress");
        }
        private bool HandleConnectResult(ConnectingResult result)
        {
            switch (result)
            {
                case ConnectingResult.Success:
                    _statusText.SetText("Connecting");
                    _statusDetailText.SetText("Handshaking In Progress");
                    return true;
                case ConnectingResult.AlreadyConnecting:
                    _statusText.SetText("Connecting");
                    _statusDetailText.SetText("Connection Already In Progress");
                    return false;
                case ConnectingResult.AlreadyConnected:
                    _statusText.SetText("Connected");
                    _statusDetailText.SetText("Already Connected");
                    return false;
                default:
                    _statusText.SetText("Offline");
                    _statusDetailText.SetText("Connection Failed");
                    return false;
            }
        }
        private bool HandleHandshakeResult(bool result)
        {
            if (result)
            {
                _statusText.SetText("Online");
                _statusDetailText.SetText("Handshaking Completed");
            }
            else
            {
                _statusText.SetText("Offline");
                _statusDetailText.SetText("Handshaking Failed");
            }

            return result;
        }
    }
}


