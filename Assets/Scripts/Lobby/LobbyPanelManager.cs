
using System;
using UnityEngine;
using YuJanggi.BootStrap;
using YuJanggi.Lobby.UI;
using YuJanggi.UI;

namespace YuJanggi.Lobby
{

    public class LobbyPanelManager : MonoBehaviour
    {
        private enum LobbyPanelType
        {
            Local   = 0,
            AI      = 1,
            Network = 2,
            Option  = 3,
            Quit    = 4
        }
        [Header("Lobby Panels")]
        [SerializeField] private LocalPanel     _localPanel;
        [SerializeField] private AIPanel        _aiPanel;
        [SerializeField] private NetworkPanel   _networkPanel;
        [SerializeField] private OptionPanel    _optionPanel;
        [SerializeField] private QuitPanel      _quitPanel;

        private UIVisible    _currPanel = null;
        private AudioManager _audio = null;
        private Action _requestStart;
        private Action _requestQuit;

        public void Initialize(AudioManager audio, Action requestQuit)
        {
            _audio = audio;
            _requestQuit = requestQuit;
        }
        public void HandleOpenPanel(int type)
        {
            if (!Enum.IsDefined(typeof(LobbyPanelType), type))
            {
                Debug.LogWarning($"Lobby: 잘못된 패널 타입입니다. ({type})");
                return;
            }
            if ((LobbyPanelType)type == LobbyPanelType.Option) return;
            if ((LobbyPanelType)type == LobbyPanelType.Quit)
            {
                _requestQuit?.Invoke();
                return;
            }

            _audio.PlayUI(UISfx.Button);
            ChangePanelView((LobbyPanelType)type);
        }
        public void HandleClosePanel()
        {
            _audio.PlayUI(UISfx.Button);
            ChangePanel();
        }

        private void ChangePanelView(LobbyPanelType type)
        {
            switch (type)
            {
                case LobbyPanelType.Local:
                    if (_currPanel != null) return;
                    ChangePanel(_localPanel, _localPanel.RequestStart);
                    break;
                case LobbyPanelType.AI:
                    if (_currPanel != null) return;
                    ChangePanel(_aiPanel, _aiPanel.RequestStart);
                    break;
                case LobbyPanelType.Network:
                    _networkPanel.RequestConnect();
                    break;
            }
        }
        private void ChangePanel(UIVisible next)
        {

        }
        private void ChangePanel(UIVisible next = null, Action requestStart = null)
        {
            if (_currPanel == next) return;
            _currPanel?.Close();
            _currPanel = next;
            _requestStart = requestStart;
            _currPanel?.Open();
        }

        public void SetNetworkPanelVisible(bool show)
        {
            if (show)
                ChangePanel(_networkPanel, _networkPanel.RequestStart);
            else if (_currPanel == _networkPanel)
                ChangePanel();
        }

        public void RequestStart()
            => _requestStart?.Invoke();

        public void ClearSelection()
        {
            _currPanel = null;
            _requestStart = null;
        }
    }
}
