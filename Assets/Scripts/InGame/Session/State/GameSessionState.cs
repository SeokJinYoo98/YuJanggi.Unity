
using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.InGame.Session
{
    using Core.V2.Domain;
    using Core.V2.Match;

    using InGame.Views;
    public enum SessionState
    {
        BaseState,
        LiveState,
        ReplayState,
        EndState,
        EndReplayState
    }
    public interface ISessionState
    {
        void Enter();
        void Exit();
        // UI 알림 (Match → State) 
        void OnPieceMoved(in MoveContext moveCtx);
        void OnTurnChanged(PlayerTeam next);
        void OnGameEnded(in GameResultInfo info);
        void OnCheckOccurred(PlayerTeam team);
        void OnCheckReleased();
        // 입력 (Controller → State)
        void OnSelectionChanged(int? pieceId, IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals);
        void RequestMove(Pos from, Pos to);
        void RequestHandicap();
        void RequestGiveUp();
        void RequestUndo();
        void RequestStepForward();
        void RequestStepBackward();
        // UI 입력
        void RequestResetGame(GameSessionInfo info, MatchModel matchModel, MatchView matchView, ReplayView replayView);

    }

    public abstract class SessionStateBase : ISessionState
    {
        protected bool _debug = false;
        protected SessionStateBase(ISessionTransition sessionFsm, IPlayerController cho, IPlayerController han, ILiveMatch liveMatch)
        {
            _liveMatch  =  liveMatch;
            _transition = sessionFsm;
            _cho        = cho;
            _han        = han;
        }
        protected readonly ILiveMatch _liveMatch;
        protected readonly ISessionTransition _transition;
        protected readonly IPlayerController _cho;
        protected readonly IPlayerController _han;

        public virtual void Enter() { if (_debug) Debug.Log($"{StateName()}_Start"); }
        public virtual void Exit() { if (_debug) Debug.Log($"{StateName()}_End"); }

        #region Match -> Session -> View
        public virtual void OnPieceMoved(in MoveContext moveCtx)
        {
            var record = moveCtx.Record;
            var from = record.From;
            var to = record.To; 
            if (_debug)
                Debug.Log($"{StateName()}_OnPieceMoved:{from.X},{from.Z} -> {to.X},{to.Z}");
            if (record.IsCapture)
            {
                from = record.To;
                if (_debug)
                    Debug.Log($"{StateName()}_Captured: {from.X},{from.Z}");
            }
        }
        public virtual void OnTurnChanged(PlayerTeam next) { if (_debug) Debug.Log($"{StateName()}_OnTurnChanged:{next}"); }
        public virtual void OnGameEnded(in GameResultInfo info) { if (_debug) Debug.Log($"{StateName()}_OnGameEnded"); }
        public virtual void OnCheckOccurred(PlayerTeam team) { if (_debug) Debug.Log($"{StateName()}_OnCheckOccured"); }
        public virtual void OnCheckReleased() { if (_debug) Debug.Log($"{StateName()}_OnCheckReleased"); }
        #endregion

        #region Input -> Session -> View
        public virtual void OnSelectionChanged(int? pieceId, IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals) { if (_debug) Debug.Log($"{StateName()}_OnSelectionChanged"); }
        #endregion

        #region Input -> Session -> Model
        public virtual void RequestMove(Pos from, Pos to) { if (_debug) Debug.Log($"{StateName()}_RequestMove:{from.X},{from.Z} -> {to.X},{to.Z}"); }
        public virtual void RequestUndo() { if (_debug) Debug.Log($"{StateName()}_RequestUndo"); }
        public virtual void RequestGiveUp() { if (_debug) Debug.Log($"{StateName()}_RequestGiveUp"); }
        public virtual void RequestHandicap() { if (_debug) Debug.Log($"{StateName()}_RequestHandicap"); }
        public virtual void RequestStepBackward() { if (_debug) Debug.Log($"{StateName()}_RequestStepBackward"); }
        public virtual void RequestStepForward() { if (_debug) Debug.Log($"{StateName()}_RequestStepForward"); }
        #endregion

        #region UIRequest
        // UI 입력
        public void RequestResetGame(GameSessionInfo info, MatchModel matchModel, MatchView matchView, ReplayView replayView)
        {
            if (_debug)
                Debug.Log($"{StateName()}_ResetGame");
            replayView.ResetGame();
            matchModel.InitGame(info.ChoFormation, info.HanFormation);
            matchView.ResetGame(matchModel.Board);
            matchModel.StartGame();
            BeginNextTurn(matchModel.PlayerTurn);
            _transition.ToLive();
        }
        #endregion

        protected virtual IPlayerController BeginNextTurn(PlayerTeam turn)
        {
            DisableAllControllers(); 
            if (_debug)
                Debug.Log($"{StateName()}_BeginNextTurn:{turn}");
            if (turn == PlayerTeam.Cho)
            {
                _cho.BeginTurn();
                return _cho;
            }
            _han.BeginTurn();

            return _han;
        }
        protected void DisableAllControllers()
        {
            if (_debug)
                Debug.Log($"{StateName()}_DisableAllControllers");
            _cho.EndTurn(); _han.EndTurn();
        }
        protected IPlayerController GetPlayer(PlayerTeam team)
        {
            if (_debug)
                Debug.Log($"{StateName()}_GetPlayer: {team}");
            return team == PlayerTeam.Cho ? _cho : _han;
        }

        protected virtual SessionState StateName() => SessionState.BaseState;
    }
}
