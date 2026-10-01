using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;

namespace YuJanggi.InGame.Views.Particle
{
    public class ParticleManager : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private ParticleView _capturedParticlePrefab;
        [SerializeField] private ParticleView _moveParticlePrefab;


        [Header("Pool Settings")]
        [SerializeField] private int _moveDefaultCapacity   = 8;
        [SerializeField] private int _moveMaxSize           = 16;

        [SerializeField] private int _captureDefaultCapacity = 4;
        [SerializeField] private int _captureMaxSize         = 8;

        private ObjectPool<ParticleView> _movePool;
        private ObjectPool<ParticleView> _capturePool;

        private const float MovementDuration = 0.16f;
        private const Ease  MovementEase     = Ease.Linear;

        private void Awake()
        {
            _movePool = CreatePool(
                _moveParticlePrefab,
                _moveDefaultCapacity,
                _moveMaxSize
            );

            _capturePool = CreatePool(
                _capturedParticlePrefab,
                _captureDefaultCapacity,
                _captureMaxSize
            );
            Prewarm(_capturePool, _captureDefaultCapacity);
        }

        public void PlayMove(Vector3 worldPosition)
            => _movePool.Get().Play(worldPosition);

        public void PlayMovementParticle(Vector3 from, Vector3 to)
        {
            _movePool.Get().PlayPath(from, to, MovementDuration, MovementEase);
        }

        public void PlayCapture(Vector3 worldPosition)
            => _capturePool.Get().Play(worldPosition);

        private ObjectPool<ParticleView> CreatePool(
            ParticleView prefab,
            int defaultCapacity,
            int maxSize)
        {
            ObjectPool<ParticleView> pool = null;

            pool = new ObjectPool<ParticleView>(
                createFunc: () =>
                {
                    ParticleView particle = Instantiate(prefab, transform);
                    particle.Initialize(pool.Release);
                    particle.gameObject.SetActive(false);
                    return particle;
                },
                actionOnGet: particle =>
                {
                    particle.gameObject.SetActive(true);
                },
                actionOnRelease: particle =>
                {
                    particle.gameObject.SetActive(false);
                },
                actionOnDestroy: particle =>
                {
                    Destroy(particle.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );

            return pool;
        }
        private void Prewarm(ObjectPool<ParticleView> pool, int count)
        {
            List<ParticleView> particles = new List<ParticleView>(count);

            for (int i = 0; i < count; i++)
            {
                particles.Add(pool.Get());
            }

            for (int i = 0; i < particles.Count; i++)
            {
                pool.Release(particles[i]);
            }
        }
    }
}



