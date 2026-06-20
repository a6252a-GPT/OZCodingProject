using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [CreateAssetMenu(menuName = "OZ/Segments/Weapon Definition", fileName = "WE_##_Name")]
    public sealed class WeaponDefinition : ScriptableObject // 무기(세그먼트) 강화 카드 1종
    {
        public string EnhancementId; // 예: WE_Cannon_DamageBoost
        public string DisplayName; // UI 이름
        [TextArea(2, 4)] public string Description; // UI 설명
        public string TargetSegmentId; // 적용 대상 세그먼트 ID (예: SG01_Cannon)

        [Header("Attack Stat Bonuses")]
        [Header("공격력 보너스")]
        [Min(0f)] public float BaseDamage; // 기본 피해량 보너스
        [Header("투사체 속도 보너스")]
        [Min(0f)] public float ProjectileSpeed; // 투사체 속도 보너스
        [Header("관통 수 보너스")]
        [Min(0)] public int PierceCount; // 관통 수 보너스
        [Header("폭발 반경 보너스")]
        [Min(0f)] public float ExplosionRadius; // 폭발 반경 보너스

        [Header("추후 — 카드 UI (비우면 CardUI addSegmentCards + 색상 틴트)")]
        public GameObject CardPrefabOverride; // 이 강화 전용 카드 프리팹 (텍스트·이미지 레이아웃)
        public GameObject RareCardPrefabOverride; // 레어 전용 프리팹
        public GameObject UniqueCardPrefabOverride; // 유니크 전용 프리팹
        public Sprite CardIconOverride; // 카드 아이콘 (프리팹 Image/Icon 에 주입, 선택)

        public readonly struct CardSpawnResolve // 생성 시 사용할 프리팹 + 색상 틴트 여부
        {
            public CardSpawnResolve(GameObject prefab, bool applyFallbackVisual)
            {
                Prefab = prefab;
                ApplyFallbackVisual = applyFallbackVisual;
            }

            public GameObject Prefab { get; } // Instantiate 대상
            public bool ApplyFallbackVisual { get; } // true = 기본 껍데기 + 등급 색 틴트
        }

        public string NormalizedId => string.IsNullOrWhiteSpace(EnhancementId) ? string.Empty : EnhancementId.Trim(); // 비교 ID
        public string NormalizedTargetSegmentId => string.IsNullOrWhiteSpace(TargetSegmentId) ? string.Empty : TargetSegmentId.Trim(); // 대상 ID
        public bool HasId => !string.IsNullOrWhiteSpace(EnhancementId); // ID 존재
        public bool HasTarget => !string.IsNullOrWhiteSpace(TargetSegmentId); // 대상 존재
        public bool HasAnyStatBonus => BaseDamage > 0f || ProjectileSpeed > 0f || PierceCount > 0 || ExplosionRadius > 0f; // 수치 보너스 존재

        public void ApplyBonuses(ref float baseDamage, ref float projectileSpeed, ref int pierceCount, ref float explosionRadius) // 공격 프로필 수치에 보너스 반영
        {
            baseDamage = Mathf.Max(0f, baseDamage + BaseDamage); // 피해량 합산
            projectileSpeed = Mathf.Max(0.1f, projectileSpeed + ProjectileSpeed); // 속도 합산
            pierceCount = Mathf.Max(0, pierceCount + PierceCount); // 관통 합산
            explosionRadius = Mathf.Max(0.1f, explosionRadius + ExplosionRadius); // 폭발 반경 합산
        }

        public CardSpawnResolve ResolveCardSpawn(StatUpgrade.StatCardTier tier, GameObject defaultTemplate, SegmentAddCard templatePresentation) // 등급·에셋·템플릿 → Instantiate 대상
        {
            if (defaultTemplate == null)
            {
                return new CardSpawnResolve(null, false); // 템플릿 없음
            }

            switch (tier)
            {
                case StatUpgrade.StatCardTier.Unique:
                    if (UniqueCardPrefabOverride != null)
                    {
                        return new CardSpawnResolve(UniqueCardPrefabOverride, false); // 유니크 전용
                    }

                    break;
                case StatUpgrade.StatCardTier.Rare:
                    if (RareCardPrefabOverride != null)
                    {
                        return new CardSpawnResolve(RareCardPrefabOverride, false); // 레어 전용
                    }

                    break;
            }

            if (CardPrefabOverride != null)
            {
                return new CardSpawnResolve(CardPrefabOverride, false); // 강화별 공통 프리팹
            }

            if (templatePresentation != null)
            {
                GameObject templateTierPrefab = templatePresentation.ResolveSpawnPrefabForTier(tier, defaultTemplate); // addSegmentCards 템플릿 등급 프리팹
                if (templateTierPrefab != null && templateTierPrefab != defaultTemplate)
                {
                    return new CardSpawnResolve(templateTierPrefab, false); // 슬롯 템플릿 등급 교체
                }
            }

            return new CardSpawnResolve(defaultTemplate, true); // 기본 껍데기 + 색상 틴트
        }
    }

    [Serializable]
    public struct WeaponStatBonusData // 세그먼트별 무기 강화 누적값
    {
        public float BaseDamageBonus; // 누적 피해 보너스
        public float ProjectileSpeedBonus; // 누적 투사체 속도 보너스
        public int PierceCountBonus; // 누적 관통 보너스
        public float ExplosionRadiusBonus; // 누적 폭발 반경 보너스

        public bool HasAny => BaseDamageBonus > 0f || ProjectileSpeedBonus > 0f || PierceCountBonus > 0 || ExplosionRadiusBonus > 0f; // 보너스 존재 여부

        public void AddDefinition(WeaponDefinition definition, float bonusMultiplier = 1f) // 강화 1종 누적
        {
            if (definition == null)
            {
                return; // null 무시
            }

            float scale = Mathf.Max(0f, bonusMultiplier); // 레어 2배 / 유니크 3배
            BaseDamageBonus += definition.BaseDamage * scale; // 카드 피해 누적
            ProjectileSpeedBonus += definition.ProjectileSpeed * scale; // 카드 속도 누적
            PierceCountBonus += Mathf.RoundToInt(definition.PierceCount * scale); // 카드 관통 누적
            ExplosionRadiusBonus += definition.ExplosionRadius * scale; // 카드 폭발 반경 누적
        }
    }
}
