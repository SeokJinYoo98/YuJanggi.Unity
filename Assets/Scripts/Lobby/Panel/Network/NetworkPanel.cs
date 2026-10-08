using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;



namespace YuJanggi.Lobby.Panel
{
    using BootStrap;
    using Engine.Domain;
    using Handler;
    using YuJanggi.Store;
    using NetworkFormationData = Network.NetworkFormationData;
  
    using NetworkMatchingData  = Network.NetworkMatchingData;

    public enum NetworkState
    { Offline, Connecting, ConnectingFailed, Handshaking, HandshakeFailed, Online, Matching, Matched };
    public class NetworkPanel : Panel, IGameStartPanel
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _statusDetailText;
        [SerializeField] private TMP_Dropdown _formationDropDown;

        private NetworkManager      _network;
        private LobbyNetworkHandler _handler;

        private CancellationTokenSource _lifecycleCts;
        private CancellationTokenSource _matchingCts;

        private NetworkPanelView  _view;
        private NetworkState      _state;
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
            if (_state != NetworkState.Matched)
                _network.DisconnectAsync().Forget();

            _handler  = null;
            _network  = null;
        }
        protected override void OnOpen()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();

            _lifecycleCts = new CancellationTokenSource();

            _network  = YuJanggiBootStrap.Instance.NetworkManager;
            _handler  = _network.Lobby;

            ConnectAsync(_lifecycleCts.Token).Forget();
        }

        #region Connection
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
        #endregion

        #region MatchMaking
        public async UniTask<bool> PrepareGameAsync()
        {
            if (!EnterPrepareGame)
                return false;

            var handler = _handler;
            var network = _network;

            var lifecycleToken = _lifecycleCts.Token;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                lifecycleToken);

            var token = cts.Token;
            _matchingCts = cts;

            try
            {
                var result = await handler.MatchRequestAsync(token);

                if (result != MatchRequestResult.Success)
                {
                    ChangeState(NetworkState.Offline, token);
                    await network.DisconnectAsync();
                    return false;
                }

                ChangeState(NetworkState.Matching, token);
                _view.StartMatchingTimer(token);

                var networkData = await handler.WaitForMatchFoundEvent(token);

                _view.StopMatchingTimer();
                ChangeState(NetworkState.Matched, token);

                await WaitFor10Sec(token);
 
                token.ThrowIfCancellationRequested();
                var formation = (Formation)_formationDropDown.value;

                await handler.SubmitFormation(
                    networkData.MatchId,
                    formation,
                    token);

                var formData = await handler.WaitForGameReadyEvent(token);
                token.ThrowIfCancellationRequested();
                return TrySaveGameData(in networkData, in formData); 
            }
            catch (OperationCanceledException)
            {
                // 패널이 열려 있으면 매칭 취소 후 다시 요청할 수 있도록 복구합니다.
                if (!lifecycleToken.IsCancellationRequested &&
                    ReferenceEquals(_matchingCts, cts))
                {
                    ChangeState(NetworkState.Online, lifecycleToken);
                }

                return false;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);

                if (!token.IsCancellationRequested)
                {
                    ChangeState(NetworkState.Offline, token);
                    await network.DisconnectAsync();
                }

                return false;
            }
            finally
            {
                // 실패·취소 시에도 타이머를 종료합니다.
                // 이전 작업이 새 작업의 타이머나 CTS를 정리하지 않도록 확인합니다.
                if (ReferenceEquals(_matchingCts, cts))
                {
                    _view.StopMatchingTimer();
                    _matchingCts = null;
                }
            }
        }
        private async UniTask WaitFor10Sec(
            CancellationToken token)
        {
            for (int timer = 10; 0 <= timer; timer--)
            {
                token.ThrowIfCancellationRequested();
                _view.HandleCountDown(timer);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(1),
                    ignoreTimeScale: true,
                    cancellationToken: token);
            }
        }
        private bool TrySaveGameData(
            in NetworkMatchingData data,
            in NetworkFormationData formData)
        {
            if (string.IsNullOrWhiteSpace(data.MatchId) ||
                data.MatchId != formData.MatchId)
                return false;

            var options = JanggiOptionFactory.CreateNetwork(
                formData,
                data.MyTeam);

            JanggiOptionStore.SaveOptions(options);
            OnlineMatchInfoStore.SaveData(data);

            CanClosePanel = true;
            return true;
        }
        #endregion

        #region MatchMaking Cancel
        public void HandleMatchMakingCancel()
        {
            RequestCancelMatch().Forget();
        }
        private async UniTask RequestCancelMatch()
        {
            var matchingCts = _matchingCts;
            if (matchingCts == null)
                return;

            var handler = _handler;
            var token   = _lifecycleCts.Token;

            if (!await handler.MatchCancelRequestAsync(token))
                return;

            token.ThrowIfCancellationRequested();

            if (ReferenceEquals(_matchingCts, matchingCts))
                matchingCts.Cancel();
        }
        #endregion

        private void ChangeState(
            NetworkState next,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            _state = next;
            _view.HandleText(next);

            if (_state == NetworkState.Matched)
                CanClosePanel = false;
        }

        private bool EnterPrepareGame
            => _state == NetworkState.Online;
    }
}


