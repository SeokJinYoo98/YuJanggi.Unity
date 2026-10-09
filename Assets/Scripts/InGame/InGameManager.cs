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

        #endregion


        private void Awake()
        {
            _mode = CreateGameMode(JanggiOptionStore.JanggiSetting.GameMode);
        }
        private void OnEnable()
        {
            _mode?.BindEvents();
        }
        private void OnDisable()
        {
            _mode?.UnBindEvents();
        }
        private void Start()
        {
            _mode.Initialize();
            _mode.StartGame();
        }
        private void OnDestroy()
        {

        }
        private void Update()
            => _mode?.Tick(Time.deltaTime);

        private IGameMode CreateGameMode(
            GameModeType type)
            => type switch
            {
                GameModeType.Local
                    => new LocalMode(
                        _inGameView,
                        Instantiate(
                            _inputs.PointerInput,
                            transform)),
                _
                    => throw new NotSupportedException(
                        $"아직 지원하지 않는 GameMode입니다: {type}")
            };
        private IInputHandler CreateInputHandler(
            GameInputType type)
        {
            return null;
        }
        

        public void HandleTakebackButton()  => _mode.HandleTakebackButton();
        public void HandlePassTurnButton()  => _mode.HandlePassTurnButton();
        public void HandleGiveUpButton()    => _mode.HandleGiveUpButton();
        public void HandlePreviousButton()  => _mode.HandlePreviousButton();
        public void HandleNextButton()      => _mode.HandleNextButton();
        public void HandleRematchButton()   => _mode.HandleRematchButton();
        public void HandleReplayButton()    => _mode.HandleReplayButton();
    }
}


