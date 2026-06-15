using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class CoreStatProvider : MonoBehaviour // 코어 성장값 보관소
    {
        public static CoreStatProvider Active { get; private set; } // 현재 코어

        [Min(1)] public int CurrentLevel = 1; // 현재 레벨
        [Min(0f)] public float DamageMultiplier = 1f; // 공격력 배율
        [Min(0.01f)] public float AttackSpeedMultiplier = 1f; // 공격속도 배율
        public float TurnSpeedBonus; // 회전력 보너스
        [Min(0f)] public float RejoinRangeBonus; // 재결합 범위 보너스
        [Min(0)] public int CurrentExperience; // 현재 레벨 경험치
        [Min(0)] public int TotalExperience; // 누적 경험치
        [Min(0)] public int CurrentGold; // 보유 골드
        [Min(1)] public int BaseExperienceToLevelUp = 5; // 1레벨 필요 경험치
        [Min(0)] public int ExtraExperiencePerLevel = 5; // 레벨당 증가량

        public event Action<CoreStatData> StatsChanged; // 성장값 변경 알림

        public int ExperienceToNextLevel => CalculateRequiredExperience(CurrentLevel); // 다음 레벨 필요량
        public float ExperienceRatio => ExperienceToNextLevel <= 0 ? 0f : Mathf.Clamp01((float)CurrentExperience / ExperienceToNextLevel); // 경험치 비율
        public CoreStatData CurrentStats => new CoreStatData(CurrentLevel, DamageMultiplier, AttackSpeedMultiplier, TurnSpeedBonus, RejoinRangeBonus, CurrentExperience, ExperienceToNextLevel, TotalExperience, CurrentGold); // 현재값

        private void Awake() // 등록
        {
            Active = this; // 현재 인스턴스
        }

        private void OnDestroy() // 해제
        {
            if (Active == this)
            {
                Active = null; // 참조 제거
            }
        }

        public void ApplyGrowth(GrowthStatData growth) // 성장 적용
        {
            if (!growth.HasAnyValue)
            {
                return; // 적용 없음
            }

            CurrentLevel = Mathf.Max(1, CurrentLevel + growth.LevelDelta); // 레벨 누적
            DamageMultiplier = Mathf.Max(0f, DamageMultiplier + growth.DamageMultiplierBonus); // 공격력 누적
            AttackSpeedMultiplier = Mathf.Max(0.01f, AttackSpeedMultiplier + growth.AttackSpeedMultiplierBonus); // 공격속도 누적
            TurnSpeedBonus += growth.TurnSpeedBonus; // 회전력 누적
            RejoinRangeBonus = Mathf.Max(0f, RejoinRangeBonus + growth.RejoinRangeBonus); // 범위 누적
            StatsChanged?.Invoke(CurrentStats); // 변경 알림
        }

        public bool ApplyReward(RewardData reward) // 데이터를 받는 곳!! 보상 입구 → 코어
        {
            if (!reward.IsValid)
            {
                return false; // 지급 없음
            }

            AddExperience(reward.Experience); // 경험치 코어 누적
            CurrentGold += reward.Gold; // 골드 코어 누적
            StatsChanged?.Invoke(CurrentStats); // HUD 갱신
            return true; // 적용 성공
        }

        public void ResetStats() // 성장값 초기화
        {
            CurrentLevel = 1; // 기본 레벨
            DamageMultiplier = 1f; // 기본 공격력
            AttackSpeedMultiplier = 1f; // 기본 공격속도
            TurnSpeedBonus = 0f; // 회전력 초기화
            RejoinRangeBonus = 0f; // 재결합 초기화
            CurrentExperience = 0; // 현재 경험치 초기화
            TotalExperience = 0; // 누적 경험치 초기화
            CurrentGold = 0; // 골드 초기화
            StatsChanged?.Invoke(CurrentStats); // 변경 알림
        }

        public static CoreStatData GetCurrentOrDefault() // 공통 조회
        {
            return Active != null ? Active.CurrentStats : CoreStatData.Default; // 없으면 기본값
        }

        public static bool TryApplyGrowth(GrowthStatData growth) // 공통 성장 입구
        {
            if (Active == null || !growth.HasAnyValue)
            {
                return false; // 적용 대상 없음
            }

            Active.ApplyGrowth(growth); // 현재 코어 반영
            return true; // 적용 성공
        }

        internal static bool TryApplyReward(RewardData reward) // 보상 입구 내부용
        {
            if (Active == null || !reward.IsValid)
            {
                return false; // 적용 대상 없음
            }

            return Active.ApplyReward(reward); // 코어 보상 반영
        }

        private void AddExperience(int amount) // 경험치 처리
        {
            if (amount <= 0)
            {
                return; // 증가 없음
            }

            TotalExperience += amount; // 총 경험치 누적
            CurrentExperience += amount; // 현재 경험치 누적
            ProcessLevelUps(); // 레벨업 확인
        }

        private void ProcessLevelUps() // 코어 레벨업 처리
        {
            int guard = 0; // 무한 루프 방지
            while (CurrentExperience >= ExperienceToNextLevel && guard < 100)
            {
                int required = ExperienceToNextLevel; // 현재 필요량
                CurrentExperience -= required; // 경험치 소비
                CurrentLevel = Mathf.Max(1, CurrentLevel + 1); // 코어 레벨 증가
                guard++; // 안전 카운트
            }
        }

        private int CalculateRequiredExperience(int level) // 필요 경험치 계산
        {
            int levelIndex = Mathf.Max(0, level - 1); // 1레벨 기준
            return Mathf.Max(1, BaseExperienceToLevelUp + ExtraExperiencePerLevel * levelIndex); // 선형 증가
        }
    }
}
