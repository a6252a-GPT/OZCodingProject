using UnityEngine;

namespace TeamProject01.Gameplay
{
    [System.Serializable]
    public struct GrowthStatData // 레벨시스템 → 코어 전달값
    {
        public int LevelDelta; // 레벨 증가량
        public float DamageMultiplierBonus; // 공격력 배율 증가
        public float AttackSpeedMultiplierBonus; // 공격속도 배율 증가
        public float TurnSpeedBonus; // 회전력 증가
        public float RejoinRangeBonus; // 재결합 범위 증가

        public bool HasAnyValue => LevelDelta != 0 || DamageMultiplierBonus != 0f || AttackSpeedMultiplierBonus != 0f || TurnSpeedBonus != 0f || RejoinRangeBonus != 0f; // 적용 여부

        public static GrowthStatData Create(int levelDelta, float damageBonus, float attackSpeedBonus, float turnBonus, float rejoinBonus) // 생성
        {
            GrowthStatData data = default; // 값 준비
            data.LevelDelta = levelDelta; // 레벨 저장
            data.DamageMultiplierBonus = damageBonus; // 공격력 저장
            data.AttackSpeedMultiplierBonus = attackSpeedBonus; // 공격속도 저장
            data.TurnSpeedBonus = turnBonus; // 회전력 저장
            data.RejoinRangeBonus = Mathf.Max(0f, rejoinBonus); // 범위 저장
            return data; // 결과 반환
        }
    }
}
