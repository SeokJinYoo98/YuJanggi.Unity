using Cysharp.Threading.Tasks;
using System.Threading;

using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Mode
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    using Input;
    using Player;

    internal sealed class LocalMode : GameMode
    {
        public override GameModeType ModeType => GameModeType.Local;

        #region Field
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer _cho;
        private readonly ILocalPlayer _han;
        #endregion

        #region Constructor
        public LocalMode(
            IJanggiEngine        engine,
            InputHandlerFactory  inputs,
            IGameCommandReceiver receiver)
            : base(engine)
        {
            _localInput = inputs.Create(GameInputType.Pointer);

            _cho = new LocalPlayer(
                PlayerTeam.Cho,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);

            _han = new LocalPlayer(
                PlayerTeam.Han,
                PlayerType.Local,
                receiver,
                _engine.ControllerQuery);
        }

        #endregion

        #region Game Start
        protected override void OnInitialize()
        {
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _cho.ResetSelection();
            _han.ResetSelection();
        }

        protected override UniTask OnGameStartAsync(
            CancellationToken token)
                => UniTask.CompletedTask;

        #endregion

        #region Move Requests
        public override UniTask RequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (!_engine.TryProcessTurn(from, to))
                UnityEngine.Debug.LogWarning(
                    $"[LocalMode] 이동이 거절되었습니다: {from} -> {to}");

            return UniTask.CompletedTask;
        }

        #endregion

        #region LiveGame UI Buttons
        public override UniTask PassTurnAsync(
            CancellationToken token)
        {
            _engine.HandleHandicap();
            return UniTask.CompletedTask;
        }

        public override UniTask TakeBackAsync(
            CancellationToken token)
        {
            _engine.Undo();
            return UniTask.CompletedTask;
        }

        public override UniTask GiveUpAsync(
            CancellationToken token)
        {
            var loser = _engine.ControllerQuery.CurrentTurn;
            _engine.GiveUp(loser);
            return UniTask.CompletedTask;
        }

        #endregion

        #region Turn & Input
        protected override void BeginNextTurn(PlayerTeam team)
        {
            _localInput.ResetPlayer();

            _cho.ResetSelection();
            _han.ResetSelection();

            if (_localInputEnabled)
            {
                var nextPlayer = team == PlayerTeam.Cho ? _cho : _han;
                _localInput.Resume();
                _localInput.SetPlayer(nextPlayer);
            }
            else
            {
                _localInput.Pause();
            }
        }

        public override PlayerType GetPlayerType(PlayerTeam team)
            => team == PlayerTeam.Cho ?
                _cho.Type :
                _han.Type;

        protected override void OnSetLocalInputEnabled(bool enabled)
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

        #endregion


        #region Game End
        protected override void HandleGameEnded(
            GameResultInfo result,
            int moveCount)
        {
            _localInput.ResetPlayer();

            NotifyGameEnded(result, moveCount);
        }

        #endregion

        #region Clear
        protected override UniTask OnClearAsync(CancellationToken token)
        {
            _localInput.ResetPlayer();
            _localInput.Pause();
            _cho.ResetSelection();
            _han.ResetSelection();
            return UniTask.CompletedTask;
        }

        #endregion
    }
}
