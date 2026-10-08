
using System.Collections.Generic;
using UnityEngine;


namespace YuJanggi.InGame.Views.Piece
{
    using Engine.JanggiBoard;
    using Engine.Domain;

    using Data;

    public class PieceManager : MonoBehaviour
    {
        private PieceSpawner _pieceSpawner;
        private readonly Dictionary<int, PieceView> _views = new();

        private void Awake()
        {
            _pieceSpawner = GetComponent<PieceSpawner>();
        }

        public bool TryGetPiece(int id, out PieceView piece)
            => _views.TryGetValue(id, out piece);
        public void ResetViews(IReadOnlyBoard boardModel)
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
                    _views[pieceInfo.Id].SetSelectable(true);
                }
            }
        }
        public void SpawnPieces(IReadOnlyBoard boardModel)
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
            view.SetSelectable(true);
        }
        public void PlaceCapturedPiece(int id, Vector3 to)
        {
            var view = _views[id];
            view.MoveTo(to);
            view.SetSelectable(false);
        }

        public void DoMove(int id, Pos to)
            => _views[id].MoveTo(to);
    }
}


