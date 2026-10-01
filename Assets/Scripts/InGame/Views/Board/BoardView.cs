using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.InGame.Views.Board
{
    using Engine.Domain;
    using Engine.JanggiBoard;
    using Audio;
    using BootStrap;
    using Particle;
    using Piece;

    public interface ILiveBoardView
    {
    }

    /// <summary>Live/Replay 여부와 무관하게 보드 위의 시각적 표현을 조정합니다.</summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private PieceManager _piece;
        [SerializeField] private ParticleManager _particle;
        [SerializeField] private MoveGuideManager _moveGuide;

        private PieceView _currPiece;
        private int _deathCnt;
        private Vector3 _deathPos = new Vector3(4, 0, -2);

        private AudioManager Audio => YuJanggiBootStrap.Instance.AudioManager;

        public void InitPieces(IReadOnlyBoard model)
            => _piece.SpawnPieces(model);

        public void SetDeathPosition(Vector3 pos) => _deathPos = pos;

        public void SyncBoardState(IReadOnlyBoard boardModel)
        {
            ClearSelection();
            _piece.ResetViews(boardModel);
        }

        public void MovePiece(int id, Pos to) => _piece.DoMove(id, to);

        public void PlaceCapturedPiece(int id, PlayerTeam team)
        {
            var deathPos = new Vector3(_deathPos.x, _deathPos.y + _deathCnt * 0.1f, _deathPos.z);
            ++_deathCnt;
            _piece.PlaceCapturedPiece(id, deathPos);
        }

        public void RestoreCapturedPiece(int id, PlayerTeam team, Pos to)
        {
            --_deathCnt;
            _piece.RestoreCapturedPiece(id, to);
        }

        public void ApplyMovement(MoveRecord record, bool playAudio = true,
            bool playParticle = true, bool clearSelection = true, bool lowerSelectedPiece = false)
        {
            if (clearSelection) ClearSelection();
            if (lowerSelectedPiece && _piece.TryGetPiece(record.MovedPiece.Id, out var selected)
                && selected == _currPiece)
                selected.ShowMovementPose();
            var from = record.From;
            var to = record.To;
            if (playParticle)
                _particle.PlayMovementParticle(
                    new Vector3(from.X, 1f, from.Z),
                    new Vector3(to.X, 1f, to.Z));
            MovePiece(record.MovedPiece.Id, to);
            if (playAudio) Audio.PlaySfxOneShot(JanggiSfx.Move);

            if (record.IsCapture)
            {
                if (playParticle) _particle.PlayCapture(new Vector3(to.X, 0f, to.Z));
                PlaceCapturedPiece(record.CapturedPiece.Id, record.CapturedPiece.Team);
                if (playAudio) Audio.PlaySfxOneShot(JanggiSfx.Capture);
            }
        }

        public void RevertMovement(MoveRecord record, bool clearSelection = true,
            bool restoreSelectionPose = false)
        {
            if (clearSelection) ClearSelection();
            MovePiece(record.MovedPiece.Id, record.From);
            if (record.IsCapture)
                RestoreCapturedPiece(record.CapturedPiece.Id, record.CapturedPiece.Team, record.To);
            if (restoreSelectionPose && _piece.TryGetPiece(record.MovedPiece.Id, out var selected)
                && selected == _currPiece)
                selected.ShowHighlightPose();
        }

        public void SelectPiece(int id, bool playAudio = true)
        {
            if (!_piece.TryGetPiece(id, out var piece))
                return;

            ClearSelection();

            _currPiece = piece;
            _currPiece.SelectPiece();

            if (playAudio) Audio.PlaySfxOneShot(JanggiSfx.Select);
        }

        public void UnSelectPiece()
        {
            _currPiece?.UnSelectPiece();
            _currPiece = null;
        }

        public void ClearSelection()
        {
            UnSelectPiece();
            HideMoveGuides();
        }

        public void ShowMoveGuides(IReadOnlyList<Pos> legals, IReadOnlyList<Pos> illegals)
        {
            HideMoveGuides();
            _moveGuide.ShowHighlight(legals, true);
            _moveGuide.ShowHighlight(illegals, false);
        }

        public void HideMoveGuides() => _moveGuide.HideHighlight();

    }
}
