using UnityEngine;

namespace YuJanggi.UI
{
    /// <summary>전체 화면을 기준으로 한 부모 안에서 조작 UI의 안전 영역을 맞춥니다.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;
        private bool _hasApplied;

        private void Awake()
            => _rectTransform = GetComponent<RectTransform>();

        private void OnEnable()
        {
            _hasApplied = false;
            ApplySafeArea();
        }

        private void Update()
            => ApplySafeArea();

        private void ApplySafeArea()
        {
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (screenSize.x <= 0 || screenSize.y <= 0)
                return;

            var safeArea = Screen.safeArea;
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                return;

            if (_hasApplied
                && _lastScreenSize == screenSize
                && _lastSafeArea == safeArea)
                return;

            _rectTransform.anchorMin = new Vector2(
                safeArea.xMin / screenSize.x,
                safeArea.yMin / screenSize.y);
            _rectTransform.anchorMax = new Vector2(
                safeArea.xMax / screenSize.x,
                safeArea.yMax / screenSize.y);
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _lastScreenSize = screenSize;
            _lastSafeArea = safeArea;
            _hasApplied = true;
        }
    }
}
