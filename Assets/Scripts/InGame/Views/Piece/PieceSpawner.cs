using UnityEngine;

namespace YuJanggi.InGame.Views.Piece
{
    using Engine.JanggiBoard;
    using Engine.Domain;
    using Data.Board;

    public class PieceSpawner : MonoBehaviour
    {
        [SerializeField] private PieceDataBase _pieceDB;
        [SerializeField] private Transform     _cho;
        [SerializeField] private Transform     _han;
        
        public PieceView SpawnPiece(PieceModel pieceInfo, Pos pos)
        {
            var team = pieceInfo.Team;
            var type = pieceInfo.Type;

            var parent = team == PlayerTeam.Cho ? _cho : _han;

            var data   = _pieceDB.GetData(team, type);
            var prefab = _pieceDB.GetPrefab(team);
            var piece  = Instantiate(prefab, parent);
            piece.Init(data, pos); ;

            return piece;
        }
    }
}


