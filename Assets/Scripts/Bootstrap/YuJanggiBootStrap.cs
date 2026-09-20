#nullable enable

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YuJanggi.BootStrap
{
    public sealed class YuJanggiBootStrap : MonoBehaviour
    {
        public static YuJanggiBootStrap Instance { get; private set; } = null!;

        [Header("Managers")]
        [field: SerializeField]
        public NetworkManager NetworkManager { get; private set; } = null!;
        [field: SerializeField]
        public AudioManager AudioManager { get; private set; } = null!;

        [Header("Network Settings")]
        [SerializeField] private string _host = "127.0.0.1";
        [SerializeField] private int _port    = 7777;

        [Header("Audio Settings")]
        [SerializeField] private float _sfxVolume = 1.0f;
        [SerializeField] private float _uiVolume  = 1.0f;

        private void Awake()
        {
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

        private async UniTask InitializeAsync()
        {
            NetworkManager.Initialize(
                _host,
                _port);

            // await AddressableManager.InitializeAsync();

            AudioManager.Initialize(
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
