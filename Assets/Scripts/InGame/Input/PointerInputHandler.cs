using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YuJanggi.Core.InGame;
namespace YuJanggi.InGame.Input
{
    using Engine.Domain;

    public class PointerInputHandler : InputHandler
    {
        [SerializeField] private Camera     _camera;
        [SerializeField] private LayerMask  _clickableLayer;
        private bool _isActivate;

        private PlayerInputs _input;
        private PlayerInputs.PlayerActions _actions;
        private void OnPointerPressPerformed(InputAction.CallbackContext context)
        {
            if (!_isActivate) 
                return;

            if (!TryRaycastToBoard(out var pos))
            {
                RaiseEmptyClicked();
                return;
            }    
      
            RaiseBoardClicked(pos);
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
            Deactivate();
            _actions.PointerPress.performed -= OnPointerPressPerformed;
            _actions.PointerPosition.Disable();
            _actions.PointerPress.Disable();
        }

        public override bool Initialize(Camera inputCamera = null)
        {
            if (inputCamera != null)
                _camera = inputCamera;
            if (_camera != null)
                return true;

            Debug.LogError("[PointerInputHandler] 입력에 사용할 Camera가 지정되지 않았습니다.", this);
            return false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _input?.Dispose();
        }

        public override void RotateCamera(PlayerTeam team)
        {
            if (team == PlayerTeam.Han)
            {
                _camera.transform.position = new Vector3(4, 9, 6);
                _camera.transform.eulerAngles = new Vector3(90, 0, 180);
                _camera.fieldOfView =
                    Camera.HorizontalToVerticalFieldOfView(
                        59f,
                        _camera.aspect);
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


