using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

using YuJanggi.Core.InGame;
namespace YuJanggi.InGame.Mode
{
    using AI.Data;
    using AI.Service;
    using AI.Strategy;

    using Engine.Domain;
    using Engine.JanggiEngine;
    using Engine.JanggiOption;

    using Input;
    using Player;
    internal sealed class AIMode : GameMode
    {
        #region Field
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer   _localPlayer;
        private readonly AIPlayer       _aiPlayer;

        private CancellationTokenSource _aiTurnCts;
        private PlayerTeam _activeTeam;
        private bool _hasTurn;
        #endregion

        #region Constructor
        public AIMode(
            IJanggiEngine engine,
            InputHandlerFactory inputs,
            IGameCommandReceiver receiver,
            JanggiOptions options,
            AIMoveStrategyType strategyType)
            : base(engine)
        {
            bool localCho = options.PlayerCho == PlayerType.Local
                && options.PlayerHan == PlayerType.AI;
            bool localHan = options.PlayerHan == PlayerType.Local
                && options.PlayerCho == PlayerType.AI;
            if (!localCho && !localHan)
                throw new ArgumentException("AI 모드는 Local과 AI가 한 명씩 필요합니다.", nameof(options));

            var localTeam = localCho ? PlayerTeam.Cho : PlayerTeam.Han;
            var aiTeam = localCho ? PlayerTeam.Han : PlayerTeam.Cho;

            _localInput = inputs.Create(GameInputType.Pointer);
            _localPlayer = new LocalPlayer(
                localTeam, PlayerType.Local, receiver, engine.ControllerQuery);

            var strategy = AIMoveStrategyFactory.Create(strategyType);
            var moves = new AIMoveService(engine, strategy);
            var aiInput = new AIInputHandler(moves, engine.ControllerQuery);
            _aiPlayer = new AIPlayer(aiTeam, receiver, aiInput);
        }

        #endregion

        #region Game Start
        protected override void OnInitialize()
        { 
            CancelAITurn();
            _hasTurn = false;
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
        }
        protected override UniTask OnGameStartAsync(
            CancellationToken token)
        {
            return UniTask.CompletedTask;
        }
        #endregion

        #region Move Requests
        public override UniTask RequestMoveAsync(
            Pos from, Pos to,
            CancellationToken token)
        {
            if (!IsEnd && !_engine.TryProcessTurn(from, to))
                Debug.LogWarning($"[AIMode] 이동이 거절되었습니다: {from} -> {to}");

            return UniTask.CompletedTask;
        }

        #endregion

        #region LiveGame UI Buttons
        public override UniTask PassTurnAsync(
            CancellationToken token)
        {
            if (IsLocalTurn)
                _engine.HandleHandicap();
            return UniTask.CompletedTask;
        }
        public override UniTask TakeBackAsync(
            CancellationToken token)
        {
            if (IsLocalTurn)
                _engine.Undo();
            return UniTask.CompletedTask;
        }
        public override UniTask GiveUpAsync(
            CancellationToken token)
        {
            if (!IsLocalTurn)
                return UniTask.CompletedTask;

            _engine.GiveUp(_localPlayer.Team);
            return UniTask.CompletedTask;
        }
        #endregion

        #region Turn & Input
        private bool IsLocalTurn
            => !IsEnd && _engine.ControllerQuery.CurrentTurn == _localPlayer.Team;

        protected override void BeginNextTurn(PlayerTeam team)
        {
            if (IsEnd || (_hasTurn && _activeTeam == team))
                return;

            SetTurn(team);
        }

        private void SetTurn(PlayerTeam team)
        {
            CancelAITurn();
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();
            _activeTeam = team;
            _hasTurn = true;

            if (team == _localPlayer.Team)
            {
                if (_localInputEnabled)
                {
                    _localInput.Resume();
                    _localInput.SetPlayer(_localPlayer);
                }
            }
            else
            {
                _localInput.Pause();
                RunAITurnAsync().Forget();
            }
        }

        public override PlayerType GetPlayerType(PlayerTeam team)
            => team == _localPlayer.Team ? _localPlayer.Type : _aiPlayer.Type;

        protected override void OnSetLocalInputEnabled(bool enabled)
        {
            _localInput.ResetPlayer();
            _localPlayer.ResetSelection();

            if (enabled && !IsEnd
                && _engine.ControllerQuery.CurrentTurn == _localPlayer.Team)
            {
                _localInput.Resume();
                _localInput.SetPlayer(_localPlayer);
            }
            else
            {
                _localInput.Pause();
            }
        }

        #endregion

        #region AI Turn
        private async UniTask RunAITurnAsync()
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_gameToken);
            _aiTurnCts = cts;
            var token = cts.Token;

            try
            {
                // 게임 시작과 턴 갱신 호출이 끝난 뒤 입력을 생성합니다.
                await UniTask.NextFrame(token);
                if (IsEnd || _engine.ControllerQuery.CurrentTurn != _aiPlayer.Team)
                    return;

                await _aiPlayer.RequestMoveAsync(token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 턴 변경, 게임 종료 또는 씬 종료에 따른 취소입니다.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                if (ReferenceEquals(_aiTurnCts, cts))
                    _aiTurnCts = null;
            }
        }

        private void CancelAITurn()
        {
            var cts = _aiTurnCts;
            _aiTurnCts = null;
            cts?.Cancel();
        }

        #endregion


        #region Game End
        protected override void HandleGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            CancelAITurn();
            _hasTurn = false;
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();

            NotifyGameEnded(result, moveCount);
        }

        #endregion

        #region Clear
        protected override UniTask OnClearAsync(CancellationToken token)
        {
            CancelAITurn();
            _hasTurn = false;
            _localInput.ResetPlayer();
            _localInput.Pause();
            _localPlayer.ResetSelection();
            return UniTask.CompletedTask;
        }

        #endregion
    }
}
