using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class GenericSegmentWeapon
    {
        private bool Fire(EnemyController target) // 공격 실행
        {
            if (AttackProfile.MoveType == SegmentAttackMoveType.Laser)
            {
                Transform muzzle = ResolveMuzzle(); // 포구
                Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // 생성 위치
                DamageData damage = CreateDamageData(spawnPosition); // 피해값
                Vector3 fireDirection = GetFireDirection(target, spawnPosition); // 실제 발사 방향
                PlayMuzzleVfx(muzzle); // 발사 VFX
                PlayFireRecoil(fireDirection, muzzle); // 포구 로컬축 대신 실제 발사 방향 기준 반동
                FireLaser(target, damage); // 레이저
                return true;
            }

            if (AttackProfile.MoveType == SegmentAttackMoveType.ChainLightning)
            {
                Transform muzzle = ResolveMuzzle(); // 시작 위치
                Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // 번개 시작점
                DamageData damage = CreateDamageData(spawnPosition); // 피해값
                Vector3 fireDirection = GetFireDirection(target, spawnPosition); // 첫 타겟 방향
                PlayMuzzleVfx(muzzle); // 시전 VFX
                PlayFireRecoil(fireDirection, muzzle); // 약한 시전 반동
                FireChainLightning(target, muzzle, spawnPosition, damage); // 즉시 체인 번개
                return true;
            }

            if (TryStartTrebuchetFireMotion(target))
            {
                return false; // 투석기 모션 코루틴이 발사/쿨타임을 처리
            }

            if (ShouldFireProjectilesSequentially())
            {
                projectileSequenceRoutine = StartCoroutine(FireProjectileSequence(target)); // 순차 발사
                return false; // 코루틴 종료 후 쿨타임
            }

            Transform projectileMuzzle = ResolveMuzzle(); // 포구
            Vector3 projectileSpawnPosition = projectileMuzzle != null ? projectileMuzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // 생성 위치
            DamageData projectileDamage = CreateDamageData(projectileSpawnPosition); // 피해값
            Vector3 projectileFireDirection = GetFireDirection(target, projectileSpawnPosition); // 실제 투사체 발사 방향
            PlayMuzzleVfx(projectileMuzzle); // 발사 VFX
            PlayFireRecoil(projectileFireDirection, projectileMuzzle); // 포구 로컬축 대신 실제 투사체 방향 기준 반동
            FireProjectiles(target, projectileSpawnPosition, projectileDamage); // 투사체
            return true;
        }

        private void FireProjectiles(EnemyController target, Vector3 spawnPosition, DamageData damage) // 투사체 발사
        {
            int count = Mathf.Max(1, AttackProfile.ProjectileCount); // 발사 수
            float spread = Mathf.Max(0f, AttackProfile.SpreadAngle); // 산탄 각도

            for (int i = 0; i < count; i++)
            {
                FireSingleProjectile(target, spawnPosition, damage, i, count, spread); // 개별 발사
                HideLoadedProjectileVisual(i); // 장전 표시 숨김
            }
        }

        private IEnumerator FireProjectileSequence(EnemyController initialTarget) // 순차 투사체 발사
        {
            isFiringProjectileSequence = true; // 중복 발사 방지
            CacheLoadedProjectileVisuals(); // 표시 목록 갱신

            int count = Mathf.Max(1, AttackProfile.ProjectileCount); // 발사 수
            float spread = Mathf.Max(0f, AttackProfile.SpreadAngle); // 산탄 각도
            float delay = Mathf.Max(0f, AttackProfile.ProjectileFireDelay); // 발사 간격

            for (int i = 0; i < count; i++)
            {
                if (!CanUseWeapon())
                {
                    break; // 분리/비활성
                }

                EnemyController target = ResolveSequenceTarget(initialTarget); // 현재 대상
                Transform muzzle = ResolveMuzzle(); // 포구
                Vector3 spawnPosition = GetProjectileSpawnPosition(i, muzzle); // 장전 위치 우선
                DamageData damage = CreateDamageData(spawnPosition); // 피해값
                Vector3 fireDirection = GetFireDirection(target, spawnPosition); // 순차 발사 실제 방향
                PlayMuzzleVfx(muzzle); // 발사 VFX
                PlayFireRecoil(fireDirection, muzzle); // 순차 발사도 실제 발사 방향 반대로 반동 적용
                FireSingleProjectile(target, spawnPosition, damage, i, count, spread); // 개별 발사
                HideLoadedProjectileVisual(i); // 사용한 장전탄 숨김

                if (i < count - 1 && delay > 0f)
                {
                    yield return new WaitForSeconds(delay); // 다음 발 지연
                }
            }

            ClearSawTargetLock(); // 연사 종료 후 다음 후보 준비
            ResetCooldown(); // 전탄 발사 후 쿨타임
            projectileSequenceRoutine = null; // 코루틴 해제
            isFiringProjectileSequence = false; // 발사 완료
        }

        private void FireSingleProjectile(EnemyController target, Vector3 spawnPosition, DamageData damage, int projectileIndex, int projectileCount, float spread) // 단일 투사체
        {
            Vector3 baseDirection = GetFireDirection(target, spawnPosition); // 기준 방향
            float startAngle = projectileCount <= 1 ? 0f : -spread * 0.5f; // 시작 각도
            float step = projectileCount <= 1 ? 0f : spread / (projectileCount - 1); // 각도 간격
            float angle = startAngle + step * projectileIndex; // 이번 탄 각도
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection; // 산탄 방향
            SegmentProjectileRuntime.Spawn(Segment.Owner.GetProjectileRoot(), AttackProfile.ProjectilePrefab, spawnPosition, direction, target, AttackProfile, damage); // 공통 투사체
        }

        private EnemyController ResolveSequenceTarget(EnemyController initialTarget) // 순차 발사 대상
        {
            if (initialTarget != null)
            {
                float range = GetUpgrade().ApplyRange(AttackProfile.SearchRange); // 사거리
                if (Vector3.Distance(transform.position, initialTarget.transform.position) <= range && IsTargetInAttackArea(initialTarget)) // 순차 발사 중에도 공격 범위 형태 유지
                {
                    return initialTarget; // 기존 대상 유지
                }
            }

            return TryFindTarget(out EnemyController target) ? target : null; // 새 대상 fallback
        }
    }
}
