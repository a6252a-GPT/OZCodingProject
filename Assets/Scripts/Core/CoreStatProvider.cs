using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class CoreStatProvider : MonoBehaviour // 코어 성장값 보관소
    {
        public static CoreStatProvider Active { get; private set; } // 현재 코어

        [Min(1)] public int CurrentLevel = 1; // 현재 레벨
        [Min(0f)] public float FlatDamageBonus; // 기본 공격력 고정 보너스
        [Min(0f)] public float DamageMultiplier = 1f; // 공격력 배율
        [Min(0.01f)] public float AttackSpeedMultiplier = 1f; // 공격속도 배율
        public float TurnSpeedBonus; // 회전력 보너스
        public float CollisionForceBonus; // 충돌힘 보너스
        [Min(0f)] public float RejoinRangeBonus; // 재결합 범위 보너스
        [Min(0)] public int CurrentExperience; // 현재 레벨 경험치
        [Min(0)] public int TotalExperience; // 누적 경험치
        [Min(0)] public int CurrentGold; // 보유 골드
        [Min(1)] public int BaseExperienceToLevelUp = 5; // 1레벨 필요 경험치
        [Min(0)] public int ExtraExperiencePerLevel = 5; // 레벨당 증가량
        public ConvoyController Convoy; // 세그먼트 추가 입구
        public SegmentCatalogEntry[] SegmentCatalog = Array.Empty<SegmentCatalogEntry>(); // 사용 가능한 세그먼트 목록

        public event Action<CoreStatData> StatsChanged; // 성장값 변경 알림

        public int ExperienceToNextLevel => CalculateRequiredExperience(CurrentLevel); // 다음 레벨 필요량
        public float ExperienceRatio => ExperienceToNextLevel <= 0 ? 0f : Mathf.Clamp01((float)CurrentExperience / ExperienceToNextLevel); // 경험치 비율
        public bool CanLevelUp => CurrentExperience >= ExperienceToNextLevel; // 레벨시스템 판단용
        public CoreStatData CurrentStats => new CoreStatData(CurrentLevel, FlatDamageBonus, DamageMultiplier, AttackSpeedMultiplier, TurnSpeedBonus, RejoinRangeBonus, CollisionForceBonus, CurrentExperience, ExperienceToNextLevel, TotalExperience, CurrentGold); // 현재값

        private readonly List<SegmentUpgradeData> segmentUpgrades = new List<SegmentUpgradeData>(); // 세그먼트별 강화 누적

        private void Awake() // 등록
        {
            Active = this; // 현재 인스턴스
            EnsureConvoyReference(); // 컨보이 참조 보강
        }

        private void OnDestroy() // 해제
        {
            if (Active == this)
            {
                Active = null; // 참조 제거
            }
        }

        public bool ApplyGrowth(GrowthStatData growth) // 레벨시스템 → 코어 성장 적용
        {
            if (!growth.HasAnyValue)
            {
                return false; // 적용 없음
            }

            if (!CanApplyGrowth(growth))
            {
                return false; // 적용 조건 미충족
            }

            ApplyLevelDeltaUnchecked(growth.LevelDelta); // 경험치 소비
            DamageMultiplier = Mathf.Max(0f, DamageMultiplier + growth.DamageMultiplierBonus); // 공격력 누적
            AttackSpeedMultiplier = Mathf.Max(0.01f, AttackSpeedMultiplier + growth.AttackSpeedMultiplierBonus); // 공격속도 누적
            TurnSpeedBonus += growth.TurnSpeedBonus; // 회전력 누적
            CollisionForceBonus += growth.CollisionForceBonus; // 충돌힘 누적
            RejoinRangeBonus = Mathf.Max(0f, RejoinRangeBonus + growth.RejoinRangeBonus); // 범위 누적
            ApplySegmentAdd(growth); // 세그먼트 추가
            ApplySegmentUpgrade(growth.SegmentUpgrade); // 세그먼트 강화
            StatsChanged?.Invoke(CurrentStats); // 변경 알림
            return true; // 적용 성공
        }

        public void ApplyRunStartBonus(RunStartBonusData bonus, float baseTurnSpeed) // 다회차 시작 보너스 적용
        {
            FlatDamageBonus = Mathf.Max(0f, FlatDamageBonus + bonus.BaseAttackFlatBonus); // 기본 공격력
            AttackSpeedMultiplier = Mathf.Max(0.01f, AttackSpeedMultiplier + bonus.AttackSpeedPercentBonus); // 공격속도
            TurnSpeedBonus += Mathf.Max(0f, baseTurnSpeed) * bonus.TurnPercentBonus; // 회전력 비율 → 고정값
            CollisionForceBonus += bonus.CollisionForcePercentBonus; // 충돌힘
            RejoinRangeBonus = Mathf.Max(0f, RejoinRangeBonus + bonus.RejoinRangeBonus); // 재결합
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
            FlatDamageBonus = 0f; // 기본 공격력 초기화
            DamageMultiplier = 1f; // 기본 공격력
            AttackSpeedMultiplier = 1f; // 기본 공격속도
            TurnSpeedBonus = 0f; // 회전력 초기화
            CollisionForceBonus = 0f; // 충돌힘 초기화
            RejoinRangeBonus = 0f; // 재결합 초기화
            CurrentExperience = 0; // 현재 경험치 초기화
            TotalExperience = 0; // 누적 경험치 초기화
            CurrentGold = 0; // 골드 초기화
            segmentUpgrades.Clear(); // 세그먼트 강화 초기화
            StatsChanged?.Invoke(CurrentStats); // 변경 알림
        }

        public static CoreStatData GetCurrentOrDefault() // 공통 조회
        {
            return Active != null ? Active.CurrentStats : CoreStatData.Default; // 없으면 기본값
        }

        public static bool TryGetCurrentStats(out CoreStatData stats) // 명시 조회 입구
        {
            stats = GetCurrentOrDefault(); // 현재값 또는 기본값
            return Active != null; // 실제 코어 존재 여부
        }

        public static bool TryApplyGrowth(GrowthStatData growth) // 공통 성장 입구
        {
            if (Active == null || !growth.HasAnyValue)
            {
                return false; // 적용 대상 없음
            }

            return Active.ApplyGrowth(growth); // 현재 코어 반영
        }

        public SegmentCatalogEntry[] GetSelectableSegmentSnapshot() // 레벨시스템용 추가 후보
        {
            if (SegmentCatalog == null || SegmentCatalog.Length == 0)
            {
                return Array.Empty<SegmentCatalogEntry>(); // 후보 없음
            }

            List<SegmentCatalogEntry> results = new List<SegmentCatalogEntry>(SegmentCatalog.Length); // 결과 목록
            TryGetSelectableSegments(results); // 후보 수집
            return results.ToArray(); // 외부 변경 방지
        }

        public bool TryGetSelectableSegments(List<SegmentCatalogEntry> results) // 레벨시스템용 추가 후보 수집
        {
            if (results == null)
            {
                return false; // 받을 목록 없음
            }

            results.Clear(); // 이전 결과 제거
            if (SegmentCatalog == null)
            {
                return false; // 카탈로그 없음
            }

            for (int i = 0; i < SegmentCatalog.Length; i++)
            {
                SegmentCatalogEntry entry = SegmentCatalog[i]; // 후보
                if (entry.CanShowAsAddChoice && CanAddSegment(entry.SegmentId))
                {
                    results.Add(entry); // 선택 가능 후보
                }
            }

            return results.Count > 0; // 후보 존재
        }

        public bool TryFindSegmentEntry(string segmentId, out SegmentCatalogEntry entry) // ID로 세그먼트 찾기
        {
            entry = default; // 기본값
            if (SegmentCatalog == null || string.IsNullOrWhiteSpace(segmentId))
            {
                return false; // 검색 불가
            }

            string normalizedId = segmentId.Trim(); // 비교 ID
            for (int i = 0; i < SegmentCatalog.Length; i++)
            {
                SegmentCatalogEntry candidate = SegmentCatalog[i]; // 후보
                if (string.Equals(candidate.NormalizedId, normalizedId, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate; // 결과 저장
                    return true; // 발견
                }
            }

            return false; // 없음
        }

        public bool CanAddSegment(string segmentId) // 세그먼트 추가 가능 여부
        {
            EnsureConvoyReference(); // 컨보이 보강
            if (Convoy == null || !TryGetAddableSegmentPrefab(segmentId, out GameObject prefab))
            {
                return false; // 추가 불가
            }

            return Convoy.CanAddSegmentPrefab(prefab); // 길이/프리팹 확인
        }

        public bool TryGetSegmentPrefab(string segmentId, out GameObject prefab) // ID → 프리팹
        {
            prefab = null; // 기본값
            if (!TryFindSegmentEntry(segmentId, out SegmentCatalogEntry entry) || !entry.IsValid)
            {
                return false; // 등록 없음
            }

            prefab = entry.Prefab; // 매핑 결과
            return true; // 성공
        }

        public static SegmentCatalogEntry[] GetSelectableSegmentSnapshotOrEmpty() // 공통 추가 후보 조회
        {
            return Active != null ? Active.GetSelectableSegmentSnapshot() : Array.Empty<SegmentCatalogEntry>(); // 없으면 빈 목록
        }

        public SegmentUpgradeData GetSegmentUpgrade(string segmentId) // 세그먼트 강화 조회
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                return SegmentUpgradeData.None; // 대상 없음
            }

            int index = FindSegmentUpgradeIndex(segmentId); // 기존 강화
            return index >= 0 ? segmentUpgrades[index] : SegmentUpgradeData.None; // 결과
        }

        public static SegmentUpgradeData GetSegmentUpgradeOrDefault(string segmentId) // 공통 세그먼트 강화 조회
        {
            return Active != null ? Active.GetSegmentUpgrade(segmentId) : SegmentUpgradeData.None; // 없으면 기본값
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
        }

        private bool CanApplyGrowth(GrowthStatData growth) // 적용 가능 확인
        {
            if (!CanApplyLevelDelta(growth.LevelDelta))
            {
                return false; // 경험치 부족
            }

            if (growth.HasSegmentAddRequest && !CanApplySegmentAdd(growth))
            {
                return false; // 세그먼트 추가 불가
            }

            if (growth.ChoiceType == GrowthChoiceType.AddSegment && !growth.HasSegmentAddRequest)
            {
                return false; // 추가 대상 없음
            }

            if (growth.ChoiceType == GrowthChoiceType.UpgradeSegment && !growth.HasSegmentUpgrade)
            {
                return false; // 강화 대상 없음
            }

            return true; // 적용 가능
        }

        private bool CanApplyLevelDelta(int levelDelta) // 레벨 증가 가능 확인
        {
            if (levelDelta <= 0)
            {
                return CurrentLevel + levelDelta >= 1; // 최소 레벨
            }

            int previewLevel = CurrentLevel; // 적용 후 레벨 미리보기
            int previewExperience = CurrentExperience; // 적용 후 경험치 미리보기
            for (int i = 0; i < levelDelta; i++)
            {
                int required = CalculateRequiredExperience(previewLevel); // 해당 레벨 필요량
                if (previewExperience < required)
                {
                    return false; // 레벨시스템 조건 판단 실패
                }

                previewExperience -= required; // 경험치 소비
                previewLevel++; // 레벨 증가
            }

            return true; // 적용 성공
        }

        private void ApplyLevelDeltaUnchecked(int levelDelta) // 레벨 증가 반영
        {
            if (levelDelta <= 0)
            {
                CurrentLevel = Mathf.Max(1, CurrentLevel + levelDelta); // 감소/변화 없음 처리
                return; // 경험치 소모 없음
            }

            for (int i = 0; i < levelDelta; i++)
            {
                int required = CalculateRequiredExperience(CurrentLevel); // 현재 필요량
                CurrentExperience -= required; // 경험치 소비
                CurrentLevel = Mathf.Max(1, CurrentLevel + 1); // 레벨 증가
            }

            CurrentExperience = Mathf.Max(0, CurrentExperience); // 안전 보정
        }

        private bool CanApplySegmentAdd(GrowthStatData growth) // 세그먼트 추가 가능 확인
        {
            EnsureConvoyReference(); // 컨보이 보강
            if (Convoy == null || !TryGetAddableSegmentPrefab(growth.SegmentId, out GameObject prefab))
            {
                return false; // 대상 없음
            }

            int addCount = Mathf.Max(1, growth.SegmentAddCount); // 추가 수
            return Convoy.SegmentCount + addCount <= Convoy.MaxSegments && Convoy.CanAddSegmentPrefab(prefab); // 여유 확인
        }

        private void ApplySegmentAdd(GrowthStatData growth) // 세그먼트 추가 적용
        {
            if (!growth.HasSegmentAddRequest)
            {
                return; // 추가 없음
            }

            EnsureConvoyReference(); // 컨보이 보강
            if (!TryGetAddableSegmentPrefab(growth.SegmentId, out GameObject prefab))
            {
                return; // 등록 없음
            }

            int addCount = Mathf.Max(1, growth.SegmentAddCount); // 추가 수
            for (int i = 0; i < addCount; i++)
            {
                Convoy.TryAddSegment(prefab); // 코어 → 컨보이 추가 요청
            }
        }

        private void ApplySegmentUpgrade(SegmentUpgradeData upgrade) // 세그먼트 강화 적용
        {
            if (!upgrade.IsValid)
            {
                return; // 강화 없음
            }

            int index = FindSegmentUpgradeIndex(upgrade.SegmentId); // 기존 강화
            if (index >= 0)
            {
                SegmentUpgradeData current = segmentUpgrades[index]; // 현재값
                current.AddValues(upgrade); // 누적
                segmentUpgrades[index] = current; // 저장
                return; // 완료
            }

            segmentUpgrades.Add(upgrade); // 신규 저장
        }

        private int FindSegmentUpgradeIndex(string segmentId) // 세그먼트 강화 검색
        {
            for (int i = 0; i < segmentUpgrades.Count; i++)
            {
                if (string.Equals(segmentUpgrades[i].SegmentId, segmentId, StringComparison.OrdinalIgnoreCase))
                {
                    return i; // 발견
                }
            }

            return -1; // 없음
        }

        private bool TryGetAddableSegmentPrefab(string segmentId, out GameObject prefab) // 추가용 ID → 프리팹
        {
            prefab = null; // 기본값
            if (!TryFindSegmentEntry(segmentId, out SegmentCatalogEntry entry) || !entry.CanShowAsAddChoice)
            {
                return false; // 선택 불가
            }

            prefab = entry.Prefab; // 프리팹
            return prefab != null; // 최종 확인
        }

        private void EnsureConvoyReference() // 컨보이 참조 보강
        {
            if (Convoy == null)
            {
                Convoy = FindFirstObjectByType<ConvoyController>(); // 씬 컨보이 찾기
            }
        }

        private int CalculateRequiredExperience(int level) // 필요 경험치 계산
        {
            int levelIndex = Mathf.Max(0, level - 1); // 1레벨 기준
            return Mathf.Max(1, BaseExperienceToLevelUp + ExtraExperiencePerLevel * levelIndex); // 선형 증가
        }
    }
}
