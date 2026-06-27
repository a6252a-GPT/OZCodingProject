using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class NormalWaveSpawner : MonoBehaviour
    {
        [Serializable]
        public sealed class SpawnScaleStep
        {
            [Min(1)]
            public int startStage = 1; // 이 Stage부터 적용할 몬스터 수 배율입니다.

            [Min(0)]
            public int spawnScalePercent = 100; // 100이면 기본 수량 그대로, 110이면 10% 증가입니다.
        }

        [Serializable]
        public sealed class DifficultyScaleStep
        {
            [Min(1)]
            public int startStage = 1; // 이 Stage부터 적용할 난이도 배율입니다.

            [Min(1)]
            public int healthScalePercent = 100; // 몬스터 최대 체력 배율입니다.

            [Min(1)]
            public int moveSpeedScalePercent = 100; // 몬스터 이동속도 배율입니다.

            [Min(1)]
            public int nexusDamageScalePercent = 100; // Nexus에 주는 피해 배율입니다.
        }

        [Serializable]
        public sealed class MonsterRatioEntry
        {
            public EnemyController prefab; // Project 창의 몬스터 Prefab을 Inspector에서 직접 연결합니다.

            [Range(0, 100)]
            public int ratioPercent = 100; // 이 조합 안에서 해당 몬스터가 차지하는 비율입니다.
        }

        [Serializable]
        public sealed class NormalComposition
        {
            public string compositionId = "N01"; // 문서와 대화에서 구분하기 쉬운 조합 ID입니다.
            public string displayName = "기본 근접"; // Inspector에 보일 이름입니다.

            [Min(1)]
            public int minStage = 1; // 이 Stage부터 후보 조합에 포함됩니다.

            [Min(0)]
            public int weight = 100; // 여러 후보 중 선택될 확률 가중치입니다.

            public MonsterRatioEntry[] monsters = Array.Empty<MonsterRatioEntry>(); // 실제로 섞어 스폰할 몬스터 목록입니다.

            public bool IsAvailable(int stage)
            {
                return stage >= minStage && weight > 0 && HasValidMonster();
            }

            private bool HasValidMonster()
            {
                if (monsters == null)
                {
                    return false;
                }

                for (int i = 0; i < monsters.Length; i++)
                {
                    MonsterRatioEntry monster = monsters[i];

                    if (monster != null && monster.prefab != null && monster.ratioPercent > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly struct CountEntry
        {
            public readonly EnemyController Prefab;
            public readonly int Count;
            public readonly string EliteCombinationType;

            public CountEntry(EnemyController prefab, int count, string eliteCombinationType = null)
            {
                Prefab = prefab;
                Count = count;
                EliteCombinationType = string.IsNullOrWhiteSpace(eliteCombinationType) ? string.Empty : eliteCombinationType.Trim();
            }

            public bool IsElite => !string.IsNullOrEmpty(EliteCombinationType);
        }

        [Header("참조")]
        [SerializeField] private EnemySpawner enemySpawner; // 실제 생성은 기존 EnemySpawner API에 맡깁니다.

        [Header("수량 설정")]
        [Min(0)]
        [SerializeField] private int baseSpawnCount = 30; // Stage 1 기준 일반 몬스터 수입니다.

        [SerializeField] private SpawnScaleStep[] spawnScaleSteps =
        {
            new SpawnScaleStep { startStage = 1, spawnScalePercent = 100 },
            new SpawnScaleStep { startStage = 6, spawnScalePercent = 110 },
            new SpawnScaleStep { startStage = 12, spawnScalePercent = 130 }
        };

        [Header("난이도 배율")]
        [SerializeField] private DifficultyScaleStep[] difficultyScaleSteps =
        {
            new DifficultyScaleStep { startStage = 1, healthScalePercent = 100, moveSpeedScalePercent = 100, nexusDamageScalePercent = 100 },
            new DifficultyScaleStep { startStage = 8, healthScalePercent = 105, moveSpeedScalePercent = 104, nexusDamageScalePercent = 100 },
            new DifficultyScaleStep { startStage = 12, healthScalePercent = 110, moveSpeedScalePercent = 107, nexusDamageScalePercent = 100 },
            new DifficultyScaleStep { startStage = 15, healthScalePercent = 115, moveSpeedScalePercent = 110, nexusDamageScalePercent = 100 },
            new DifficultyScaleStep { startStage = 18, healthScalePercent = 120, moveSpeedScalePercent = 112, nexusDamageScalePercent = 100 },
            new DifficultyScaleStep { startStage = 20, healthScalePercent = 130, moveSpeedScalePercent = 115, nexusDamageScalePercent = 110 },
            new DifficultyScaleStep { startStage = 25, healthScalePercent = 145, moveSpeedScalePercent = 118, nexusDamageScalePercent = 110 },
            new DifficultyScaleStep { startStage = 30, healthScalePercent = 160, moveSpeedScalePercent = 121, nexusDamageScalePercent = 120 },
            new DifficultyScaleStep { startStage = 35, healthScalePercent = 180, moveSpeedScalePercent = 124, nexusDamageScalePercent = 120 },
            new DifficultyScaleStep { startStage = 40, healthScalePercent = 200, moveSpeedScalePercent = 127, nexusDamageScalePercent = 130 }
        };

        [Header("일반 몬스터 조합")]
        [SerializeField] private NormalComposition[] normalCompositions =
        {
            CreateComposition("N01", "기본 근접", 1, 100, 100),
            CreateComposition("N02", "근접 섞기", 3, 80, 75, 25),
            CreateComposition("N03", "근접 + 원거리", 5, 70, 65, 20, 15)
        };

        [Header("고급 설정")]
        [Range(1, 100)]
        [SerializeField] private int spawnWindowPercent = 40; // Stage 시간 중 앞쪽 몇 % 안에 집중 스폰할지입니다.

        [Min(1)]
        [SerializeField] private int spawnBatchCount = 4; // 전체 수량을 몇 묶음으로 나누어 스폰할지입니다.

        [Range(1, 8)]
        [SerializeField] private int batchGateCount = 1; // 한 묶음이 실제로 사용할 게이트 방향 수입니다.

        [Range(1, 8)]
        [SerializeField] private int earlyGateCount = 2; // 초반에 사용할 랜덤 게이트 방향 수입니다.

        [Min(1)]
        [SerializeField] private int midGateStartStage = 10; // 중반 게이트 수를 적용할 시작 Stage입니다.

        [Range(1, 8)]
        [SerializeField] private int midGateCount = 4; // 중반에 사용할 랜덤 게이트 방향 수입니다.

        [Min(1)]
        [SerializeField] private int lateGateStartStage = 20; // 후반 게이트 수를 적용할 시작 Stage입니다.

        [Range(1, 8)]
        [SerializeField] private int lateGateCount = 8; // 후반에 사용할 랜덤 게이트 방향 수입니다.

        [Min(1)]
        [SerializeField] private int frontRowCount = 5; // 한 게이트에서 앞줄에 몇 마리씩 세울지입니다.

        [Header("스폰 혼잡 보정")]
        [SerializeField] private bool useCongestionPush = false; // 켜면 스폰 예정 위치가 붐빌 때 넥서스 반대 방향으로 조금 더 멀리 생성합니다.

        [Min(0.1f)]
        [SerializeField] private float congestionCheckRadius = 8.0f; // 스폰 예정 위치 주변을 검사할 반경입니다.

        [Min(1)]
        [SerializeField] private int congestionMonsterThreshold = 12; // 이 수 이상 몬스터가 있으면 혼잡하다고 판단합니다.

        [Min(0.0f)]
        [SerializeField] private float congestionPushDistance = 5.0f; // 혼잡할 때 한 번에 뒤로 미는 거리입니다.

        [Min(0.0f)]
        [SerializeField] private float congestionMaxPushDistance = 20.0f; // 한 번 스폰에서 최대한 뒤로 밀 수 있는 거리입니다.

        private Coroutine spawnRoutine; // 현재 Stage의 일반 몬스터 스폰 루틴입니다.

        public int CalculateTotalSpawnCount(int stage)
        {
            float scale = GetScaleForStage(stage) / 100.0f;
            return Mathf.Max(0, Mathf.RoundToInt(baseSpawnCount * scale));
        }

        public void BeginStage(int stage, float stageDurationSeconds, int spawnCount, EliteMixController.EliteStagePlan elitePlan = default, WaveController waveTracker = null)
        {
            ResolveEnemySpawner();
            StopCurrentRoutine();

            if (enemySpawner == null)
            {
                return;
            }

            List<CountEntry> totalEntries = new List<CountEntry>();

            if (spawnCount > 0)
            {
                NormalComposition composition = PickComposition(stage);

                if (composition != null)
                {
                    totalEntries.AddRange(BuildCountEntries(composition.monsters, spawnCount));
                }
            }

            AddEliteEntries(totalEntries, elitePlan);
            waveTracker?.BeginCurrentStageEnemyTracking(stage, GetTotalCount(totalEntries));

            if (totalEntries.Count == 0)
            {
                return;
            }

            int gateCount = GetGateCountForStage(stage);
            EnemySpawner.ExternalSpawnDirectionSet directionSet = enemySpawner.PickExternalSpawnDirections(gateCount);
            spawnRoutine = StartCoroutine(SpawnStageRoutine(stage, stageDurationSeconds, totalEntries, directionSet, waveTracker));
        }

        private IEnumerator SpawnStageRoutine(int stage, float stageDurationSeconds, List<CountEntry> totalEntries, EnemySpawner.ExternalSpawnDirectionSet directionSet, WaveController waveTracker)
        {
            int safeBatchCount = Mathf.Max(1, spawnBatchCount);
            float spawnWindowSeconds = Mathf.Max(0.1f, stageDurationSeconds * (spawnWindowPercent / 100.0f));
            WaveStageDifficulty difficulty = ResolveDifficultyForStage(stage);
            EnemySpawner.ExternalSpawnCongestionOptions congestionOptions = BuildCongestionOptions();
            List<EnemyController> spawnedBatchMonsters = new List<EnemyController>();

            for (int batchIndex = 0; batchIndex < safeBatchCount; batchIndex++)
            {
                if (batchIndex > 0)
                {
                    yield return new WaitForSeconds(spawnWindowSeconds / safeBatchCount);
                }

                EnemySpawner.ExternalSpawnDirectionSet batchDirectionSet = BuildBatchDirectionSet(directionSet, batchIndex);
                EnemySpawner.ExternalSpawnEntry[] normalBatchEntries = BuildBatchEntries(totalEntries, batchIndex, safeBatchCount, false);

                if (normalBatchEntries.Length > 0)
                {
                    spawnedBatchMonsters.Clear();

                    if (enemySpawner.TrySpawnExternalEntriesDistributed(normalBatchEntries, batchDirectionSet, frontRowCount, congestionOptions, spawnedBatchMonsters))
                    {
                        ApplyStageDifficulty(spawnedBatchMonsters, difficulty);
                        waveTracker?.RegisterCurrentStageEnemies(stage, spawnedBatchMonsters);
                    }
                }

                EnemySpawner.ExternalSpawnEntry[] eliteBatchEntries = BuildBatchEntries(totalEntries, batchIndex, safeBatchCount, true);

                if (eliteBatchEntries.Length > 0)
                {
                    spawnedBatchMonsters.Clear();

                    if (enemySpawner.TrySpawnExternalEntriesDistributed(eliteBatchEntries, batchDirectionSet, frontRowCount, congestionOptions, spawnedBatchMonsters))
                    {
                        ApplyStageDifficulty(spawnedBatchMonsters, difficulty);
                        MarkSpawnedElites(spawnedBatchMonsters, GetEliteCombinationType(totalEntries));
                        waveTracker?.RegisterCurrentStageEnemies(stage, spawnedBatchMonsters);
                    }
                }
            }

            waveTracker?.CompleteCurrentStageEnemySpawning(stage);
            spawnRoutine = null;
        }

        private EnemySpawner.ExternalSpawnCongestionOptions BuildCongestionOptions()
        {
            if (!useCongestionPush)
            {
                return EnemySpawner.ExternalSpawnCongestionOptions.Disabled;
            }

            return new EnemySpawner.ExternalSpawnCongestionOptions(
                true,
                congestionCheckRadius,
                congestionMonsterThreshold,
                congestionPushDistance,
                congestionMaxPushDistance);
        }

        private void StopCurrentRoutine()
        {
            if (spawnRoutine == null)
            {
                return;
            }

            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        private int GetScaleForStage(int stage)
        {
            int result = 100;

            if (spawnScaleSteps == null)
            {
                return result;
            }

            for (int i = 0; i < spawnScaleSteps.Length; i++)
            {
                SpawnScaleStep step = spawnScaleSteps[i];

                if (step != null && stage >= step.startStage)
                {
                    result = step.spawnScalePercent;
                }
            }

            return Mathf.Max(0, result);
        }

        private WaveStageDifficulty ResolveDifficultyForStage(int stage)
        {
            DifficultyScaleStep step = GetDifficultyStepForStage(stage);
            int healthPercent = step != null ? step.healthScalePercent : 100;
            int speedPercent = step != null ? step.moveSpeedScalePercent : 100;
            int damagePercent = step != null ? step.nexusDamageScalePercent : 100;

            return new WaveStageDifficulty(stage, healthPercent / 100.0f, speedPercent / 100.0f, damagePercent / 100.0f);
        }

        private DifficultyScaleStep GetDifficultyStepForStage(int stage)
        {
            DifficultyScaleStep result = null;

            if (difficultyScaleSteps == null)
            {
                return result;
            }

            for (int i = 0; i < difficultyScaleSteps.Length; i++)
            {
                DifficultyScaleStep step = difficultyScaleSteps[i];

                if (step != null && stage >= step.startStage)
                {
                    result = step;
                }
            }

            return result;
        }

        private int GetGateCountForStage(int stage)
        {
            if (stage >= lateGateStartStage)
            {
                return lateGateCount;
            }

            if (stage >= midGateStartStage)
            {
                return midGateCount;
            }

            return earlyGateCount;
        }

        private NormalComposition PickComposition(int stage)
        {
            if (normalCompositions == null)
            {
                return null;
            }

            int totalWeight = 0;

            for (int i = 0; i < normalCompositions.Length; i++)
            {
                NormalComposition composition = normalCompositions[i];

                if (composition != null && composition.IsAvailable(stage))
                {
                    totalWeight += composition.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < normalCompositions.Length; i++)
            {
                NormalComposition composition = normalCompositions[i];

                if (composition == null || !composition.IsAvailable(stage))
                {
                    continue;
                }

                randomWeight -= composition.weight;

                if (randomWeight < 0)
                {
                    return composition;
                }
            }

            return null;
        }

        private static List<CountEntry> BuildCountEntries(MonsterRatioEntry[] ratios, int totalCount)
        {
            List<CountEntry> results = new List<CountEntry>();

            if (ratios == null || ratios.Length == 0 || totalCount <= 0)
            {
                return results;
            }

            int totalRatio = 0;

            for (int i = 0; i < ratios.Length; i++)
            {
                MonsterRatioEntry ratio = ratios[i];

                if (ratio != null && ratio.prefab != null && ratio.ratioPercent > 0)
                {
                    totalRatio += ratio.ratioPercent;
                }
            }

            if (totalRatio <= 0)
            {
                return results;
            }

            int assignedCount = 0;
            int lastValidIndex = -1;

            for (int i = 0; i < ratios.Length; i++)
            {
                MonsterRatioEntry ratio = ratios[i];

                if (ratio == null || ratio.prefab == null || ratio.ratioPercent <= 0)
                {
                    continue;
                }

                int count = Mathf.FloorToInt(totalCount * (ratio.ratioPercent / (float)totalRatio));
                assignedCount += count;
                lastValidIndex = results.Count;
                results.Add(new CountEntry(ratio.prefab, count));
            }

            int remainder = totalCount - assignedCount;

            if (remainder > 0 && lastValidIndex >= 0)
            {
                CountEntry last = results[lastValidIndex];
                results[lastValidIndex] = new CountEntry(last.Prefab, last.Count + remainder);
            }

            results.RemoveAll(entry => entry.Count <= 0);
            return results;
        }

        private static void AddEliteEntries(List<CountEntry> results, EliteMixController.EliteStagePlan elitePlan)
        {
            if (results == null || !elitePlan.HasEntries)
            {
                return;
            }

            for (int i = 0; i < elitePlan.Entries.Length; i++)
            {
                EnemySpawner.ExternalSpawnEntry entry = elitePlan.Entries[i];

                if (entry.IsValid)
                {
                    results.Add(new CountEntry(entry.Prefab, entry.Count, elitePlan.CombinationType));
                }
            }
        }

        private static int GetTotalCount(List<CountEntry> entries)
        {
            int totalCount = 0;

            if (entries == null)
            {
                return totalCount;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                totalCount += Mathf.Max(0, entries[i].Count);
            }

            return totalCount;
        }

        private static EnemySpawner.ExternalSpawnEntry[] BuildBatchEntries(List<CountEntry> totalEntries, int batchIndex, int batchCount, bool eliteOnly)
        {
            List<EnemySpawner.ExternalSpawnEntry> batchEntries = new List<EnemySpawner.ExternalSpawnEntry>();

            for (int i = 0; i < totalEntries.Count; i++)
            {
                CountEntry entry = totalEntries[i];

                if (entry.IsElite != eliteOnly)
                {
                    continue;
                }

                int baseCount = entry.Count / batchCount;
                int remainder = entry.Count % batchCount;
                int count = baseCount + (batchIndex < remainder ? 1 : 0);

                if (entry.Prefab != null && count > 0)
                {
                    batchEntries.Add(new EnemySpawner.ExternalSpawnEntry(entry.Prefab, count));
                }
            }

            return batchEntries.ToArray();
        }

        private EnemySpawner.ExternalSpawnDirectionSet BuildBatchDirectionSet(EnemySpawner.ExternalSpawnDirectionSet stageDirectionSet, int batchIndex)
        {
            int[] stageDirectionIndexes = stageDirectionSet.GetDirectionIndexes();

            if (stageDirectionIndexes == null || stageDirectionIndexes.Length == 0)
            {
                return stageDirectionSet;
            }

            int safeBatchGateCount = Mathf.Clamp(batchGateCount, 1, stageDirectionIndexes.Length);

            if (safeBatchGateCount >= stageDirectionIndexes.Length)
            {
                return stageDirectionSet;
            }

            // Stage 시작 때 뽑힌 후보 방향 안에서만 돌려 쓰기 때문에 초반 방향 제한은 유지됩니다.
            int[] batchDirectionIndexes = new int[safeBatchGateCount];
            int startIndex = Mathf.Abs(batchIndex * safeBatchGateCount) % stageDirectionIndexes.Length;

            for (int i = 0; i < batchDirectionIndexes.Length; i++)
            {
                int directionIndex = (startIndex + i) % stageDirectionIndexes.Length;
                batchDirectionIndexes[i] = stageDirectionIndexes[directionIndex];
            }

            return new EnemySpawner.ExternalSpawnDirectionSet(batchDirectionIndexes);
        }

        private static void MarkSpawnedElites(List<EnemyController> spawnedElites, string eliteCombinationType)
        {
            if (spawnedElites == null || string.IsNullOrWhiteSpace(eliteCombinationType))
            {
                return;
            }

            for (int i = 0; i < spawnedElites.Count; i++)
            {
                EnemyController enemy = spawnedElites[i];

                if (enemy == null)
                {
                    continue;
                }

                WaveSpawnedEliteMarker marker = enemy.GetComponent<WaveSpawnedEliteMarker>();

                if (marker == null)
                {
                    marker = enemy.gameObject.AddComponent<WaveSpawnedEliteMarker>();
                }

                marker.Initialize(eliteCombinationType);
            }
        }

        private static void ApplyStageDifficulty(List<EnemyController> spawnedMonsters, WaveStageDifficulty difficulty)
        {
            if (spawnedMonsters == null)
            {
                return;
            }

            for (int i = 0; i < spawnedMonsters.Count; i++)
            {
                EnemyStageDifficultyApplier.Apply(spawnedMonsters[i], difficulty);
            }
        }

        private static string GetEliteCombinationType(List<CountEntry> totalEntries)
        {
            if (totalEntries == null)
            {
                return string.Empty;
            }

            for (int i = 0; i < totalEntries.Count; i++)
            {
                CountEntry entry = totalEntries[i];

                if (entry.IsElite)
                {
                    return entry.EliteCombinationType;
                }
            }

            return string.Empty;
        }

        private void ResolveEnemySpawner()
        {
            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }
        }

        private static NormalComposition CreateComposition(string id, string name, int minStage, int weight, params int[] ratios)
        {
            NormalComposition composition = new NormalComposition
            {
                compositionId = id,
                displayName = name,
                minStage = minStage,
                weight = weight,
                monsters = new MonsterRatioEntry[Mathf.Max(0, ratios.Length)]
            };

            for (int i = 0; i < composition.monsters.Length; i++)
            {
                composition.monsters[i] = new MonsterRatioEntry
                {
                    ratioPercent = ratios[i]
                };
            }

            return composition;
        }
    }
}
