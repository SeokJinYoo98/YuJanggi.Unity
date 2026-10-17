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
        private bool IsLocalTurn
            => !IsEnd && _engine.ControllerQuery.CurrentTurn == _localPlayer.Team;
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
            ServerGameStart = null;
            LastServerTurn = null;
            _desynced = false;
            _appliedTurn = null;
            _commandPending = false;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
            _commandPending = true;
        }
        protected override async UniTask OnGameStartAsync(
            CancellationToken token)
        {
            _commandPending = true;
            _netHandler.SetReceiver(this);

            await _netHandler.SendInGameReadyAsync(token);
            await _netHandler.WaitForGameStartEvent(token);

            _commandPending = false;
        }
        #endregion

        #region Move
        public async override UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (_desynced || _commandPending || ServerTurnNumber is not { } turnNumber)
                return;
            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, gameToken);
            var isAccepted = false;
            _commandPending = true;

            try
            {
                isAccepted = await _netHandler.SendMoveRequestAsync(
                    _localPlayer.Team,
                    turnNumber,
                    from,
                    to,
                    cts.Token);
            }
            finally
            {
                // 이전 턴/게임의 늦은 실패가 현재 pending이나 종료 상태를 바꾸지 않습니다.
                if (!isAccepted && !IsEnd && gameToken == _gameToken && ServerTurnNumber == turnNumber)
                    _commandPending = false;
            }
        }
        #endregion

        #region EndGame
        protected override async UniTask OnClearAsync(
            CancellationToken token)
        {
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

            var gameToken = _gameToken;
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, gameToken);
            var isAccepted = false;
            _commandPending = true;

            try
            {
                isAccepted = await _netHandler.SendPassTurnRequest(
                    _localPlayer.Team,
                    turnNumber,
                    cts.Token);
            }
            finally
            {
                if (!isAccepted && !IsEnd && gameToken == _gameToken && ServerTurnNumber == turnNumber)
                    _commandPending = false;
            }
        }
        public async override UniTask GiveUpAsync(
            CancellationToken token)
        {
            if (!IsLocalTurn || _commandPending)
                return;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token, _gameToken);
            _commandPending = true;

            var isAccepted = await _netHandler.SendGiveUpRequest(
                _localPlayer.Team,
                cts.Token);

            if (!isAccepted)
            {
                _commandPending = false;
                Debug.Log("기권 요청이 거절되었습니다.");
            }
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
            if (data.Team == _localPlayer.Team)
                _commandPending = false;

            base.HandleTurnCompleted(appliedTurn);
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
        }
        public void ApplyServerGameEnded(
            GameResultInfo result,
            int totalMoves)
        {
            if (IsEnd)
                return;

            if (!_engine.References.Turn.IsEnd)
                _engine.EndEngine();

            _commandPending = true;
            _localInputEnabled = false;
            ClearLocalInput();

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
