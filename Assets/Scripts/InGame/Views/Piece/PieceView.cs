using UnityEngine;
using DG.Tweening;

using YuJanggi.Runtime.Input;


namespace YuJanggi.InGame.Views.Piece
{
    using Engine.Domain;
    using Data.Board;

    public interface IPieceView
    {
        public void MoveTo(Pos toPos);
    }

    public class PieceView : MonoBehaviour, IPieceView, IBoardClickable
    {
        [SerializeField] private float _moveDuration = 0.16f;
        public Pos BoardPos
            => _boardPos;
        private Pos          _boardPos;
        private BoxCollider  _boxCollider;
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
     
        private Tween _moveTween;


        [SerializeField] private Transform _visual;

        [SerializeField] private float _highlightHeight = 1f;
        [SerializeField] private float _highlightDuration = 0.2f;
        [SerializeField] private float _highlightRotateSpeed = 30f;

        private Tween _highlightMoveTween;
        private Tween _highlightRotateTween;

        private bool        _highlight;
        private Vector3 _visualOriginLocalPos;
        private Quaternion _visualOriginLocalRot;

        private float _defaultZ = 0.1f;
        void Awake()
        {
            _boxCollider  = GetComponent<BoxCollider>();
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();

            _visualOriginLocalPos = _visual.localPosition;
            _visualOriginLocalRot = _visual.localRotation;
        }
        public void Init(PieceData data, Pos pos)
        {
            _boardPos                = pos;
            _meshFilter.sharedMesh   = data.PieceMesh;

            var team = data.Team;
            var type = data.Type;

            MaterialCheck(team, type);
            transform.position = new Vector3(pos.X, _defaultZ, pos.Z);
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
        public void MoveTo(Pos toPos)
        {
            _boardPos = toPos;
            MoveTo(new Vector3(toPos.X, _defaultZ, toPos.Z));
        }

        public void SetSelectable(bool selectable)
            => _boxCollider.enabled = selectable;
        
        public void MoveToHighlightPosition()
        {
            _highlightMoveTween?.Kill();

            _highlightMoveTween = _visual
                .DOLocalMoveY(
                    _visualOriginLocalPos.y + _highlightHeight,
                    _highlightDuration)
                .SetEase(Ease.OutQuad);
        }

        public void MoveToOriginPosition()
        {
            _highlightMoveTween?.Kill();

            _highlightMoveTween = _visual
                .DOLocalMoveY(
                    _visualOriginLocalPos.y,
                    _highlightDuration)
                .SetEase(Ease.OutQuad);
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
        public void Highlight()
        {
            SwapMaterial();
        }
    }

}


