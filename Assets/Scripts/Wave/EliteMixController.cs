using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EliteMixController : MonoBehaviour
    {
        [Serializable]
        public sealed class EliteRatioStep
        {
            [Min(1)]
            public int startStage = 6; // 이 Stage부터 엘리트 비율을 적용합니다.

            [Range(0, 100)]
            public int eliteRatioPercent = 10; // 전체 웨이브 수량 중 몇 %를 엘리트로 바꿀지입니다.
        }

        [Serializable]
        public sealed class EliteEntry
        {
            public EnemyController prefab; // Inspector에서 엘리트 몬스터 Prefab을 연결합니다.

            [Range(0, 100)]
            public int ratioPercent = 100; // 이 조합 안에서 해당 엘리트가 차지하는 비율입니다.
        }

        [Serializable]
        public sealed class EliteComposition
        {
            public string compositionId = "E01"; // Inspector에서 구분하기 위한 ID입니다.

            [Min(1)]
            public int minStage = 1; // 이 Stage부터 조합 후보에 포함됩니다.

            [Min(0)]
            public int weight = 100; // 여러 조합 후보 중 선택될 확률 가중치입니다.

            public string combinationType = "진행방해"; // 충돌 규칙에서 비교할 조합 성격입니다.

            public EliteEntry[] elites = Array.Empty<EliteEntry>(); // 실제로 섞어 스폰할 엘리트 목록입니다.

            public bool IsAvailable()
            {
                return weight > 0 && HasValidElite();
            }

            private bool HasValidElite()
            {
                if (elites == null)
                {
                    return false;
                }

                for (int i = 0; i < elites.Length; i++)
                {
                    EliteEntry entry = elites[i];

                    if (entry != null && entry.prefab != null && entry.ratioPercent > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [Serializable]
        public sealed class BlockedEliteCombination
        {
            public string combinationA = "진행방해"; // 같이 나오면 안 되는 첫 번째 조합 성격입니다.
            public string combinationB = "강제분리"; // 같이 나오면 안 되는 두 번째 조합 성격입니다.

            public bool IsMatch(string first, string second)
            {
                string safeFirst = Normalize(first);
                string safeSecond = Normalize(second);
                string safeA = Normalize(combinationA);
                string safeB = Normalize(combinationB);

                if (string.IsNullOrEmpty(safeFirst) || string.IsNullOrEmpty(safeSecond) || string.IsNullOrEmpty(safeA) || string.IsNullOrEmpty(safeB))
                {
                    return false;
                }

                return safeFirst == safeA && safeSecond == safeB || safeFirst == safeB && safeSecond == safeA;
            }
        }

        public readonly struct EliteStagePlan
        {
            public readonly EnemySpawner.ExternalSpawnEntry[] Entries;
            public readonly string CombinationType;
            public readonly int TotalCount;

            public EliteStagePlan(EnemySpawner.ExternalSpawnEntry[] entries, string combinationType)
            {
                Entries = entries ?? Array.Empty<EnemySpawner.ExternalSpawnEntry>();
                CombinationType = Normalize(combinationType);
                TotalCount = CalculateTotalCount(Entries);
            }

            public bool HasEntries => Entries != null && Entries.Length > 0 && TotalCount > 0;

            private static int CalculateTotalCount(EnemySpawner.ExternalSpawnEntry[] entries)
            {
                int total = 0;

                for (int i = 0; i < entries.Length; i++)
                {
                    EnemySpawner.ExternalSpawnEntry entry = entries[i];

                    if (entry.IsValid)
                    {
                        total += entry.Count;
                    }
                }

                return total;
            }
        }

        private readonly struct CountEntry
        {
            public readonly EnemyController Prefab;
            public readonly int Count;

            public CountEntry(EnemyController prefab, int count)
            {
                Prefab = prefab;
                Count = count;
            }
        }

        private readonly HashSet<string> activeCombinationTypes = new HashSet<string>(); // 살아있는 엘리트 조합 성격을 임시로 모읍니다.

        [Header("엘리트 비율 단계")]
        [SerializeField] private EliteRatioStep[] eliteRatioSteps =
        {
            new EliteRatioStep { startStage = 6, eliteRatioPercent = 10 },
            new EliteRatioStep { startStage = 12, eliteRatioPercent = 15 },
            new EliteRatioStep { startStage = 20, eliteRatioPercent = 20 }
        };

        [Header("엘리트 조합")]
        [SerializeField] private EliteComposition[] eliteCompositions =
        {
            CreateComposition("E01", 100, "진행방해", 70, 30),
            CreateComposition("E02", 80, "강제분리", 50, 50),
            CreateComposition("E03", 70, "소환압박", 50, 50)
        };

        [Header("엘리트 충돌 규칙")]
        [SerializeField] private bool checkAliveEliteCombinations = true; // 살아있는 엘리트와 새 조합이 충돌하는지 검사합니다.
        [SerializeField] private BlockedEliteCombination[] blockedEliteCombinations =
        {
            new BlockedEliteCombination { combinationA = "진행방해", combinationB = "강제분리" }
        };

        public int CalculateEliteCount(int stage, int totalSpawnCount)
        {
            int ratio = GetEliteRatioForStage(stage);
            return Mathf.Clamp(Mathf.RoundToInt(totalSpawnCount * (ratio / 100.0f)), 0, Mathf.Max(0, totalSpawnCount));
        }

        public EliteStagePlan BuildStagePlan(int stage, int eliteCount)
        {
            if (eliteCount <= 0)
            {
                return default;
            }

            EliteComposition composition = PickComposition(stage);

            if (composition == null)
            {
                return default;
            }

            List<CountEntry> counts = BuildCountEntries(composition.elites, eliteCount);
            return new EliteStagePlan(BuildEntries(counts), composition.combinationType);
        }

        private int GetEliteRatioForStage(int stage)
        {
            int result = 0;

            if (eliteRatioSteps == null)
            {
                return result;
            }

            for (int i = 0; i < eliteRatioSteps.Length; i++)
            {
                EliteRatioStep step = eliteRatioSteps[i];

                if (step != null && stage >= step.startStage)
                {
                    result = step.eliteRatioPercent;
                }
            }

            return Mathf.Clamp(result, 0, 100);
        }

        private EliteComposition PickComposition(int stage)
        {
            if (eliteCompositions == null)
            {
                return null;
            }

            if (checkAliveEliteCombinations)
            {
                WaveSpawnedEliteMarker.CollectActiveCombinationTypes(activeCombinationTypes);
            }
            else
            {
                activeCombinationTypes.Clear();
            }

            int totalWeight = 0;

            for (int i = 0; i < eliteCompositions.Length; i++)
            {
                EliteComposition composition = eliteCompositions[i];

                if (CanUseComposition(composition, stage))
                {
                    totalWeight += composition.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < eliteCompositions.Length; i++)
            {
                EliteComposition composition = eliteCompositions[i];

                if (!CanUseComposition(composition, stage))
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

        private bool CanUseComposition(EliteComposition composition, int stage)
        {
            if (composition == null || !composition.IsAvailable())
            {
                return false;
            }

            if (stage < Mathf.Max(1, composition.minStage))
            {
                return false;
            }

            if (!checkAliveEliteCombinations)
            {
                return true;
            }

            string newType = Normalize(composition.combinationType);

            if (string.IsNullOrEmpty(newType))
            {
                return true;
            }

            foreach (string activeType in activeCombinationTypes)
            {
                if (IsBlockedCombination(activeType, newType))
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsBlockedCombination(string activeType, string newType)
        {
            if (blockedEliteCombinations == null)
            {
                return false;
            }

            for (int i = 0; i < blockedEliteCombinations.Length; i++)
            {
                BlockedEliteCombination blocked = blockedEliteCombinations[i];

                if (blocked != null && blocked.IsMatch(activeType, newType))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<CountEntry> BuildCountEntries(EliteEntry[] ratios, int totalCount)
        {
            List<CountEntry> results = new List<CountEntry>();

            if (ratios == null || ratios.Length == 0 || totalCount <= 0)
            {
                return results;
            }

            int totalRatio = 0;

            for (int i = 0; i < ratios.Length; i++)
            {
                EliteEntry ratio = ratios[i];

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
                EliteEntry ratio = ratios[i];

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

        private static EnemySpawner.ExternalSpawnEntry[] BuildEntries(List<CountEntry> counts)
        {
            List<EnemySpawner.ExternalSpawnEntry> entries = new List<EnemySpawner.ExternalSpawnEntry>();

            for (int i = 0; i < counts.Count; i++)
            {
                CountEntry count = counts[i];

                if (count.Prefab != null && count.Count > 0)
                {
                    entries.Add(new EnemySpawner.ExternalSpawnEntry(count.Prefab, count.Count));
                }
            }

            return entries.ToArray();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static EliteComposition CreateComposition(string id, int weight, string combinationType, params int[] ratios)
        {
            EliteComposition composition = new EliteComposition
            {
                compositionId = id,
                weight = weight,
                combinationType = combinationType,
                elites = new EliteEntry[Mathf.Max(0, ratios.Length)]
            };

            for (int i = 0; i < composition.elites.Length; i++)
            {
                composition.elites[i] = new EliteEntry
                {
                    ratioPercent = ratios[i]
                };
            }

            return composition;
        }
    }
}
