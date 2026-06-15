using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class NexusController : MonoBehaviour // 넥서스 체력 입구
    {
        public static NexusController Active { get; private set; } // 현재 넥서스

        [Min(1)] public int MaxHealth = 100; // 최대 체력
        [Min(0)] public int CurrentHealth = 100; // 현재 체력

        public bool IsDead { get; private set; } // 사망 여부
        public float HealthRatio => MaxHealth <= 0 ? 0f : Mathf.Clamp01((float)CurrentHealth / MaxHealth); // HUD 비율

        public event Action<int, int> HealthChanged; // 현재/최대 체력
        public event Action<NexusController> Died; // 사망 알림

        private void Awake() // 등록
        {
            Active = this; // 현재 인스턴스
            CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth); // 체력 보정
            IsDead = CurrentHealth <= 0; // 초기 사망 상태
        }

        private void OnDestroy() // 해제
        {
            if (Active == this)
            {
                Active = null; // 참조 제거
            }
        }

        public void ResetHealth() // 체력 초기화
        {
            CurrentHealth = MaxHealth; // 최대 체력 복구
            IsDead = false; // 사망 해제
            HealthChanged?.Invoke(CurrentHealth, MaxHealth); // 변경 알림
        }

        public void ApplyDamage(int amount) // 피해 입구
        {
            if (IsDead || amount <= 0)
            {
                return; // 처리 없음
            }

            CurrentHealth = Mathf.Max(0, CurrentHealth - amount); // 체력 감소
            HealthChanged?.Invoke(CurrentHealth, MaxHealth); // 변경 알림

            if (CurrentHealth <= 0)
            {
                IsDead = true; // 사망 표시
                Died?.Invoke(this); // 사망 알림
            }
        }

        public void Heal(int amount) // 회복 입구
        {
            if (amount <= 0 || IsDead)
            {
                return; // 처리 없음
            }

            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount); // 체력 회복
            HealthChanged?.Invoke(CurrentHealth, MaxHealth); // 변경 알림
        }

        public static bool TryApplyDamage(Transform target, int amount) // 외부 피해 연결
        {
            NexusController nexus = target != null ? target.GetComponentInParent<NexusController>() : null; // 대상 검색
            if (nexus == null)
            {
                nexus = Active; // 등록 넥서스 fallback
            }

            if (nexus == null)
            {
                return false; // 넥서스 없음
            }

            nexus.ApplyDamage(amount); // 피해 적용
            return true; // 호출 성공
        }
    }
}
