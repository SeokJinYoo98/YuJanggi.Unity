using System.Collections.Generic;

namespace YuJanggi.InGame.Session
{
    using Engine.Domain;

    using InGame.Views;

    using Controller;
    using Runtime.Input;
    using YuJanggi.Engine.JanggiEngine;

    public interface ISessionTransition
    {
        void ToLive();
        void ToReplay();
        void ToEnd();
        void ToEndReplay();
    }
    public class GameSession : ISessionTransition, IGameInputReceiver, IGameResultContext
    {

        #region Fields
        // 내부 상태와 참조를 저장하는 변수
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
            LiveView matchView,
            ReplayView replayView,
            IInGameController cho, IInGameController han,
            IInputHandler localInput)
        {
            _liveView    = matchView;

            _replayView   = replayView;
            _playerCho    = cho;
            _playerHan    = han;
            _localInput   = localInput;
            _states       = CreateStates();
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
            _liveView.InitMatchView(_engine.Board);
        }
        public void StartGame()
        {
            _play = true;
          
            ChangeState(SessionState.LiveState);
            _matchModel.StartGame();
            _playerCho.BeginTurn();
            _playerHan.EndTurn();
        }
        public void BindEvents()
        {
            _matchModel.BindEvents();
            _liveView.BindUI(_matchModel);

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
            _engine.UnBindEvents();
            _liveView.UnBindUI(_matchModel);

            var events = _engine.GameEvents;
            events.OnPieceMoved    -= OnPieceMoved;
            events.OnCheckOccurred -= OnCheckOccured;
            events.OnCheckReleased -= OnCheckReleased;
            events.OnTurnChanged   -= OnTurnChanged;
            events.OnGameEnded     -= OnGameEnded;

            _playerCho.UnBindEvents(this); 
            _playerHan.UnBindEvents(this);
        }

        public void Tick(float deltaTime)
        {
            if (_play)
                _engine.Tick(deltaTime);
        }
        #endregion

        #region private Field Member   
        private SessionState _currState = SessionState.BaseState;
        private readonly Dictionary<SessionState, ISessionState> _states;

        private readonly IJanggiEngine          _engine;
        private readonly IInputHandler          _localInput;
        private readonly IInGameController      _playerCho;
        private readonly IInGameController      _playerHan;

        private readonly ReplayView             _replayView;
        private readonly LiveView               _liveView;
        private bool                            _play = false;
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
            _replayView.ResetGame();
            _liveView.ResetGame(_engine.Board);
            _engine.StartEngine();
            ToLive();
        }
        #endregion

        #region State
        private Dictionary<SessionState, ISessionState> CreateStates()
        {
            var states = new Dictionary<SessionState, ISessionState>();
            states[SessionState.LiveState]   = new SessionLiveState(this, _matchModel, _playerCho, _playerHan, _liveView);
            states[SessionState.ReplayState] = new SessionReplayState(this, _matchModel, _playerCho, _playerHan, _replayView, _liveView);
            states[SessionState.EndState]    = new SessionEndState(this, this, _playerCho, _playerHan, _matchModel, _liveView);
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


