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
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _statusDetailText;
        [SerializeField] private TMP_Dropdown _formationDropDown;

        private NetworkManager           _network;
        private LobbyNetworkHandler      _handler;
        private CancellationTokenSource  _panelCts;

        private bool _online = false;

        void OnEnable()
        {
            _network = YuJanggiBootStrap.Instance.NetworkManager;
        }
        void OnDisable()
        {
            _network = null;
        }
        protected override void OnOpen()
        {
            HandleOpenPanel();
            ConnectAsync().Forget();
        }
        protected override void OnClose()
        {
            _panelCts?.Cancel();
            _panelCts?.Dispose();
            _panelCts = null;

            _handler = null;
        }
        public UniTask<bool>    PrepareGameAsync()
        {
            // StartMatchMaking
            throw new NotImplementedException();
        }
        private async UniTask   ConnectAsync()
        {
            _handler = _network.Lobby;

            _panelCts?.Dispose();
            _panelCts = new CancellationTokenSource();

            var token = _panelCts.Token;

            _online = false;

            try
            {
                var connected = await _handler.Panel_ConnectAsync(token);

                if (!HandleConnectResult(connected))
                    return;

                var handshaked = await _handler.Panel_HandshakeAsync(token);

                if (!HandleHandshakeResult(handshaked))
                    return;

                _network.Panel_StartReceiveLoop();

                _online = true;
            }
            catch (OperationCanceledException)
            {
                // 패널이 닫혀 연결 작업이 취소됨
            }
            finally
            {
                if (!_online)
                {
                    _handler?.Panel_Disconnect();

                    _panelCts?.Dispose();
                    _panelCts = null;
                }
            }
        }

        private void HandleOpenPanel()
        {
            _statusText.SetText("Offline");
            _statusDetailText.SetText("Connecting In Progress");
        }
        private bool HandleConnectResult(bool result)
        {
            if (result)
            {
                _statusText.SetText("Connecting");
                _statusDetailText.SetText("Handshaking In Progress");
            }
            else
            {
                _statusText.SetText("Offline");
                _statusDetailText.SetText("Connection Failed");
            }

            return result;
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


