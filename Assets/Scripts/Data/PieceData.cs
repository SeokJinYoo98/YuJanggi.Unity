using UnityEngine;
using YuJanggi.Core.V2.Domain;
namespace YuJanggi.Data.Board
{

    [CreateAssetMenu(fileName = "PieceData", menuName = "Piece/PieceData")]
    public class PieceData : ScriptableObject
    {
        [SerializeField] private PlayerTeam      _playerType;
        [SerializeField] private PieceType       _pieceType;
        [SerializeField] private Mesh            _mesh;
        public Mesh         PieceMesh   => _mesh;
        public PlayerTeam   Team        => _playerType;
        public PieceType    Type        => _pieceType;
    }
}
