

using System;
using DG.Tweening;
using UnityEngine;

namespace YuJanggi.Runtime.Particle
{
    public sealed class PooledParticle : MonoBehaviour
    {
        [SerializeField] private ParticleSystem _particleSystem;

        private Action<PooledParticle> _onStopped;
        private Tween _moveTween;

        public void Initialize(Action<PooledParticle> onStopped)
        {
            _onStopped = onStopped;

            if (_particleSystem == null)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }

            if (_particleSystem == null)
            {
                Debug.LogError($"{nameof(PooledParticle)} requires ParticleSystem.");
                return;
            }

            var main            = _particleSystem.main;
            main.playOnAwake    = false;
            main.loop           = false;
            main.stopAction     = ParticleSystemStopAction.Callback;
        }
        public void Play(Vector3 worldPosition)
        {
            CancelMoveTween();

            transform.position = worldPosition;
            gameObject.SetActive(true);

            var main = _particleSystem.main;
            main.loop = false;
            _particleSystem.Clear(true);
            _particleSystem.Play(true);
        }
        public void PlayPath(Vector3 from, Vector3 to, float duration, Ease ease)
        {
            CancelMoveTween();

            transform.position = from;
            gameObject.SetActive(true);

            var main = _particleSystem.main;
            main.loop = true;

            _particleSystem.Clear(true);
            _particleSystem.Play(true);

            _moveTween = transform
                .DOMove(to, duration)
                .SetEase(ease)
                .OnComplete(() =>
                {
                    StopEmitting();
                    _moveTween = null;
                });
        }

        private void OnParticleSystemStopped()
        {
            CancelMoveTween();
            _particleSystem.Clear(true);
            gameObject.SetActive(false);
            _onStopped?.Invoke(this);
        }
        private void CancelMoveTween()
        {
            _moveTween?.Kill();
            _moveTween = null;
        }
        private void StopEmitting()
        {
            var main = _particleSystem.main;
            main.loop = false;
            _particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
