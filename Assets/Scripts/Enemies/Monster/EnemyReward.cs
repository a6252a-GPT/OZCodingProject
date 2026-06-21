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
            RewardDropService.SpawnReward(reward, position); // 전찬우수정-0621 추가: 보상을 즉시 지급하지 않고 월드 경험치/골드 아이템으로 드랍
            // RewardGateway.SubmitReward(reward); // 전찬우수정-0621 삭제: 몬스터 사망 즉시 코어로 보상 지급하던 기존 방식 제거
        }
    }
}
