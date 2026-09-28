using System;
using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.Controller
{
    using Engine.Domain;
    using Engine.JanggiBoard;
    using YuJanggi.Engine.JanggiEngine;

    public class LocalController : IPlayerController, ILocalPlayer
    {
        #region Fields
        // 내부 상태와 참조를 저장하는 변수
        private readonly IInputHandler _input;
        private readonly IControllerQuery _query;
        private readonly List<Pos> _legal = new(25);
        private readonly List<Pos> _illegal = new(25);
        private Pos _selectedPos
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
        internal event SelectionChangedHandler? OnSelectionChanged;
        internal event MoveRequestHandler? OnMoveRequest;
        #endregion
        #region Delgates
        internal delegate void MoveRequestHandler(Pos from, Pos to);
        internal delegate void SelectionChangedHandler(
            int? pieceId,
            IReadOnlyList<Pos> legalWays,
            IReadOnlyList<Pos> illegalWays);
        #endregion
        #region Constructors
        // 순수 C#
        public LocalController(
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
        public void BindEvents(IGameInputReceiver receiver)
        {
            _input.OnBoardClicked += HandleBoardClicked;
            _input.OnEmptyClicked += HandleEmptyClicked;
            OnSelectionChanged    += receiver.ChangeSelection;
            OnMoveRequest         += receiver.RequestMove;
        }
        public void UnBindEvents(IGameInputReceiver receiver)
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
        #endregion









        private void HandleBoardClicked(Pos pos)
        {
            if (!IsMyTurn)
                return;

            if (!HasSelection)
            {
                TrySelectPiece(pos);
                return;
            }

            if (TryMovePiece(pos))
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
        private bool TryMovePiece(Pos toPos)
        {
            if (!_legal.Contains(toPos))
                return false;

            OnMoveRequest?.Invoke(
                )
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

        private void ClearSelection()
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


    }
}


