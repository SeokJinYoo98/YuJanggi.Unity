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
        #endregion

        private LocalMode _mode;
        private void Awake()
            => _mode = new LocalMode(_inGameView, _localInput);
        private void OnEnable()
            => _mode?.BindEvents();
        private void OnDisable()
            => _mode?.UnBindEvents();
        private void Start()
        {
            _mode.Initialize();
            _mode.StartGame();
        }
        private void Update()
            => _mode?.Tick(Time.deltaTime);



    }
}


