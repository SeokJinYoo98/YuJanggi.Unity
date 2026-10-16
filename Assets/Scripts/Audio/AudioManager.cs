using UnityEngine;
using UnityEngine.Audio;

namespace YuJanggi.Audio
{
    using Audio.Player;
    public enum JanggiSfx
    {
        Select,
        Move,
        Capture,
        Check,
        UnCheck,
        CheckMate,
        TurnAlert,
        Win,
        Lose
    }
    public enum UISfx
    {
        Button
    }
    public enum Bgm
    {
        Lobby
    }

    public class AudioManager : MonoBehaviour
    {
        private const float BGMMaxVolumeDb = -7f;

        private static class MixerParam
        {
            public const string Master      = "MasterVolume";
            public const string SFX         = "SFXVolume";
            public const string UI          = "UIVolume";
            public const string BGM         = "BGMVolume";
        }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer  _audioMixer;

        [Header("Audio Player")]
        [SerializeField] private AudioPlayer _sfxPlayer;
        [SerializeField] private AudioPlayer _uiPlayer;
        [SerializeField] private AudioPlayer _bgmPlayer;

        private PlayerAudioSettings _settings = new();

        public float MasterVolume => _settings.MasterVolume;
        public float SfxVolume => _settings.SfxVolume;
        public float UIVolume => _settings.UIVolume;
        public float BGMVolume => _settings.BGMVolume;
        public void Initialize(
            PlayerAudioSettings audioSettings)
        {
            _settings = audioSettings;

            SetMasterVolume(audioSettings.MasterVolume);
            SetSfxVolume(audioSettings.SfxVolume);
            SetUIVolume(audioSettings.UIVolume);
            SetBGMVolume(audioSettings.BGMVolume);
            PlayBGM(Bgm.Lobby);
        }

        public void PlaySfx(JanggiSfx type)
            => _sfxPlayer.Play((int)type);
        public void PlayUI(UISfx type)
            => _uiPlayer.Play((int)type);
        public void PlayBGM(Bgm type)
            => _bgmPlayer.PlayLoop((int)type);
        public void StopBGM()
            => _bgmPlayer.Stop();

        public void SetMasterVolume(float value)
        {
            _settings.MasterVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.Master, MasterVolume);
        }
        public void SetSfxVolume(float value)
        {
            _settings.SfxVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.SFX, SfxVolume);
        }
        public void SetUIVolume(float value)
        {
            _settings.UIVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.UI, UIVolume);
        }
        public void SetBGMVolume(float value)
        {
            _settings.BGMVolume = Mathf.Clamp01(value);
            _audioMixer.SetFloat(
                MixerParam.BGM,
                BGMVolume <= 0f ? -80f : NormalizedToDecibel(BGMVolume) + BGMMaxVolumeDb);
        }

        private void SetVolume(string parameter, float normalized)
        {
            _audioMixer.SetFloat(
                parameter,
                NormalizedToDecibel(normalized));
        }
        private static float NormalizedToDecibel(float value)
        {
            if (value <= 0f)
                return -80f;

            return Mathf.Log10(Mathf.Clamp01(value)) * 20f;
        }
    }

}


