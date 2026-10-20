#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;

    using Handler;
    using Input;
    using Network;
    using Player;
    using Store;

    internal sealed class NetworkMode
        : GameMode, IInGameNetworkReceiver
    {
        public override GameModeType ModeType => GameModeType.Network;
        protected override bool ShouldTickEngine => false;

        private readonly ILocalPlayer _localPlayer;
        private readonly RemotePlayer _remotePlayer;

        private readonly ILocalInputHandler     _localInput;
        private readonly InGameNetworkHandler   _netHandler;
        private readonly NetworkManager         _network;
        private readonly NetworkTurnSynchronizer _serverTurns;
        private readonly PendingNetworkCommand  _commands;

        internal NetworkGameStartData? ServerGameStart => _serverTurns.ServerGameStart;
        internal NetworkMoveData? LastServerTurn => _serverTurns.LastServerTurn;
        internal int? ServerTurnNumber => _serverTurns.ServerTurnNumber;
        internal PlayerTeam ServerTeam => _serverTurns.ServerTeam;
        internal DateTimeOffset? ServerDeadlineAt => _serverTurns.ServerDeadlineAt;
        internal (int cho, int han) GetServerTimerDisplay()
            => _serverTurns.GetServerTimerDisplay();

        private bool            _desynced;
        private GameResultInfo? _engineGameResult;
        private CancellationTokenSource? _rematchCts;
        private CancellationTokenRegistration _connectionRegistration;
        private CancellationTokenRegistration _gameRegistration;
        private bool IsLocalTurn
            => !IsEnd && !_gameToken.IsCancellationRequested
                && !_engine.References.Turn.IsEnd
                && _engine.ControllerQuery.CurrentTurn == _localPlayer.Team;
        public NetworkMode(
            IJanggiEngine        engine,
            InputHandlerFactory  inputs,
            NetworkManager       network,
            PlayerTeam           myTeam,
            IGameCommandReceiver receiver)
            : base(engine)
        {
            _localInput = inputs.Create(JanggiOptionStore.TYPE);
            _network = network;
            _netHandler = network.InGame;
            _serverTurns = new NetworkTurnSynchronizer(engine);
            _commands = new PendingNetworkCommand(network);

            var localTeam = myTeam;

            _localPlayer = new LocalPlayer(
                localTeam,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);

            var remoteTeam = localTeam == PlayerTeam.Cho
                ? PlayerTeam.Han
                : PlayerTeam.Cho;

            _remotePlayer = new RemotePlayer(
                remoteTeam,
                receiver);
        }

        #region Init & Start
        protected override void OnInitialize()
        {
            _connectionRegistration.Dispose();
            _gameRegistration.Dispose();
            CancelRematchWait();
            _netHandler.CancelPendingEvents();
            _commands.Cancel();
            _serverTurns.Reset();
            _engineGameResult = null;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            var gameToken = _gameToken;
            _connectionRegistration = _network.ConnectionToken.Register(() =>
            {
                if (gameToken == _gameToken && !gameToken.IsCancellationRequested)
                    BlockForNetworkFailure("연결의 수신 처리가 종료되었습니다.");
            });
            _gameRegistration = gameToken.Register(() =>
            {
                if (gameToken != _gameToken)
                    return;
                _connectionRegistration.Dispose();
                _localInputEnabled = false;
                ClearLocalInput();
                _commands.Cancel();
            });
        }
        protected override async UniTask OnGameStartAsync(
            CancellationToken token)
        {
            _netHandler.SetReceiver(this);

            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, gameToken, _network.ConnectionToken);
            await _netHandler.SendInGameReadyAsync(cts.Token);
            await _netHandler.WaitForGameStartEvent(cts.Token);
        }
        #endregion

        #region Move
        public async override UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (_desynced || !IsLocalTurn || _commands.IsPending || ServerTurnNumber is not { } turnNumber)
                return;

            await SendCommandAsync(turnNumber, token,
                commandToken => _netHandler.SendMoveRequestAsync(
                    _localPlayer.Team,
                    turnNumber,
                    from,
                    to,
                    commandToken));
        }
        #endregion

        #region EndGame
        protected override async UniTask OnClearAsync(
            CancellationToken token)
        {
            _connectionRegistration.Dispose();
            _gameRegistration.Dispose();
            _serverTurns.Reset();
            _engineGameResult = null;
            CancelRematchWait();
            _netHandler.CancelPendingEvents();
            _commands.Cancel();
            ClearLocalInput();

            await _network.DisconnectAsync();
        }
        protected override void HandleGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            // 로컬 Engine 결과로 종료를 요청하지 않고 서버 GameEndedEvent를 기다립니다.
            _engineGameResult = result;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
        }
        #endregion

        #region GameEnd UI Buttons
        public async override UniTask<bool> RequestRematchAsync(
            CancellationToken token)
        {
            if (!IsEnd || !ServerGameStart.HasValue || _rematchCts is not null)
                return false;

            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _network.ConnectionToken);
            _rematchCts = cts;
            try
            {
                if (!await _netHandler.SendRematchRequest(_localPlayer.Team, cts.Token))
                    return false;

                await _netHandler.WaitForRematchConfirmedEvent(cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                return gameToken == _gameToken && IsEnd && ReferenceEquals(_rematchCts, cts);
            }
            finally
            {
                if (gameToken == _gameToken && ReferenceEquals(_rematchCts, cts))
                    _rematchCts = null;
            }
        }

        private void CancelRematchWait()
        {
            var cts = _rematchCts;
            _rematchCts = null;
            cts?.Cancel();
        }

        #endregion

        #region LiveGame UI Buttons

        public async override UniTask PassTurnAsync(
            CancellationToken token)
        {
            if (_desynced || !IsLocalTurn || _commands.IsPending || ServerTurnNumber is not { } turnNumber)
                return;

            await SendCommandAsync(turnNumber, token,
                commandToken => _netHandler.SendPassTurnRequest(
                    _localPlayer.Team,
                    turnNumber,
                    commandToken));
        }
        public async override UniTask GiveUpAsync(
            CancellationToken token)
        {
            if (_desynced || !IsLocalTurn || _commands.IsPending || ServerTurnNumber is not { } turnNumber)
                return;

            await SendCommandAsync(turnNumber, token,
                commandToken => _netHandler.SendGiveUpRequest(_localPlayer.Team, commandToken),
                waitForGameEnd: true);
        }

        private UniTask SendCommandAsync(
            int turnNumber,
            CancellationToken token,
            Func<CancellationToken, UniTask<bool>> sendRequest,
            bool waitForGameEnd = false)
        {
            var gameToken = _gameToken;
            return _commands.ExecuteAsync(sendRequest, token, gameToken,
                exception =>
                {
                    if (gameToken == _gameToken && !IsEnd
                        && (waitForGameEnd || ServerTurnNumber == turnNumber))
                        BlockForNetworkFailure($"게임 명령의 서버 결과를 확인하지 못했습니다: {exception}");
                }, waitForGameEnd);
        }

        private void BlockForNetworkFailure(string reason)
        {
            if (_desynced)
                return;
            _desynced = true;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
            Debug.LogError($"[NetworkMode] 서버 상태 불확실: {reason}");
            _commands.Cancel();
        }
        #endregion

        #region Server Commands
        public void ApplyServerGameStart(NetworkGameStartData data)
        {
            if (!IsEnd || ServerGameStart.HasValue || _gameToken.IsCancellationRequested)
            {
                Debug.Log("[NetworkMode] 이미 수신했거나 대기 중이 아닌 GameStartEvent를 무시합니다.");
                return;
            }

            _serverTurns.ApplyGameStart(data);
            _engineGameResult = null;
            // 준비/Rematch 응답이 아니라 서버의 새 게임 시작에서만 이전 desync를 해제합니다.
            _desynced = false;
        }

        public void ApplyServerMove(NetworkMoveData data)
        {
            if (LastServerTurn is { } lastTurn && data.TurnNumber <= lastTurn.TurnNumber)
            {
                Debug.Log($"[NetworkMode] 중복/과거 턴 무시: received={data.TurnNumber}, last={lastTurn.TurnNumber}");
                return;
            }

            if (_desynced)
                return;

            var status = _serverTurns.ApplyTurn(data, IsEnd, out var appliedTurn, out var message);
            if (status == NetworkTurnApplyStatus.Ignored)
            {
                Debug.Log($"[NetworkMode] {message}");
                return;
            }
            if (status == NetworkTurnApplyStatus.Desynced || appliedTurn is null)
            {
                BlockForDesync(data, message ?? "Engine의 턴 완료 결과를 확인할 수 없습니다.");
                return;
            }

            _commands.CompleteTurn(data.Team == _localPlayer.Team,
                () => base.HandleTurnCompleted(appliedTurn));
        }

        private void BlockForDesync(NetworkMoveData data, string reason)
        {
            _desynced = true;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
            Debug.LogError($"[NetworkMode] desync: {reason} "
                + $"expected={ServerTurnNumber}, received={data.TurnNumber}, "
                + $"serverTeam={ServerTeam}, actingTeam={data.Team}, nextTeam={data.NextTeam}, "
                + $"engineTeam={_engine.ControllerQuery.CurrentTurn}, "
                + $"engineRecordCount={_engine.References.ReadOnlyRecord.Count}");
            _commands.Cancel();
        }
        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves)
        {
            if (IsEnd || _gameToken.IsCancellationRequested)
                return;

            LogGameEndMismatch(result, totalMoves);

            if (!_engine.References.Turn.IsEnd)
                _engine.EndEngine();

            _commands.CompleteGame(() => NotifyGameEnded(result, totalMoves));
        }

        private void LogGameEndMismatch(GameResultInfo result, int totalMoves)
        {
            var recordCount = _engine.References.ReadOnlyRecord.Count;
            if (_engineGameResult is { } engineResult)
            {
                if (engineResult.Type != result.Type || engineResult.Winner != result.Winner
                    || recordCount != totalMoves)
                    Debug.LogWarning($"[NetworkMode] 종료 결과 불일치: "
                        + $"server={result.Type}/{result.Winner}/{totalMoves}, "
                        + $"engine={engineResult.Type}/{engineResult.Winner}/{recordCount}");
            }
            else if (result.Type != GameResult.GiveUp || recordCount != totalMoves)
            {
                Debug.LogWarning($"[NetworkMode] 서버 종료 결과에 대응하는 Engine 결과/기록을 확인할 수 없습니다: "
                    + $"server={result.Type}/{result.Winner}/{totalMoves}, engineRecordCount={recordCount}");
            }
        }
        #endregion

        #region Turn & Input
        // 동기적으로 발생한 Engine 결과는 검증 후 기존 턴 알림/입력 처리로 전달합니다.
        protected override void HandleTurnCompleted(TurnData data)
            => _serverTurns.CaptureTurnCompleted(data);

        protected override void BeginNextTurn(
            PlayerTeam team)
        {
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            if (!_desynced && _localInputEnabled && team == _localPlayer.Team)
            {
                _localInput.Resume();
                _localInput.SetPlayer(_localPlayer);
            }
            else
            {
                _localInput.Pause();
            }
        }
        public override PlayerType GetPlayerType(
            PlayerTeam team)
            => team == _localPlayer.Team ? PlayerType.Local : PlayerType.Remote;
        protected override void OnSetLocalInputEnabled(
            bool enabled)
        {
            if (enabled && !IsEnd && !_engine.References.Turn.IsEnd)
            {
                BeginNextTurn(_engine.ControllerQuery.CurrentTurn);
            }
            else
            {
                _localInput.ResetPlayer();
                _localInput.Pause();
            }
        }

        private void ClearLocalInput()
        {
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();
            _netHandler.SetReceiver(null);
        }
        #endregion
    }
}
