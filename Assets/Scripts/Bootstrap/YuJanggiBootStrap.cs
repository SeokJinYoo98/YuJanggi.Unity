#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuJanggi.BootStrap
{
    using Audio;
    public sealed class YuJanggiBootStrap : MonoBehaviour
    {
        public static YuJanggiBootStrap Instance { get; private set; } = null!;

        [Header("Managers")]
        [field: SerializeField]
        public NetworkManager NetworkManager { get; private set; } = null!;
        [field: SerializeField]
        public AudioManager AudioManager { get; private set; } = null!;

        [Header("Audio Settings")]
        [SerializeField] private float _sfxVolume    = 1.0f;
        [SerializeField] private float _uiVolume     = 1.0f;
        [SerializeField] private float _masterVolume = 1.0f;

        private const string _host = "127.0.0.1";
        private const int    _port = 7777;


        private void Awake()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        private async void Start()
        {
            await InitializeAsync();

            await SceneManager.LoadSceneAsync("LobbyScene");
        }

        private (string host, int port) GetServerEndpoint()
        {
            var serverHost = System.Environment.GetEnvironmentVariable("SERVER_HOST");
            var host = string.IsNullOrWhiteSpace(serverHost)
                ? _host
                : serverHost.Trim();

            var serverPort = System.Environment.GetEnvironmentVariable("SERVER_PORT");
            var port = int.TryParse(serverPort, out var parsedPort)
                ? parsedPort
                : _port;

            return (host, port);
        }
        private async UniTask InitializeAsync()
        {
            var data = GetServerEndpoint();

            NetworkManager.Initialize(
                data.host,
                data.port);


            AudioManager.Initialize(
                _masterVolume,
                _sfxVolume,
                _uiVolume);

            await UniTask.CompletedTask;
        }


        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null!;
        }
    }

}


