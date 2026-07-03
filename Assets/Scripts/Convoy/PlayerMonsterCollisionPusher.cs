using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TeamProject01.Gameplay
{
    public sealed class PlayerMonsterCollisionPusher : MonoBehaviour // CollisionPushCenter 기준 플레이어 충돌힘 처리
    {
        private const string RuntimeObjectName = "PlayerMonsterCollisionForce_Runtime";
        private const string CollisionPushCenterName = "CollisionPushCenter";

        [SerializeField] private bool enableCollisionPush = true; // 충돌힘 사용 여부
        [SerializeField, Min(0.05f)] private float collisionRadius = 1.0f; // CollisionPushCenter 기준 원형 판정
        [SerializeField, Min(0.0f)] private float baseKnockbackDistance = 1.05f; // 충돌힘 0일 때 직접 밀림 거리
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.14f; // 직접 밀림 시간
        [SerializeField, Min(0.0f)] private float visualLiftHeight = 0.32f; // 일반/엘리트 비주얼만 살짝 띄우는 높이
        [SerializeField, Min(0.0f)] private float baseCrowdPressureDistance = 0.42f; // 군중 연쇄에 넣는 첫 압력
        [SerializeField, Min(0.0f)] private float perEnemyCooldown = 0.18f; // 같은 몬스터 반복 충돌 간격
        [SerializeField, Min(1)] private int maxEnemiesPerFrame = 32; // 과부하 방지
        private readonly List<EnemyController> overlapEnemies = new List<EnemyController>(32); // 범위 안 몬스터
        private readonly Dictionary<int, float> nextPushTimes = new Dictionary<int, float>(128); // 몬스터별 재충돌 시간
        private float debugNextPushLogTime; // 선택권 튐 원인 대조용 로그 스로틀

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeHooks()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallRuntimePusher()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            EnsureRuntimePusher();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureRuntimePusher();
        }

        private static void EnsureRuntimePusher()
        {
            if (FindFirstObjectByType<PlayerMonsterCollisionPusher>() != null)
            {
                return;
            }

            if (FindFirstObjectByType<PlayerPickupInteractor>() == null && FindFirstObjectByType<ConvoyController>() == null)
            {
                return;
            }

            GameObject runner = new GameObject(RuntimeObjectName);
            runner.AddComponent<PlayerMonsterCollisionPusher>();
            StartingSegmentChoiceTicketDebug.Log(
                $"PlayerMonsterCollisionPusher.Installed scene={StartingSegmentChoiceTicketDebug.SceneName}, hasInteractor={(FindFirstObjectByType<PlayerPickupInteractor>() != null)}, hasConvoy={(FindFirstObjectByType<ConvoyController>() != null)}",
                runner);
        }

        private void Update()
        {
            if (!TryResolveCollisionPushCenter(out Transform pushCenter))
            {
                return;
            }

            if (!enableCollisionPush || collisionRadius <= 0.0f)
            {
                return;
            }

            Vector3 center = pushCenter.position;
            EnemyController.CollectActiveInRange(center, collisionRadius, overlapEnemies, CanAffectEnemy);
            if (StartingSegmentChoiceTicketDebug.ShouldLog && overlapEnemies.Count > 0 && Time.time >= debugNextPushLogTime)
            {
                debugNextPushLogTime = Time.time + 0.5f;
                StartingSegmentChoiceTicketDebug.Log(
                    $"PlayerMonsterCollisionPusher.Overlap center={StartingSegmentChoiceTicketDebug.Format(center)}, radius={collisionRadius:0.00}, enemyCount={overlapEnemies.Count}",
                    this);
            }

            int count = Mathf.Min(overlapEnemies.Count, Mathf.Max(1, maxEnemiesPerFrame));
            for (int i = 0; i < count; i++)
            {
                TryPushEnemy(overlapEnemies[i], center, pushCenter);
            }

            if (nextPushTimes.Count > 2048)
            {
                nextPushTimes.Clear(); // 장시간 플레이에서만 방어적으로 정리
            }
        }

        private bool CanAffectEnemy(EnemyController enemy)
        {
            if (enemy == null || enemy.IsDead)
            {
                return false;
            }

            return enemy.Grade == EnemyGrade.Monster || enemy.Grade == EnemyGrade.Elite;
        }

        private void TryPushEnemy(EnemyController enemy, Vector3 center, Transform pushCenter)
        {
            if (enemy == null || enemy.IsDead)
            {
                return;
            }

            float now = Time.time;
            int enemyId = enemy.EnemyId;
            if (nextPushTimes.TryGetValue(enemyId, out float nextAllowedTime) && now < nextAllowedTime)
            {
                return;
            }

            Vector3 direction = enemy.transform.position - center;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = pushCenter != null ? pushCenter.forward : transform.forward;
                direction.y = 0.0f;
            }

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            float forceMultiplier = GetCollisionForceMultiplier();
            float knockbackDistance = baseKnockbackDistance * forceMultiplier;
            MonsterFeedbackData feedback = MonsterFeedbackData.Create(
                center,
                direction,
                enemy.transform.position,
                knockbackDistance,
                knockbackDuration,
                0.0f,
                -1,
                DamageType.Direct,
                gameObject);
            feedback.VisualLiftHeight = visualLiftHeight * forceMultiplier;

            if (!MonsterFeedbackApi.TryApplyFeedback(enemy, feedback))
            {
                return;
            }

            QueueCrowdPressure(enemy, direction, forceMultiplier);
            nextPushTimes[enemyId] = now + Mathf.Max(0.0f, perEnemyCooldown);
        }

        private void QueueCrowdPressure(EnemyController enemy, Vector3 direction, float forceMultiplier)
        {
            if (baseCrowdPressureDistance <= 0.0f)
            {
                return;
            }

            EnemyCrowdBlocker crowdBlocker = enemy.GetComponent<EnemyCrowdBlocker>();
            if (crowdBlocker == null)
            {
                return;
            }

            crowdBlocker.QueueExternalPush(direction * baseCrowdPressureDistance * forceMultiplier, nonBossOnly: true);
        }

        private static float GetCollisionForceMultiplier()
        {
            CoreStatProvider core = CoreStatProvider.Active;
            if (core == null)
            {
                return 1.0f;
            }

            return Mathf.Max(0.0f, 1.0f + core.CollisionForceBonus);
        }

        private static bool TryResolveCollisionPushCenter(out Transform pushCenter)
        {
            pushCenter = null;
            if (MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget)
                && TryFindActiveChildRecursive(convoyTarget, CollisionPushCenterName, out pushCenter))
            {
                return true;
            }

            ConvoyController controller = FindFirstObjectByType<ConvoyController>();
            return controller != null && TryFindActiveChildRecursive(controller.transform, CollisionPushCenterName, out pushCenter);
        }

        private static bool TryFindActiveChildRecursive(Transform root, string childName, out Transform found)
        {
            found = null;
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return false;
            }

            if (root.name == childName && root.gameObject.activeInHierarchy)
            {
                found = root;
                return true;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (TryFindActiveChildRecursive(child, childName, out found))
                {
                    return true;
                }
            }

            return false;
        }

    }
}
