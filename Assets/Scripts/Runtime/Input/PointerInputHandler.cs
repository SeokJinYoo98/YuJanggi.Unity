using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YuJanggi.Runtime.Input
{
    using Core.Domain;

    public class PointerInputHandler : InputHandlerBehaviour
    {
        [SerializeField] private Camera     _camera;
        [SerializeField] private LayerMask  _clickableLayer;
        private bool _isActivate = true;
        public override event Action<Pos> OnBoardClicked;
        public override event Action      OnEmptyClicked;

        private PlayerInputs _input;
        private PlayerInputs.PlayerActions _actions;
        private void OnPointerPressPerformed(InputAction.CallbackContext context)
        {
            if (!_isActivate) 
                return;

            if (!TryRaycastToBoard(out var pos))
            {
                OnEmptyClicked?.Invoke();
                return;
            }    
      
            OnBoardClicked?.Invoke((pos));
        }

        void Awake()
        {
            _input = new PlayerInputs();
            _actions = _input.Player;
        }    
        private void OnEnable()
        {
            _actions.PointerPress.Enable();
            _actions.PointerPosition.Enable();
            _actions.PointerPress.performed += OnPointerPressPerformed;

        }
        private void OnDisable()
        {
            _actions.PointerPress.performed -= OnPointerPressPerformed;
            _actions.PointerPosition.Disable();
            _actions.PointerPress.Disable();
        }

        public override void RotateCamera(PlayerTeam team)
        {
            if (team == PlayerTeam.Han)
            {
                _camera.transform.position = new Vector3(4, 9, 6);
                _camera.transform.eulerAngles = new Vector3(90, 0, 180);
            }
            else
            {
                _camera.transform.position = new Vector3(4, 9, 3);
                _camera.transform.eulerAngles = new Vector3(90, 0, 0);
            }
        }
        public override void Activate()   => _isActivate = true;
        public override void Deactivate() => _isActivate = false;

        private bool TryRaycastToBoard(out Pos pos)
        {
            pos = default;

            if (_camera == null)
                return false;

            Vector2 pointerPosition = _actions.PointerPosition.ReadValue<Vector2>();

            Ray ray = _camera.ScreenPointToRay(pointerPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, _clickableLayer))
                return false;

            if (!hit.collider.TryGetComponent(out IBoardClickable clickable))
                return false;
            
            pos = clickable.BoardPos;

            return true;
        }
    }
}
