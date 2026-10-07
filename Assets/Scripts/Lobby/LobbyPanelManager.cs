
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
        private void Start()
        {
            _audio = YuJanggiBootStrap.Instance.AudioManager;
        }
        public void HandleOpenPanel(int type)
        {
            if (!Enum.IsDefined(typeof(LobbyPanelType), type))
            {
                Debug.LogWarning($"Lobby: 잘못된 패널 타입입니다. ({type})");
                return;
            }
            _audio.PlayButton();
            ChangePanelView((LobbyPanelType)type);
        }
        public void HandleClosePanel()
        {

        }

        private void ChangePanelView(LobbyPanelType type)
        {
            if (_currPanel != null)
                HandleClosePanel();


        }
    }
}
