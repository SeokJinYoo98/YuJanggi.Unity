using System.Collections.Generic;

namespace YuJanggi.Runtime.GameSession
{
    using Core.Domain;
    using Core.Match;

    using Piece;

    public class GameSession : ISessionTransition, IGameInputReceiver, IGameResultContext
    {
        #region public Field F
        public GameSession(
            GameSessionInfo    sessionInfo,
            MatchView          matchView,
            MatchModel         matchModel,
            ReplayView         replayView,
            IPlayerController  cho, IPlayerController  han,
            IInputHandler      localInput)
        {
            _matchView    = matchView;
            _sessionInfo  = sessionInfo;
            _matchModel   = matchModel;
            _replayView   = replayView;
            _playerCho    = cho;
            _playerHan    = han;
            _localInput   = localInput;
            _states       = CreateStates();
        }
        public void StartGame()
        {
            ChangeState(SessionState.LiveState);

            _matchModel.InitGame(_sessionInfo.ChoFormation, _sessionInfo.HanFormation);
            _matchView.StartGame(_matchModel.Board);
            _matchModel.StartGame();

            _playerCho.BeginTurn();
            _playerHan.EndTurn();
        }
        public void BindEvents()
        {
            _matchModel.BindEvents();
            _matchView.BindUI(_matchModel);

            var events = _matchModel.MatchEvent;
            events.OnPieceMoved    += OnPieceMoved;
            events.OnCheckOccurred += OnCheckOccured;
            events.OnCheckReleased += OnCheckReleased;
            events.OnGameEnded     += OnGameEnded;
            events.OnTurnChanged   += OnTurnChanged;

            _playerCho.BindEvents(this); // this = IGameInputReceiver
            _playerHan.BindEvents(this); // this = IGameInputReceiver
        }
        public void UnBindEvents()
        {
            _matchModel.UnBindEvents();
            _matchView.UnBindUI(_matchModel);

            var events = _matchModel.MatchEvent;
            events.OnPieceMoved    -= OnPieceMoved;
            events.OnCheckOccurred -= OnCheckOccured;
            events.OnCheckReleased -= OnCheckReleased;
            events.OnTurnChanged   -= OnTurnChanged;
            events.OnGameEnded     -= OnGameEnded;

            _playerCho.UnBindEvents(this); 
            _playerHan.UnBindEvents(this);
        }

        public void Tick(float deltaTime)
            => _matchModel.Tick(deltaTime);
        #endregion

        #region private Field Member   
        private SessionState _currState = SessionState.BaseState;
        private readonly Dictionary<SessionState, ISessionState> _states;

        private readonly GameSessionInfo        _sessionInfo;
        private readonly IInputHandler          _localInput;
        private readonly IPlayerController      _playerCho;
        private readonly IPlayerController      _playerHan;

        private readonly ReplayView             _replayView;
        private readonly MatchView              _matchView;
        private readonly MatchModel             _matchModel;

        public GameResultInfo? GameResult { get; private set; }

        #endregion
        private void ChangeState(SessionState next)
        {
            if (_currState == next)
                return;

            if (_states.TryGetValue(_currState, out var curr))
                curr.Exit();

            _currState = next;
            _states[_currState].Enter();
        }
        public void RequestMove(Pos from, Pos to)
            => _states[_currState].RequestMove(from, to);
        public void ChangeSelection(int? pieceId, IReadOnlyList<Pos> legal, IReadOnlyList<Pos> illegal)
            => _states[_currState].OnSelectionChanged(pieceId, legal, illegal);

        #region private Field F
        // Events
        private void OnPieceMoved(MoveContext moveCtx)
        => _states[_currState].OnPieceMoved(in moveCtx);
        private void OnCheckReleased()
            => _states[_currState].OnCheckReleased();
        private void OnCheckOccured(PlayerTeam team)
            => _states[_currState].OnCheckOccurred(team);
        private void OnTurnChanged(PlayerTeam next)
            => _states[_currState].OnTurnChanged(next);
        private void OnGameEnded(GameResultInfo info)
        {
            GameResult = info; 
            _states[_currState].OnGameEnded(in info);
        }
        // Player

        // UI
        public void  StepForward()
            => _states[_currState].RequestStepForward();
        public void  StepBackward()
            => _states[_currState].RequestStepBackward();
        public void  Handicap()
            => _states[_currState].RequestHandicap();
        public void  GiveUp()
            => _states[_currState].RequestGiveUp();
        public void  UnDo()
            => _states[_currState].RequestUndo();
        public void  ResetGame()
        {
            GameResult = null;
            _states[_currState].RequestResetGame(_sessionInfo, _matchModel, _matchView, _replayView);
        }
        #endregion

        #region State
        private Dictionary<SessionState, ISessionState> CreateStates()
        {
            var states = new Dictionary<SessionState, ISessionState>();
            states[SessionState.LiveState]   = new SessionLiveState(this, _matchModel, _playerCho, _playerHan, _matchView);
            states[SessionState.ReplayState] = new SessionReplayState(this, _matchModel, _playerCho, _playerHan, _replayView, _matchView);
            states[SessionState.EndState]    = new SessionEndState(this, this, _playerCho, _playerHan, _matchModel, _matchView);
            states[SessionState.EndReplayState] = new SessionEndReplayState(this, _playerCho, _playerHan, _matchModel, _replayView);
            return states;
        }

        public void ToLive()
        {
            ChangeState(SessionState.LiveState);
            _localInput.Activate();
        }
        public void ToReplay()
        {
            _localInput.Deactivate();
            ChangeState(SessionState.ReplayState);
        }
        public void ToEnd()
        {
            _localInput.Deactivate();
            ChangeState(SessionState.EndState);
        }
        public void ToEndReplay()
            => ChangeState(SessionState.EndReplayState);
        #endregion
    }
}
