using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class CoreStatProvider : MonoBehaviour // 코어 성장값 보관소
    {
        public static CoreStatProvider Active { get; private set; } // 현재 코어
        [Header("코어 레벨")]
        [Min(1)] public int CurrentLevel = 1; // 현재 레벨
        [Header("코어 공격력 보너스")]
        [Min(0f)] public float FlatDamageBonus; // 기본 공격력 고정 보너스
        [Header("코어 공격력 배율")]
        [Min(0f)] public float DamageMultiplier = 1f; // 공격력 배율
        [Header("코어 공격속도 배율")]
        [Min(0.01f)] public float AttackSpeedMultiplier = 1f; // 공격속도 배율
        [Header("코어 회전력 보너스")]
        public float TurnSpeedBonus; // 회전력 보너스
        [Header("코어 충돌힘 보너스")]
        public float CollisionForceBonus; // 충돌힘 보너스
        [Header("코어 재결합 범위 보너스")]
        [Min(0f)] public float RejoinRangeBonus; // 재결합 범위 보너스
        [Header("보유 골드")]
        [Min(0)] public int CurrentGold; // 보유 골드
        [Header("")]
        [Min(0)] public int CurrentExperience; // 현재 레벨 경험치
        [Min(0)] public int TotalExperience; // 누적 경험치        
        [Min(1)] public int BaseExperienceToLevelUp = 5; // 1레벨 필요 경험치
        [Min(0)] public int ExtraExperiencePerLevel = 5; // 레벨당 증가량
        public ConvoyController Convoy; // 세그먼트 추가 입구
        public SegmentCatalogAsset SegmentCatalogAsset; // 새 세그먼트 데이터에셋 목록
        public WeaponCatalogAsset WeaponCatalogAsset; // CardUI 무기 강화 카드 목록 조회용 카탈로그
        public SegmentCatalogEntry[] SegmentCatalog = Array.Empty<SegmentCatalogEntry>(); // 사용 가능한 세그먼트 목록

        public event Action<CoreStatData> StatsChanged; // 성장값 변경 알림

        public int ExperienceToNextLevel => CalculateRequiredExperience(CurrentLevel); // 다음 레벨 필요량
        public float ExperienceRatio => ExperienceToNextLevel <= 0 ? 0f : Mathf.Clamp01((float)CurrentExperience / ExperienceToNextLevel); // 경험치 비율
        public bool CanLevelUp => CurrentExperience >= ExperienceToNextLevel; // 레벨시스템 판단용
        public CoreStatData CurrentStats => new CoreStatData(CurrentLevel, FlatDamageBonus, DamageMultiplier, AttackSpeedMultiplier, TurnSpeedBonus, RejoinRangeBonus, CollisionForceBonus, CurrentExperience, ExperienceToNextLevel, TotalExperience, CurrentGold); // 현재값

        private readonly List<SegmentUpgradeData> segmentUpgrades = new List<SegmentUpgradeData>(); // 세그먼트별 강화 누적
        private readonly List<WeaponStatBonusEntry> weaponStatBonuses = new List<WeaponStatBonusEntry>(); // 세그먼트 ID별 무기 강화 보너스 누적 저장
        private int levelUpCardCycleIndex; // 레벨업 카드 순환 (0=스탯, 1=무기강화, 2=세그먼트)

        public int LevelUpCardCycleIndex => levelUpCardCycleIndex; // CardUI에서 현재 순환 위치 조회

        public bool IsLevelUpChoicePending => pendingLevelUpChoiceCommitted; // 레벨업 카드 선택 UI 표시 중 여부

        public void AdvanceLevelUpCardCycle() // 카드 1회 선택 완료 후 다음 종류로 이동
        {
            levelUpCardCycleIndex++; // 0→1→2→0 순환
        }

        // 경험치 충족 시 카드 UI만 열고, 경험치/레벨은 카드 선택 시 소비
        public bool TryBeginLevelUpChoice() // 레벨업 카드 UI 오픈 허용
        {
            if (pendingLevelUpChoiceCommitted)
            {
                return true; // 이미 UI 표시 중
            }

            if (!CanLevelUp || !CanApplyLevelDelta(1))
            {
                return false; // 경험치 부족 등 레벨업 불가
            }

            pendingLevelUpChoiceCommitted = true; // UI 중복 오픈 방지 (경험치는 아직 미소비)
            return true; // ExpBarController에서 패널 오픈 가능
        }

        public void CompleteLevelUpChoice() // 카드 선택·적용 완료 후 호출
        {
            pendingLevelUpChoiceCommitted = false; // 선택 UI 종료
            AdvanceLevelUpCardCycle(); // 다음 레벨업 카드 종류로 이동
            StatsChanged?.Invoke(CurrentStats); // UI·연속 레벨업 재판단
        }

        public void CancelLevelUpChoice() // 선택 없이 UI만 닫힌 경우
        {
            pendingLevelUpChoiceCommitted = false; // UI 상태 해제
            StatsChanged?.Invoke(CurrentStats); // 경험치 유지 상태로 UI 재오픈 허용
        }

        private bool pendingLevelUpChoiceCommitted; // 레벨업 카드 UI 표시 중 (경험치 미소비)

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

            ApplyLevelDeltaIfNeeded(growth.LevelDelta); // 카드 선택 시 플레이어 레벨·경험치 소비
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

        public bool CanSpendGold(int amount) // 골드 소비 가능 확인
        {
            return amount <= 0 || CurrentGold >= amount; // 0 이하면 무료 처리
        }

        public bool TrySpendGold(int amount) // 골드 소비
        {
            if (amount <= 0)
            {
                return true; // 무료
            }

            if (CurrentGold < amount)
            {
                return false; // 골드 부족
            }

            CurrentGold = Mathf.Max(0, CurrentGold - amount); // 차감
            StatsChanged?.Invoke(CurrentStats); // HUD 갱신
            return true; // 소비 성공
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
            weaponStatBonuses.Clear(); // 무기 강화 보너스 초기화
            levelUpCardCycleIndex = 0; // 레벨업 카드 순환 초기화
            pendingLevelUpChoiceCommitted = false; // 레벨업 카드 UI 상태 초기화
            StatsChanged?.Invoke(CurrentStats); // 변경 알림
        }

        public void DebugAddLevel(int amount) // CoreTest 디버그 레벨 증가
        {
            int delta = Mathf.Max(1, amount); // 최소 1레벨
            CurrentLevel = Mathf.Max(1, CurrentLevel + delta); // 레벨 증가
            CurrentExperience = 0; // 디버그 레벨업 후 자동 카드 재오픈 방지
            pendingLevelUpChoiceCommitted = false; // 선택 대기 해제
            StatsChanged?.Invoke(CurrentStats); // HUD 갱신
        }

        public void DebugAddGold(int amount) // CoreTest 디버그 골드 지급
        {
            CurrentGold = Mathf.Max(0, CurrentGold + Mathf.Max(0, amount)); // 골드 증가
            StatsChanged?.Invoke(CurrentStats); // HUD 갱신
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

        public static bool TrySpendCurrentGold(int amount) // 공통 골드 소비 입구
        {
            return Active != null && Active.TrySpendGold(amount); // 현재 코어에서 차감
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
            SegmentCatalogEntry[] catalog = GetCatalogEntries(); // 데이터에셋 우선
            if (catalog.Length == 0)
            {
                return Array.Empty<SegmentCatalogEntry>(); // 후보 없음
            }

            List<SegmentCatalogEntry> results = new List<SegmentCatalogEntry>(catalog.Length); // 결과 목록
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
            SegmentCatalogEntry[] catalog = GetCatalogEntries(); // 데이터에셋 우선
            if (catalog.Length == 0)
            {
                return false; // 카탈로그 없음
            }

            for (int i = 0; i < catalog.Length; i++)
            {
                SegmentCatalogEntry entry = catalog[i]; // 후보
                if (entry.CanShowAsAddChoice && CanAddSegment(entry.SegmentId))
                {
                    results.Add(entry); // 선택 가능 후보
                }
            }

            return results.Count > 0; // 후보 존재
        }

        // 세그먼트 ADD 카드용 후보 수집: 추가 가능 또는 레벨업 가능이면 후보에 노출
        public bool TryGetSegmentChoiceCandidates(List<SegmentCatalogEntry> results)
        {
            if (results == null)
            {
                return false; // 받을 목록 없음
            }

            results.Clear(); // 이전 결과 제거
            SegmentCatalogEntry[] catalog = GetCatalogEntries(); // 데이터에셋 우선
            if (catalog.Length == 0)
            {
                return false; // 카탈로그 없음
            }

            for (int i = 0; i < catalog.Length; i++)
            {
                SegmentCatalogEntry entry = catalog[i]; // 후보
                bool canAdd = entry.CanShowAsAddChoice && CanAddSegment(entry.SegmentId); // 추가 가능 여부
                bool canLevelUp = entry.CanShowAsUpgradeChoice && CanLevelUpSegmentModel(entry.SegmentId); // 모델 레벨업 가능 여부
                if (canAdd || canLevelUp)
                {
                    results.Add(entry); // 세그먼트 3번 카드 후보 등록
                }
            }

            return results.Count > 0; // 후보 존재
        }


        // 무기 강화 1단계 - Segment Catalog 풀 전체를 후보로 반환
        public bool TryGetWeaponEnhanceChoiceCandidates(List<SegmentCatalogEntry> results)
        {
            if (results == null)
            {
                return false; // 받을 목록 없음
            }

            results.Clear(); // 이전 결과 제거
            SegmentCatalogEntry[] catalog = GetCatalogEntries(); // 데이터에셋 우선
            if (catalog.Length == 0)
            {
                return false; // 카탈로그 없음
            }

            for (int i = 0; i < catalog.Length; i++)
            {
                SegmentCatalogEntry entry = catalog[i]; // 카탈로그 풀 항목
                if (!entry.HasId)
                {
                    continue; // ID 없음
                }

                results.Add(entry); // Cannon/Missile 등 풀 전체 노출
            }

            return results.Count > 0; // 후보 존재
        }

        // 현재 세그먼트 모델 레벨/최대 레벨 조회
        public bool TryGetSegmentModelLevelInfo(string segmentId, out int currentLevel, out int maxLevel)
        {
            currentLevel = 1; // 기본 현재 레벨
            maxLevel = 1; // 기본 최대 레벨
            EnsureConvoyReference(); // 컨보이 보강
            if (Convoy == null || !TryFindSegmentDefinition(segmentId, out SegmentDefinition definition))
            {
                return false; // 조회 불가
            }

            maxLevel = Mathf.Max(1, definition.MaxLevel); // 정의 최대 레벨
            currentLevel = Mathf.Clamp(Convoy.GetCurrentSegmentLevel(definition.NormalizedId, definition), 1, maxLevel); // 컨보이 현재 레벨
            return true; // 조회 성공
        }

        // 해당 세그먼트가 현재 붙어 있고 모델 레벨업 가능한지 확인
        public bool CanLevelUpSegmentModel(string segmentId)
        {
            EnsureConvoyReference(); // 컨보이 보강
            if (Convoy == null || !TryFindSegmentDefinition(segmentId, out SegmentDefinition definition))
            {
                return false; // 정의/컨보이 없음
            }

            string targetId = definition.NormalizedId; // 비교 ID
            if (!definition.UseLevels || definition.MaxLevel <= 1)
            {
                return false; // 레벨 미사용
            }

            if (!definition.CanUpgradeByLevelChoice)
            {
                return false; // 레벨 선택 강화 비허용
            }

            if (Convoy.CountAttachedSegments(targetId) <= 0)
            {
                return false; // 붙어 있는 해당 세그먼트 없음
            }

            int currentLevel = Convoy.GetCurrentSegmentLevel(targetId, definition); // 현재 모델 레벨
            int maxLevel = Mathf.Max(1, definition.MaxLevel); // 최대 레벨
            if (currentLevel >= maxLevel)
            {
                return false; // 이미 만렙
            }

            return definition.TryGetLevel(currentLevel + 1, out _); // 다음 레벨 프리팹 존재 여부
        }

        // 카드 2차 선택에서 세그먼트 추가를 코어 경유로 적용
        public bool TryApplySegmentAddChoice(string segmentId, int levelDelta, int count = 1)
        {
            GrowthStatData growth = GrowthStatData.CreateAddSegment(levelDelta, segmentId, count); // 추가 성장 데이터
            return ApplyGrowth(growth); // 기존 성장 적용 흐름 사용
        }

        // 카드 2차 선택에서 해당 세그먼트 모델 레벨업을 코어 경유로 적용
        public bool TryApplySegmentLevelUpChoice(string segmentId, int levelDelta)
        {
            EnsureConvoyReference(); // 컨보이 보강
            if (Convoy == null || !CanConsumeLevelDeltaForChoice(levelDelta) || !CanLevelUpSegmentModel(segmentId))
            {
                return false; // 경험치 부족 또는 레벨업 불가
            }

            if (!TryFindSegmentDefinition(segmentId, out SegmentDefinition definition))
            {
                return false; // 정의 없음
            }

            int changed = Convoy.LevelUpAttachedSegments(definition.NormalizedId, definition, out _); // 같은 ID 세그먼트 전체 Lv+1
            if (changed <= 0)
            {
                return false; // 실제 변경 없음
            }

            ApplyLevelDeltaIfNeeded(levelDelta); // 카드 선택 시 플레이어 레벨·경험치 소비
            StatsChanged?.Invoke(CurrentStats); // UI 갱신
            return true; // 적용 성공
        }

        // 무기 강화 2단계 - 선택한 WeaponDefinition 보너스를 코어에 누적
        public bool TryApplyWeaponEnhancementChoice(string segmentId, int levelDelta, WeaponDefinition definition, float bonusMultiplier = 1f)
        {
            if (definition == null)
            {
                Debug.LogWarning("무기 강화 실패."); // 카드 데이터 없음
                return false;
            }

            if (!definition.HasAnyStatBonus)
            {
                Debug.LogWarning($"무기 강화 실패: {definition.name} ", definition); // 보너스 수치 0
                return false;
            }

            if (!CanConsumeLevelDeltaForChoice(levelDelta))
            {
                Debug.LogWarning($"무기 강화 실패: 레벨/경험치 부족 (LevelDelta={levelDelta}, Level={CurrentLevel}, Exp={CurrentExperience}/{ExperienceToNextLevel})");
                return false; // 경험치 부족
            }

            string normalizedSegmentId = definition.NormalizedTargetSegmentId; // 저장 키 = 대상 세그먼트 ID
            if (string.IsNullOrWhiteSpace(normalizedSegmentId))
            {
                Debug.LogWarning($"무기 강화 실패: {definition.name}", definition); // TargetSegmentId 비어 있음
                return false;
            }

            string requestedSegmentId = string.IsNullOrWhiteSpace(segmentId) ? string.Empty : segmentId.Trim(); // UI에서 고른 세그먼트
            if (!string.Equals(requestedSegmentId, normalizedSegmentId, StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"무기 강화 실패: 세그먼트 불일치 (선택={requestedSegmentId}, 대상={normalizedSegmentId}, 카드={definition.name})", definition);
                return false; // 1단계 선택과 카드 대상 불일치
            }

            int index = FindWeaponStatBonusIndex(normalizedSegmentId); // 기존 누적 검색
            WeaponStatBonusData bonus = index >= 0 ? weaponStatBonuses[index].Bonus : default; // 현재 누적값
            bonus.AddDefinition(definition, bonusMultiplier); // WeaponDefinition 보너스 합산 (레어/유니크 배율)
            if (index >= 0)
            {
                weaponStatBonuses[index] = new WeaponStatBonusEntry(normalizedSegmentId, bonus); // 기존 항목 갱신
            }
            else
            {
                weaponStatBonuses.Add(new WeaponStatBonusEntry(normalizedSegmentId, bonus)); // 신규 세그먼트 등록
            }

            ApplyLevelDeltaIfNeeded(levelDelta); // 카드 선택 시 플레이어 레벨·경험치 소비
            StatsChanged?.Invoke(CurrentStats); // HUD·전투 스탯 갱신
            return true; // 적용 성공
        }

        public WeaponStatBonusData GetWeaponStatBonus(string segmentId) // 세그먼트별 누적 무기 강화 조회
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                return default; // ID 없음
            }

            int index = FindWeaponStatBonusIndex(segmentId); // 리스트 검색
            return index >= 0 ? weaponStatBonuses[index].Bonus : default; // 없으면 0 보너스
        }

        public static WeaponStatBonusData GetWeaponStatBonusOrDefault(string segmentId) // GenericSegmentWeapon 등에서 호출
        {
            return Active != null ? Active.GetWeaponStatBonus(segmentId) : default; // 코어 없으면 기본값
        }

        public bool TryFindSegmentEntry(string segmentId, out SegmentCatalogEntry entry) // ID로 세그먼트 찾기
        {
            entry = default; // 기본값
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                return false; // 검색 불가
            }

            string normalizedId = segmentId.Trim(); // 비교 ID
            SegmentCatalogEntry[] catalog = GetCatalogEntries(); // 데이터에셋 우선
            for (int i = 0; i < catalog.Length; i++)
            {
                SegmentCatalogEntry candidate = catalog[i]; // 후보
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

        private bool CanConsumeLevelDeltaForChoice(int levelDelta) // 카드 선택 시 경험치로 레벨업 가능한지
        {
            return levelDelta <= 0 || CanApplyLevelDelta(levelDelta); // 0 이하면 소비 없음, 아니면 경험치 검사
        }

        private void ApplyLevelDeltaIfNeeded(int levelDelta) // 카드 선택 시 플레이어 레벨·경험치 반영
        {
            if (levelDelta <= 0)
            {
                return; // 레벨 변동 없음
            }

            ApplyLevelDeltaUnchecked(levelDelta); // 경험치 차감 + CurrentLevel 증가
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

        private int FindWeaponStatBonusIndex(string segmentId) // weaponStatBonuses 리스트에서 세그먼트 ID 검색
        {
            if (string.IsNullOrWhiteSpace(segmentId))
            {
                return -1; // 검색 불가
            }

            string normalizedId = segmentId.Trim(); // 비교용 ID 정규화
            for (int i = 0; i < weaponStatBonuses.Count; i++)
            {
                if (string.Equals(weaponStatBonuses[i].SegmentId, normalizedId, StringComparison.OrdinalIgnoreCase))
                {
                    return i; // 동일 ID 항목 발견
                }
            }

            return -1; // 아직 강화 기록 없음
        }

        private readonly struct WeaponStatBonusEntry // 세그먼트 ID + 누적 WeaponStatBonusData 한 쌍
        {
            public readonly string SegmentId; // 대상 세그먼트 ID (예: SG01_Cannon)
            public readonly WeaponStatBonusData Bonus; // 해당 세그먼트 누적 강화 보너스

            public WeaponStatBonusEntry(string segmentId, WeaponStatBonusData bonus)
            {
                SegmentId = segmentId; // Dictionary 대신 리스트 키
                Bonus = bonus; // 누적 보너스 스냅샷
            }
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

        // 카탈로그 데이터에셋에서 세그먼트 정의 찾기
        private bool TryFindSegmentDefinition(string segmentId, out SegmentDefinition definition)
        {
            definition = null; // 기본값
            if (SegmentCatalogAsset != null && SegmentCatalogAsset.TryFind(segmentId, out definition))
            {
                return true; // 데이터에셋에서 발견
            }

            return false; // 배열 fallback은 프리팹만 있어서 모델 레벨업 정의를 알 수 없음
        }

        private SegmentCatalogEntry[] GetCatalogEntries() // 현재 세그먼트 목록
        {
            if (SegmentCatalogAsset != null)
            {
                return SegmentCatalogAsset.BuildCatalogEntries(); // 데이터에셋 사용
            }

            return SegmentCatalog ?? Array.Empty<SegmentCatalogEntry>(); // 기존 배열 fallback
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
