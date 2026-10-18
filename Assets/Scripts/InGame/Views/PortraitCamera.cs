using UnityEngine;
using UnityEngine.Rendering;

namespace YuJanggi.InGame.Views
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PortraitCamera : MonoBehaviour
    {
        // 기존 720 x 1280 구도의 가로 표시 범위를 유지합니다.
        private const float ReferenceAspect = 9f / 16f;

        private Camera _camera;
        private float _horizontalFieldOfView;
        private float _lastAspect;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _horizontalFieldOfView = Camera.VerticalToHorizontalFieldOfView(
                _camera.fieldOfView,
                ReferenceAspect);
        }

        private void OnEnable()
        {
            _lastAspect = -1f;
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            ApplyAspect();
        }

        private void OnDisable()
            => RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _camera)
                ApplyAspect();
        }

        private void ApplyAspect()
        {
            var aspect = _camera.aspect;
            if (aspect <= 0f || Mathf.Approximately(aspect, _lastAspect))
                return;

            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(
                _horizontalFieldOfView,
                aspect);
            _lastAspect = aspect;
        }
    }
}
