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

        public void Initialize(
            float sfxVolume,
            float uiVolume)
        {
            AudioOptions.SfxVolume = sfxVolume;
            AudioOptions.UIVolume = uiVolume;
        }
        public void PlaySfxOneShot(JanggiSfx type)
            => _sfx.PlaySfx(type);
        public void PlayUI(UISfx type)
            => _ui.PlayUI(type);
        public void PlayButton()
            => PlayUI(UISfx.Button);
    }

}


