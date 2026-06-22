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
            FireProjectiles(target, projectileSpawnPosition, projectileDamage, projectileMuzzle); // 투사체
            return true;
        }

        private void FireProjectiles(EnemyController target, Vector3 spawnPosition, DamageData damage, Transform muzzle) // 투사체 발사
        {
            int count = Mathf.Max(1, AttackProfile.ProjectileCount); // 발사 수
            float spread = Mathf.Max(0f, AttackProfile.SpreadAngle); // 산탄 각도

            for (int i = 0; i < count; i++)
            {
                FireSingleProjectile(target, spawnPosition, damage, i, count, spread, muzzle); // 개별 발사
                HideLoadedProjectileVisual(i); // 장전 표시 숨김
            }
        }

        private IEnumerator FireProjectileSequence(EnemyController initialTarget) // 순차 투사체 발사
        {
            isFiringProjectileSequence = true; // 중복 발사 방지
            projectileSequenceTarget = ResolveSequenceTarget(initialTarget); // 첫 조준 대상
            UpdateProjectileSequencePreferredSide(projectileSequenceTarget); // 첫 발사 방향 기억
            CacheLoadedProjectileVisuals(); // 표시 목록 갱신

            int count = Mathf.Max(1, AttackProfile.ProjectileCount); // 발사 수
            float spread = Mathf.Max(0f, AttackProfile.SpreadAngle); // 산탄 각도
            float delay = Mathf.Max(0f, AttackProfile.ProjectileFireDelay); // 발사 간격
            bool useSustainedMuzzleVfx = ShouldUseSustainedMuzzleVfx();
            if (useSustainedMuzzleVfx)
            {
                StartSustainedMuzzleVfx(ResolveMuzzle());
            }

            for (int i = 0; i < count; i++)
            {
                if (!CanUseWeapon())
                {
                    break; // 분리/비활성
                }

                EnemyController target = ResolveSequenceTarget(projectileSequenceTarget); // 현재 대상
                projectileSequenceTarget = target; // 다음 틱 조준용 저장
                UpdateProjectileSequencePreferredSide(target); // 이번 발사 콘 방향 기억
                Transform muzzle = ResolveMuzzle(); // 포구
                AimHeadAtTarget(target, Time.deltaTime, GetFiringHeadTurnSpeedMultiplier()); // 발사 순간에도 느리게 재조준
                Vector3 spawnPosition = GetProjectileSpawnPosition(i, muzzle); // 장전 위치 우선
                DamageData damage = CreateDamageData(spawnPosition); // 피해값
                Vector3 fireDirection = GetProjectileFireDirection(target, spawnPosition); // 순차 발사 실제 방향
                if (useSustainedMuzzleVfx)
                {
                    UpdateSustainedMuzzleVfx(muzzle);
                }
                else
                {
                    PlayMuzzleVfx(muzzle); // 발사 VFX
                }
                PlayFireRecoil(fireDirection, muzzle); // 순차 발사도 실제 발사 방향 반대로 반동 적용
                FireSingleProjectile(target, spawnPosition, damage, i, count, spread, muzzle); // 개별 발사
                HideLoadedProjectileVisual(i); // 사용한 장전탄 숨김

                if (i < count - 1 && delay > 0f)
                {
                    yield return new WaitForSeconds(delay); // 다음 발 지연
                }
            }

            StopSustainedMuzzleVfx(false);
            ClearSawTargetLock(); // 연사 종료 후 다음 후보 준비
            ResetCooldown(); // 전탄 발사 후 쿨타임
            projectileSequenceRoutine = null; // 코루틴 해제
            isFiringProjectileSequence = false; // 발사 완료
            projectileSequenceTarget = null; // 대상 초기화
            projectileSequencePreferredSide = 0; // 선호 방향 초기화
        }

        private void FireSingleProjectile(EnemyController target, Vector3 spawnPosition, DamageData damage, int projectileIndex, int projectileCount, float spread, Transform muzzle) // 단일 투사체
        {
            Vector3 baseDirection = GetProjectileFireDirection(target, spawnPosition); // 기준 방향
            float startAngle = projectileCount <= 1 ? 0f : -spread * 0.5f; // 시작 각도
            float step = projectileCount <= 1 ? 0f : spread / (projectileCount - 1); // 각도 간격
            float angle = startAngle + step * projectileIndex; // 이번 탄 각도
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * baseDirection; // 산탄 방향
            WeaponStatBonusData weaponBonus = CoreStatProvider.GetWeaponStatBonusOrDefault(GetEffectiveSegmentId()); // 무기 강화
            Transform flameInfluenceAnchor = AttackProfile != null && AttackProfile.MoveType == SegmentAttackMoveType.ExpandingFlameSphere ? muzzle : null;
            SegmentProjectileRuntime.Spawn(Segment.Owner.GetProjectileRoot(), AttackProfile.ProjectilePrefab, spawnPosition, direction, target, AttackProfile, damage, weaponBonus, flameInfluenceAnchor); // 공통 투사체
        }

        private EnemyController ResolveSequenceTarget(EnemyController initialTarget) // 순차 발사 대상
        {
            if (IsTargetUsable(initialTarget))
            {
                float range = GetUpgrade().ApplyRange(AttackProfile.SearchRange); // 사거리
                if (Vector3.Distance(transform.position, initialTarget.transform.position) <= range && IsTargetInAttackArea(initialTarget)) // 순차 발사 중에도 공격 범위 형태 유지
                {
                    return initialTarget; // 기존 대상 유지
                }
            }

            return TryFindProjectileSequenceTargetBySide(out EnemyController target) ? target : null; // 새 대상 fallback
        }

        private void UpdateProjectileSequenceAim(float deltaTime) // 발사 중 느린 재조준
        {
            if (AttackProfile == null || !AttackProfile.ContinueAimingDuringProjectileSequence)
            {
                return; // 일반 순차 발사는 기존 감각 유지
            }

            projectileSequenceTarget = ResolveSequenceTarget(projectileSequenceTarget); // 죽은 대상이면 새 대상 검색
            UpdateProjectileSequencePreferredSide(projectileSequenceTarget); // 조준 방향 갱신
            AimHeadAtTarget(projectileSequenceTarget, deltaTime, GetFiringHeadTurnSpeedMultiplier()); // 느리게 따라감
            if (IsSustainedMuzzleVfxActive())
            {
                Transform muzzle = ResolveMuzzle();
                UpdateSustainedMuzzleVfx(muzzle);
            }
        }

        private bool TryFindProjectileSequenceTargetBySide(out EnemyController target) // 같은 쪽 콘 우선 재탐색
        {
            target = null;
            if (AttackProfile == null
                || !AttackProfile.ContinueAimingDuringProjectileSequence
                || AttackProfile.AttackAreaMode != SegmentAttackAreaMode.SideCones)
            {
                return TryFindTarget(out target); // 일반 무기는 기존 방식
            }

            int preferredSide = NormalizeSideSign(projectileSequencePreferredSide);
            if (TryFindTargetInSideCone(preferredSide, out target))
            {
                return true; // 방금 발사하던 쪽 우선
            }

            return TryFindTargetInSideCone(-preferredSide, out target); // 없으면 반대편
        }

        private void UpdateProjectileSequencePreferredSide(EnemyController target) // 현재 타겟 기준 선호 콘 갱신
        {
            if (AttackProfile == null || AttackProfile.AttackAreaMode != SegmentAttackAreaMode.SideCones || !IsTargetUsable(target))
            {
                return; // 갱신 대상 없음
            }

            projectileSequencePreferredSide = GetTargetSideSign(target); // 이번 발사 방향 저장
        }

        private float GetFiringHeadTurnSpeedMultiplier() // 발사 중 회전 배율
        {
            return AttackProfile != null ? Mathf.Clamp(AttackProfile.FiringHeadTurnSpeedMultiplier, 0.01f, 1f) : 1f;
        }

        private Vector3 GetProjectileFireDirection(EnemyController target, Vector3 spawnPosition) // 투사체 방향 선택
        {
            if (AttackProfile != null && AttackProfile.UseMuzzleDirectionDuringProjectileSequence && isFiringProjectileSequence)
            {
                Transform muzzle = ResolveMuzzle();
                Transform pivot = ResolveHeadYawPivot();
                Vector3 direction = GetCurrentMuzzleDirection(pivot, muzzle);
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized; // 채널링 중에는 조준에 쓰는 현재 포구 방향
                }
            }

            return GetFireDirection(target, spawnPosition); // 기존 타겟 방향
        }
    }
}
