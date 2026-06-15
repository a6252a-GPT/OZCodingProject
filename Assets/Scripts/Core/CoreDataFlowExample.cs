using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class CoreDataFlowExample : MonoBehaviour // 코어 송수신 예시
    {
        public CoreStatProvider CoreStats; // 실제 코어값
        public bool ApplyGrowthToCore; // 실제 반영 여부
        public CoreStatData LastSentStats; // 마지막 송신값
        public GrowthStatData LastReceivedGrowth; // 마지막 수신값

        private void Reset() // 자동 참조
        {
            CoreStats = FindFirstObjectByType<CoreStatProvider>(); // 씬 코어 찾기
        }

        public CoreStatData SendCoreStats() // 데이터를 보내는 곳!! 코어 → 세그먼트
        {
            LastSentStats = CoreStats != null ? CoreStats.CurrentStats : CoreStatProvider.GetCurrentOrDefault(); // 보낼 CoreStatData
            Debug.Log($"[CoreExample] CoreStatData 송신: Level={LastSentStats.Level}, Exp={LastSentStats.CurrentExperience}/{LastSentStats.ExperienceToNextLevel}, Gold={LastSentStats.Gold}, DamageMul={LastSentStats.DamageMultiplier:0.00}", this); // 송신 로그
            return LastSentStats; // 세그먼트에 전달
        }

        public void ReceiveGrowth(GrowthStatData growth) // 데이터를 받는 곳!! 레벨 → 코어
        {
            LastReceivedGrowth = growth; // 받은 GrowthStatData
            Debug.Log($"[CoreExample] GrowthStatData 수신: LevelDelta={growth.LevelDelta}, DamageBonus={growth.DamageMultiplierBonus:0.00}", this); // 수신 로그

            if (!ApplyGrowthToCore || CoreStats == null || !growth.HasAnyValue)
            {
                return; // 예시만 확인
            }

            CoreStats.ApplyGrowth(growth); // 실제 코어 반영
        }
    }
}
