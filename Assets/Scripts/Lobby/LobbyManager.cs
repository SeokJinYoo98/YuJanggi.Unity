using System;
using UnityEngine;
using UnityEngine.SceneManagement;

using YuJanggi.UI;

namespace YuJanggi.Lobby
{
    using BootStrap;
    using Data.AI;

    using Flow;
    using Store;
    using UI;
    public class LobbyManager : MonoBehaviour
    {
        private enum PanelType
        {
            Local = 0,
            AI = 1,
            Network = 2,
            Option = 3,
            Quit = 4
        }

        [SerializeField] private NetworkPanel _networkPanel;
        [SerializeField] private AIPanel _aiPanel;
        [SerializeField] private LocalPanel _localPanel;
        private AudioManager _audioManager;

        private UIVisible _curr;
        private Action _requestStart;

        private ILobbyFlow[] _flows;
        private NetworkLobbyFlow _networkFlow;
        private bool _eventsBound;
        private bool _isEnteringGame;

        private void Awake()
        {
            var bootstrap = YuJanggiBootStrap.Instance;

            _audioManager = bootstrap.AudioManager;
            var networkManager = bootstrap.NetworkManager;

            _networkFlow = new NetworkLobbyFlow(
                _networkPanel, networkManager.Lobby, networkManager);

            _flows = new ILobbyFlow[]
            {
                new LocalLobbyFlow(_localPanel, () => _networkFlow.CanStartOtherMode),
                new AILobbyFlow(_aiPanel, () => _networkFlow.CanStartOtherMode),
                _networkFlow
            };
        }

        private void OnEnable()
        {
            if (_eventsBound) return;
            _eventsBound = true;

            foreach (var flow in _flows)
                flow.GameStartReady += HandleGameStartReady;

            _networkFlow.PanelRequested += HandleNetworkPanelRequested;

            foreach (var flow in _flows)
                flow.BindEvents();
        }

        private void Update()
            => _networkFlow?.Tick();

        private void OnDisable()
            => UnBindFlows();
        private void OnDestroy()
            => UnBindFlows();

        private void UnBindFlows()
        {
            if (!_eventsBound) return;

            _eventsBound = false;

            _networkFlow.PanelRequested -= HandleNetworkPanelRequested;

            foreach (var flow in _flows)
            {
                flow.GameStartReady -= HandleGameStartReady;
                flow.UnBindEvents();
            }
        }

        private void HandleGameStartReady(LobbyGameStartContext context)
        {
            if (!_eventsBound || _isEnteringGame) return;
            _isEnteringGame = true;
            JanggiOptionStore.SetOptions(context.Options);
            NetworkMatchInfoStore.Current = context.NetworkMatch;
            if (context.AIStrategy.HasValue)
                AISessionSettings.Strategy = context.AIStrategy.Value;
            _audioManager.PlayButton();
            _curr = null;
            _requestStart = null;
            SceneManager.LoadScene("JanggiScene");
        }

        private void HandleNetworkPanelRequested(bool show)
        {
            if (_isEnteringGame) return;
            if (show)
                ChangePanel(_networkPanel, _networkPanel.RequestStart);
            else if (_curr == _networkPanel)
                ChangePanel();
        }

        private void ChangePanel(UIVisible next = null, Action requestStart = null)
        {
            if (_curr == next) return;
            _curr?.Hide();
            _curr = next;
            _requestStart = requestStart;
            _curr?.Show();
        }



        public void HandleGameStart()
        {
            if (!_isEnteringGame)
                _requestStart?.Invoke();
        }

        public void HandleClosePanel()
        {
            _audioManager.PlayButton();
            ChangePanel();
        }

        public void HandleMatchMakingButton()
        {
            _audioManager.PlayButton();
            _networkPanel.RequestMatching();
        }

        public void HandleCloseNetworkPanel()
        {
            _audioManager.PlayButton();
            _networkPanel.RequestClose();
        }

        public void HandleQuitGame()
        {
            _audioManager.PlayButton();
            Application.Quit();
        }
    }
}



