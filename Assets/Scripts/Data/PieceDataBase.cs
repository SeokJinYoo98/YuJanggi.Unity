using System.Collections.Generic;
using UnityEngine;

namespace YuJanggi.Data.Board
{
    using Core.V2.Domain;

    using Runtime.Piece;

    [CreateAssetMenu(fileName = "PieceDataBase", menuName = "Piece/PieceDataBase")]
    public class PieceDataBase : ScriptableObject
    {
        [SerializeField] private List<PieceView>    _prefabs;
        [SerializeField] private Vector3            _baseScale;

        [SerializeField] private List<PieceData> _chos;
        [SerializeField] private List<PieceData> _hans;

        public Vector3 BaseScale => _baseScale;
        public PieceView GetPrefab(PlayerTeam type)
        {
            int index = (int)type;

            if (_prefabs == null || index < 0 || index >= _prefabs.Count || _prefabs[index] == null)
            {
                Debug.LogError($"[PieceDataBase] Invalid prefab for team: {type}");
                return null;
            }

            return _prefabs[index];
        }

        public PieceData GetData(PlayerTeam playerType, PieceType pieceType)
        {
            var list = playerType == PlayerTeam.Cho ? _chos : _hans;
            int index = (int)pieceType;

            if (list == null || index < 0 || index >= list.Count || list[index] == null)
            {
                Debug.LogError($"[PieceDataBase] Invalid piece data. Team: {playerType}, Type: {pieceType}");
                return null;
            }

            return list[index];
        }
    }
}
