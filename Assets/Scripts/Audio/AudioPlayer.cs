

using UnityEngine;
using YuJanggi.Audio.Data;

namespace YuJanggi.Audio.Player
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioData _data;
        private AudioSource _source;
        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.outputAudioMixerGroup = _data.MixerGroup;
        }

        public void Play(int index)
            => _source.PlayOneShot(_data.GetClip(index));
    }
}
