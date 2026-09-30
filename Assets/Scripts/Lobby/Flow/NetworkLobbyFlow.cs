#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using YuJanggi.BootStrap;
using YuJanggi.Engine.Domain;
using YuJanggi.Engine.JanggiOption;
using YuJanggi.Lobby.Matching;
using YuJanggi.Lobby.Network;
using YuJanggi.Network.Status;
using YuJanggi.Runtime.UI;

namespace YuJanggi.Lobby.Flow
{
    internal sealed class NetworkLobbyFlow : ILobbyFlow
    {
        private const int GameStartCountdownSeconds = 10;
        private readonly NetworkPanelView _view;
        private readonly ILobbyNetwork _network;
        private readonly NetworkManager _connection;
        private CancellationTokenSource? _lifetimeCts;
        private CancellationToken _lifetimeToken;
        private bool _bound;
        private bool _readyRaised;
        private MatchingState _timerState = MatchingState.Idle;
        private bool _showTimer;
        private double _timerStartedAt;
        private int _lastTimerSeconds = -1;
        private string? _timerMatchId;
        private string? _formationFailure;
        private bool _formationAttempted;

        public event Action<LobbyGameStartContext>? GameStartReady;
        internal bool CanStartOtherMode => _network.State == MatchingState.Idle;
        // Manager가 현재 패널을 선택하고 교체한다.
        public event Action<bool>? PanelRequested;

        internal NetworkLobbyFlow(NetworkPanelView view, ILobbyNetwork network,
            NetworkManager connection)
        {
            _view = view;
            _network = network;
            _connection = connection;
        }

        public void BindEvents()
        {
            if (_bound) return;
            _bound = true;
            _readyRaised = false;
            _lifetimeCts = new CancellationTokenSource();
            _lifetimeToken = _lifetimeCts.Token;
            _view.ConnectRequested += HandleConnectRequested;
            _view.MatchingRequested += HandleMatchingRequested;
            _view.CloseRequested += HandleCloseRequested;
            _view.StartRequested += HandleStartRequested;
            _connection.OnNetworkChanged += HandleNetworkChanged;
            _network.MatchFound += HandleMatchFound;
            _network.GameReadyReceived += HandleGameReady;
            HandleNetworkChanged();
        }

        public void UnBindEvents()
        {
            if (!_bound) return;
            _bound = false;
            _view.ConnectRequested -= HandleConnectRequested;
            _view.MatchingRequested -= HandleMatchingRequested;
            _view.CloseRequested -= HandleCloseRequested;
            _view.StartRequested -= HandleStartRequested;
            _connection.OnNetworkChanged -= HandleNetworkChanged;
            _network.MatchFound -= HandleMatchFound;
            _network.GameReadyReceived -= HandleGameReady;
            if (_formationAttempted && !_network.IsFormationSubmitted)
                _formationFailure ??= "포진 제출이 취소되었습니다.";
            var lifetime = _lifetimeCts;
            _lifetimeCts = null;
            lifetime?.Cancel();
            lifetime?.Dispose();
        }

        public void Tick()
        {
            if (!_bound || _readyRaised || !_showTimer || _formationAttempted ||
                _network.IsFormationSubmitting || _network.IsFormationSubmitted)
                return;

            int seconds = GetTimerSeconds();
            if (seconds == _lastTimerSeconds) return;
            _lastTimerSeconds = seconds;
            _view.UpdateTimer(_timerState, seconds);
            if (_timerState == MatchingState.Matched && seconds == 0)
                SubmitSelectedFormationAsync(_lifetimeToken).Forget();
        }

        private void HandleConnectRequested()
        {
            if (!_bound || _connection.IsOnline) return;
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
            if (!_bound || _readyRaised) return;
            if (_connection.IsOnline && _network.State == MatchingState.Idle)
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
            if (!_bound) return;
            if (_network.State == MatchingState.Matched)
            {
                Debug.Log("이미 매치가 성사되어 취소하지 못합니다.");
                return;
            }
            _connection.Disconnect();
            PanelRequested?.Invoke(false);
        }

        private void HandleMatchFound(MatchInfo match)
        {
            if (!_bound || _network.Match?.MatchId != match.MatchId) return;
            HandleNetworkChanged();
        }

        private void HandleGameReady(string matchId, Formation cho, Formation han)
        {
            if (_bound && _network.Match?.MatchId == matchId)
                HandleStartRequested();
        }

        private void HandleStartRequested()
        {
            var match = _network.Match;
            if (!_bound || _readyRaised || !_connection.IsOnline || !_network.IsGameReady ||
                match == null || string.IsNullOrWhiteSpace(match.MatchId) ||
                match.Team is not (PlayerTeam.Cho or PlayerTeam.Han) ||
                !_network.TryGetReadyFormations(out var cho, out var han))
                return;

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
            _showTimer = false;
            PanelRequested?.Invoke(true);
            GameStartReady?.Invoke(new LobbyGameStartContext(options, networkMatch: match));
        }

        private void HandleNetworkChanged()
        {
            if (!_bound || _readyRaised) return;
            var status = _connection.Status;
            var match = _network.Match;
            if (_timerMatchId != match?.MatchId || status.MatchingState != MatchingState.Matched)
            {
                _formationFailure = null;
                _formationAttempted = false;
            }
            if (_network.IsGameReady)
            {
                HandleStartRequested();
                if (_readyRaised) return;
            }
            UpdateTimerState(status);
            // 실패 문구는 남기고 정상 연결 해제 시 패널을 닫는다.
            if ((status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Disconnected)
                PanelRequested?.Invoke(false);

            int? seconds = _showTimer ? GetTimerSeconds() : (int?)null;
            _view.ChangeMessage(status, match?.Team ?? PlayerTeam.None, seconds);
            _lastTimerSeconds = seconds ?? -1;
            if (status.ConnectionState == ConnectionState.Connected &&
                _network.State == MatchingState.Matched &&
                (_formationAttempted || _network.IsFormationSubmitting || _network.IsFormationSubmitted))
            {
                _view.ShowFormationProgress(_formationFailure ??
                    (_network.IsFormationSubmitted
                        ? "포진 전송 완료 · 서버 준비 대기 중"
                        : "포진 제출 중"));
            }
            if (_timerState == MatchingState.Matched && seconds == 0)
                SubmitSelectedFormationAsync(_lifetimeToken).Forget();
        }

        private async UniTask SubmitSelectedFormationAsync(CancellationToken token)
        {
            string? matchId = _network.Match?.MatchId;
            if (!IsCurrent(token) || _readyRaised || !_connection.IsOnline ||
                string.IsNullOrWhiteSpace(matchId) || _network.State != MatchingState.Matched ||
                _formationAttempted || _network.IsFormationSubmitting || _network.IsFormationSubmitted)
                return;

            // 전송 알림이 await 복귀보다 먼저 와도 자동 재전송하지 않는다.
            _formationAttempted = true;
            _formationFailure = null;
            var formation = (Formation)_view.Selected;
            _view.ShowFormationProgress("포진 제출 중");
            try
            {
                await _network.SubmitFormationAsync(formation, token);
            }
            catch (OperationCanceledException)
            {
                if (IsCurrent(token) && _network.Match?.MatchId == matchId)
                    _formationFailure = "포진 제출이 취소되었습니다.";
            }
            catch (Exception exception)
            {
                if (IsCurrent(token) && _network.Match?.MatchId == matchId)
                {
                    _formationFailure = "포진 제출에 실패했습니다.";
                    Debug.LogException(exception);
                }
            }
            // 실패 시 서버 접수 여부가 불명확하므로 기존처럼 자동 재제출하지 않는다.
            if (IsCurrent(token) && !_readyRaised && _network.Match?.MatchId == matchId)
                HandleNetworkChanged();
        }

        private bool IsCurrent(CancellationToken token)
            => _bound && !token.IsCancellationRequested && token == _lifetimeToken;

        private void UpdateTimerState(in NetworkStatus status)
        {
            bool showTimer = (status.Error ?? NetworkError.None) == NetworkError.None &&
                status.ConnectionState == ConnectionState.Connected &&
                (status.MatchingState == MatchingState.Matching ||
                 status.MatchingState == MatchingState.Matched);
            string? matchId = _network.Match?.MatchId;
            if (showTimer && (!_showTimer || _timerState != status.MatchingState ||
                (status.MatchingState == MatchingState.Matched && _timerMatchId != matchId)))
                _timerStartedAt = Time.realtimeSinceStartupAsDouble;
            _timerState = status.MatchingState;
            _timerMatchId = matchId;
            _showTimer = showTimer;
        }

        private int GetTimerSeconds()
        {
            int elapsed = (int)Math.Floor(Time.realtimeSinceStartupAsDouble - _timerStartedAt);
            return _timerState == MatchingState.Matched
                ? Math.Max(0, GameStartCountdownSeconds - elapsed) : elapsed;
        }
    }
}
