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
        private readonly ILocalInputHandler _localInput;
        private readonly ILocalPlayer _cho;
        private readonly ILocalPlayer _han;
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

        protected override UniTask OnGameStartAsync(
            CancellationToken token)
                => UniTask.CompletedTask;


        protected override UniTask OnRequestMoveAsync(
            Pos from,
            Pos to,
            CancellationToken token)
        {
            if (!_engine.TryProcessTurn(from, to))
                UnityEngine.Debug.LogWarning(
                    $"[LocalMode] 이동이 거절되었습니다: {from} -> {to}");

            return UniTask.CompletedTask;
        }
        protected override UniTask OnPassTurnAsync(
            CancellationToken token)
        {
            _engine.HandleHandicap();
            return UniTask.CompletedTask;
        }

        protected override UniTask<bool> OnRequestRematchAsync(
            CancellationToken token)
            => UniTask.FromResult(true);

        protected override UniTask OnTakeBackAsync(
            CancellationToken token)
        {
            _engine.Undo();
            return UniTask.CompletedTask;
        }
        protected override UniTask OnInitializeAsync(
            CancellationToken token)
        {
            _localInput.Initialize();
            _localInput.ResetPlayer();
            _cho.ResetSelection();
            _han.ResetSelection();
            return UniTask.CompletedTask;
        }

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
        #region 게임 종료


        protected override UniTask<GameResultInfo> OnGameEndAsync(
            GameResultInfo info,
            int totalMoves,
            CancellationToken token)
        {
            _localInput.ResetPlayer();
            return UniTask.FromResult(info);
        }

        protected override UniTask OnClearAsync(CancellationToken token)
        {
            _localInput.ResetPlayer();
            _localInput.Pause();
            _cho.ResetSelection();
            _han.ResetSelection();
            return UniTask.CompletedTask;
        }

        protected override UniTask<PlayerTeam?> OnGiveUpAsync(
            CancellationToken token)
            => UniTask.FromResult<PlayerTeam?>(
                _engine.ControllerQuery.CurrentTurn);
        #endregion
    }
}
