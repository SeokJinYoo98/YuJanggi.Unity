
using UnityEngine;

namespace YuJanggi.Audio
{
    public static class AudioOptions
    {
        private static float _sfxVolume = 1f;
        private static float _uiVolume = 1f;

        public static float SfxVolume
        {
            get => _sfxVolume;
            set => _sfxVolume = Mathf.Clamp01(value);
        }

        public static float UIVolume
        {
            get => _uiVolume;
            set => _uiVolume = Mathf.Clamp01(value);
        }
    }
}
