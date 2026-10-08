using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuJanggi.Lobby
{
    using BootStrap;
    using UI;
    using Audio;
    using Panel;
    

    public class LobbyPanelManager : MonoBehaviour
    {
        private enum PanelType { Local, AI, Network, Option, Quit }

        [Header("Lobby Panels")]
        [SerializeField] private LocalPanel     _localPanel;
        [SerializeField] private AIPanel        _aiPanel;
        [SerializeField] private NetworkPanel   _networkPanel;
        [SerializeField] private OptionPanel    _optionPanel;
        [SerializeField] private QuitPanel      _quitPanel;

        private AudioManager _audio = null;

        private IPanel _currPanel = null;
        private Dictionary<PanelType, IPanel> _uis;

        private void Start()
        {
            _uis = new()
            {
                { PanelType.Local,   _localPanel },
                { PanelType.AI,      _aiPanel },
                { PanelType.Network, _networkPanel },
                { PanelType.Option,  _optionPanel },
                { PanelType.Quit,    _quitPanel }
            };

            _audio = YuJanggiBootStrap.Instance.AudioManager;
        }
 
        public void HandleOpenPanel(int type)
        {
            _audio.PlayUI(UISfx.Button);
            if (!Enum.IsDefined(typeof(PanelType), type))
            {
                Debug.LogWarning($"Lobby: 잘못된 패널 타입입니다. ({type})");
                return;
            }
            ChangePanelView((PanelType)type);
        }
        public void HandleClosePanel()
        {
            _audio.PlayUI(UISfx.Button);
            ClosePanel();
        }
        public void HandleQuitGame()
        {
            _audio.PlayUI(UISfx.Button);
            Application.Quit();
        }
        public void HandleStartGame()
            => StartGameAsync().Forget();

        #region Private Methods
        // 클래스 내부에서 사용하는 보조 로직
        private async UniTask StartGameAsync()
        {
            _audio.PlayUI(UISfx.Button);
            if (_currPanel is not IGameStartPanel gameStartPanel)
                return;

            if (!await gameStartPanel.PrepareGameAsync())
                return;

            ClosePanel();
            SceneManager.LoadScene("JanggiScene");
        }
        private void ClosePanel()
        {
            // CTS 정리 및 정리할거 정리시키기
            _currPanel.Close();
            _currPanel = null;
        }
        private void ChangePanelView(PanelType type)
        {
            if (_currPanel != null)
                ClosePanel();

            if (!_uis.TryGetValue(type, out var panel))
                return;

            _currPanel = panel;
            _currPanel.Open();
        }
        #endregion
        ////////////////////////////////////////////////////////////

    }
}
