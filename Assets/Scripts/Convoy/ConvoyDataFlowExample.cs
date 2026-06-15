using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class ConvoyDataFlowExample : MonoBehaviour // 컨보이 송수신 예시
    {
        public CoreDataFlowExample Core; // 코어 입구
        public MonsterDataFlowExample Monster; // 몬스터 입구
        [Min(0f)] public float BaseDamage = 1f; // 기본 피해
        public int SegmentIndex = 1; // 세그먼트 순번
        public DamageData LastSentDamage; // 마지막 송신값

        private void Reset() // 자동 참조
        {
            Core = FindFirstObjectByType<CoreDataFlowExample>(); // 코어 예시 찾기
            Monster = FindFirstObjectByType<MonsterDataFlowExample>(); // 몬스터 예시 찾기
        }

        [ContextMenu("데이터 흐름 실행: 코어→컨보이→몬스터→보상입구→코어→레벨→코어")]
        public void RunExampleFlow() // 전체 흐름 시작
        {
            CoreStatData stats = Core != null ? Core.SendCoreStats() : CoreStatProvider.GetCurrentOrDefault(); // 데이터를 받는 곳!! 코어 → 세그먼트
            float finalDamage = stats.ApplyDamage(BaseDamage); // 컨보이/세그먼트 계산
            LastSentDamage = DamageData.Create(finalDamage, DamageType.Projectile, SegmentIndex, transform.position, gameObject); // 더미 피해값 생성
            Debug.Log($"[ConvoyExample] DamageData 송신: Amount={LastSentDamage.Amount:0.00}, Segment={LastSentDamage.SourceSegmentIndex}", this); // 송신 로그

            if (Monster != null)
            {
                Monster.ReceiveDamage(LastSentDamage); // 몬스터에 전달
            }
        }
    }
}
