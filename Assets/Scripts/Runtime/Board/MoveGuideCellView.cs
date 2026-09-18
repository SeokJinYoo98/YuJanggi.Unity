using UnityEngine;
using YuJanggi.Runtime.Input;

namespace YuJanggi.Runtime.Board
{
    using Core.Domain;


    public class MoveGuideCellView : MonoBehaviour, IBoardClickable
    {
        Renderer    _renderer;
        BoxCollider _collider;
        [SerializeField] private Material _legalMat;
        [SerializeField] private Material _illegalMat;

        private bool _isLegalState;

        public Pos _boardPos;
        public Pos BoardPos => _boardPos;

        private void Awake()
        {
            _collider = GetComponent<BoxCollider>();
            _renderer = GetComponent<Renderer>();
            _renderer.sharedMaterial = _legalMat;
            _isLegalState = true;
        }
        public void Show(bool isLegal)
        {
            if (isLegal != _isLegalState)
            {
                _renderer.sharedMaterial = isLegal ? _legalMat : _illegalMat;
                _isLegalState = isLegal;
            }
            
            _renderer.enabled = true;
            _collider.enabled = true;
        }

        public void Hide()
        {
            _renderer.enabled = false;
            _collider.enabled = false;
        }
        public void MoveTo(Pos boardPos)
        {
            _boardPos = boardPos;
            transform.position = new Vector3(boardPos.X, transform.position.y, boardPos.Z);
        }
    }
}
