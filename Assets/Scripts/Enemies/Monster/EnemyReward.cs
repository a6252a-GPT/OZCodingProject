using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyReward : MonoBehaviour // 몬스터 보상
    {
        private const int MinimumExperiencePickupAmount = 3; // 몬스터 경험치 픽업 최소값

        [Min(0)]
        [SerializeField] private int experienceReward = MinimumExperiencePickupAmount; // 처치 경험치

        [Min(0)]
        [SerializeField] private int goldReward = 1; // 처치 골드

        [Header("Elite Diamond")]
        [SerializeField] private bool enableEliteDiamondDrop = true; // 엘리트 다이아 드랍
        [Range(0f, 1f)]
        [SerializeField] private float eliteDiamondDropChance = 0.5f; // 엘리트 50%
        [Min(0)]
        [SerializeField] private int eliteDiamondReward = 2; // 엘리트 드랍 다이아

        public void GiveReward(int enemyId, Vector3 position, EnemyGrade grade) // 처치 보상 지급
        {
            int resolvedExperience = experienceReward > 0 ? Mathf.Max(MinimumExperiencePickupAmount, experienceReward) : 0; // 기존 1/2 값은 3짜리 픽업으로 보정
            RewardData reward = RewardData.Create(resolvedExperience, goldReward, enemyId, position); // 보상 데이터 생성
            RewardDropService.SpawnReward(reward, position); // 전찬우수정-0621 추가: 보상을 즉시 지급하지 않고 월드 경험치/골드 아이템으로 드랍
            TrySpawnEliteDiamondReward(enemyId, position, grade); // 엘리트 다이아 보너스
            // RewardGateway.SubmitReward(reward); // 전찬우수정-0621 삭제: 몬스터 사망 즉시 코어로 보상 지급하던 기존 방식 제거
        }

        private void TrySpawnEliteDiamondReward(int enemyId, Vector3 position, EnemyGrade grade) // 엘리트 다이아 드랍
        {
            if (!enableEliteDiamondDrop || grade != EnemyGrade.Elite || eliteDiamondReward <= 0)
            {
                return; // 조건 미충족
            }

            if (Random.value > Mathf.Clamp01(eliteDiamondDropChance))
            {
                return; // 확률 실패
            }

            RewardDropService.SpawnDiamond(eliteDiamondReward, position, enemyId); // 월드 픽업 생성
        }
    }
}
