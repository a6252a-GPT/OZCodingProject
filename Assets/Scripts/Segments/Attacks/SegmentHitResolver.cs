using UnityEngine;

namespace TeamProject01.Gameplay
{
    public enum SegmentMonsterFeedbackKind
    {
        Direct,
        Explosion,
        Pierce,
        Chain,
        Continuous
    }

    public static class SegmentHitResolver
    {
        public static void ApplyDamageAndFeedback(
            EnemyController enemy,
            DamageData damage,
            SegmentAttackProfile profile,
            Vector3 hitPosition,
            Vector3 feedbackOrigin,
            SegmentMonsterFeedbackKind feedbackKind)
        {
            if (!SegmentTargetQuery.IsEnemyUsable(enemy))
            {
                return;
            }

            DamageData resolvedDamage = damage.WithHitPosition(hitPosition);
            enemy.ApplyDamage(resolvedDamage);

            MonsterFeedbackData feedback = CreateFeedback(enemy, resolvedDamage, profile, hitPosition, feedbackOrigin, feedbackKind);
            MonsterFeedbackApi.TryApplyFeedback(enemy, feedback);
            ApplyStatusEffect(enemy, resolvedDamage, profile, hitPosition);
        }

        private static void ApplyStatusEffect(EnemyController enemy, DamageData damage, SegmentAttackProfile profile, Vector3 hitPosition)
        {
            if (profile == null || profile.StatusEffectOnHit == CombatStatusEffectKind.None)
            {
                return;
            }

            EnemySupportDebuffState state = EnemySupportDebuffState.GetOrAdd(enemy);
            if (state != null)
            {
                float incomingDamageMultiplier = ResolveStatusIncomingDamageMultiplier(profile.StatusEffectOnHit, damage.SourceObject);
                state.ApplyStatusEffect(
                    profile.StatusEffectOnHit,
                    damage.SourceSegmentIndex,
                    damage.SourceObject,
                    hitPosition,
                    profile.StatusEffectVfxPrefab,
                    incomingDamageMultiplierOverride: incomingDamageMultiplier);
            }
        }

        private static float ResolveStatusIncomingDamageMultiplier(CombatStatusEffectKind kind, GameObject sourceObject)
        {
            if (kind != CombatStatusEffectKind.Shock)
            {
                return 0f; // 감전 외 상태효과는 카탈로그 기본값 사용
            }

            int level = ResolveSourceSegmentLevel(sourceObject);
            switch (Mathf.Clamp(level, 1, 3))
            {
                case 1:
                    return 1.05f; // Lv1: 받피증 5%
                case 2:
                    return 1.07f; // Lv2: 받피증 7%
                default:
                    return 1.10f; // Lv3+: 받피증 10%
            }
        }

        private static int ResolveSourceSegmentLevel(GameObject sourceObject)
        {
            SegmentWeaponBehaviour weapon = sourceObject != null
                ? sourceObject.GetComponentInParent<SegmentWeaponBehaviour>()
                : null;
            string segmentId = weapon != null ? weapon.EffectiveSegmentId : string.Empty;
            CoreStatProvider core = CoreStatProvider.Active;
            return core != null
                && !string.IsNullOrWhiteSpace(segmentId)
                && core.TryGetSegmentModelLevelInfo(segmentId, out int currentLevel, out _)
                ? currentLevel
                : 1;
        }

        private static MonsterFeedbackData CreateFeedback(
            EnemyController enemy,
            DamageData damage,
            SegmentAttackProfile profile,
            Vector3 hitPosition,
            Vector3 feedbackOrigin,
            SegmentMonsterFeedbackKind feedbackKind)
        {
            if (profile == null || !profile.ApplyMonsterFeedback)
            {
                return default;
            }

            float scale = GetFeedbackScale(profile, feedbackKind);

            if (scale <= 0.0f)
            {
                return default;
            }

            float knockbackDistance = profile.MonsterKnockbackDistance * scale;
            float staggerDuration = profile.MonsterStaggerDuration * scale;

            if (feedbackKind == SegmentMonsterFeedbackKind.Chain)
            {
                knockbackDistance = 0.0f;
            }

            Vector3 direction = enemy.transform.position - feedbackOrigin;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f && damage.SourceObject != null)
            {
                direction = enemy.transform.position - damage.SourceObject.transform.position;
                direction.y = 0.0f;
            }

            return MonsterFeedbackData.Create(
                feedbackOrigin,
                direction,
                hitPosition,
                knockbackDistance,
                profile.MonsterKnockbackDuration,
                staggerDuration,
                damage.SourceSegmentIndex,
                damage.Type,
                damage.SourceObject);
        }

        private static float GetFeedbackScale(SegmentAttackProfile profile, SegmentMonsterFeedbackKind feedbackKind)
        {
            return feedbackKind switch
            {
                SegmentMonsterFeedbackKind.Explosion => profile.MonsterExplosionFeedbackMultiplier,
                SegmentMonsterFeedbackKind.Pierce => profile.MonsterPierceFeedbackMultiplier,
                SegmentMonsterFeedbackKind.Continuous => profile.MonsterContinuousFeedbackMultiplier,
                _ => 1.0f
            };
        }
    }
}
