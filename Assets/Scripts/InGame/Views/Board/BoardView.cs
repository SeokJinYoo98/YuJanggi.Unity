using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.InGame.Views.Board
{
    using Audio;
    using BootStrap;
    using Engine.Domain;
    using Engine.JanggiBoard;
    using Particle;
    using Piece;
    using static UnityEngine.Audio.ProcessorInstance;

    public interface ILiveBoardView
    {
    }

    /// <summary>Live/Replay 여부와 무관하게 보드 위의 시각적 표현을 조정합니다.</summary>
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private PieceManager       _piece;
        [SerializeField] private ParticleManager    _particle;
        [SerializeField] private MoveGuideManager   _moveGuide;

        private PieceView   _currPiece;
        private int         _deathCnt;

        private Vector3 _deathPos;
        private static readonly Vector3 OriginDeath = new Vector3(4, 0, -2);
        private AudioManager Audio
            => YuJanggiBootStrap.Instance.AudioManager;

        public void InitPieces(IReadOnlyBoard model)
        {
            _deathPos = OriginDeath;
            _piece.SpawnPieces(model);
        }

        public void SyncBoardState(IReadOnlyBoard boardModel)
        {
            ClearSelection();
            _piece.ResetViews(boardModel);
        }
 
        public void ClearSelection()
        {
            UnSelectPiece();
            HideMoveGuides();
        }
        public void ApplyMovement(PieceModel piece , Pos from, Pos to)
        {
            if (!_piece.TryGetPiece(piece.Id, out var movedTarget))
                return;

            Audio.PlaySfx(JanggiSfx.Move);

            movedTarget.MoveTo(to);

            _particle.PlayMovementParticle(
                 new Vector3(from.X, 1f, from.Z),
                 new Vector3(to.X, 1f, to.Z));
        }
        public void RevertMovement(PieceModel piece, Pos from, Pos to)
        {
            if (!_piece.TryGetPiece(piece.Id, out var movedTarget))
                return;

            Audio.PlaySfx(JanggiSfx.Move);

            movedTarget.MoveTo(from);

            _particle.PlayMovementParticle(
                new Vector3(to.X, 1f, to.Z),
                new Vector3(from.X, 1f, from.Z));
        }
        public void ApplyCapture(PieceModel captured, Pos capturedPos)
        {
            Audio.PlaySfx(JanggiSfx.Capture);

            _particle.PlayCapture(
                new Vector3(capturedPos.X, 0f, capturedPos.Z));

            if (!_piece.TryGetPiece(
                    captured.Id,
                    out var capturedTarget))
                return;

            var deathPos = new Vector3(
                _deathPos.x,
                _deathPos.y + _deathCnt * 0.1f,
                _deathPos.z);

            ++_deathCnt;

            capturedTarget.MoveTo(deathPos);
            capturedTarget.SetSelectable(false);
        }
        public void RevertCapture(PieceModel captured, Pos restoredPos)
        {
            if (!_piece.TryGetPiece(captured.Id, out var capturedTarget))
                return;

            _deathCnt = Mathf.Max(0, _deathCnt - 1);
            capturedTarget.MoveTo(restoredPos);
            capturedTarget.SetSelectable(true);
        }


        public void SelectPiece(int id)
        {
            if (!_piece.TryGetPiece(id, out var piece))
                return;

            ClearSelection();

            _currPiece = piece;
            _currPiece.SelectPiece();

            Audio.PlaySfx(JanggiSfx.Select);
        }

        private void UnSelectPiece()
        {
            _currPiece?.UnSelectPiece();
            _currPiece = null;
        }
        public void ShowMoveGuides(
            IReadOnlyList<Pos> legals,
            IReadOnlyList<Pos> illegals)
        {
            HideMoveGuides();
            if (legals != null)
                _moveGuide.ShowHighlight(legals, true);
            if (illegals != null)
                _moveGuide.ShowHighlight(illegals, false);
        }

        private void HideMoveGuides()
            => _moveGuide.HideHighlight();

    }
}
