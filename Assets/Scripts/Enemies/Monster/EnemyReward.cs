using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyReward : MonoBehaviour // 몬스터 보상
    {
        [Min(0)]
        [SerializeField] private int experienceReward = 1; // 처치 경험치

        [Min(0)]
        [SerializeField] private int goldReward = 1; // 처치 골드

        public void GiveReward(int enemyId, Vector3 position) // 처치 보상 지급
        {
            RewardData reward = RewardData.Create(experienceReward, goldReward, enemyId, position); // 보상 데이터 생성
            RewardGateway.SubmitReward(reward); // 코어 보상 입구 전달
        }
    }
}