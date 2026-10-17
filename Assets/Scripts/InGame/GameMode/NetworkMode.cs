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
    using Views.UI;

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

        internal NetworkGameStartData? ServerGameStart { get; private set; }
        internal NetworkMoveData? LastServerTurn { get; private set; }

        // 서버 Event로 갱신된 상태에서 다음 요청에 사용할 턴 번호를 조회합니다.
        internal int? ServerTurnNumber => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTeam == PlayerTeam.None
                ? (int?)null
                : LastServerTurn.Value.TurnNumber + 1
            : ServerGameStart?.TurnNumber;
        internal PlayerTeam ServerTeam => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTeam
            : ServerGameStart?.CurrentTeam ?? PlayerTeam.None;
        internal DateTimeOffset? ServerDeadlineAt => LastServerTurn.HasValue
            ? LastServerTurn.Value.NextTurnDeadlineAt
            : ServerGameStart?.TurnDeadlineAt;

        private bool            _commandPending = false;
        private bool            _desynced;
        private TurnData?       _appliedTurn;
        private UniTaskCompletionSource? _commandCompletion;
        private bool _waitingForGameEnd;
        private CancellationTokenRegistration _connectionRegistration;
        private CancellationTokenRegistration _gameRegistration;
        private bool IsLocalTurn
            => !IsEnd && !_gameToken.IsCancellationRequested
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
            _commandCompletion = null;
            _waitingForGameEnd = false;
            ServerGameStart = null;
            LastServerTurn = null;
            _desynced = false;
            _appliedTurn = null;
            _commandPending = false;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
            _commandPending = true;

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
                _commandCompletion?.TrySetCanceled();
                _commandPending = false;
            });
        }
        protected override async UniTask OnGameStartAsync(
            CancellationToken token)
        {
            _commandPending = true;
            _netHandler.SetReceiver(this);

            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, gameToken, _network.ConnectionToken);
            try
            {
                await _netHandler.SendInGameReadyAsync(cts.Token);
                await _netHandler.WaitForGameStartEvent(cts.Token);
            }
            finally
            {
                if (gameToken == _gameToken)
                    _commandPending = false;
            }
        }
        #endregion

        #region Move
        public async override UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (_desynced || !IsLocalTurn || _commandPending || ServerTurnNumber is not { } turnNumber)
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
            _commandCompletion?.TrySetCanceled();
            _commandCompletion = null;
            _waitingForGameEnd = false;
            _commandPending = false;
            ClearLocalInput();

            await _network.DisconnectAsync();
        }
        protected override void HandleGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            // 로컬 Engine 결과로 종료를 요청하지 않고 서버 GameEndedEvent를 기다립니다.
            _commandPending = true;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
        }
        #endregion

        #region GameEnd UI Buttons
        public async override UniTask<bool> RequestRematchAsync(
            CancellationToken token)
        {
            bool accepted = await _netHandler.SendRematchRequest(
                _localPlayer.Team,
                token);

            if (!accepted)
                return false;

            await _netHandler.WaitForRematchConfirmedEvent(token);

            return true;
        }

        #endregion

        #region LiveGame UI Buttons

        public async override UniTask PassTurnAsync(
            CancellationToken token)
        {
            if (_desynced || !IsLocalTurn || _commandPending || ServerTurnNumber is not { } turnNumber)
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
            if (_desynced || !IsLocalTurn || _commandPending || ServerTurnNumber is not { } turnNumber)
                return;

            await SendCommandAsync(turnNumber, token,
                commandToken => _netHandler.SendGiveUpRequest(_localPlayer.Team, commandToken),
                waitForGameEnd: true);
        }

        private async UniTask SendCommandAsync(
            int turnNumber,
            CancellationToken token,
            Func<CancellationToken, UniTask<bool>> sendRequest,
            bool waitForGameEnd = false)
        {
            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                token, gameToken, _network.ConnectionToken);
            var completion = new UniTaskCompletionSource();
            _commandCompletion = completion;
            _waitingForGameEnd = waitForGameEnd;
            _commandPending = true;

            try
            {
                if (!await sendRequest(cts.Token))
                    return; // 명확한 서버 거절일 때만 재입력을 허용합니다.

                await WaitForCommandEventAsync(completion, waitForGameEnd, cts.Token);
            }
            catch (OperationCanceledException) when (gameToken.IsCancellationRequested)
            {
                // 게임/씬 종료에 따른 취소이며 새 게임의 상태는 변경하지 않습니다.
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(_commandCompletion, completion) && gameToken == _gameToken
                    && !IsEnd && completion.Task.Status != UniTaskStatus.Succeeded
                    && (waitForGameEnd || ServerTurnNumber == turnNumber))
                    BlockForNetworkFailure($"게임 명령의 서버 결과를 확인하지 못했습니다: {exception}");
            }
            finally
            {
                // 이전 명령의 늦은 완료가 다음 명령/재대결의 pending을 해제하지 않습니다.
                if (ReferenceEquals(_commandCompletion, completion) && gameToken == _gameToken)
                {
                    _commandCompletion = null;
                    _waitingForGameEnd = false;
                    _commandPending = false;
                }
            }
        }

        private async UniTask WaitForCommandEventAsync(
            UniTaskCompletionSource completion,
            bool waitForGameEnd,
            CancellationToken token)
        {
            using var timeoutCts = new CancellationTokenSource();
            using var timeout = timeoutCts.CancelAfterSlim(_network.RequestTimeout, DelayType.Realtime);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
            try
            {
                await completion.Task.AttachExternalCancellation(cts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !token.IsCancellationRequested)
            {
                var eventName = waitForGameEnd ? "GameEndedEvent" : "TurnCompletedEvent";
                throw new TimeoutException(
                    $"승인 후 {eventName} 대기 제한시간({_network.RequestTimeout.TotalSeconds}초)을 초과했습니다.");
            }
        }

        private void BlockForNetworkFailure(string reason)
        {
            if (_desynced)
                return;
            _desynced = true;
            SetLocalInputEnabled(false);
            _localPlayer.ResetSelection();
            _commandPending = false;
            Debug.LogError($"[NetworkMode] 서버 상태 불확실: {reason}");
            _commandCompletion?.TrySetCanceled();
        }
        #endregion

        #region Server Commands
        public void ApplyServerGameStart(NetworkGameStartData data)
        {
            ServerGameStart = data;
            LastServerTurn = null;
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

            if (ServerTurnNumber is not { } expectedTurn)
            {
                BlockForDesync(data, "적용할 서버 턴이 없습니다.");
                return;
            }

            if (data.TurnNumber < expectedTurn)
            {
                Debug.Log($"[NetworkMode] 과거 턴 무시: received={data.TurnNumber}, expected={expectedTurn}");
                return;
            }

            if (data.TurnNumber > expectedTurn)
            {
                BlockForDesync(data, "TurnNumber gap이 발생했습니다.");
                return;
            }

            // IsEnd는 StartEngine 완료 전에도 true입니다. 준비 전 이벤트는 적용하거나 보관하지 않습니다.
            if (IsEnd || _engine.References.Turn.IsEnd)
            {
                BlockForDesync(data, "Engine이 시작되지 않았거나 이미 종료되었습니다.");
                return;
            }

            var recordCount = _engine.References.ReadOnlyRecord.Count;
            if (recordCount + 1 != expectedTurn
                || data.Team != ServerTeam
                || data.Team != _engine.ControllerQuery.CurrentTurn)
            {
                BlockForDesync(data, "적용 전 Engine 턴/기록과 서버 상태가 일치하지 않습니다.");
                return;
            }

            TurnData? appliedTurn = null;
            _appliedTurn = null;
            try
            {
                switch (data.Action)
                {
                    case NetworkTurnAction.Move:
                        if (!_engine.TryProcessTurn(data.From, data.To))
                        {
                            BlockForDesync(data, "Engine.TryProcessTurn이 실패했습니다.");
                            return;
                        }
                        break;

                    case NetworkTurnAction.Pass:
                        _engine.HandleHandicap();
                        break;

                    default:
                        BlockForDesync(data, "지원하지 않는 턴 동작입니다.");
                        return;
                }

                appliedTurn = _appliedTurn;
            }
            catch (Exception exception)
            {
                BlockForDesync(data, $"Engine 적용 중 예외가 발생했습니다: {exception}");
                return;
            }
            finally
            {
                _appliedTurn = null;
            }

            if (appliedTurn is null
                || _engine.References.ReadOnlyRecord.Count != recordCount + 1
                || appliedTurn.MoveCount != data.TurnNumber
                || appliedTurn.ActingTeam != data.Team
                || (appliedTurn.MovedRecord is not null) != (data.Action == NetworkTurnAction.Move))
            {
                BlockForDesync(data, "Engine의 턴 완료 결과/기록을 확인할 수 없습니다.");
                return;
            }

            if (data.NextTeam == PlayerTeam.None)
            {
                // 정상 종료 시 Engine은 마지막 행동 팀을 유지하므로 CurrentTurn과 None을 비교하지 않습니다.
                if (!_engine.References.Turn.IsEnd || !appliedTurn.GameResult.HasValue)
                {
                    BlockForDesync(data, "서버는 종료 턴이지만 Engine의 GameResult/종료 상태가 없습니다.");
                    return;
                }
            }
            else if (_engine.References.Turn.IsEnd
                || appliedTurn.GameResult.HasValue
                || data.NextTeam != _engine.ControllerQuery.CurrentTurn)
            {
                BlockForDesync(data, "서버 NextTeam과 Engine의 다음 턴/종료 상태가 일치하지 않습니다.");
                return;
            }

            LastServerTurn = data;
            if (data.Team == _localPlayer.Team && !_waitingForGameEnd)
                _commandPending = false;

            base.HandleTurnCompleted(appliedTurn);
            if (data.Team == _localPlayer.Team && !_waitingForGameEnd)
                _commandCompletion?.TrySetResult();
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
            _commandCompletion?.TrySetCanceled();
        }
        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves)
        {
            if (IsEnd)
                return;

            if (!_engine.References.Turn.IsEnd)
                _engine.EndEngine();

            _commandPending = false;
            _localInputEnabled = false;
            ClearLocalInput();

            _commandCompletion?.TrySetResult();
            NotifyGameEnded(result, totalMoves);
        }
        #endregion

        #region Turn & Input
        // 동기적으로 발생한 Engine 결과는 검증 후 기존 턴 알림/입력 처리로 전달합니다.
        protected override void HandleTurnCompleted(TurnData data)
            => _appliedTurn = data;

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
            if (enabled && !IsEnd)
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
