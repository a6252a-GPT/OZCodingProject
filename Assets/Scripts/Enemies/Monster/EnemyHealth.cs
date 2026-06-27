using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyHealth : MonoBehaviour //몬스터 사망처리
    {
        [Min(1)]
        [SerializeField] private float maxHp = 3f; // 최대 체력

        public float MaxHp
        {
            get
            {
                return maxHp; // 최대 체력 읽기값
            }
        }

        public float CurrentHp { get; private set; } // 현재 체력
        public bool IsDead { get; private set; } // 몬스터가 죽었는지 확인하는 상태값

        private void Awake()
        {
            CurrentHp = maxHp; // 시작 현재 체력을 최대 체력으로 설정한다.
        }

        public void TakeDamage(float damage) // 외부에서 들어온 피해량을 받아 체력을 감소시키는 함수
        {
            if (IsDead) // 이미 죽은 몬스터라면
            {
                return; // 더 이상 피해 처리를 하지 않고 종료한다.
            }

            if (damage <= 0f) // 피해량이 0 이하라면
            {
                return; // 체력을 줄이지 않고 종료한다.
            }

            CurrentHp -= damage; // 현재 체력에서 들어온 피해량을 빼고, 그 결과를 다시 CurrentHp에 저장한다.

            if (CurrentHp <= 0f) // 체력이 0 이하가 되었다면
            {
                CurrentHp = 0f; // 체력이 음수로 내려가지 않도록 0으로 고정한다.

                IsDead = true; // 죽은 상태로 표시해서 이후 중복 피해나 중복 사망 처리를 막는다.
            }
        }

        public void IncreaseMaxHpByPercentKeepingRatio(float increasePercent) // 현재 체력 비율을 유지하면서 최대 체력을 증가시키는 함수
        {
            if (IsDead) // 이미 죽은 몬스터라면
            {
                return; // 최대 체력을 증가시키지 않고 종료한다.
            }

            if (increasePercent <= 0f) // 증가율이 0 이하라면
            {
                return; // 최대 체력을 증가시키지 않고 종료한다.
            }

            if (maxHp <= 0f) // 최대 체력이 비정상 값이라면
            {
                return; // 계산하지 않고 종료한다.
            }

            float hpRatio = CurrentHp / maxHp; // 현재 체력이 최대 체력에서 차지하는 비율을 계산한다.

            float increaseMultiplier = 1f + increasePercent; // 증가율을 배율로 바꾼다.

            maxHp *= increaseMultiplier; // 최대 체력을 증가시킨다.

            CurrentHp = maxHp * hpRatio; // 기존 체력 비율에 맞춰 현재 체력도 같이 증가시킨다.

            CurrentHp = Mathf.Clamp(CurrentHp, 0f, maxHp); // 현재 체력이 0과 최대 체력 사이에 있도록 제한한다.
        }

        public void ApplyMaxHpMultiplierKeepingRatio(float multiplier) // 웨이브 난이도 체력 배율 적용
        {
            if (IsDead || multiplier <= 0f || Mathf.Approximately(multiplier, 1f))
            {
                return; // 적용 없음
            }

            if (maxHp <= 0f)
            {
                return; // 비정상 체력 방지
            }

            float hpRatio = CurrentHp / maxHp; // 현재 체력 비율 보존
            maxHp = Mathf.Max(1f, maxHp * multiplier); // 최대 체력 배율
            CurrentHp = Mathf.Clamp(maxHp * hpRatio, 0f, maxHp); // 현재 체력도 같은 비율
        }
    }
}
