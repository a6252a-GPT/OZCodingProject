using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class MonsterDataFlowExample : MonoBehaviour // 몬스터 송수신 예시
    {
        public int EnemyId = 1001; // 더미 몬스터 ID
        [Min(0)] public int ExperienceReward = 10; // 지급 경험치
        [Min(0)] public int GoldReward = 1; // 지급 골드
        public DamageData LastReceivedDamage; // 마지막 수신값
        public RewardData LastSentReward; // 마지막 송신값

        public void ReceiveDamage(DamageData damage) // 데이터를 받는 곳!! 공격 → 몬스터
        {
            LastReceivedDamage = damage; // 받은 DamageData
            Debug.Log($"[MonsterExample] DamageData 수신: Amount={damage.Amount:0.00}, Type={damage.Type}", this); // 수신 로그

            if (!damage.IsValid)
            {
                return; // 피해 없음
            }

            LastSentReward = RewardData.Create(ExperienceReward, GoldReward, EnemyId, transform.position); // 데이터를 보내는 곳!! 몬스터 → 보상 입구
            Debug.Log($"[MonsterExample] RewardData 송신: Exp={LastSentReward.Experience}, Gold={LastSentReward.Gold}", this); // 송신 로그
            GrowthRewardReceiver.SubmitReward(LastSentReward); // 보상 입구 → 코어
        }
    }
}
