
using System.Collections.Generic;
using UnityEngine;


namespace YuJanggi.Runtime.Piece
{
    using Core.Board;
    using Core.Domain;
    public class PieceManager : MonoBehaviour
    {
        private PieceSpawner _pieceSpawner;
        private readonly Dictionary<int, PieceView> _views = new();
        private int _currPiece;
        private void Awake()
        {
            _pieceSpawner = GetComponent<PieceSpawner>();
        }
        public void HighlightPiece(int id)
        {
            if (id == -1)
            {
                UnHighlightPiece();
                return;
            }

            UnHighlightPiece();

            if (!_views.TryGetValue(id, out var view))
            {
                Debug.LogError($"PieceView not found. id:{id}");
                return;
            }

            _currPiece = id;
            view.Highlight();
        }
        public void UnHighlightPiece()
        {
            if (_views.TryGetValue(_currPiece, out var curr))
                curr.UnHighlight();

            _currPiece = -1;
        }

        public void ResetViews(IBoardModel boardModel)
        {
            int width = boardModel.WIDTH;
            int height = boardModel.HEIGHT;

            for (int x = 0; x < width; ++x)
            {
                for (int z = 0; z < height; ++z)
                {
                    var pos = new Pos(x, z);
                    if (!boardModel.HasPiece(pos))
                        continue;

                    var pieceInfo = boardModel.GetPiece(pos);
                    _views[pieceInfo.Id].MoveTo(new Pos(x, z));
                    _views[pieceInfo.Id].SetDead(false);
                }
            }
        }
        public void SpawnPieces(IBoardModel boardModel)
        {
            int width  = boardModel.WIDTH;
            int height = boardModel.HEIGHT;

            for (int x = 0; x < width; ++ x)
            {
                for (int z = 0; z < height; ++z)
                {
                    var pos = new Pos(x, z);
                    if (!boardModel.HasPiece(pos))
                        continue;

                    var pieceInfo = boardModel.GetPiece(pos);
                    var piece     = _pieceSpawner.SpawnPiece(pieceInfo, pos);
                    _views[pieceInfo.Id] = piece;
                }
            }
        }
        public void RestoreCapturedPiece(int id, Pos to)
        {
            var view = _views[id];
            view.MoveTo(to);
            view.SetDead(false);
        }
        public void PlaceCapturedPiece(int id, Vector3 to)
        {
            var view = _views[id];
            view.MoveTo(to);
            view.SetDead(true);
        }

        public void DoMove(int id, Pos to)
            => _views[id].MoveTo(to);
    }
}
