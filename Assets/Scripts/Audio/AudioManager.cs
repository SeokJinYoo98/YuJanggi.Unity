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

    public class AudioManager : MonoBehaviour
    {
        private static class MixerParam
        {
            public const string Master      = "MasterVolume";
            public const string SFX         = "SFXVolume";
            public const string UI          = "UIVolume";
            public const float  MaxBoostDb  = 6f;
        }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer  _audioMixer;

        [Header("Audio Player")]
        [SerializeField] private AudioPlayer _sfxPlayer;
        [SerializeField] private AudioPlayer _uiPlayer;

        public float MasterVolume   { get; private set; } = 1f;
        public float SfxVolume      { get; private set; } = 1f;
        public float UIVolume       { get; private set; } = 1f;

        public void Initialize(
               float masterVolume,
               float sfxVolume,
               float uiVolume)
        {
            SetMasterVolume(masterVolume);
            SetSfxVolume(sfxVolume);
            SetUIVolume(uiVolume);
        }
        public void PlaySfx(JanggiSfx type)
            => _sfxPlayer.Play((int)type);
        public void PlayUI(UISfx type)
            => _uiPlayer.Play((int)type);

        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.Master, MasterVolume);
        }
        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.SFX, SfxVolume);
        }
        public void SetUIVolume(float value)
        {
            UIVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.UI, UIVolume);
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

            return Mathf.Log10(Mathf.Clamp01(value)) * 20f + MixerParam.MaxBoostDb;
        }
    }

}


