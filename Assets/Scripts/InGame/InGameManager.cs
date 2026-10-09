using UnityEngine;
namespace YuJanggi.InGame
{
    using Input;
    using Mode;
    using Views;
    using YuJanggi.Engine.Domain;

    public class InGameManager : MonoBehaviour
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;
        [SerializeField] private Camera       _inputCamera;

        [Header("View")]
        [SerializeField] private InGameView   _inGameView;

        private LocalMode _mode;

        private InputHandler _activeInput;
        private bool _started;
        #endregion


        private void Awake()
        {
            if (!_activeInput.Initialize(_inputCamera))
            {
                enabled = false;
                return;
            }
            _mode = new LocalMode(_inGameView, _activeInput);
        }
        private void OnEnable()
        {
            _mode?.BindEvents();
            if (_started)
                _activeInput.Activate();
        }
        private void OnDisable()
        {
            _activeInput?.Deactivate();
            _mode?.UnBindEvents();
        }
        private void Start()
        {
            _mode.Initialize();
            _mode.BindInput(_activeInput);
            _mode.StartGame();
            _started = true;
            _activeInput.Activate();
        }
        private void OnDestroy()
        {
            if (_activeInput == null)
                return;
            _activeInput.Release();
            Destroy(_activeInput.gameObject);
        }
        private void Update()
            => _mode?.Tick(Time.deltaTime);
    }
}


