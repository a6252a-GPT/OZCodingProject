using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyReward : MonoBehaviour
    {
        private const int MinimumExperiencePickupAmount = 3;
        private const float DefaultWorldRewardDropChance = 0.5f;
        private const int DefaultWorldRewardAmountMultiplier = 2;

        [Min(0)]
        [SerializeField] private int experienceReward = MinimumExperiencePickupAmount;

        [Min(0)]
        [SerializeField] private int goldReward = 1;

        [Header("World Reward Drop")]
        [Range(0f, 1f)]
        [SerializeField] private float experienceDropChance = DefaultWorldRewardDropChance;
        [Range(0f, 1f)]
        [SerializeField] private float goldDropChance = DefaultWorldRewardDropChance;
        [Min(1)]
        [SerializeField] private int worldRewardAmountMultiplier = DefaultWorldRewardAmountMultiplier;

        [Header("Elite Diamond")]
        [SerializeField] private bool enableEliteDiamondDrop = true;
        [Range(0f, 1f)]
        [SerializeField] private float eliteDiamondDropChance = 0.5f;
        [Min(0)]
        [SerializeField] private int eliteDiamondReward = 2;

        public void GiveReward(int enemyId, Vector3 position, EnemyGrade grade)
        {
            ResolveWorldRewardSet(out int resolvedExperience, out int resolvedGold);
            RewardData reward = RewardData.Create(resolvedExperience, resolvedGold, enemyId, position);
            RewardDropService.SpawnReward(reward, position);
            TrySpawnEliteDiamondReward(enemyId, position, grade);
        }

        private void ResolveWorldRewardSet(out int resolvedExperience, out int resolvedGold)
        {
            resolvedExperience = 0;
            resolvedGold = 0;

            if (experienceReward <= 0 || goldReward <= 0 || !RollDrop(GetWorldRewardSetDropChance()))
            {
                return;
            }

            int multiplier = Mathf.Max(1, worldRewardAmountMultiplier);
            int baseExperience = Mathf.Max(MinimumExperiencePickupAmount, experienceReward);
            resolvedExperience = baseExperience * multiplier;
            resolvedGold = Mathf.Max(0, goldReward) * multiplier;
        }

        private float GetWorldRewardSetDropChance()
        {
            return Mathf.Min(Mathf.Clamp01(experienceDropChance), Mathf.Clamp01(goldDropChance));
        }

        private void TrySpawnEliteDiamondReward(int enemyId, Vector3 position, EnemyGrade grade)
        {
            if (!enableEliteDiamondDrop || grade != EnemyGrade.Elite || eliteDiamondReward <= 0)
            {
                return;
            }

            if (Random.value > Mathf.Clamp01(eliteDiamondDropChance))
            {
                return;
            }

            RewardDropService.SpawnDiamond(eliteDiamondReward, position, enemyId);
        }

        private static bool RollDrop(float chance)
        {
            return Random.value <= Mathf.Clamp01(chance);
        }
    }
}
