using UnityEngine;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ParticleSystemWarmStart : MonoBehaviour // 파티클 VFX를 약간 진행된 상태로 시작
    {
        [SerializeField, Min(0f)] private float warmStartSeconds = 0.3f;
        [SerializeField] private bool playAfterWarmStart = true;

        private ParticleSystem rootParticle;
        private ParticleSystem[] childParticles;

        private void Awake()
        {
            CacheParticles();
        }

        private void OnEnable()
        {
            ApplyWarmStart();
        }

        public void Configure(float seconds, bool playAfterWarmStart)
        {
            warmStartSeconds = Mathf.Max(0f, seconds);
            this.playAfterWarmStart = playAfterWarmStart;
        }

        private void ApplyWarmStart()
        {
            if (warmStartSeconds <= 0f)
            {
                return;
            }

            LoadedProjectileRegrowVisual regrowVisual = GetComponent<LoadedProjectileRegrowVisual>();
            if (regrowVisual != null)
            {
                regrowVisual.ApplyParticleIntensity();
            }

            if (rootParticle == null && childParticles == null)
            {
                CacheParticles();
            }

            if (rootParticle != null)
            {
                WarmParticle(rootParticle, true);
                return;
            }

            if (childParticles == null)
            {
                return;
            }

            for (int i = 0; i < childParticles.Length; i++)
            {
                ParticleSystem particle = childParticles[i];
                if (particle != null)
                {
                    WarmParticle(particle, false);
                }
            }
        }

        private void CacheParticles()
        {
            rootParticle = GetComponent<ParticleSystem>();
            childParticles = rootParticle == null ? GetComponentsInChildren<ParticleSystem>(true) : null;
        }

        private void WarmParticle(ParticleSystem particle, bool withChildren)
        {
            particle.Stop(withChildren, ParticleSystemStopBehavior.StopEmittingAndClear);
            particle.Simulate(warmStartSeconds, withChildren, true, false);
            if (playAfterWarmStart)
            {
                particle.Play(withChildren);
            }
        }
    }
}
