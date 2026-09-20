using UnityEngine;
namespace YuJanggi.BootStrap
{
    using Audio;

    public class AudioManager : MonoBehaviour
    {
        [SerializeField]
        private SfxAudios   _sfx = null!;
        [SerializeField]
        private UIAudio     _ui = null!;

        private float _sfxVolume;
        private float _uiVolume;
        public void Initialize(
            float sfxVolume,
            float uiVolume)
        {
            _sfxVolume = Mathf.Clamp01(sfxVolume);
            _uiVolume  = Mathf.Clamp01(uiVolume);
        }
        public void PlaySfxOneShot(JanggiSfx type)
            => _sfx.PlaySfx(type, _sfxVolume);
        public void PlayUI(UISfx type)
            => _ui.PlayUI(type, _uiVolume);
        public void PlayButton()
            => PlayUI(UISfx.Button);
    }

}
