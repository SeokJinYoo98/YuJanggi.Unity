using System.Collections.Generic;

namespace YuJanggi.InGame.Session
{
    using Engine.JanggiEngine;
    using Engine.Domain;

    using InGame.Views;
    using InGame.Views.Board;

    using Controller;
    using Runtime.Input;


    internal interface ISessionTransition
    {
        void ToLive();
        void ToReplay();
        void ToEnd();
        void ToEndReplay();
    }
    internal class GameSession : ISessionTransition, IGameResultContext
    {

        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        private SessionState _currState = SessionState.BaseState;
        private readonly Dictionary<SessionState, ISessionState> _states;

        private readonly IJanggiEngine      _engine;
        private readonly IInputHandler      _localInput;
        private readonly IInGameController  _playerCho;
        private readonly IInGameController  _playerHan;

        private readonly LiveView   _liveView;
        private readonly ReplayPlayback _replayPlayback;
        private readonly BoardView _boardView;

        private bool _play = false;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트
        #endregion

        #region Constructors
        // 순수 C#
        public GameSession(
            IJanggiEngine engine,
            IInputHandler localInput,
            IInGameController cho, IInGameController han,
            LiveView liveView, ReplayPlayback replayPlayback, BoardView boardView)
        {
            _engine     = engine;
            _localInput = localInput;
            _playerCho  = cho;
            _playerHan  = han;
            _liveView = liveView; _replayPlayback = replayPlayback;
            _boardView = boardView;
            _states = CreateStates();
        }

        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        #endregion
        #region public Field F

        public void InitGame()
        {
            _engine.InitEngine();
            _boardView.InitPieces(_engine.Board);
        }
        public void StartGame()
        {
            _play = true;
          
            ChangeState(SessionState.LiveState);
            _engine.StartEngine();
            _playerCho.BeginTurn();
            _playerHan.EndTurn();
        }
        public bool IsCurrentTurn(PlayerTeam team)
            => _currState == SessionState.LiveState && _engine.CurrentTurn == team;

        public bool CanApplyConfirmedMove(PlayerTeam team)
            => (_currState == SessionState.LiveState || _currState == SessionState.ReplayState)
                && _engine.CurrentTurn == team;

        public bool IsStarted => _play;

        public void BindEvents(IGameInputReceiver inputReceiver)
        {
            _engine.BindEvents();
            _liveView.BindUI(_engine.GameStateEvents);

            var events = _engine.GameEvents;
            events.OnPieceMoved    += OnPieceMoved;
            events.OnCheckOccurred += OnCheckOccured;
            events.OnCheckReleased += OnCheckReleased;
            events.OnGameEnded     += OnGameEnded;
            events.OnTurnChanged   += OnTurnChanged;

            _playerCho.BindEvents(inputReceiver);
            _playerHan.BindEvents(inputReceiver);
        }
        public void UnBindEvents(IGameInputReceiver inputReceiver)
        {
            _engine.UnBindEvents();
            _liveView.UnBindUI(_engine.GameStateEvents);

            var events = _engine.GameEvents;
            events.OnPieceMoved    -= OnPieceMoved;
            events.OnCheckOccurred -= OnCheckOccured;
            events.OnCheckReleased -= OnCheckReleased;
            events.OnTurnChanged   -= OnTurnChanged;
            events.OnGameEnded     -= OnGameEnded;

            _playerCho.UnBindEvents(inputReceiver);
            _playerHan.UnBindEvents(inputReceiver);
        }

        public void Tick(float deltaTime)
        {
            if (_play)
                _engine.Tick(deltaTime);
        }
        #endregion

        #region private Field Member   

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
            var board = _engine.Board;

            _engine.InitEngine();
            _replayPlayback.ResetGame();
            _liveView.ResetGame();
            _boardView.SyncBoardState(_engine.Board);
            _engine.StartEngine();
            ToLive();
        }
        #endregion

        #region State
        private Dictionary<SessionState, ISessionState> CreateStates()
        {
            var states = new Dictionary<SessionState, ISessionState>();
            states[SessionState.LiveState]   = new SessionLiveState(this, _engine, _playerCho, _playerHan, _liveView, _boardView);
            states[SessionState.ReplayState] = new SessionReplayState(this, _engine, _playerCho, _playerHan, _replayPlayback, _liveView);
            states[SessionState.EndState]    = new SessionEndState(this, this, _playerCho, _playerHan, _engine, _liveView, _boardView);
            states[SessionState.EndReplayState] = new SessionEndReplayState(this, _playerCho, _playerHan, _engine, _replayPlayback);
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


