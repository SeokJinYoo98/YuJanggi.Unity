#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuJanggi.BootStrap
{
    using Audio;
    using Network;
    using YuJanggi.InGame.Input;

    public sealed class YuJanggiBootStrap : MonoBehaviour
    {
        public static YuJanggiBootStrap Instance { get; private set; } = null!;

        [Header("Managers")]
        [field: SerializeField]
        public NetworkManager NetworkManager { get; private set; } = null!;
        [field: SerializeField]
        public AudioManager AudioManager { get; private set; } = null!;
        [field: SerializeField]
        public HistroyManager HistoryManger { get; private set; } = null!;

        private const string _host = "127.0.0.1";
        private const int    _port = 7777;

        private PlayerSettings _playerSettings = new();
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

            LoadPlayerSettings();

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


            //return ("127.0.0.1", port);
            return (host, port);
        }
        private async UniTask InitializeAsync()
        {
            var data = GetServerEndpoint();

            NetworkManager.Initialize(
                data.host,
                data.port);


            AudioManager.Initialize(_playerSettings.Audio);

            await UniTask.CompletedTask;
        }

        private void OnDestroy()
        {
            if (Instance != this)
                return;

            Instance = null!;
        }

        private string SettingsPath => System.IO.Path.Combine(
            Application.persistentDataPath,
            "PlayerSettings.json");

        private void LoadPlayerSettings()
        {
            _playerSettings = System.IO.File.Exists(SettingsPath)
                ? JsonUtility.FromJson<PlayerSettings>(
                    System.IO.File.ReadAllText(SettingsPath))
                : new PlayerSettings();
        }

        public void SavePlayerSettings()
        {
            var json = JsonUtility.ToJson(_playerSettings, prettyPrint: true);
            System.IO.File.WriteAllText(SettingsPath, json);
        }

        private void OnApplicationQuit()
        {
            if (Instance != this)
                return;

            SavePlayerSettings();
        }
    }

}


