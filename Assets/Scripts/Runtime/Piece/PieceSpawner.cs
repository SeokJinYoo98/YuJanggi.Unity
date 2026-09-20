using UnityEngine;

namespace YuJanggi.Runtime.Piece
{
    using Core.V2.Board;
    using Core.V2.Domain;
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
