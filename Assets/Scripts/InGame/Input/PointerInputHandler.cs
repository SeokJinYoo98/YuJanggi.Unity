using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YuJanggi.Core.InGame;
namespace YuJanggi.InGame.Input
{
    using Engine.Domain;
    using YuJanggi.Store;

    public class PointerInputHandler : MonoBehaviour, ILocalInputHandler
    {
        [SerializeField] private LayerMask  _clickableLayer;
  
        private PlayerInputs _input;
        private PlayerInputs.PlayerActions _actions;

        private Camera  _camera;

        private IBoardInputReceiver _currReceiver;
        void Awake()
        {
            _input = new PlayerInputs();
            _actions = _input.Player;
        }
        void OnEnable()
        {
            _actions.PointerPress.Enable();
            _actions.PointerPosition.Enable();
            _actions.PointerPress.performed += OnPointerPressPerformed;
        }
        void OnDisable()
        {
            _actions.PointerPress.performed -= OnPointerPressPerformed;
            _actions.PointerPosition.Disable();
            _actions.PointerPress.Disable();
        }
        void OnDestroy()
        {
   
            _input?.Dispose();
        }

        private void OnPointerPressPerformed(InputAction.CallbackContext context)
        {
  
            if (_currReceiver == null)
                return;

            if (!TryRaycastToBoard(out var pos))
            {
                _currReceiver.HandleInvalidClick();
                return;
            }

            _currReceiver.HandleValidClick(pos);
        }
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
        public void  Initialize()
        {
            _camera     = Camera.main;

            if (_camera == null)
                return;

            RotateCamera();
        }
        public void  ResetPlayer()
            => _currReceiver = null;
        public void  SetPlayer(IBoardInputReceiver receiver)
            => _currReceiver = receiver;
        private void RotateCamera()
        {
            var options = JanggiOptionStore.JanggiSetting;
            if (options.GameMode == GameModeType.Local)
                return;

            var localTeam = options.PlayerCho ==
                PlayerType.Local ? PlayerTeam.Cho : PlayerTeam.Han;
            if (localTeam == PlayerTeam.Han)
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
    }
}


