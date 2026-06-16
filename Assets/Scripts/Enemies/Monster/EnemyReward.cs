using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyReward : MonoBehaviour //몬스터 보상
    {
        [Min(0)]
        [SerializeField] private int experienceReward = 1; // 처치 경험치

        [Min(0)]
        [SerializeField] private int goldReward = 1; // 처치 골드

        public void GiveReward(int enemyId, Vector3 position)
        {
            RewardData reward = RewardData.Create(experienceReward, goldReward, enemyId, position);
            GrowthRewardReceiver.SubmitReward(reward);
        }
    }
}