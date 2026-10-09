using UnityEngine;
using System.Collections.Generic;
using YuJanggi.Core.InGame;

namespace YuJanggi.InGame.Player
{
    using Engine.Domain;
    using Engine.JanggiEngine;
    public class LocalPlayer : InGamePlayer, ILocalPlayer
    {
        protected readonly IControllerQuery _query;
        protected readonly List<Pos> _legal   = new(25);
        protected readonly List<Pos> _illegal = new(25);
        protected Pos _selectedPos = Pos.Invalid;
        private bool HasSelection
            => _selectedPos != Pos.Invalid;
        public LocalPlayer(
            PlayerTeam team,
            PlayerType type,
            IGameCommandReceiver receiver,
            IControllerQuery query)
            : base(team, type, receiver)
        {
            _query = query;
        }

        public void HandleInvalidClick()
            => ClearSelection();
        public void HandleValidClick(Pos pos)
        {
            Debug.Log("HandleValidClick");
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

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직

        private bool TrySelectPiece(Pos pos)
        {
            if (!_query.IsValidPiece(Team, pos, out int id))
                return false;

            Select(id, pos);
            return true;
        }
        private bool TryRequestMove(Pos toPos)
        {
            if (!_legal.Contains(toPos))
                return false;

            Receiver.RequestMove(_selectedPos, toPos);

            ResetSelection();
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
        private void ResetSelection()
        {
            _selectedPos = Pos.Invalid;

            _legal.Clear();
            _illegal.Clear();
        }
        private void ClearSelection()
        {
            ResetSelection();

            Receiver.SelectPiece(
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

            Receiver.SelectPiece(
                idx,
                _legal,
                _illegal);
        }
        #endregion
    }

}
