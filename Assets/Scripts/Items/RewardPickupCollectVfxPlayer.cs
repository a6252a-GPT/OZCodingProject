using UnityEngine;

namespace TeamProject01.Gameplay
{
    internal static class RewardPickupCollectVfxPlayer // 보상 픽업 획득 VFX 공용 재생
    {
        private const string DefaultPickupVfxResourcePath = "RewardPickups/VFX_Pickup_Cast"; // Resources 기본 VFX
        private const float DefaultPickupVfxLifetime = 3f;
        private const float DefaultPickupVfxScale = 1f;

        private static GameObject cachedPickupVfxPrefab;
        private static bool loadAttempted;
        private static bool missingWarningLogged;

        public static void Play(Vector3 position) // 월드 위치 기준 획득 VFX
        {
            PlayInternal(position, null, Vector3.zero); // 고정 위치 재생
        }

        public static void PlayFollowing(Transform target, Vector3 localOffset) // 타겟 추적 획득 VFX
        {
            if (target == null)
            {
                return; // 추적 대상 없음
            }

            PlayInternal(target.TransformPoint(localOffset), target, localOffset); // 현재 위치에서 시작
        }

        private static void PlayInternal(Vector3 position, Transform followTarget, Vector3 localOffset)
        {
            GameObject prefab = ResolvePickupVfxPrefab();
            if (prefab == null)
            {
                LogMissingPrefabOnce();
                return;
            }

            GameObject instance = Object.Instantiate(prefab, position, Quaternion.identity); // 풀 반환 이후에도 남도록 독립 생성
            instance.name = "Reward_Pickup_Cast_VFX";
            instance.transform.localScale = Vector3.one * DefaultPickupVfxScale;
            if (followTarget != null)
            {
                RuntimeVfxFollowTarget.Attach(instance, followTarget, localOffset, Quaternion.identity); // 재생 중 Looting 추적
                RuntimeVfxParticleUtility.ConfigureParticlesForFollow(instance); // 파티클 잔상 추적
            }

            DisableRuntimeColliders(instance);
            PlayParticles(instance);
            Object.Destroy(instance, ResolveLifetime(instance, DefaultPickupVfxLifetime)); // 잔여 파티클 정리
        }

        private static GameObject ResolvePickupVfxPrefab()
        {
            if (cachedPickupVfxPrefab != null)
            {
                return cachedPickupVfxPrefab;
            }

            if (!loadAttempted)
            {
                loadAttempted = true;
                cachedPickupVfxPrefab = Resources.Load<GameObject>(DefaultPickupVfxResourcePath); // Resources fallback
            }

            return cachedPickupVfxPrefab;
        }

        private static float ResolveLifetime(GameObject root, float fallback)
        {
            if (root == null)
            {
                return fallback;
            }

            float lifetime = fallback;
            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particle.main;
                if (main.loop)
                {
                    continue; // 루프형은 기본 수명으로 정리
                }

                float particleLifetime = main.duration + main.startDelay.constantMax + main.startLifetime.constantMax;
                lifetime = Mathf.Max(lifetime, particleLifetime + 0.25f);
            }

            return Mathf.Max(0.1f, lifetime);
        }

        private static void PlayParticles(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].Play(true); // 즉시 재생
            }
        }

        private static void DisableRuntimeColliders(GameObject root)
        {
            if (root == null)
            {
                return;
            }

            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false; // 보상 수집 충돌과 분리
            }
        }

        private static void LogMissingPrefabOnce()
        {
            if (missingWarningLogged)
            {
                return;
            }

            missingWarningLogged = true;
            Debug.LogWarning("[RewardPickupCollectVfx] Resources/RewardPickups/VFX_Pickup_Cast prefab을 찾지 못했습니다.");
        }
    }

    internal sealed class RuntimeVfxFollowTarget : MonoBehaviour // 런타임 VFX 위치 추적
    {
        [SerializeField] private Transform followTarget; // 따라갈 기준점
        [SerializeField] private Vector3 localOffset; // 기준점 로컬 오프셋
        [SerializeField] private Quaternion worldRotation = Quaternion.identity; // VFX 월드 방향

        public static RuntimeVfxFollowTarget Attach(GameObject instance, Transform target, Vector3 offset, Quaternion rotation)
        {
            if (instance == null || target == null)
            {
                return null; // 추적 불가
            }

            RuntimeVfxFollowTarget follower = instance.GetComponent<RuntimeVfxFollowTarget>();
            if (follower == null)
            {
                follower = instance.AddComponent<RuntimeVfxFollowTarget>(); // 런타임 전용
            }

            follower.Configure(target, offset, rotation);
            return follower;
        }

        public void Configure(Transform target, Vector3 offset, Quaternion rotation)
        {
            followTarget = target; // 기준점 저장
            localOffset = offset; // 로컬 위치 저장
            worldRotation = rotation; // 회전은 부모에 끌려가지 않게 고정
            FollowNow();
        }

        private void LateUpdate()
        {
            FollowNow(); // 컨보이 이동 후 위치 보정
        }

        private void FollowNow()
        {
            if (followTarget == null)
            {
                return; // 대상이 사라지면 마지막 위치 유지
            }

            transform.position = followTarget.TransformPoint(localOffset); // 기준점 추적
            transform.rotation = worldRotation; // VFX 기본 방향 유지
        }
    }

    internal static class RuntimeVfxParticleUtility // 추적형 파티클 보정
    {
        public static void ConfigureParticlesForFollow(GameObject root)
        {
            if (root == null)
            {
                return; // 대상 없음
            }

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                ParticleSystem particle = particles[i];
                if (particle == null)
                {
                    continue; // null 방지
                }

                ParticleSystem.MainModule main = particle.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local; // 재생 중 이동 추적
                main.scalingMode = ParticleSystemScalingMode.Hierarchy; // 루트 스케일 반영
            }
        }
    }
}
