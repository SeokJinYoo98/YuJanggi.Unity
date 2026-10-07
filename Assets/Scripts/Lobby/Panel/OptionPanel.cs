using UnityEngine;


namespace YuJanggi.Lobby.UI
{
    using Audio;
    using YuJanggi.BootStrap;
    using YuJanggi.UI;
    using YuJanggi.UI.Volume;

    public class OptionPanel : UIVisible
    {
        [Header("Sliders")]
        [SerializeField] private VolumeSlider _masterSlider;
        [SerializeField] private VolumeSlider _uiSlider;
        [SerializeField] private VolumeSlider _sfxSlider;

        private AudioManager AudioManager
            => YuJanggiBootStrap.Instance.AudioManager;

        protected override void OnOpen()
        {
            var audio = AudioManager;

            _masterSlider.SetValue(audio.MasterVolume);
            _uiSlider.SetValue(audio.UIVolume);
            _sfxSlider.SetValue(audio.SfxVolume);
        }

        protected override void OnClose()
        {
            var audio = AudioManager;

            audio.SetMasterVolume(_masterSlider.NormalizedValue);
            audio.SetSfxVolume(_sfxSlider.NormalizedValue);
            audio.SetUIVolume(_uiSlider.NormalizedValue);
        }
    }
}
