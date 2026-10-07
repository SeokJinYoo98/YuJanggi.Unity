#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using YuJanggi.BootStrap;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;

using YuJanggi.Lobby.Network;
using YuJanggi.Network.Status;


namespace YuJanggi.Lobby.Flow
{
    using UI;
    using Panel;
    internal sealed class NetworkLobbyFlow : LocalFlow
    {
        // 의존성
        private readonly NetworkPanel _view;
        private readonly ILobbyNetwork _network;
        private readonly NetworkManager _connection;
        private readonly LobbyNetworkTimer _timer = new();

        // Flow 생명주기
        private CancellationTokenSource? _lifetimeCts;
        private CancellationToken _lifetimeToken;
        private bool _readyRaised;

        // 포진 제출 시도와 실패 표시
        private string? _formationFailure;
        private bool _formationAttempted;

        internal bool CanStartOtherMode => _network.State == MatchingState.Idle;
        // Manager가 현재 패널을 선택하고 교체한다.
        public event Action<bool>? PanelRequested;

        internal NetworkLobbyFlow(NetworkPanel view, ILobbyNetwork network,
            NetworkManager connection)
        {
            _view = view;
            _network = network;
            _connection = connection;
        }

        protected override void OnBindEvents()
        {
            //_readyRaised = false;

            //_lifetimeCts = new CancellationTokenSource();
            //_lifetimeToken = _lifetimeCts.Token;

            //_view.ConnectRequested += HandleConnectRequested;
            //_view.MatchingRequested += HandleMatchingRequested;
            //_view.CloseRequested += HandleCloseRequested;
            //_view.StartRequested += HandleStartRequested;

            //_connection.OnNetworkChanged += HandleNetworkChanged;

            //_network.MatchFound += HandleMatchFound;
            //_network.GameReadyReceived += HandleGameReady;

            //HandleNetworkChanged();
        }

        protected override void OnUnBindEvents()
        {
            //_view.ConnectRequested -= HandleConnectRequested;
            //_view.MatchingRequested -= HandleMatchingRequested;
            //_view.CloseRequested -= HandleCloseRequested;
            //_view.StartRequested -= HandleStartRequested;

            //_connection.OnNetworkChanged -= HandleNetworkChanged;

            //_network.MatchFound -= HandleMatchFound;
            //_network.GameReadyReceived -= HandleGameReady;

            //if (_formationAttempted && !_network.IsFormationSubmitted)
            //    _formationFailure ??= "포진 제출이 취소되었습니다.";

            //var lifetime = _lifetimeCts;
            //_lifetimeCts = null;
            //lifetime?.Cancel();
            //lifetime?.Dispose();
        }

        public void Tick()
        {
            //if (!IsBound || _readyRaised || _formationAttempted ||
            //    _network.IsFormationSubmitting || _network.IsFormationSubmitted)
            //    return;

            //if (!_timer.TryGetChangedSeconds(Time.realtimeSinceStartupAsDouble, out int seconds))
            //    return;

            //_view.UpdateTimer(_timer.State, seconds);
            //if (_timer.State == MatchingState.Matched && seconds == 0)
            //    SubmitSelectedFormationAsync(_lifetimeToken).Forget();
        }

        private void HandleConnectRequested()
        {
            if (!IsBound || _connection.IsOnline) return;
            PanelRequested?.Invoke(true);
            ConnectAsync(_lifetimeToken).Forget();
        }

        private async UniTask ConnectAsync(CancellationToken token)
        {
            try
            {
                await _connection.ConnectAsync(token);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                if (IsCurrent(token)) Debug.LogException(exception);
            }
        }

        private void HandleMatchingRequested()
        {
            if (!IsBound || _readyRaised) return;

            if (_connection.IsOnline &&
                _network.State == MatchingState.Idle)
                ChangeMatchingAsync(false, _lifetimeToken).Forget();

            else if (_network.State == MatchingState.Matching)
                ChangeMatchingAsync(true, _lifetimeToken).Forget();
        }

        private async UniTask ChangeMatchingAsync(bool cancel, CancellationToken token)
        {
            try
            {
                if (cancel)
                    await _network.MatchingCancelRequestAsync(token);
                else
                    await _network.MatchingStartRequestAsync(token);
            }
            catch (OperationCanceledException) { }
            catch (Exception exception)
            {
                if (IsCurrent(token)) Debug.LogException(exception);
            }
        }

        private void HandleCloseRequested()
        {
            if (!IsBound) return;
            if (_network.State == MatchingState.Matched)
            {
                Debug.Log("이미 매치가 성사되어 취소하지 못합니다.");
                return;
            }
            _connection.Disconnect();
            PanelRequested?.Invoke(false);
        }

        private void HandleMatchFound(NetworkSetting match)
        {
            if (!IsBound ||
                _network.Match?.MatchId != match.MatchId)
               { return; }

            HandleNetworkChanged();
        }

        private void HandleGameReady(string matchId, Formation cho, Formation han)
        {
            if (IsBound &&
                _network.Match?.MatchId == matchId)
                { HandleStartRequested(); }
        }

        private void HandleStartRequested()
        {
            var match = _network.Match;
            if (!IsBound ||
                _readyRaised ||
                !_connection.IsOnline ||
                !_network.IsGameReady ||
                match == null || string.IsNullOrWhiteSpace(match.MatchId) ||
                match.Team is not (PlayerTeam.Cho or PlayerTeam.Han) ||
                !_network.TryGetReadyFormations(out var cho, out var han))
                { return; }

            bool localIsCho = match.Team == PlayerTeam.Cho;
            var options = new JanggiOptions
            {
                GameMode = GameModeType.Network,
                PlayerCho = localIsCho ? PlayerType.Network : PlayerType.Remote,
                PlayerHan = localIsCho ? PlayerType.Remote : PlayerType.Network,
                ChoFormation = cho,
                HanFormation = han,
                // 서버 TurnTime 계약이 생기기 전까지 기존 기본값을 유지한다.
                TurnTime = 30
            };
            _readyRaised = true;
            _timer.Stop();
            PanelRequested?.Invoke(true);
            RaiseGameStartReady(new LobbyGameStartContext(options, networkMatch: match));
        }

        private void HandleNetworkChanged()
        {
            //if (!IsBound || _readyRaised) return;
            //var status = _connection.Status;
            //var match = _network.Match;
            //if (_timer.MatchId != match?.MatchId || status.MatchingState != MatchingState.Matched)
            //{
            //    _formationFailure = null;
            //    _formationAttempted = false;
            //}
            //if (_network.IsGameReady)
            //{
            //    HandleStartRequested();
            //    if (_readyRaised) return;
            //}
            //_timer.Update(status, match?.MatchId, Time.realtimeSinceStartupAsDouble);
            //// 실패 문구는 남기고 정상 연결 해제 시 패널을 닫는다.
            //if ((status.Error ?? NetworkError.None) == NetworkError.None &&
            //    status.ConnectionState == ConnectionState.Disconnected)
            //    PanelRequested?.Invoke(false);

            //int? seconds = _timer.GetSecondsForDisplay(Time.realtimeSinceStartupAsDouble);
            //_view.ChangeMessage(status, match?.Team ?? PlayerTeam.None, seconds);
            //if (status.ConnectionState == ConnectionState.Connected &&
            //    _network.State == MatchingState.Matched &&
            //    (_formationAttempted || _network.IsFormationSubmitting || _network.IsFormationSubmitted))
            //{
            //    _view.ShowFormationProgress(_formationFailure ??
            //        (_network.IsFormationSubmitted
            //            ? "포진 전송 완료 · 서버 준비 대기 중"
            //            : "포진 제출 중"));
            //}
            //if (_timer.State == MatchingState.Matched && seconds == 0)
            //    SubmitSelectedFormationAsync(_lifetimeToken).Forget();
        }

        private async UniTask SubmitSelectedFormationAsync(CancellationToken token)
        {
            //string? matchId = _network.Match?.MatchId;
            //if (!IsCurrent(token) || _readyRaised || !_connection.IsOnline ||
            //    string.IsNullOrWhiteSpace(matchId) || _network.State != MatchingState.Matched ||
            //    _formationAttempted || _network.IsFormationSubmitting || _network.IsFormationSubmitted)
            //    return;

            //// 전송 알림이 await 복귀보다 먼저 와도 자동 재전송하지 않는다.
            //_formationAttempted = true;
            //_formationFailure = null;
            //var formation = (Formation)_view.Selected;
            //_view.ShowFormationProgress("포진 제출 중");
            //try
            //{
            //    await _network.SubmitFormationAsync(formation, token);
            //}
            //catch (OperationCanceledException)
            //{
            //    if (IsCurrent(token) && _network.Match?.MatchId == matchId)
            //        _formationFailure = "포진 제출이 취소되었습니다.";
            //}
            //catch (Exception exception)
            //{
            //    if (IsCurrent(token) && _network.Match?.MatchId == matchId)
            //    {
            //        _formationFailure = "포진 제출에 실패했습니다.";
            //        Debug.LogException(exception);
            //    }
            //}
            //// 실패 시 서버 접수 여부가 불명확하므로 기존처럼 자동 재제출하지 않는다.
            //if (IsCurrent(token) && !_readyRaised && _network.Match?.MatchId == matchId)
            //    HandleNetworkChanged();
        }

        private bool IsCurrent(CancellationToken token)
            => IsBound && !token.IsCancellationRequested && token == _lifetimeToken;

    }
}
