using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class GoldCollectSpecialWave : MonoBehaviour
    {
        [Header("Special Wave Chance")]
        [SerializeField] private bool enableSpecialWave = true; // 골드 수집 특수웨이브 사용 여부입니다.
        [Min(1)]
        [SerializeField] private int minStartStage = 6; // 이 Stage부터 등장 확률을 체크합니다.
        [Range(0, 100)]
        [SerializeField] private int baseChancePercent = 20; // 첫 등장 체크 확률입니다.
        [Range(0, 100)]
        [SerializeField] private int chanceIncreaseOnFailPercent = 5; // 등장 실패 시 다음 체크에 더할 확률입니다.
        [Range(0, 100)]
        [SerializeField] private int maxChancePercent = 60; // 확률이 이 값보다 커지지 않게 제한합니다.
        [Min(0)]
        [SerializeField] private int cooldownStageCount = 5; // 한 번 등장한 뒤 다시 등장하기 전 대기 Stage 수입니다.
        [SerializeField] private bool blockBossStage = true; // 보스 Stage에서는 등장하지 않게 합니다.

        [Header("Gold Collect")]
        [SerializeField] private GameObject goldPickupPrefab; // 맵에 뿌릴 점수용 골드 오브젝트입니다.
        [SerializeField] private Transform goldRoot; // 생성된 골드를 정리할 부모입니다.
        [SerializeField] private Transform nexus; // 골드 생성 반경의 중심입니다.
        [Min(1)]
        [SerializeField] private int goldSpawnCount = 80; // 특수웨이브 중 생성할 골드 개수입니다.
        [Min(1.0f)]
        [SerializeField] private float collectDurationSeconds = 20.0f; // 골드를 수집할 수 있는 시간입니다.
        [Range(0.0f, 200.0f)]
        [SerializeField] private float minSpawnRadius = 15.0f; // Nexus 기준 최소 생성 반경입니다.
        [Range(0.0f, 250.0f)]
        [SerializeField] private float maxSpawnRadius = 45.0f; // Nexus 기준 최대 생성 반경입니다.
        [SerializeField] private float goldHeightOffset = 0.35f; // 골드 생성 높이 보정값입니다.
        [Min(0.1f)]
        [SerializeField] private float collectRadius = 1.5f; // 골드 수집 판정 거리입니다.
        [Min(0)]
        [SerializeField] private int goldRewardPerPickup = 1; // 이벤트 골드 1개를 먹을 때 실제 지급할 골드입니다.

        [Header("Reward Threshold")]
        [Range(0, 100)]
        [SerializeField] private int normalChestPercent = 50; // 일반 상자가 나오는 최소 수집률입니다.
        [Range(0, 100)]
        [SerializeField] private int rareChestPercent = 70; // 레어 상자가 나오는 최소 수집률입니다.
        [Range(0, 100)]
        [SerializeField] private int uniqueChestPercent = 90; // 유니크 상자가 나오는 최소 수집률입니다.

        [Header("Reward Chest Prefabs")]
        [SerializeField] private BonusChest normalChestPrefab; // 일반 상자 프리팹입니다.
        [SerializeField] private BonusChest rareChestPrefab; // 레어 상자 프리팹입니다.
        [SerializeField] private BonusChest uniqueChestPrefab; // 유니크 상자 프리팹입니다.
        [SerializeField] private Transform chestRoot; // 생성된 보상 상자를 정리할 부모입니다.

        [Header("Reward Layout")]
        [SerializeField] private Transform rewardCenter; // 보상 상자 줄맞춤 중심 위치입니다.
        [SerializeField] private Vector3 fallbackRewardDirection = Vector3.back; // rewardCenter가 없을 때 Nexus 기준 배치 방향입니다.
        [Min(0.0f)]
        [SerializeField] private float fallbackRewardDistance = 8.0f; // rewardCenter가 없을 때 Nexus에서 떨어질 거리입니다.
        [Min(0.0f)]
        [SerializeField] private float chestSpacing = 4.0f; // 상자 사이 간격입니다.
        [SerializeField] private float chestHeightOffset = 0.0f; // 상자 생성 높이 보정값입니다.
        [Min(0.0f)]
        [SerializeField] private float rewardStageMaxWaitSeconds = 0.0f; // 0이면 상자가 모두 사라질 때까지 기다립니다.

        private readonly List<GoldCollectPickup> activeGoldPickups = new List<GoldCollectPickup>(); // 현재 이벤트 골드 목록입니다.
        private readonly List<BonusChest> activeRewardChests = new List<BonusChest>(); // 현재 보상 상자 목록입니다.
        private Coroutine runningRoutine; // 특수웨이브 진행 루틴입니다.
        private Action onFinished; // 특수웨이브 완료 시 WaveController로 돌려줄 콜백입니다.
        private int failedChanceCount; // 등장 실패 누적 횟수입니다.
        private int lastTriggeredStage = -99999; // 마지막으로 특수웨이브가 등장한 Stage입니다.
        private int collectedGoldCount; // 이번 이벤트에서 먹은 골드 수입니다.
        private int spawnedGoldCount; // 이번 이벤트에서 생성한 골드 수입니다.
        private bool rewardStageActive; // 보상 상자 대기 중인지 여부입니다.
        private bool collectStageActive; // 골드를 먹을 수 있는 수집 시간인지 기록합니다.
        private float collectEndTime; // 수집 단계가 끝나는 Time.time 기준 시각입니다.

        public bool IsRunning => runningRoutine != null;
        public bool IsCollectStageActive => collectStageActive;
        public bool IsRewardStageActive => rewardStageActive;
        public int CollectedGoldCount => collectedGoldCount;
        public int SpawnedGoldCount => spawnedGoldCount;
        public int GoldRewardPerPickup => Mathf.Max(0, goldRewardPerPickup);
        public float RemainingCollectSeconds => collectStageActive ? Mathf.Max(0.0f, collectEndTime - Time.time) : 0.0f;
        public float CollectedPercent => spawnedGoldCount > 0 ? collectedGoldCount / (float)spawnedGoldCount * 100.0f : 0.0f;
        public int CurrentChancePercent => Mathf.Clamp(baseChancePercent + failedChanceCount * chanceIncreaseOnFailPercent, baseChancePercent, maxChancePercent);

        public bool TryBeginStage(int stage, bool isBossStage, Action finishedCallback)
        {
            if (!CanCheckStage(stage, isBossStage))
            {
                return false;
            }

            int chance = CurrentChancePercent;
            int roll = UnityEngine.Random.Range(0, 100);

            if (roll >= chance)
            {
                failedChanceCount++;
                return false;
            }

            failedChanceCount = 0;
            lastTriggeredStage = stage;
            BeginSpecialWave(finishedCallback);
            return true;
        }

        public void BeginSpecialWave(Action finishedCallback)
        {
            StopSpecialWave(false);
            onFinished = finishedCallback;
            runningRoutine = StartCoroutine(RunSpecialWaveRoutine());
        }

        public void StopSpecialWave(bool notifyFinished)
        {
            if (runningRoutine != null)
            {
                StopCoroutine(runningRoutine);
                runningRoutine = null;
            }

            ClearGoldPickups();
            ClearRewardChests();
            collectStageActive = false;
            rewardStageActive = false;

            if (notifyFinished)
            {
                NotifyFinished();
            }
        }

        public void NotifyGoldCollected(GoldCollectPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            activeGoldPickups.Remove(pickup);
            collectedGoldCount = Mathf.Min(spawnedGoldCount, collectedGoldCount + 1);
        }

        [ContextMenu("Test Start Gold Collect Special Wave")]
        public void TestStartSpecialWave()
        {
            BeginSpecialWave(null);
        }

        [ContextMenu("Clear Special Wave Objects")]
        public void ClearSpecialWaveObjects()
        {
            StopSpecialWave(false);
        }

        private bool CanCheckStage(int stage, bool isBossStage)
        {
            if (!enableSpecialWave || IsRunning)
            {
                return false;
            }

            if (stage < minStartStage)
            {
                return false;
            }

            if (blockBossStage && isBossStage)
            {
                return false;
            }

            if (stage - lastTriggeredStage <= cooldownStageCount)
            {
                return false;
            }

            return true;
        }

        private IEnumerator RunSpecialWaveRoutine()
        {
            collectedGoldCount = 0;
            spawnedGoldCount = Mathf.Max(0, goldSpawnCount);
            collectStageActive = true;
            rewardStageActive = false;

            SpawnGoldPickups();

            collectEndTime = Time.time + Mathf.Max(0.1f, collectDurationSeconds);
            while (Time.time < collectEndTime)
            {
                CleanupGoldList();
                yield return null;
            }

            collectStageActive = false;
            ClearGoldPickups();
            SpawnRewardChests();
            rewardStageActive = true;

            float rewardStartTime = Time.time;
            while (!AreRewardChestsCleared())
            {
                if (rewardStageMaxWaitSeconds > 0.0f && Time.time - rewardStartTime >= rewardStageMaxWaitSeconds)
                {
                    break;
                }

                yield return null;
            }

            ClearRewardChests();
            rewardStageActive = false;
            runningRoutine = null;
            NotifyFinished();
        }

        private void SpawnGoldPickups()
        {
            ClearGoldPickups();

            Transform root = goldRoot != null ? goldRoot : transform;
            Vector3 center = ResolveNexusPosition();

            for (int i = 0; i < spawnedGoldCount; i++)
            {
                Vector3 position = GetRandomGoldPosition(center);
                GameObject goldObject = goldPickupPrefab != null
                    ? Instantiate(goldPickupPrefab, position, Quaternion.identity, root)
                    : CreateFallbackGoldPickup(position, root);

                if (goldObject == null)
                {
                    continue;
                }

                GoldCollectPickup pickup = goldObject.GetComponent<GoldCollectPickup>();
                if (pickup == null)
                {
                    pickup = goldObject.AddComponent<GoldCollectPickup>();
                }

                pickup.Configure(this, collectRadius);
                activeGoldPickups.Add(pickup);
            }
        }

        private void SpawnRewardChests()
        {
            ClearRewardChests();

            List<BonusChest> rewardPrefabs = BuildRewardPrefabList(Mathf.RoundToInt(CollectedPercent));
            if (rewardPrefabs.Count <= 0)
            {
                return;
            }

            Vector3 center = ResolveRewardCenter();
            Vector3 right = ResolveRewardRight();
            Transform root = chestRoot != null ? chestRoot : transform;

            for (int i = 0; i < rewardPrefabs.Count; i++)
            {
                BonusChest prefab = rewardPrefabs[i];
                if (prefab == null)
                {
                    continue;
                }

                float centeredIndex = i - (rewardPrefabs.Count - 1) * 0.5f;
                Vector3 position = center + right * centeredIndex * chestSpacing;
                position.y += chestHeightOffset;

                BonusChest chest = Instantiate(prefab, position, Quaternion.identity, root);
                chest.ConfigureChoiceGroup(root, false, 0.0f);
                activeRewardChests.Add(chest);
            }
        }

        private List<BonusChest> BuildRewardPrefabList(int collectPercent)
        {
            List<BonusChest> results = new List<BonusChest>(3);

            if (collectPercent >= normalChestPercent && normalChestPrefab != null)
            {
                results.Add(normalChestPrefab);
            }

            if (collectPercent >= rareChestPercent && rareChestPrefab != null)
            {
                results.Add(rareChestPrefab);
            }

            if (collectPercent >= uniqueChestPercent && uniqueChestPrefab != null)
            {
                results.Add(uniqueChestPrefab);
            }

            return results;
        }

        private bool AreRewardChestsCleared()
        {
            for (int i = activeRewardChests.Count - 1; i >= 0; i--)
            {
                BonusChest chest = activeRewardChests[i];

                if (chest == null)
                {
                    activeRewardChests.RemoveAt(i);
                }
            }

            return activeRewardChests.Count <= 0;
        }

        private void CleanupGoldList()
        {
            for (int i = activeGoldPickups.Count - 1; i >= 0; i--)
            {
                if (activeGoldPickups[i] == null)
                {
                    activeGoldPickups.RemoveAt(i);
                }
            }
        }

        private void ClearGoldPickups()
        {
            for (int i = activeGoldPickups.Count - 1; i >= 0; i--)
            {
                GoldCollectPickup pickup = activeGoldPickups[i];

                if (pickup != null)
                {
                    DestroyImmediateSafe(pickup.gameObject);
                }
            }

            activeGoldPickups.Clear();
        }

        private void ClearRewardChests()
        {
            for (int i = activeRewardChests.Count - 1; i >= 0; i--)
            {
                BonusChest chest = activeRewardChests[i];

                if (chest != null)
                {
                    DestroyImmediateSafe(chest.gameObject);
                }
            }

            activeRewardChests.Clear();
        }

        private Vector3 GetRandomGoldPosition(Vector3 center)
        {
            float safeMinRadius = Mathf.Min(minSpawnRadius, maxSpawnRadius);
            float safeMaxRadius = Mathf.Max(minSpawnRadius, maxSpawnRadius);
            float radius = UnityEngine.Random.Range(safeMinRadius, safeMaxRadius);
            float angle = UnityEngine.Random.Range(0.0f, Mathf.PI * 2.0f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
            Vector3 position = center + offset;
            position.y += goldHeightOffset;
            return position;
        }

        private Vector3 ResolveNexusPosition()
        {
            if (nexus != null)
            {
                return nexus.position;
            }

            return transform.position;
        }

        private Vector3 ResolveRewardCenter()
        {
            if (rewardCenter != null)
            {
                return rewardCenter.position;
            }

            Vector3 basePosition = ResolveNexusPosition();
            Vector3 direction = fallbackRewardDirection;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.back;
            }

            direction.Normalize();
            return basePosition + direction * fallbackRewardDistance;
        }

        private Vector3 ResolveRewardRight()
        {
            if (rewardCenter != null)
            {
                Vector3 right = rewardCenter.right;
                right.y = 0.0f;

                if (right.sqrMagnitude > 0.0001f)
                {
                    right.Normalize();
                    return right;
                }
            }

            return Vector3.right;
        }

        private void NotifyFinished()
        {
            Action callback = onFinished;
            onFinished = null;
            callback?.Invoke();
        }

        private static GameObject CreateFallbackGoldPickup(Vector3 position, Transform root)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "GoldCollectPickup_Fallback";
            fallback.transform.SetParent(root, false);
            fallback.transform.position = position;
            fallback.transform.localScale = Vector3.one * 0.7f;
            Renderer renderer = fallback.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = null;
                renderer.material.color = new Color(1.0f, 0.78f, 0.08f, 1.0f);
            }

            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            return fallback;
        }

        private static void DestroyImmediateSafe(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = ResolveNexusPosition();

            Gizmos.color = new Color(1.0f, 0.78f, 0.08f, 0.75f);
            Gizmos.DrawWireSphere(center, minSpawnRadius);
            Gizmos.DrawWireSphere(center, maxSpawnRadius);

            Gizmos.color = new Color(0.3f, 0.8f, 1.0f, 0.85f);
            Gizmos.DrawWireSphere(ResolveRewardCenter(), 0.6f);
        }
    }
}
