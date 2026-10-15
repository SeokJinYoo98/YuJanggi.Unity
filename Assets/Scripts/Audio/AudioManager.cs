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
        // 볼륨 변환식(20 * log10(value))에서 -7 dB에 해당합니다.
        private const float DefaultBGMVolume = 0.4466836f;

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

        public float MasterVolume   { get; private set; } = 1f;
        public float SfxVolume      { get; private set; } = 1f;
        public float UIVolume       { get; private set; } = 1f;
        public float BGMVolume      { get; private set; } = DefaultBGMVolume;
        public void Initialize(
               float masterVolume,
               float sfxVolume,
               float uiVolume,
               float bgmVolume = DefaultBGMVolume)
        {
            SetMasterVolume(masterVolume);
            SetSfxVolume(sfxVolume);
            SetUIVolume(uiVolume);
            SetBGMVolume(bgmVolume);
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
        public void SetBGMVolume(float value)
        {
            BGMVolume = Mathf.Clamp01(value);
            SetVolume(MixerParam.BGM, BGMVolume);
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


