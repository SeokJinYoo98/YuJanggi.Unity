using System;
using System.Collections.Generic;

namespace YuJanggi.InGame.Controller
{
    using Engine.Domain;
    using YuJanggi.Engine.JanggiEngine;
    using Runtime.Input;
    internal class InGameLocalController
        : IInGameController, IInGameLocalController
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        protected readonly IInputHandler _input;
        protected readonly IControllerQuery _query;
        protected readonly List<Pos> _legal   = new(25);
        protected readonly List<Pos> _illegal = new(25);
        protected Pos _selectedPos
            = Pos.Invalid;
        #endregion

        #region Properties
        // 상태를 조회하거나 변경하는 접근 속성
        public PlayerTeam Team { get; }
        // 개별 내 턴인지 가지지 않고,
        // 엔진의 팀과 비교해서 턴을 알게
        public bool IsMyTurn
            => Team == _query.CurrentTurn;
        public bool IsLocal
            => true;
        private bool HasSelection
            => _selectedPos != Pos.Invalid;

        #endregion

        #region Events
        // 상태 변화나 특정 동작을 외부에 알리는 이벤트
        public event SelectionChangedHandler  OnSelectionChanged;
        public event MoveRequestHandler       OnMoveRequest;
        #endregion

        #region Constructors
        // 순수 C#
        public InGameLocalController(
            PlayerTeam team,
            IControllerQuery query,
            IInputHandler input)
        {
            Team    = team;
            _input  = input;
            _query  = query;
        }

        #endregion

        #region Public Methods
        // 외부에서 호출하는 기능
        public virtual void BindEvents(IGameInputReceiver receiver)
        {
            _input.OnBoardClicked += HandleBoardClicked;
            _input.OnEmptyClicked += HandleEmptyClicked;
            OnSelectionChanged    += receiver.ChangeSelection;
            OnMoveRequest         += receiver.RequestMove;
        }
        public virtual void UnBindEvents(IGameInputReceiver receiver)
        {
            if (_input != null)
            {
                _input.OnBoardClicked -= HandleBoardClicked;
                _input.OnEmptyClicked -= HandleEmptyClicked;
            }

            OnSelectionChanged    -= receiver.ChangeSelection;
            OnMoveRequest         -= receiver.RequestMove;
        }
        public void BeginTurn()
        {

        }
        public void EndTurn()
        {

        }
        #endregion

        #region Event Handlers
        // 구독한 이벤트가 발생했을 때 실행하는 처리 메서드
        #endregion

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        protected void HandleBoardClicked(Pos pos)
        {
            if (!IsMyTurn)
                return;

            if (!HasSelection)
            {
                TrySelectPiece(pos);
                return;
            }

            if (TryRequestMove(pos))
                return;


            if (TryReselectPiece(pos))
                return;

            ClearSelection();
        }
        private void HandleEmptyClicked()
        {
            ClearSelection();
        }
        private bool TrySelectPiece(Pos pos)
        {
            if (!_query.IsValidPiece(Team, pos, out int id))
                return false;

            Select(id, pos);
            return true;
        }
        protected virtual bool TryRequestMove(Pos toPos)
        {
            if (!_legal.Contains(toPos))
                return false;

            OnMoveRequest(_selectedPos, toPos);

            ClearSelection();
            return true;
        }
        private bool TryReselectPiece(Pos pos)
        {
            if (pos == _selectedPos)
                return false;

            if (!_query.IsValidPiece(Team, pos, out int id))
                return false;

            Select(id, pos);

            return true;
        }
        protected void ClearSelection()
        {
            _selectedPos = Pos.Invalid;

            _legal.Clear();
            _illegal.Clear();

            OnSelectionChanged?.Invoke(
                null,
                _legal,
                _illegal);
        }
        private void Select(int idx, Pos pos)
        {
            _selectedPos = pos;

            _query.GetMovableCells(
                _selectedPos,
                _legal,
                _illegal);

            OnSelectionChanged?.Invoke(
                idx,
                _legal,
                _illegal);
        }
        #endregion
    }
}


