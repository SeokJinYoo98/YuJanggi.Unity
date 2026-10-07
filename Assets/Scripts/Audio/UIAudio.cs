using UnityEngine;
using System.Collections.Generic;

namespace YuJanggi.Audio
{
    public enum UISfx
    { Button }
    public class UIAudio : MonoBehaviour
    {
        [SerializeField] private List<AudioClip> _audios;
        private AudioSource     _uiSource;


        private void Awake()
        {
            _uiSource = GetComponent<AudioSource>();
        }
        public void PlayUI(UISfx type)
            => _uiSource.PlayOneShot(_audios[(int)type], AudioOptions.UIVolume);
    }
}


