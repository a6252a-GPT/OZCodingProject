using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TeamProject01.Gameplay
{
    public sealed class ManaOrbCollectSpecialWave : MonoBehaviour
    {
        private enum ManaOrbSpawnShape
        {
            Star = 0,
            Square = 1,
            Triangle = 2,
            Circle = 3,
            Diamond = 4,
        }

        private const int ManaOrbSpawnShapeCount = 5;

        [Header("Special Wave Chance")]
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

        [Header("Mana Orb Collect")]
        [FormerlySerializedAs("goldPickupPrefab")]
        [SerializeField] private GameObject manaOrbPickupPrefab; // 맵에 뿌릴 마력 구슬 오브젝트입니다.
        [FormerlySerializedAs("goldRoot")]
        [SerializeField] private Transform manaOrbRoot; // 생성된 마력 구슬을 정리할 부모입니다.
        [SerializeField] private Transform nexus; // 마력 구슬 생성 반경의 중심입니다.
        [Min(1)]
        [FormerlySerializedAs("goldSpawnCount")]
        [SerializeField] private int manaOrbSpawnCount = 80; // 특수웨이브 중 생성할 마력 구슬 개수입니다.
        [Min(1.0f)]
        [SerializeField] private float collectDurationSeconds = 20.0f; // 마력 구슬을 수집할 수 있는 시간입니다.
        [Range(0.0f, 200.0f)]
        [SerializeField] private float minSpawnRadius = 15.0f; // Nexus 기준 최소 생성 반경입니다.
        [Range(0.0f, 250.0f)]
        [SerializeField] private float maxSpawnRadius = 45.0f; // Nexus 기준 최대 생성 반경입니다.
        [FormerlySerializedAs("useStarSpawnPattern")]
        [SerializeField] private bool randomizeSpawnShape = true; // 특수웨이브마다 배치 도형을 랜덤 선택합니다.
        [SerializeField] private ManaOrbSpawnShape fixedSpawnShape = ManaOrbSpawnShape.Star; // 랜덤을 끌 때 사용할 고정 도형입니다.
        [Range(-180.0f, 180.0f)]
        [FormerlySerializedAs("starSpawnRotationDegrees")]
        [SerializeField] private float shapeSpawnRotationDegrees = 90.0f; // 도형 첫 꼭짓점이 향하는 각도입니다.
        [FormerlySerializedAs("goldHeightOffset")]
        [SerializeField] private float manaOrbHeightOffset = 0.35f; // 마력 구슬 생성 높이 보정값입니다.
        [Min(0.1f)]
        [SerializeField] private float collectRadius = 1.5f; // 마력 구슬 수집 판정 거리입니다.

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

        private readonly List<ManaOrbPickup> activeManaOrbPickups = new List<ManaOrbPickup>(); // 현재 이벤트 마력 구슬 목록입니다.
        private readonly List<BonusChest> activeRewardChests = new List<BonusChest>(); // 현재 보상 상자 목록입니다.
        private Coroutine runningRoutine; // 특수웨이브 진행 루틴입니다.
        private Action onFinished; // 특수웨이브 완료 시 WaveController로 돌려줄 콜백입니다.
        private int failedChanceCount; // 등장 실패 누적 횟수입니다.
        private int lastTriggeredStage = -99999; // 마지막으로 특수웨이브가 등장한 Stage입니다.
        private int collectedManaOrbCount; // 이번 이벤트에서 먹은 마력 구슬 수입니다.
        private int spawnedManaOrbCount; // 이번 이벤트에서 생성한 마력 구슬 수입니다.
        private bool rewardStageActive; // 보상 상자 대기 중인지 여부입니다.
        private bool collectStageActive; // 마력 구슬을 먹을 수 있는 수집 시간인지 기록합니다.
        private float collectEndTime; // 수집 단계가 끝나는 Time.time 기준 시각입니다.
        private ManaOrbSpawnShape currentSpawnShape = ManaOrbSpawnShape.Star; // 이번 특수웨이브에서 선택된 배치 도형입니다.

        public bool IsRunning => runningRoutine != null;
        public bool IsCollectStageActive => collectStageActive;
        public bool IsRewardStageActive => rewardStageActive;
        public int CollectedManaOrbCount => collectedManaOrbCount;
        public int SpawnedManaOrbCount => spawnedManaOrbCount;
        public int RemainingManaOrbCount => Mathf.Clamp(spawnedManaOrbCount - collectedManaOrbCount, 0, spawnedManaOrbCount);
        public float RemainingCollectSeconds => collectStageActive ? Mathf.Max(0.0f, collectEndTime - Time.time) : 0.0f;
        public float CollectedPercent => spawnedManaOrbCount > 0 ? collectedManaOrbCount / (float)spawnedManaOrbCount * 100.0f : 0.0f;
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

            ClearManaOrbPickups();
            ClearRewardChests();
            collectStageActive = false;
            rewardStageActive = false;

            if (notifyFinished)
            {
                NotifyFinished();
            }
        }

        public void NotifyManaOrbCollected(ManaOrbPickup pickup)
        {
            if (pickup == null)
            {
                return;
            }

            activeManaOrbPickups.Remove(pickup);
            collectedManaOrbCount = Mathf.Min(spawnedManaOrbCount, collectedManaOrbCount + 1);
        }

        [ContextMenu("Test Start Mana Orb Collect Special Wave")]
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
            if (IsRunning)
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
            collectedManaOrbCount = 0;
            spawnedManaOrbCount = Mathf.Max(0, manaOrbSpawnCount);
            collectStageActive = true;
            rewardStageActive = false;

            SpawnManaOrbPickups();

            collectEndTime = Time.time + Mathf.Max(0.1f, collectDurationSeconds);
            while (Time.time < collectEndTime)
            {
                CleanupManaOrbList();

                if (HasCollectedAllManaOrbs())
                {
                    break;
                }

                yield return null;
            }

            collectStageActive = false;
            ClearManaOrbPickups();
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

        private void SpawnManaOrbPickups()
        {
            ClearManaOrbPickups();

            Transform root = manaOrbRoot != null ? manaOrbRoot : transform;
            Vector3 center = ResolveNexusPosition();
            currentSpawnShape = ResolveManaOrbSpawnShape();

            for (int i = 0; i < spawnedManaOrbCount; i++)
            {
                Vector3 position = GetManaOrbSpawnPosition(center, i, spawnedManaOrbCount, currentSpawnShape);
                GameObject manaOrbObject = manaOrbPickupPrefab != null
                    ? Instantiate(manaOrbPickupPrefab, position, Quaternion.identity, root)
                    : CreateFallbackManaOrbPickup(position, root);

                if (manaOrbObject == null)
                {
                    continue;
                }

                ManaOrbPickup pickup = manaOrbObject.GetComponent<ManaOrbPickup>();
                if (pickup == null)
                {
                    pickup = manaOrbObject.AddComponent<ManaOrbPickup>();
                }

                pickup.Configure(this, collectRadius);
                activeManaOrbPickups.Add(pickup);
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

        private bool HasCollectedAllManaOrbs()
        {
            return spawnedManaOrbCount > 0 && RemainingManaOrbCount <= 0;
        }

        private ManaOrbSpawnShape ResolveManaOrbSpawnShape()
        {
            if (!randomizeSpawnShape)
            {
                return fixedSpawnShape;
            }

            return (ManaOrbSpawnShape)UnityEngine.Random.Range(0, ManaOrbSpawnShapeCount);
        }

        private Vector3 GetManaOrbSpawnPosition(Vector3 center, int index, int count, ManaOrbSpawnShape shape)
        {
            float innerRadius = Mathf.Min(minSpawnRadius, maxSpawnRadius);
            float outerRadius = Mathf.Max(minSpawnRadius, maxSpawnRadius);

            if (outerRadius <= 0.001f)
            {
                Vector3 fallbackPosition = center;
                fallbackPosition.y += manaOrbHeightOffset;
                return fallbackPosition;
            }

            if (innerRadius <= 0.001f)
            {
                innerRadius = outerRadius * 0.42f;
            }

            Vector3 position = center + GetShapePerimeterOffset(shape, index, count, innerRadius, outerRadius);
            position.y += manaOrbHeightOffset;
            return position;
        }

        private Vector3 GetShapePerimeterOffset(ManaOrbSpawnShape shape, int index, int count, float innerRadius, float outerRadius)
        {
            switch (shape)
            {
                case ManaOrbSpawnShape.Square:
                    return GetRegularPolygonPerimeterOffset(index, count, 4, outerRadius, 45.0f);
                case ManaOrbSpawnShape.Triangle:
                    return GetRegularPolygonPerimeterOffset(index, count, 3, outerRadius, 0.0f);
                case ManaOrbSpawnShape.Circle:
                    return GetCirclePerimeterOffset(index, count, outerRadius);
                case ManaOrbSpawnShape.Diamond:
                    return GetRegularPolygonPerimeterOffset(index, count, 4, outerRadius, 0.0f);
                default:
                    return GetStarPerimeterOffset(index, count, innerRadius, outerRadius);
            }
        }

        private Vector3 GetStarPerimeterOffset(int index, int count, float innerRadius, float outerRadius)
        {
            const int starPointCount = 5;
            const int starVertexCount = starPointCount * 2;

            float normalizedIndex = count <= 1 ? 0.0f : Mathf.Repeat(index / (float)count, 1.0f);
            float segmentPosition = normalizedIndex * starVertexCount;
            int segmentIndex = Mathf.FloorToInt(segmentPosition) % starVertexCount;
            int nextSegmentIndex = (segmentIndex + 1) % starVertexCount;
            float segmentT = segmentPosition - Mathf.Floor(segmentPosition);

            Vector3 start = GetStarVertexOffset(segmentIndex, innerRadius, outerRadius);
            Vector3 end = GetStarVertexOffset(nextSegmentIndex, innerRadius, outerRadius);
            return Vector3.Lerp(start, end, segmentT);
        }

        private Vector3 GetStarVertexOffset(int vertexIndex, float innerRadius, float outerRadius)
        {
            const int starPointCount = 5;

            float radius = vertexIndex % 2 == 0 ? outerRadius : innerRadius;
            float angle = shapeSpawnRotationDegrees * Mathf.Deg2Rad + vertexIndex * Mathf.PI / starPointCount;
            return new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
        }

        private Vector3 GetRegularPolygonPerimeterOffset(int index, int count, int vertexCount, float radius, float rotationOffsetDegrees)
        {
            float normalizedIndex = count <= 1 ? 0.0f : Mathf.Repeat(index / (float)count, 1.0f);
            float segmentPosition = normalizedIndex * vertexCount;
            int segmentIndex = Mathf.FloorToInt(segmentPosition) % vertexCount;
            int nextSegmentIndex = (segmentIndex + 1) % vertexCount;
            float segmentT = segmentPosition - Mathf.Floor(segmentPosition);

            Vector3 start = GetRegularPolygonVertexOffset(segmentIndex, vertexCount, radius, rotationOffsetDegrees);
            Vector3 end = GetRegularPolygonVertexOffset(nextSegmentIndex, vertexCount, radius, rotationOffsetDegrees);
            return Vector3.Lerp(start, end, segmentT);
        }

        private Vector3 GetRegularPolygonVertexOffset(int vertexIndex, int vertexCount, float radius, float rotationOffsetDegrees)
        {
            float angleStep = Mathf.PI * 2.0f / vertexCount;
            float angle = (shapeSpawnRotationDegrees + rotationOffsetDegrees) * Mathf.Deg2Rad + vertexIndex * angleStep;
            return new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
        }

        private Vector3 GetCirclePerimeterOffset(int index, int count, float radius)
        {
            float normalizedIndex = count <= 1 ? 0.0f : Mathf.Repeat(index / (float)count, 1.0f);
            float angle = shapeSpawnRotationDegrees * Mathf.Deg2Rad + normalizedIndex * Mathf.PI * 2.0f;
            return new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
        }

        private void CleanupManaOrbList()
        {
            for (int i = activeManaOrbPickups.Count - 1; i >= 0; i--)
            {
                if (activeManaOrbPickups[i] == null)
                {
                    activeManaOrbPickups.RemoveAt(i);
                }
            }
        }

        private void ClearManaOrbPickups()
        {
            for (int i = activeManaOrbPickups.Count - 1; i >= 0; i--)
            {
                ManaOrbPickup pickup = activeManaOrbPickups[i];

                if (pickup != null)
                {
                    DestroyImmediateSafe(pickup.gameObject);
                }
            }

            activeManaOrbPickups.Clear();
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

        private static GameObject CreateFallbackManaOrbPickup(Vector3 position, Transform root)
        {
            GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallback.name = "ManaOrbPickup_Fallback";
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

            ManaOrbSpawnShape previewShape = Application.isPlaying && IsRunning ? currentSpawnShape : fixedSpawnShape;
            DrawShapeSpawnGizmo(center, previewShape);

            Gizmos.color = new Color(0.3f, 0.8f, 1.0f, 0.85f);
            Gizmos.DrawWireSphere(ResolveRewardCenter(), 0.6f);
        }

        private void DrawShapeSpawnGizmo(Vector3 center, ManaOrbSpawnShape shape)
        {
            float innerRadius = Mathf.Min(minSpawnRadius, maxSpawnRadius);
            float outerRadius = Mathf.Max(minSpawnRadius, maxSpawnRadius);

            if (outerRadius <= 0.001f)
            {
                return;
            }

            if (innerRadius <= 0.001f)
            {
                innerRadius = outerRadius * 0.42f;
            }

            Gizmos.color = new Color(1.0f, 0.95f, 0.15f, 0.95f);

            int lineCount = shape == ManaOrbSpawnShape.Circle ? 64 : GetShapeGizmoLineCount(shape);
            Vector3 previous = center + GetShapePerimeterOffset(shape, 0, lineCount, innerRadius, outerRadius);
            for (int i = 1; i <= lineCount; i++)
            {
                Vector3 next = center + GetShapePerimeterOffset(shape, i % lineCount, lineCount, innerRadius, outerRadius);
                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }

        private static int GetShapeGizmoLineCount(ManaOrbSpawnShape shape)
        {
            switch (shape)
            {
                case ManaOrbSpawnShape.Square:
                case ManaOrbSpawnShape.Diamond:
                    return 4;
                case ManaOrbSpawnShape.Triangle:
                    return 3;
                default:
                    return 10;
            }
        }
    }
}
