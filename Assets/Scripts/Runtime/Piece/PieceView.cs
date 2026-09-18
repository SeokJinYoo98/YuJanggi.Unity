using UnityEngine;
using DG.Tweening;

using YuJanggi.Runtime.Input;


namespace YuJanggi.Runtime.Piece
{
    using Core.Domain;
    using Data.Board;

    public interface IPieceView
    {
        public void Highlight();
        public void MoveTo(Pos toPos);
    }

    public class PieceView : MonoBehaviour, IPieceView, IBoardClickable
    {
        [SerializeField] private float _moveDuration = 0.16f;
        public Pos BoardPos => _boardPos;
        
        private Pos          _boardPos;
        private BoxCollider  _boxCollider;
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private bool         _highlight;
        private Tween        _moveTween;
        void Awake()
        {
            _boxCollider  = GetComponent<BoxCollider>();
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
        }
        public void Init(PieceData data, Pos pos)
        {
            _boardPos                = pos;
            _meshFilter.sharedMesh   = data.PieceMesh;

            var team = data.Team;
            var type = data.Type;

            MaterialCheck(team, type);
            transform.position = new Vector3(pos.X, 1, pos.Z);
            transform.Rotate(new Vector3(0, 180, 0));
        }
        public void  MoveTo(Vector3 toPos)
        {
            _moveTween?.Kill();

            _moveTween = transform
                .DOMove(toPos, _moveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    transform.position = toPos;
                    _moveTween         = null;
                });
        }
        public void  MoveTo(Pos toPos)
        {
            Vector3 worldPos = new Vector3(toPos.X, 1f, toPos.Z);

            _moveTween?.Kill();

            _moveTween = transform
                .DOMove(worldPos, _moveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    _boardPos           = toPos;
                    transform.position  = worldPos;
                    _moveTween          = null;
                });
        }

        public void SetDead(bool dead)
        {
            _boxCollider.enabled = !dead;
            UnHighlight();
        }
        public void  Highlight()
        {
            if (_highlight) return;
            SwapMaterial();
            _highlight = !_highlight;
        }
        public void  UnHighlight()
        {
            if (!_highlight) return;
            SwapMaterial();
            _highlight = !_highlight;
        }
        private void MaterialCheck(PlayerTeam team, PieceType type)
        {
            if (team == PlayerTeam.Cho)
            {
                if (type ==  PieceType.Guard)
                {
                    SwapMaterial();
                }
            }

            else if (team == PlayerTeam.Han)
            {
                if (type == PieceType.Soldier || type == PieceType.Cannon)
                {
                    SwapMaterial();
                }
            }
        }
        private void SwapMaterial()
        {
            var mats = _meshRenderer.sharedMaterials;

            if (mats.Length < 2)
                return;

            (mats[0], mats[1]) = (mats[1], mats[0]);
            _meshRenderer.sharedMaterials = mats;
        }

    }

}
