using UnityEngine;
using UnityEngine.SceneManagement;
namespace YuJanggi.InGame
{
    using Controller.Input;
    using Views;
    using Mode;

    public class InGameManager : MonoBehaviour
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputHandlerBehaviour _localInput;
        [SerializeField] private InGameView            _inGameView;
        [SerializeField] private RecordInputHandler    _recordInput;
        #endregion

        private LocalMode _mode;
        private bool _useRecordInput;
        private bool _started;
        private void Awake()
        {
            _useRecordInput = _recordInput != null && _recordInput.isActiveAndEnabled;
            _mode = new LocalMode(_inGameView, _localInput,
                _useRecordInput ? _recordInput.CreateOptions() : null);
        }
        private void OnEnable()
        {
            _mode?.BindEvents();
            if (_useRecordInput && _started)
            {
                _localInput.Deactivate();
                _recordInput.StartPlayback();
            }
        }
        private void OnDisable()
        {
            if (_useRecordInput)
                _recordInput.StopPlayback();
            _mode?.UnBindEvents();
        }
        private void Start()
        {
            _mode.Initialize();
            if (_useRecordInput)
            {
                _localInput.Deactivate();
                if (!_mode.InitializeRecordInput(_recordInput))
                    return;
            }
            _mode.StartGame();
            _started = true;
            if (_useRecordInput)
                _recordInput.StartPlayback();
        }
        private void OnDestroy()
        {
            if (_useRecordInput)
                _recordInput.Release();
        }
        private void Update()
            => _mode?.Tick(Time.deltaTime);



    }
}


