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
        [Min(0f)] public float BaseDamage; // 기본 피해량 보너스
        [Min(0f)] public float ProjectileSpeed; // 투사체 속도 보너스
        [Min(0)] public int PierceCount; // 관통 수 보너스
        [Min(0f)] public float ExplosionRadius; // 폭발 반경 보너스

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
    }

    [Serializable]
    public struct WeaponStatBonusData // 세그먼트별 무기 강화 누적값
    {
        public float BaseDamageBonus; // 누적 피해 보너스
        public float ProjectileSpeedBonus; // 누적 투사체 속도 보너스
        public int PierceCountBonus; // 누적 관통 보너스
        public float ExplosionRadiusBonus; // 누적 폭발 반경 보너스

        public bool HasAny => BaseDamageBonus > 0f || ProjectileSpeedBonus > 0f || PierceCountBonus > 0 || ExplosionRadiusBonus > 0f; // 보너스 존재 여부

        public void AddDefinition(WeaponDefinition definition) // 강화 1종 누적
        {
            if (definition == null)
            {
                return; // null 무시
            }

            BaseDamageBonus += definition.BaseDamage; // 카드 피해 누적
            ProjectileSpeedBonus += definition.ProjectileSpeed; // 카드 속도 누적
            PierceCountBonus += definition.PierceCount; // 카드 관통 누적
            ExplosionRadiusBonus += definition.ExplosionRadius; // 카드 폭발 반경 누적
        }
    }
}
