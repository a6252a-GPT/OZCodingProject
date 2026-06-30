using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BossWaveController : MonoBehaviour
    {
        [Serializable]
        public sealed class BossEntry
        {
            public string bossId = "B01"; // 문서와 Inspector에서 구분하기 쉬운 보스 ID입니다.
            public string displayName = "초반 보스"; // Inspector에 보일 이름입니다.
            public EnemyController prefab; // Inspector에서 보스 Prefab을 직접 연결합니다.
        }

        [Serializable]
        public sealed class BossCombinationEntry
        {
            public EnemyController prefab; // 조합에 같이 등장할 보스 Prefab입니다.
        }

        [Serializable]
        public sealed class BossCombination
        {
            public string combinationId = "BC01"; // Inspector에서 구분하기 위한 조합 ID입니다.
            public string displayName = "보스 조합"; // Inspector에 보일 조합 이름입니다.
            public BossCombinationEntry[] bosses = Array.Empty<BossCombinationEntry>(); // 같이 등장할 보스 목록입니다.

            public bool IsAvailable()
            {
                if (bosses == null)
                {
                    return false;
                }

                for (int i = 0; i < bosses.Length; i++)
                {
                    BossCombinationEntry boss = bosses[i];

                    if (boss != null && boss.prefab != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [Header("참조")]
        [SerializeField] private EnemySpawner enemySpawner; // 실제 생성은 기존 EnemySpawner API에 맡깁니다.
        [SerializeField] private BonusChestWaveSpawner bonusChestWaveSpawner; // 보스 처치 후 상자 생성에 사용합니다.

        [Header("보스 진행 설정")]
        [HideInInspector]
        [SerializeField] private bool enableBossWave = true; // 이전 씬 데이터 호환용입니다. 사용 여부는 WaveController가 관리합니다.

        [Min(1)]
        [SerializeField] private int bossStartStage = 15; // 첫 보스가 나올 수 있는 Stage입니다.

        [Min(1)]
        [SerializeField] private int bossIntervalStage = 10; // 다음 보스가 다시 등장하기까지 필요한 Stage 간격입니다.

        [SerializeField] private bool blockAdditionalBossWhileAlive = true; // 보스가 살아있으면 새 보스 등장을 막습니다.
        [SerializeField] private bool spawnChestAfterBossClear = true; // 보스를 처치하면 보너스 상자를 생성합니다.
        [SerializeField] private bool pauseNormalSpawnWhileBossAlive = true; // 보스 Stage에서는 새 일반/엘리트 스폰을 멈춥니다.
        [SerializeField] private bool endBossStageOnBossClear = true; // 보스 Stage는 시간 대신 보스 처치로 종료합니다.

        [Header("보스 등장 순서")]
        [SerializeField] private BossEntry[] bossSequence =
        {
            new BossEntry()
        };

        [Header("보스 조합")]
        [SerializeField] private bool enableBossCombination; // 켜두면 일정 Stage 이후 보스 조합을 사용합니다.

        [Min(1)]
        [SerializeField] private int bossCombinationStartStage = 60; // 이 Stage부터 보스 조합을 사용할 수 있습니다.

        [SerializeField] private BossCombination[] bossCombinations =
        {
            new BossCombination()
        };

        private readonly List<EnemyController> activeBosses = new List<EnemyController>(); // 현재 살아있는 보스 목록입니다.
        private bool waitingBossClearReward; // 보스 처치 보상 상자를 한 번만 주기 위한 플래그입니다.

        public bool HasActiveBoss
        {
            get
            {
                CleanupActiveBosses();
                return activeBosses.Count > 0;
            }
        }

        public bool ShouldPauseNormalSpawn
        {
            get
            {
                return pauseNormalSpawnWhileBossAlive && HasActiveBoss;
            }
        }

        public bool ShouldEndStageOnBossClear => endBossStageOnBossClear;

        public bool IsBossStage(int stage)
        {
            if (stage < bossStartStage)
            {
                return false;
            }

            int safeInterval = Mathf.Max(1, bossIntervalStage);
            return (stage - bossStartStage) % safeInterval == 0;
        }

        public bool BeginStage(int stage)
        {
            CleanupActiveBosses();

            if (!CanSpawnBoss(stage))
            {
                return false;
            }

            EnemySpawner.ExternalSpawnEntry[] entries = BuildBossSpawnEntries(stage);

            if (entries == null || entries.Length == 0)
            {
                return false;
            }

            ResolveReferences();

            if (enemySpawner == null)
            {
                return false;
            }

            List<EnemyController> spawnedBosses = new List<EnemyController>(entries.Length);

            int bossDirectionCount = Mathf.Max(1, entries.Length);

            if (!enemySpawner.TrySpawnExternalEntriesDistributed(entries, bossDirectionCount, 1, spawnedBosses))
            {
                return false;
            }

            for (int i = 0; i < spawnedBosses.Count; i++)
            {
                EnemyController spawnedBoss = spawnedBosses[i];

                if (spawnedBoss != null)
                {
                    activeBosses.Add(spawnedBoss);
                }
            }

            waitingBossClearReward = activeBosses.Count > 0;
            return activeBosses.Count > 0;
        }

        private void Update()
        {
            CleanupActiveBosses();

            // WaveController가 먼저 HasActiveBoss를 확인하면 activeBosses가 이미 정리될 수 있습니다.
            // 그래서 "이전에 보스가 있었는지"보다 보상 대기 플래그와 현재 생존 여부만 봅니다.
            if (!waitingBossClearReward || activeBosses.Count > 0)
            {
                return;
            }

            waitingBossClearReward = false;

            if (spawnChestAfterBossClear)
            {
                ResolveReferences();

                if (bonusChestWaveSpawner != null)
                {
                    bonusChestWaveSpawner.SpawnBonusChestWave();
                }
            }
        }

        private bool CanSpawnBoss(int stage)
        {
            if (!IsBossStage(stage))
            {
                return false;
            }

            if (blockAdditionalBossWhileAlive && HasActiveBoss)
            {
                return false;
            }

            return true;
        }

        private EnemySpawner.ExternalSpawnEntry[] BuildBossSpawnEntries(int stage)
        {
            if (enableBossCombination && stage >= bossCombinationStartStage)
            {
                EnemySpawner.ExternalSpawnEntry[] combinationEntries = BuildBossCombinationEntries(stage);

                if (combinationEntries.Length > 0)
                {
                    return combinationEntries;
                }
            }

            BossEntry boss = PickSingleBossForStage(stage);

            if (boss == null || boss.prefab == null)
            {
                return Array.Empty<EnemySpawner.ExternalSpawnEntry>();
            }

            return new[]
            {
                new EnemySpawner.ExternalSpawnEntry(boss.prefab, 1)
            };
        }

        private BossEntry PickSingleBossForStage(int stage)
        {
            if (bossSequence == null || bossSequence.Length == 0)
            {
                return null;
            }

            int bossWaveIndex = GetBossWaveIndex(stage);
            int safeIndex = Mathf.Clamp(bossWaveIndex, 0, bossSequence.Length - 1);
            return bossSequence[safeIndex];
        }

        private EnemySpawner.ExternalSpawnEntry[] BuildBossCombinationEntries(int stage)
        {
            BossCombination combination = PickBossCombination(stage);

            if (combination == null || !combination.IsAvailable())
            {
                return Array.Empty<EnemySpawner.ExternalSpawnEntry>();
            }

            List<EnemySpawner.ExternalSpawnEntry> entries = new List<EnemySpawner.ExternalSpawnEntry>();

            for (int i = 0; i < combination.bosses.Length; i++)
            {
                BossCombinationEntry boss = combination.bosses[i];

                if (boss != null && boss.prefab != null)
                {
                    entries.Add(new EnemySpawner.ExternalSpawnEntry(boss.prefab, 1));
                }
            }

            return entries.ToArray();
        }

        private BossCombination PickBossCombination(int stage)
        {
            if (bossCombinations == null || bossCombinations.Length == 0)
            {
                return null;
            }

            int combinationWaveIndex = Mathf.Max(0, (stage - bossCombinationStartStage) / Mathf.Max(1, bossIntervalStage));
            int validIndex = 0;
            BossCombination fallback = null;

            for (int i = 0; i < bossCombinations.Length; i++)
            {
                BossCombination combination = bossCombinations[i];

                if (combination == null || !combination.IsAvailable())
                {
                    continue;
                }

                fallback = combination;

                if (validIndex == combinationWaveIndex)
                {
                    return combination;
                }

                validIndex++;
            }

            // 조합이 부족하면 마지막 유효 조합을 반복합니다.
            return fallback;
        }

        private int GetBossWaveIndex(int stage)
        {
            return Mathf.Max(0, (stage - bossStartStage) / Mathf.Max(1, bossIntervalStage));
        }

        private void CleanupActiveBosses()
        {
            activeBosses.RemoveAll(boss => boss == null || boss.IsDead);
        }

        private void ResolveReferences()
        {
            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (bonusChestWaveSpawner == null)
            {
                bonusChestWaveSpawner = FindFirstObjectByType<BonusChestWaveSpawner>();
            }
        }
    }
}
