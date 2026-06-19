using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class SegmentProjectileRuntime
    {
        private Vector3 GetEnemyHitPosition(EnemyController enemy) // 몬스터 중심 위치
        {
            float targetAimHeight = profile != null ? profile.TargetAimHeight : 0.45f; // 조준 높이
            return SegmentTargetQuery.GetEnemyHitPosition(enemy, transform.position, targetAimHeight); // 공용 중심 계산
        }

        private void TryApplyHitAt(Vector3 position) // 위치 명중 확인
        {
            Collider[] hits = Physics.OverlapSphere(position, profile.ProjectileHitRadius); // 반경 검색
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponentInParent<EnemyController>(); // 몬스터
                if (enemy == null || hitEnemyIds.Contains(enemy.EnemyId))
                {
                    continue; // 대상 아님/중복
                }

                Vector3 hitPosition = GetEnemyHitPosition(enemy); // 명중 위치
                ApplyImpactAt(hitPosition, enemy); // 명중 처리
                if (this == null)
                {
                    return; // 제거됨
                }

                if (profile.MoveType != SegmentAttackMoveType.PiercingProjectile && profile.ImpactType != SegmentAttackImpactType.PierceDamage)
                {
                    return; // 단일 명중
                }
            }
        }

        private void ApplyImpactAt(Vector3 position, EnemyController enemy) // 명중 처리
        {
            if (profile.ImpactType == SegmentAttackImpactType.ExplosionArea)
            {
                ApplyExplosion(position); // 범위 피해
                Destroy(gameObject); // 투사체 제거
                return;
            }

            if (enemy != null)
            {
                SegmentHitResolver.ApplyDamageAndFeedback(enemy, damage, profile, position, transform.position, SegmentMonsterFeedbackKind.Direct); // 직접 피해 + 피드백
                PlayHitVfx(position); // 명중 VFX
                hitEnemyIds.Add(enemy.EnemyId); // 관통 중복 방지
            }

            if (profile.MoveType == SegmentAttackMoveType.PiercingProjectile || profile.ImpactType == SegmentAttackImpactType.PierceDamage)
            {
                remainingPierces--; // 관통 소모
                if (remainingPierces > 0)
                {
                    return; // 계속 비행
                }
            }

            Destroy(gameObject); // 종료
        }

        private void ApplyExplosion(Vector3 position) // 폭발 처리
        {
            ApplyExplosion(position, GetExplosionRadius(), explosionEnemyIds, true); // 강화 반경 폭발
        }

        private void ApplyExplosion(Vector3 position, float radius, List<int> hitIds, bool playVfx) // 반경 피해 처리
        {
            float damageRadius = Mathf.Max(0f, radius); // 반경 보정
            if (damageRadius <= 0f)
            {
                return; // 범위 없음
            }

            if (playVfx)
            {
                PlayExplosionVfx(position, damageRadius); // 폭발 VFX
            }

            DamageData explosionDamage = DamageData.Create(damage.Amount, DamageType.Explosion, damage.SourceSegmentIndex, position, damage.SourceObject); // 폭발 피해
            Collider[] hits = Physics.OverlapSphere(position, damageRadius); // 범위 검색
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponentInParent<EnemyController>(); // 몬스터
                if (enemy == null || hitIds.Contains(enemy.EnemyId))
                {
                    continue; // 대상 아님/중복
                }

                hitIds.Add(enemy.EnemyId); // 중복 방지
                Vector3 hitPosition = GetEnemyHitPosition(enemy); // 명중 위치
                SegmentHitResolver.ApplyDamageAndFeedback(enemy, explosionDamage, profile, hitPosition, position, SegmentMonsterFeedbackKind.Explosion); // 범위 피해 + 피드백
            }
        }

        private void ApplyLandingImpactDamage(Vector3 position) // 투석기 돌 착지 순간 작은 범위 피해
        {
            float radius = profile.LandingImpactRadius > 0f ? profile.LandingImpactRadius : profile.ProjectileHitRadius; // 작은 착지 반경
            ApplyExplosion(position, radius, explosionEnemyIds, true); // 착지 충격파
        }

        private void ApplyLandingRollDamage(Vector3 position) // 투석기 돌이 구르는 동안 주는 피해
        {
            float radius = profile.LandingRollDamageRadius > 0f ? profile.LandingRollDamageRadius : profile.ProjectileHitRadius; // 구르기 피해 반경
            if (radius <= 0f)
            {
                return; // 피해 반경 없음
            }

            DamageData rollDamage = DamageData.Create(damage.Amount, DamageType.Projectile, damage.SourceSegmentIndex, position, damage.SourceObject); // 구르기 피해
            Collider[] hits = Physics.OverlapSphere(position, radius); // 돌 주변 검색
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponentInParent<EnemyController>(); // 몬스터
                if (enemy == null || hitEnemyIds.Contains(enemy.EnemyId))
                {
                    continue; // 대상 아님/이미 구르기 피해 받음
                }

                hitEnemyIds.Add(enemy.EnemyId); // 구르기 중복 방지
                Vector3 hitPosition = GetEnemyHitPosition(enemy); // 명중 위치
                SegmentHitResolver.ApplyDamageAndFeedback(enemy, rollDamage, profile, hitPosition, position, SegmentMonsterFeedbackKind.Direct); // 구르기 충돌 피해 + 피드백
                PlayHitVfx(hitPosition); // 명중 VFX
            }
        }
    }
}
