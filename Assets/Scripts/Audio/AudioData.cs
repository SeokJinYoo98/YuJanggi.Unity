

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace YuJanggi.Audio.Data
{
    [CreateAssetMenu(
        fileName = "Audio Data",
        menuName = "YuJanggi/Audio/Audio Data")]
    public class AudioData : ScriptableObject
    {
        [SerializeField] private AudioMixerGroup _mixerGroup;
        [SerializeField] private List<AudioClip> _clips;

        public AudioMixerGroup MixerGroup
            => _mixerGroup;
        public AudioClip GetClip(int index)
            => _clips[index];
    }
}
