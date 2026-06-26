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

        [Header("참조")]
        [SerializeField] private EnemySpawner enemySpawner; // 실제 생성은 기존 EnemySpawner API에 맡깁니다.
        [SerializeField] private BonusChestWaveSpawner bonusChestWaveSpawner; // 보스 처치 후 상자 생성에 사용합니다.

        [Header("보스 진행 설정")]
        [SerializeField] private bool enableBossWave = true; // 꺼두면 보스 로직을 사용하지 않습니다.

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

        [Header("확장 설정")]
        [SerializeField] private bool enableBossCombination; // 추후 보스가 부족할 때 조합 보스로 확장하기 위한 자리입니다.

        private readonly List<EnemyController> activeBosses = new List<EnemyController>(); // 현재 살아있는 보스 목록입니다.
        private int lastBossStage = -9999; // 마지막으로 보스를 스폰한 Stage입니다.
        private int nextBossIndex; // 다음에 사용할 보스 순서입니다.
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

        public bool BeginStage(int stage)
        {
            CleanupActiveBosses();

            if (!CanSpawnBoss(stage))
            {
                return false;
            }

            BossEntry boss = PickNextBoss();

            if (boss == null || boss.prefab == null)
            {
                return false;
            }

            ResolveReferences();

            if (enemySpawner == null)
            {
                return false;
            }

            EnemySpawner.ExternalSpawnEntry[] entries =
            {
                new EnemySpawner.ExternalSpawnEntry(boss.prefab, 1)
            };

            List<EnemyController> spawnedBosses = new List<EnemyController>(1);

            if (!enemySpawner.TrySpawnExternalEntriesDistributed(entries, 1, 1, spawnedBosses))
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

            lastBossStage = stage;
            waitingBossClearReward = activeBosses.Count > 0;
            AdvanceBossIndex();
            return activeBosses.Count > 0;
        }

        private void Update()
        {
            bool hadActiveBoss = activeBosses.Count > 0;
            CleanupActiveBosses();

            if (!waitingBossClearReward || !hadActiveBoss || activeBosses.Count > 0)
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
            if (!enableBossWave)
            {
                return false;
            }

            if (stage < bossStartStage)
            {
                return false;
            }

            if (blockAdditionalBossWhileAlive && HasActiveBoss)
            {
                return false;
            }

            return stage - lastBossStage >= bossIntervalStage;
        }

        private BossEntry PickNextBoss()
        {
            if (bossSequence == null || bossSequence.Length == 0)
            {
                return null;
            }

            int safeIndex = Mathf.Clamp(nextBossIndex, 0, bossSequence.Length - 1);
            return bossSequence[safeIndex];
        }

        private void AdvanceBossIndex()
        {
            if (bossSequence == null || bossSequence.Length == 0)
            {
                nextBossIndex = 0;
                return;
            }

            if (nextBossIndex < bossSequence.Length - 1)
            {
                nextBossIndex++;
            }

            if (enableBossCombination)
            {
                nextBossIndex = Mathf.Min(nextBossIndex, bossSequence.Length - 1);
            }
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
