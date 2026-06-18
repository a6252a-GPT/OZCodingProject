using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [Serializable]
    public struct WeaponCategoryEntry // 추가 무기 종류용 확장 슬롯
    {
        public string CategoryId; // 예: SG03_RapidShot
        public string DisplayName; // UI 표시명
        public WeaponDefinition[] Enhancements; // 해당 종류 강화 목록
    }

    [CreateAssetMenu(menuName = "OZ/Segments/Weapon Catalog", fileName = "WeaponCatalog")]
    public sealed class WeaponCatalogAsset : ScriptableObject // 무기 강화 2단계 카드용 카탈로그
    {
        [Header("Cannon")]
        public WeaponDefinition[] CannonEnhancements = Array.Empty<WeaponDefinition>(); // 캐논 강화 목록

        [Header("Missile")]
        public WeaponDefinition[] MissileEnhancements = Array.Empty<WeaponDefinition>(); // 미사일 강화 목록

        [Header("Additional Categories")]
        public WeaponCategoryEntry[] AdditionalCategories = Array.Empty<WeaponCategoryEntry>(); // 추후 추가 무기 종류

        public bool TryFind(string enhancementId, out WeaponDefinition definition) // 강화 ID 검색
        {
            definition = null; // 기본값
            if (string.IsNullOrWhiteSpace(enhancementId))
            {
                return false; // 검색 불가
            }

            if (TryFindInArray(CannonEnhancements, enhancementId, out definition))
            {
                return true; // 캐논 목록
            }

            if (TryFindInArray(MissileEnhancements, enhancementId, out definition))
            {
                return true; // 미사일 목록
            }

            if (AdditionalCategories == null)
            {
                return false; // 추가 목록 없음
            }

            for (int i = 0; i < AdditionalCategories.Length; i++)
            {
                if (TryFindInArray(AdditionalCategories[i].Enhancements, enhancementId, out definition))
                {
                    return true; // 추가 카테고리
                }
            }

            return false; // 없음
        }

        public bool TryGetEnhancementsForSegment(string targetSegmentId, out WeaponDefinition[] enhancements) // 세그먼트 ID → 강화 목록
        {
            enhancements = Array.Empty<WeaponDefinition>(); // 기본값
            if (string.IsNullOrWhiteSpace(targetSegmentId))
            {
                return false; // 대상 없음
            }

            string normalizedTarget = targetSegmentId.Trim(); // 비교 ID
            if (IsTargetCategory(normalizedTarget, "SG01_Cannon", CannonEnhancements))
            {
                enhancements = CannonEnhancements ?? Array.Empty<WeaponDefinition>();
                return enhancements.Length > 0;
            }

            if (IsTargetCategory(normalizedTarget, "SG02_Missile", MissileEnhancements))
            {
                enhancements = MissileEnhancements ?? Array.Empty<WeaponDefinition>();
                return enhancements.Length > 0;
            }

            if (AdditionalCategories == null)
            {
                return false; // 추가 카테고리 없음
            }

            for (int i = 0; i < AdditionalCategories.Length; i++)
            {
                WeaponCategoryEntry category = AdditionalCategories[i]; // 후보 카테고리
                if (string.Equals(category.CategoryId, normalizedTarget, StringComparison.OrdinalIgnoreCase)
                    && category.Enhancements != null
                    && category.Enhancements.Length > 0)
                {
                    enhancements = category.Enhancements;
                    return true;
                }
            }

            return false; // 매칭 없음
        }

        private static bool IsTargetCategory(string normalizedTarget, string categorySegmentId, WeaponDefinition[] entries) // 고정 카테고리 매칭
        {
            if (!string.Equals(normalizedTarget, categorySegmentId, StringComparison.OrdinalIgnoreCase))
            {
                return false; // 다른 세그먼트
            }

            return entries != null && entries.Length > 0; // 목록 존재
        }

        private static bool TryFindInArray(WeaponDefinition[] entries, string enhancementId, out WeaponDefinition definition) // 배열 내부 검색
        {
            definition = null; // 기본값
            if (entries == null || entries.Length == 0)
            {
                return false; // 비어 있음
            }

            string normalizedId = enhancementId.Trim(); // 비교 ID
            for (int i = 0; i < entries.Length; i++)
            {
                WeaponDefinition candidate = entries[i]; // 후보
                if (candidate != null && string.Equals(candidate.NormalizedId, normalizedId, StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate; // 발견
                    return true;
                }
            }

            return false; // 없음
        }
    }
}
