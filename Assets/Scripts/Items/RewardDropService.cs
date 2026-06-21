using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class RewardDropService : MonoBehaviour // 몬스터 보상 월드 드랍 입구
    {
        private const string ExperiencePickupResourcePath = "RewardPickups/PF_RewardPickup_Exp";
        private const string GoldPickupResourcePath = "RewardPickups/PF_RewardPickup_Gold";
        private const float MinimumDropSpreadRadius = 1.08f;

        public static RewardDropService Active { get; private set; }

        public WorldRewardPickup ExperiencePickupPrefab;
        public WorldRewardPickup GoldPickupPrefab;
        public Transform DropRoot;
        [Min(0f)] public float DropSpreadRadius = 0.42f;
        [Min(0f)] public float GroundHeightOffset = 0.02f;

        private static WorldRewardPickup cachedExperiencePrefab;
        private static WorldRewardPickup cachedGoldPrefab;
        private static int dropSerial;

        private void Awake()
        {
            Active = this;
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

        private void SpawnRewardInternal(RewardData reward, Vector3 position)
        {
            Vector3 basePosition = GroundService.ProjectToGround(position, GroundHeightOffset);
            if (reward.Experience > 0)
            {
                SpawnPickup(ResolveExperiencePrefab(), RewardPickupKind.Experience, reward.Experience, reward.EnemyId, basePosition, basePosition + GetDropOffset(0), DropRoot, GroundHeightOffset);
            }

            if (reward.Gold > 0)
            {
                SpawnPickup(ResolveGoldPrefab(), RewardPickupKind.Gold, reward.Gold, reward.EnemyId, basePosition, basePosition + GetDropOffset(1), DropRoot, GroundHeightOffset);
            }
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

        private static WorldRewardPickup LoadPickupPrefab(string resourcePath)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            return prefab != null ? prefab.GetComponent<WorldRewardPickup>() : null;
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
