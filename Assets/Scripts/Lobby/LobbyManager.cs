using UnityEngine;
using UnityEngine.SceneManagement;


namespace YuJanggi.Lobby
{
    using BootStrap;
    using Data.AI;

    using Flow;
    using Store;
    using UI;
    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private LobbyPanelManager _panelManager;
        [SerializeField] private NetworkPanel _networkPanel;
        [SerializeField] private AIPanel _aiPanel;
        [SerializeField] private LocalPanel _localPanel;
        private AudioManager _audioManager;

        private ILobbyFlow[] _flows;
        private NetworkLobbyFlow _networkFlow;
        private bool _eventsBound;
        private bool _isEnteringGame;

        private void Awake()
        {
            var bootstrap = YuJanggiBootStrap.Instance;

            _audioManager = bootstrap.AudioManager;
            _panelManager.Initialize(_audioManager, HandleQuitGame);
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
            _audioManager.PlayUI(UISfx.Button);
            _panelManager.ClearSelection();
            SceneManager.LoadScene("JanggiScene");
        }

        private void HandleNetworkPanelRequested(bool show)
        {
            if (_isEnteringGame) return;
            _panelManager.SetNetworkPanelVisible(show);
        }



        public void HandleGameStart()
        {
            if (!_isEnteringGame)
                _panelManager.RequestStart();
        }

        public void HandleMatchMakingButton()
        {
            _audioManager.PlayUI(UISfx.Button);
            _networkPanel.RequestMatching();
        }

        public void HandleCloseNetworkPanel()
        {
            _audioManager.PlayUI(UISfx.Button);
            _networkPanel.RequestClose();
        }

        public void HandleQuitGame()
        {
            _audioManager.PlayUI(UISfx.Button);
            Application.Quit();
        }
    }
}



