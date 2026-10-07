using UnityEngine;
using System.Collections.Generic;

namespace YuJanggi.Audio
{
    public enum JanggiSfx
    { Select, Move, Capture, Check, UnCheck, CheckMate, TurnAlert, Win, Lose }
    public class SfxAudios : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> _audios;
        private AudioSource _audio;
        public float SfxVolume = 1f;
        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
        }
        public void PlaySfx(JanggiSfx type, float volume = 1.0f)
            => _audio.PlayOneShot(_audios[(int)type], volume);
    }
}


