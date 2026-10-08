using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

namespace YuJanggi.Lobby.Panel
{
    using Engine.Domain;

    using Handler;
    using BootStrap;
    public enum NetworkState
    { Offline, Connecting, ConnectingFailed, Handshaking, HandshakeFailed, Online, Matching, Matched };
    public class NetworkPanel : Panel, IGameStartPanel
    {
        private struct NetworkSettings
        {
            public NetworkState State;
            public string MatchId;
            public PlayerTeam Team;
            public string OpponentPlayerId;
            public string OpponentNickname;
            public PlayerTeam OpponentTeam;
        }

        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _statusDetailText;
        [SerializeField] private TMP_Dropdown _formationDropDown;

        private NetworkManager _network;
        private LobbyNetworkHandler _handler;

        private CancellationTokenSource _lifecycleCts;
        private CancellationTokenSource _matchingCts;
        private NetworkPanelView _view;
        private NetworkSettings _settings;

        protected override void Start()
        {
            _view = new(
                _statusText,
                _statusDetailText);

            base.Start();
        }

        protected override void OnClose()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;

            // 연결 끊으면 안됌... 다음 씬으로 넘어가서도 써야함.
            if (_settings.State != NetworkState.Matched)
                _network.DisconnectAsync().Forget();

            _handler = null;
            _network = null;
        }
        protected override void OnOpen()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();

            _lifecycleCts = new CancellationTokenSource();

            _network = YuJanggiBootStrap.Instance.NetworkManager;
            _handler = _network.Lobby;

            ConnectAsync(_lifecycleCts.Token).Forget();
        }

        private async UniTask ConnectAsync(
            CancellationToken token)
        {
            var network = _network;
            var handler = _handler;

            try
            {
                ChangeState(NetworkState.Offline, token);

                if (!await ConnectNetworkAsync(network, token))
                    return;

                if (!await HandshakeAsync(handler, token))
                {
                    await network.DisconnectAsync();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                // Panel 작업 취소
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (!token.IsCancellationRequested)
                    ChangeState(NetworkState.Offline, token);
            }
        }

        private async UniTask<bool> ConnectNetworkAsync(
            NetworkManager network,
            CancellationToken token)
        {
            // 로그인씬으로 넘길거라
            // 예외는 네트워크매니저가 지금 처리함.
            ChangeState(NetworkState.Connecting, token);
            switch (await network.ConnectAsync(token))
            {
                case ConnectingResult.Success:
                    return true;

                case ConnectingResult.Failed:
                    ChangeState(NetworkState.Offline, token);
                    return false;

                case ConnectingResult.AlreadyConnecting:
                case ConnectingResult.AlreadyConnected:
                    return false;

                default: return false;
            }
        }
        private async UniTask<bool> HandshakeAsync(
            LobbyNetworkHandler handler,
            CancellationToken token)
        {
            ChangeState(NetworkState.Handshaking, token);
            if (!await handler.HandshakeAsync(token))
            {
                ChangeState(NetworkState.Offline, token);
                return false;
            }
            ChangeState(NetworkState.Online, token);
            return true;
        }


        public async UniTask<bool> PrepareGameAsync()
        {
            if (!EnterPrepareGame) return false;

            var handler = _handler;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                _lifecycleCts.Token);
            var token = cts.Token;
            _matchingCts = cts;

            try
            {
                // 
                var result = await handler.MatchRequestAsync(token);

                if (result != MatchRequestResult.Success)
                    return false;

                ChangeState(NetworkState.Matching, token);
                _view.StartMatchingTimer(token);
                await handler.WaitForMatchFoundEvent(token);
                _view.StopMatchingTimer();

                ChangeState(NetworkState.Matched, token);
                await WaitFor10Sec(token);
                await handler.SubmitFormation(token);
                await handler.WaitForGameReadyEvent(token);

                SaveNetworkData();
                SaveJanggiData();

                CanClosePanel = true;
                return true;
            }
            finally
            {
                if (ReferenceEquals(_matchingCts, cts))
                    _matchingCts = null;
            }
        }
        private void SaveNetworkData()
        {

        }
        private void SaveJanggiData()
        {

        }
        private async UniTask WaitFor10Sec(
            CancellationToken token)
        {

        }
        private void ChangeState(
            NetworkState next,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _settings.State = next;
            _view.HandleText(next);

            if (_settings.State == NetworkState.Matched)
                CanClosePanel = false;
        }

        private bool EnterPrepareGame
            => _settings.State == NetworkState.Online;
    }
}


