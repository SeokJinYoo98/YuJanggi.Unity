using System;
using UnityEngine;
namespace YuJanggi.InGame
{
    using Input;
    using Mode;
    using Views;
    using YuJanggi.Core.InGame;
    using YuJanggi.Engine.Domain;
    using YuJanggi.Store;

    public class InGameManager : MonoBehaviour
    {
        #region Fields
        [Header("Inputs")]
        [SerializeField] private InputPrefabs _inputs;

        [Header("View")]
        [SerializeField] private InGameView   _inGameView;

        private IGameMode     _mode;
        private IInputHandler _activeInput;
        private bool _started;
        #endregion


        private void Awake()
        {
            CreateInputHandler(JanggiOptionStore.TYPE);
            if (!_activeInput.Initialize())
            {
                enabled = false;
                return;
            }
            _mode = CreateGameMode(JanggiOptionStore.JanggiSetting.GameMode);
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
            _mode.StartGame();
            _started = true;
            _activeInput.Activate();
        }
        private void OnDestroy()
        {

        }
        private void Update()
            => _mode?.Tick(Time.deltaTime);

        private IGameMode CreateGameMode(GameModeType type)
            => type switch
            {
                GameModeType.Local => new LocalMode(_inGameView, _activeInput),
                _ => throw new NotSupportedException($"아직 지원하지 않는 GameMode입니다: {type}")
            };
        private IInputHandler CreateInputHandler(GameInputType type)
        {
            var prefab = _inputs.GetPrefab(
                type);

            _activeInput = Instantiate(
                prefab,
                transform);

            _activeInput.Deactivate();
            return _activeInput;
        }
    }
}


