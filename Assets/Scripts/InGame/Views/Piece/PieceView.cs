using UnityEngine;
using DG.Tweening;


using YuJanggi.Core.InGame;
namespace YuJanggi.InGame.Views.Piece
{
    using Engine.Domain;
    using Data;

    public interface IPieceView
    {
        public void MoveTo(Pos toPos);
    }

    public class PieceView : MonoBehaviour, IPieceView, IBoardClickable
    {
        #region Fields
        [Header("Movement Options")]
        [SerializeField] private float _moveDuration = 0.16f;
        [Header("Highlight Options")]
        [SerializeField] private Transform _visual;

        private Pos          _boardPos;
        private BoxCollider  _boxCollider;
        private MeshFilter   _meshFilter;
        private MeshRenderer _meshRenderer;
        private Material[]   _materials;
     
        private Tween _moveTween;
        private Tween _highlightMoveTween;
        private Tween _highlightRotateTween;
        private TweenCallback _onMoveCompleted;
        private TweenCallback _onHighlightRotationCompleted;
        private Vector3 _moveTarget;

        private Vector3    _visualOriginLocalPos;
        private Quaternion _visualOriginLocalRot;
        private const float _highlightHeight = 1f;
        private const float _highlightRotateSpeed = 360f;
        private const float _defaultZ = 0.1f;
        #endregion

        #region Properties
        public Pos BoardPos => _boardPos;
        #endregion

        #region Unity Lifecycle
        void Awake()
        {
            _boxCollider  = GetComponent<BoxCollider>();
            _meshFilter   = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _materials = _meshRenderer.sharedMaterials;
            _onMoveCompleted = HandleMoveCompleted;
            _onHighlightRotationCompleted = HandleHighlightRotationCompleted;

            _visualOriginLocalPos = _visual.localPosition;
            _visualOriginLocalRot = _visual.localRotation;
        }

        private void OnDisable()
        {
            _moveTween?.Kill();
            _moveTween = null;
            StopHighlightTweens();
            _visual.localPosition = _visualOriginLocalPos;
            _visual.localRotation = _visualOriginLocalRot;
        }

        #endregion

        #region Public Methods
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
            _moveTarget = toPos;

            _moveTween = transform
                .DOMove(toPos, _moveDuration)
                .SetEase(Ease.Linear)
                .OnComplete(_onMoveCompleted);
        }
        public void MoveTo(Pos toPos)
        {
            _boardPos = toPos;
            MoveTo(new Vector3(toPos.X, _defaultZ, toPos.Z));
        }

        public void SetSelectable(bool selectable)
            => _boxCollider.enabled = selectable;
        public void SelectPiece()
        {
            MoveToHighlightPosition();
            SwapMaterial();
            StartHighlightRotation();
        }
        public void UnSelectPiece()
        {
            StopHighlightTweens();
            MoveToOriginPosition();
            RestoreHighlightRotation();
            SwapMaterial();
        }


        public void ShowHighlightPose()
        {
            MoveToHighlightPosition();
            StartHighlightRotation();
        }

        public void ShowMovementPose()
        {
            StopHighlightTweens();
            MoveToOriginPosition();
            RestoreHighlightRotation();
        }


        #endregion

        #region Private Methods

        private void HandleMoveCompleted()
        {
            transform.position = _moveTarget;
            _moveTween = null;
        }

        private void HandleHighlightRotationCompleted()
            => _highlightRotateTween = null;

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
            if (_materials.Length < 2)
                return;

            (_materials[0], _materials[1]) = (_materials[1], _materials[0]);
            _meshRenderer.sharedMaterials = _materials;
        }
        private void MoveToHighlightPosition()
        {
            _highlightMoveTween?.Kill();

            _highlightMoveTween = _visual
                .DOLocalMoveY(
                    _visualOriginLocalPos.y + _highlightHeight,
                    _moveDuration)
                .SetEase(Ease.OutQuad);
        }

        private void MoveToOriginPosition()
        {
            _highlightMoveTween?.Kill();

            _highlightMoveTween = _visual
                .DOLocalMoveY(
                    _visualOriginLocalPos.y,
                    _moveDuration)
                .SetEase(Ease.OutQuad);
        }

        private void StartHighlightRotation()
        {
            _highlightRotateTween?.Kill();
            _highlightRotateTween = null;

            if (Mathf.Abs(_highlightRotateSpeed) < 0.001f)
                return;

            _highlightRotateTween = _visual
                .DOLocalRotate(
                    new Vector3(Mathf.Sign(_highlightRotateSpeed) * 360f, 0f, 0f),
                    360f / Mathf.Abs(_highlightRotateSpeed),
                    RotateMode.LocalAxisAdd)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental);
        }

        private void StopHighlightTweens()
        {
            _highlightMoveTween?.Kill();
            _highlightMoveTween = null;
            _highlightRotateTween?.Kill();
            _highlightRotateTween = null;
        }

        private void RestoreHighlightRotation()
        {
            _highlightRotateTween = _visual
                .DOLocalRotateQuaternion(_visualOriginLocalRot, _moveDuration)
                .SetEase(Ease.InOutSine)
                .OnComplete(_onHighlightRotationCompleted);
        }
        #endregion
    }

}


