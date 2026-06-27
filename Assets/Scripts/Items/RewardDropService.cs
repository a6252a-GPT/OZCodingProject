using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class RewardDropService : MonoBehaviour // 몬스터 보상 월드 드랍 입구
    {
        private const string ExperiencePickupResourcePath = "RewardPickups/PF_RewardPickup_Exp";
        private const string GoldPickupResourcePath = "RewardPickups/PF_RewardPickup_Gold";
        private const string SegmentChoiceTicketPickupResourcePath = "RewardPickups/PF_RewardPickup_SegmentChoiceTicket";
        private const float MinimumDropSpreadRadius = 1.08f;

        public static RewardDropService Active { get; private set; }

        public WorldRewardPickup ExperiencePickupPrefab;
        public WorldRewardPickup GoldPickupPrefab;
        public WorldRewardPickup SegmentChoiceTicketPickupPrefab;
        public Transform DropRoot;
        [Min(0f)] public float DropSpreadRadius = 0.42f;
        [Min(0f)] public float GroundHeightOffset = 0.02f;
        [Header("Pooling")]
        [Min(0)] public int InitialPoolSizePerKind = 16;
        public bool AllowPoolExpansion = true;

        private static WorldRewardPickup cachedExperiencePrefab;
        private static WorldRewardPickup cachedGoldPrefab;
        private static WorldRewardPickup cachedSegmentChoiceTicketPrefab;
        private static int dropSerial;
        private readonly Dictionary<WorldRewardPickup, Queue<WorldRewardPickup>> pickupPools = new Dictionary<WorldRewardPickup, Queue<WorldRewardPickup>>();
        private Transform poolRoot;

        private void Awake()
        {
            Active = this;
            PrewarmPools();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        public static void SpawnReward(RewardData reward, Vector3 position)
        {
            if (!reward.IsValid)
            {
                return;
            }

            if (Active != null)
            {
                Active.SpawnRewardInternal(reward, position);
                return;
            }

            SpawnRewardDefault(reward, position);
        }

        public static void SpawnSegmentChoiceTicket(int ticketCount, Vector3 position)
        {
            int safeCount = Mathf.Max(1, ticketCount);
            if (Active != null)
            {
                Active.SpawnSegmentChoiceTicketInternal(safeCount, position);
                return;
            }

            Vector3 basePosition = GroundService.ProjectToGround(position, 0.02f);
            SpawnPickup(GetCachedSegmentChoiceTicketPrefab(), RewardPickupKind.SegmentChoiceTicket, safeCount, 0, basePosition, basePosition + GetDefaultDropOffset(2), null, 0.02f);
        }

        private void SpawnRewardInternal(RewardData reward, Vector3 position)
        {
            Vector3 basePosition = GroundService.ProjectToGround(position, GroundHeightOffset);
            if (reward.Experience > 0)
            {
                SpawnPickupFromPool(ResolveExperiencePrefab(), RewardPickupKind.Experience, reward.Experience, reward.EnemyId, basePosition, basePosition + GetDropOffset(0), DropRoot, GroundHeightOffset);
            }

            if (reward.Gold > 0)
            {
                SpawnPickupFromPool(ResolveGoldPrefab(), RewardPickupKind.Gold, reward.Gold, reward.EnemyId, basePosition, basePosition + GetDropOffset(1), DropRoot, GroundHeightOffset);
            }
        }

        private void SpawnSegmentChoiceTicketInternal(int ticketCount, Vector3 position)
        {
            Vector3 basePosition = GroundService.ProjectToGround(position, GroundHeightOffset);
            SpawnPickupFromPool(ResolveSegmentChoiceTicketPrefab(), RewardPickupKind.SegmentChoiceTicket, Mathf.Max(1, ticketCount), 0, basePosition, basePosition + GetDropOffset(2), DropRoot, GroundHeightOffset);
        }

        private static void SpawnRewardDefault(RewardData reward, Vector3 position)
        {
            Vector3 basePosition = GroundService.ProjectToGround(position, 0.02f);
            if (reward.Experience > 0)
            {
                SpawnPickup(GetCachedExperiencePrefab(), RewardPickupKind.Experience, reward.Experience, reward.EnemyId, basePosition, basePosition + GetDefaultDropOffset(0), null, 0.02f);
            }

            if (reward.Gold > 0)
            {
                SpawnPickup(GetCachedGoldPrefab(), RewardPickupKind.Gold, reward.Gold, reward.EnemyId, basePosition, basePosition + GetDefaultDropOffset(1), null, 0.02f);
            }
        }

        private WorldRewardPickup ResolveExperiencePrefab()
        {
            return ExperiencePickupPrefab != null ? ExperiencePickupPrefab : GetCachedExperiencePrefab();
        }

        private WorldRewardPickup ResolveGoldPrefab()
        {
            return GoldPickupPrefab != null ? GoldPickupPrefab : GetCachedGoldPrefab();
        }

        private WorldRewardPickup ResolveSegmentChoiceTicketPrefab()
        {
            return SegmentChoiceTicketPickupPrefab != null ? SegmentChoiceTicketPickupPrefab : GetCachedSegmentChoiceTicketPrefab();
        }

        private Vector3 GetDropOffset(int index)
        {
            float radius = Mathf.Max(MinimumDropSpreadRadius, DropSpreadRadius);
            if (radius <= 0f)
            {
                return Vector3.zero;
            }

            float angle = index == 0 ? -35f : 35f;
            Quaternion rotation = Quaternion.Euler(0f, angle + Random.Range(-18f, 18f), 0f);
            return rotation * Vector3.forward * Random.Range(radius * 0.45f, radius);
        }

        private static Vector3 GetDefaultDropOffset(int index)
        {
            float angle = index == 0 ? -35f : 35f;
            Quaternion rotation = Quaternion.Euler(0f, angle + Random.Range(-18f, 18f), 0f);
            return rotation * Vector3.forward * Random.Range(MinimumDropSpreadRadius * 0.45f, MinimumDropSpreadRadius);
        }

        private static WorldRewardPickup GetCachedExperiencePrefab()
        {
            if (cachedExperiencePrefab == null)
            {
                cachedExperiencePrefab = LoadPickupPrefab(ExperiencePickupResourcePath);
            }

            return cachedExperiencePrefab;
        }

        private static WorldRewardPickup GetCachedGoldPrefab()
        {
            if (cachedGoldPrefab == null)
            {
                cachedGoldPrefab = LoadPickupPrefab(GoldPickupResourcePath);
            }

            return cachedGoldPrefab;
        }

        private static WorldRewardPickup GetCachedSegmentChoiceTicketPrefab()
        {
            if (cachedSegmentChoiceTicketPrefab == null)
            {
                cachedSegmentChoiceTicketPrefab = LoadPickupPrefab(SegmentChoiceTicketPickupResourcePath);
            }

            return cachedSegmentChoiceTicketPrefab;
        }

        private static WorldRewardPickup LoadPickupPrefab(string resourcePath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            return prefab != null ? prefab.GetComponent<WorldRewardPickup>() : null;
        }

        private void PrewarmPools()
        {
            int count = Mathf.Max(0, InitialPoolSizePerKind);
            PrewarmPool(ResolveExperiencePrefab(), count);
            PrewarmPool(ResolveGoldPrefab(), count);
            PrewarmPool(ResolveSegmentChoiceTicketPrefab(), count);
        }

        private void PrewarmPool(WorldRewardPickup prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            Queue<WorldRewardPickup> pool = GetPool(prefab);
            for (int i = pool.Count; i < count; i++)
            {
                WorldRewardPickup pickup = CreatePooledPickup(prefab);
                if (pickup != null)
                {
                    pool.Enqueue(pickup);
                }
            }
        }

        private void SpawnPickupFromPool(WorldRewardPickup prefab, RewardPickupKind kind, int amount, int enemyId, Vector3 spawnPosition, Vector3 landingPosition, Transform parent, float groundHeightOffset)
        {
            if (amount <= 0)
            {
                return;
            }

            WorldRewardPickup pickup = prefab != null
                ? GetPickupFromPool(prefab, parent)
                : CreateFallbackPickup(kind, spawnPosition, parent, groundHeightOffset);

            if (pickup == null)
            {
                return;
            }

            pickup.name = $"{kind}RewardPickup_{++dropSerial:000}";
            pickup.transform.SetParent(parent, true);
            pickup.AttachPoolOwner(prefab != null ? this : null, prefab);
            pickup.Configure(kind, amount, enemyId, landingPosition, spawnPosition);
            if (!pickup.gameObject.activeSelf)
            {
                pickup.gameObject.SetActive(true);
            }
        }

        private WorldRewardPickup GetPickupFromPool(WorldRewardPickup prefab, Transform parent)
        {
            Queue<WorldRewardPickup> pool = GetPool(prefab);
            while (pool.Count > 0)
            {
                WorldRewardPickup pickup = pool.Dequeue();
                if (pickup != null)
                {
                    pickup.transform.SetParent(parent, true);
                    return pickup;
                }
            }

            return AllowPoolExpansion ? CreatePooledPickup(prefab) : null;
        }

        private WorldRewardPickup CreatePooledPickup(WorldRewardPickup prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            WorldRewardPickup pickup = Instantiate(prefab, GetPoolRoot());
            pickup.AttachPoolOwner(this, prefab);
            pickup.gameObject.SetActive(false);
            return pickup;
        }

        private Queue<WorldRewardPickup> GetPool(WorldRewardPickup prefab)
        {
            if (!pickupPools.TryGetValue(prefab, out Queue<WorldRewardPickup> pool))
            {
                pool = new Queue<WorldRewardPickup>();
                pickupPools.Add(prefab, pool);
            }

            return pool;
        }

        private Transform GetPoolRoot()
        {
            if (poolRoot != null)
            {
                return poolRoot;
            }

            GameObject root = new GameObject("RewardPickupPool");
            root.transform.SetParent(transform, false);
            poolRoot = root.transform;
            return poolRoot;
        }

        internal bool ReleasePickup(WorldRewardPickup pickup, WorldRewardPickup sourcePrefab)
        {
            if (pickup == null || sourcePrefab == null)
            {
                return false;
            }

            pickup.ResetForPool();
            pickup.gameObject.SetActive(false);
            pickup.transform.SetParent(GetPoolRoot(), false);
            GetPool(sourcePrefab).Enqueue(pickup);
            return true;
        }

        private static void SpawnPickup(WorldRewardPickup prefab, RewardPickupKind kind, int amount, int enemyId, Vector3 spawnPosition, Vector3 landingPosition, Transform parent, float groundHeightOffset)
        {
            if (amount <= 0)
            {
                return;
            }

            WorldRewardPickup pickup = prefab != null
                ? Instantiate(prefab, spawnPosition, Quaternion.identity, parent)
                : CreateFallbackPickup(kind, spawnPosition, parent, groundHeightOffset);

            pickup.name = $"{kind}RewardPickup_{++dropSerial:000}";
            pickup.Configure(kind, amount, enemyId, landingPosition, spawnPosition);
        }

        private static WorldRewardPickup CreateFallbackPickup(RewardPickupKind kind, Vector3 position, Transform parent, float groundHeightOffset)
        {
            PrimitiveType primitiveType = kind == RewardPickupKind.Experience ? PrimitiveType.Sphere : PrimitiveType.Cylinder;
            GameObject fallback = GameObject.CreatePrimitive(primitiveType);
            fallback.transform.SetParent(parent, true);
            fallback.transform.position = GroundService.ProjectToGround(position, groundHeightOffset);
            fallback.transform.localScale = kind == RewardPickupKind.Experience ? Vector3.one * 0.34f : new Vector3(0.38f, 0.12f, 0.38f);

            Collider collider = fallback.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            WorldRewardPickup pickup = fallback.AddComponent<WorldRewardPickup>();
            return pickup;
        }
    }
}
