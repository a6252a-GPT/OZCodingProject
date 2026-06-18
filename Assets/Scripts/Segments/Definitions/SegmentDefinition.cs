using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [CreateAssetMenu(menuName = "OZ/Segments/Segment Definition", fileName = "SG##_Name")]
    public sealed class SegmentDefinition : ScriptableObject // 세그먼트 하나의 등록표
    {
        public string SegmentId; // 예: SG01_Cannon
        public string DisplayName; // UI 이름
        [TextArea(2, 4)] public string Description; // UI 설명

        [Header("Simple Rules")]
        public bool StarterOnly; // 스타터 전용
        public string SharedUpgradeSegmentId; // 스타터가 공유할 강화 ID
        public bool UseLevels = true; // 레벨 사용 여부
        [Min(1)] public int MaxLevel = 1; // 최대 레벨
        public bool CanAddByLevelChoice = true; // 추가 선택지 노출
        public bool CanUpgradeByLevelChoice = true; // 강화 선택지 노출

        [Header("Level Prefabs")]
        public SegmentLevelDefinition[] Levels = Array.Empty<SegmentLevelDefinition>(); // Lv 프리팹 목록

        public string NormalizedId => string.IsNullOrWhiteSpace(SegmentId) ? string.Empty : SegmentId.Trim(); // 비교 ID
        public string UpgradeId => string.IsNullOrWhiteSpace(SharedUpgradeSegmentId) ? NormalizedId : SharedUpgradeSegmentId.Trim(); // 강화 ID
        public bool HasId => !string.IsNullOrWhiteSpace(SegmentId); // ID 존재

        public bool TryGetLevel(int requestedLevel, out SegmentLevelDefinition level) // 레벨 데이터 찾기
        {
            level = default; // 기본값
            if (Levels == null || Levels.Length == 0)
            {
                return false; // 데이터 없음
            }

            int targetLevel = UseLevels ? Mathf.Clamp(requestedLevel, 1, Mathf.Max(1, MaxLevel)) : 1; // 레벨 보정
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].Level == targetLevel && Levels[i].HasSegmentPrefab)
                {
                    level = Levels[i]; // 정확한 레벨
                    return true;
                }
            }

            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].HasSegmentPrefab)
                {
                    level = Levels[i]; // fallback
                    return true;
                }
            }

            return false; // 사용 가능 프리팹 없음
        }

        public bool TryGetSegmentPrefab(int requestedLevel, out GameObject prefab) // 레벨 프리팹 찾기
        {
            prefab = null; // 기본값
            if (!TryGetLevel(requestedLevel, out SegmentLevelDefinition level))
            {
                return false; // 레벨 없음
            }

            prefab = level.SegmentPrefab; // 프리팹 반환
            return prefab != null;
        }

        public SegmentCatalogEntry ToCatalogEntry() // 기존 코어 카탈로그 호환
        {
            TryGetSegmentPrefab(1, out GameObject prefab); // 추가는 기본 Lv1
            SegmentCatalogEntry entry = default; // 변환값
            entry.SegmentId = NormalizedId;
            entry.DisplayName = DisplayName;
            entry.Description = Description;
            entry.Prefab = prefab;
            entry.IsSelectable = !StarterOnly && CanAddByLevelChoice;
            entry.IsUpgradeable = !StarterOnly && CanUpgradeByLevelChoice;
            return entry;
        }
    }
}
