using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BonusChestWaveSpawner : MonoBehaviour // 보너스 상자 웨이브 생성 담당
    {
        [System.Serializable]
        private sealed class BonusChestSpawnRule // 상자 등급 하나의 생성 규칙
        {
            [Tooltip("Inspector에서 구분하기 위한 이름입니다.")]
            public string displayName = "낡은 상자"; // 상자 등급 이름

            [Tooltip("이 등급 상자를 몇 개 생성할지 정합니다.")]
            [Range(0, 10)]
            public int count = 1; // 생성 개수

            [Tooltip("이 등급 상자가 줄 경험치입니다.")]
            [Min(0)]
            public int experienceReward = 20; // 경험치 보상

            [Tooltip("이 등급 상자가 줄 골드입니다.")]
            [Min(0)]
            public int goldReward = 20; // 골드 보상
        }

        [Header("Reference")]
        [Tooltip("생성할 상자 Prefab입니다. Animated Fantasy Polygon Chest Prefab을 연결하세요.")]
        [SerializeField] private BonusChest chestPrefab; // 상자 Prefab

        [Tooltip("생성된 상자를 정리할 부모 Transform입니다. 비워두면 자기 자신 아래에 생성합니다.")]
        [SerializeField] private Transform chestRoot; // 생성 상자 부모

        [Header("Spawn Area")]
        [Tooltip("컨보이 주변을 기준으로 랜덤 생성할지 정합니다.")]
        [SerializeField] private bool spawnAroundConvoy = true; // 컨보이 주변 생성 여부

        [Tooltip("컨보이 위치를 못 찾았을 때 사용할 기준 위치입니다.")]
        [SerializeField] private Transform fallbackCenter; // 대체 기준 위치

        [Tooltip("기준 위치에서 최소 몇 m 떨어져 생성할지 정합니다.")]
        [Range(0.0f, 80.0f)]
        [SerializeField] private float minSpawnRadius = 16.0f; // 최소 생성 거리

        [Tooltip("기준 위치에서 최대 몇 m 떨어져 생성할지 정합니다.")]
        [Range(1.0f, 120.0f)]
        [SerializeField] private float maxSpawnRadius = 34.0f; // 최대 생성 거리

        [Tooltip("상자가 바닥 위에 떠 보이지 않게 보정할 높이입니다.")]
        [Range(0.0f, 5.0f)]
        [SerializeField] private float groundHeightOffset = 0.0f; // 바닥 높이 보정

        [Header("Chest Rules")]
        [Tooltip("등급별 상자 생성 규칙입니다. 예: 낡은 2개, 중간 1개, 최고급 1개")]
        [SerializeField] private BonusChestSpawnRule[] chestRules =
        {
            new BonusChestSpawnRule
            {
                displayName = "낡은 상자",
                count = 2,
                experienceReward = 20,
                goldReward = 20
            },
            new BonusChestSpawnRule
            {
                displayName = "중간 상자",
                count = 1,
                experienceReward = 40,
                goldReward = 40
            },
            new BonusChestSpawnRule
            {
                displayName = "최고급 상자",
                count = 1,
                experienceReward = 80,
                goldReward = 80
            }
        };

        [ContextMenu("Spawn Bonus Chest Wave")]
        public void SpawnBonusChestWave() // Inspector 우클릭 메뉴나 다른 시스템에서 호출할 상자 웨이브 입구
        {
            if (chestPrefab == null) // 상자 Prefab이 없으면 생성할 수 없다.
            {
                Debug.LogWarning("[BonusChestWaveSpawner] Chest Prefab이 연결되지 않았습니다.", this);
                return;
            }

            if (chestRules == null || chestRules.Length == 0) // 생성 규칙이 없다면
            {
                return;
            }

            Transform root = chestRoot != null ? chestRoot : transform; // 정리 부모
            Vector3 center = ResolveSpawnCenter(); // 기준 위치

            for (int ruleIndex = 0; ruleIndex < chestRules.Length; ruleIndex++)
            {
                BonusChestSpawnRule rule = chestRules[ruleIndex];

                if (rule == null || rule.count <= 0)
                {
                    continue;
                }

                for (int countIndex = 0; countIndex < rule.count; countIndex++)
                {
                    SpawnChest(rule, center, root);
                }
            }
        }

        private void SpawnChest(BonusChestSpawnRule rule, Vector3 center, Transform root) // 상자 하나 생성
        {
            Vector3 position = GetRandomSpawnPosition(center);
            Quaternion rotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
            BonusChest chest = Instantiate(chestPrefab, position, rotation, root);
            chest.ConfigureReward(rule.experienceReward, rule.goldReward);
        }

        private Vector3 ResolveSpawnCenter() // 상자 생성 기준 위치 결정
        {
            if (spawnAroundConvoy && MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget))
            {
                return convoyTarget.position;
            }

            if (fallbackCenter != null)
            {
                return fallbackCenter.position;
            }

            return transform.position;
        }

        private Vector3 GetRandomSpawnPosition(Vector3 center) // 기준 위치 주변 랜덤 지점 생성
        {
            float safeMinRadius = Mathf.Max(0.0f, minSpawnRadius);
            float safeMaxRadius = Mathf.Max(safeMinRadius + 0.1f, maxSpawnRadius);
            float radius = Random.Range(safeMinRadius, safeMaxRadius);
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle)) * radius;
            Vector3 position = center + offset;

            return GroundService.ProjectToGround(position, groundHeightOffset);
        }
    }
}
