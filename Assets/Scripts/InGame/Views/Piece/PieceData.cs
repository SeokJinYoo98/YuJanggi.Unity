using UnityEngine;
using YuJanggi.Engine.Domain;
namespace YuJanggi.InGame.Views.Piece.Data
{

    [CreateAssetMenu(fileName = "PieceData", menuName = "YuJanggi/Piece/PieceData")]
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


