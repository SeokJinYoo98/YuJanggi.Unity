using System;
using UnityEngine;
using System.Collections.Generic;

namespace YuJanggi.Runtime.Controller
{
    using Core.V2.Board;
    using Core.V2.Domain;
    using Core.V2.Rule;
    public class LocalController : IPlayerController, ILocalPlayer
    {
        public  PlayerTeam              Team { get; }
        private bool                    _isTurn;
        private readonly IInputHandler  _input;
        private readonly IBoardModel    _board;
        private readonly IJanggiRule    _rule;

        private readonly Selection      _selection;
        public bool IsLocal() => true;
        public event Action<int?, IReadOnlyList<Pos>, IReadOnlyList<Pos>> OnSelectionChanged;
        public event Action<Pos, Pos> OnMoveRequest;
        public LocalController(IJanggiRule rule, IBoardModel board, PlayerTeam team, IInputHandler input)
        {
            Team        = team;
            _board      = board;
            _rule       = rule;
            _input      = input;

            _selection = new Selection();

            _isTurn = false;
        }
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
            => _isTurn = true;
        public void EndTurn()
            => _isTurn = false;
        private void HandleBoardClicked(Pos pos)
        {
            if (!_isTurn)
                return;

            if (!_selection.HasSelection)
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
            if (!TryGetOwnPiece(pos, out var piece))
                return false;

            Select(piece.Id, pos);
            return true;
        }
        private bool TryMovePiece(Pos toPos)
        {
            if (!_selection.IsMovable(toPos))
                return false;

            OnMoveRequest?.Invoke(_selection.FromPos, toPos);
            ClearSelection();
            return true;
        }
        private bool TryReselectPiece(Pos pos)
        {
            if (pos == _selection.FromPos)
                return false;
            if (!_board.HasPiece(pos))
                return false;
            if (!TryGetOwnPiece(pos, out var piece))
                return false;

            Select(piece.Id, pos);

            return true;
        }
        private bool TryGetOwnPiece(Pos pos, out PieceModel piece)
        {
            piece = default;
            if (!_board.HasPiece(pos))
                return false;

            piece = _board.GetPiece(pos);
            return piece.Team == Team;
        }
        private void ClearSelection()
        {
            if (!_selection.HasSelection) return;
            _selection.Clear();
            _selection.FromPos = Pos.Invalid;
            OnSelectionChanged?.Invoke(null, _selection.LegalCells, _selection.IllegalCells);
        }
        private void Select(int idx, Pos pos)
        {
            _selection.Clear();
            _selection.FromPos = pos;
            _rule.FindWays(_board, _selection);
            

            OnSelectionChanged?.Invoke(idx, _selection.LegalCells, _selection.IllegalCells);
        }


    }
}
