using UnityEngine;
namespace YuJanggi.Runtime.Audio
{
    public class AudioManager : MonoBehaviour
    {
        [SerializeField] private SfxAudios  _sfx;
        [SerializeField] private UIAudio    _ui;

        public static AudioManager Instance { get; private set; }
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
        public void OnApplicationQuit()
        {
            Instance = null;
        }
        private float _sfxVolume = 1.0f;
        private float _uiVolume  = 1.0f;
        public void PlaySfxOneShot(JanggiSfx type)
            => _sfx.PlaySfx(type, _sfxVolume);
        public void PlayUI(UISfx type)
            => _ui.PlayUI(type, _uiVolume);
        public void PlayButton()
            => PlayUI(UISfx.Button);
    }

}
