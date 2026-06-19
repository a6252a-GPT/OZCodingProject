using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class GenericSegmentWeapon : SegmentWeaponBehaviour // 데이터 기반 세그먼트 무기
    {
        public SegmentAttackProfile AttackProfile; // 공격 데이터
        public Transform HeadYawPivot; // 머리 회전축
        public Transform Muzzle; // 발사 위치
        public Transform MuzzleVfxSocket; // 발사 VFX 위치
        public Transform LoadedProjectileRoot; // 장전 미사일 표시 루트

        private readonly List<Transform> loadedProjectileVisuals = new List<Transform>(4); // 장전 표시 목록
        private float fireTimer; // 남은 쿨타임
        private float fireIntervalDuration; // 현재 쿨타임 길이
        private bool loadedProjectilesRestored = true; // 장전 표시 복구 여부
        private bool isFiringProjectileSequence; // 순차 발사 중
        private bool wasWeaponActive; // 이전 작동 상태
        private bool hasInitializedLoadedVisuals; // 초기 장전 표시 완료
        private Coroutine laserRoutine; // 레이저 지속 피해
        private Coroutine projectileSequenceRoutine; // 순차 투사체 발사

        public override void Configure(ConvoySegmentRuntime segment) // 세그먼트 연결
        {
            base.Configure(segment); // 공통 저장
            CacheLoadedProjectileVisuals(); // 장전 표시 수집
            if (!hasInitializedLoadedVisuals)
            {
                RestoreLoadedProjectileVisuals(); // 최초 장전 상태
                hasInitializedLoadedVisuals = true;
            }
        }

        public override void SetWeaponActive(bool active) // 작동 상태
        {
            bool becameActive = active && !wasWeaponActive; // 비활성 -> 활성
            base.SetWeaponActive(active); // 공통 상태
            if (becameActive)
            {
                RestoreLoadedProjectileVisuals(); // 재활성화 시 장전 복구
            }
            wasWeaponActive = active; // 상태 저장

            if (!active && projectileSequenceRoutine != null)
            {
                StopCoroutine(projectileSequenceRoutine); // 분리 시 발사 중지
                projectileSequenceRoutine = null;
                isFiringProjectileSequence = false;
            }
        }

        public override void TickWeapon(float deltaTime) // 무기 갱신
        {
            if (!CanUseWeapon())
            {
                return; // 발사 불가
            }

            fireTimer -= deltaTime; // 쿨타임 감소
            UpdateLoadedProjectileReloadVisuals(); // 재장전 표시 복구
            if (isFiringProjectileSequence)
            {
                return; // 순차 발사 진행 중
            }

            if (!TryFindTarget(out EnemyController target))
            {
                return; // 대상 없음
            }

            bool aimed = AimHeadAtTarget(target, deltaTime); // 머리 조준
            if (fireTimer > 0f)
            {
                return; // 조준만 유지
            }

            if (AttackProfile.RequireAimBeforeFire && !aimed)
            {
                return; // 아직 조준 중
            }

            if (Fire(target))
            {
                ResetCooldown(); // 다음 공격 준비
            }
        }

        private bool CanUseWeapon() // 작동 가능 확인
        {
            return IsWeaponActive && Segment != null && Segment.Owner != null && AttackProfile != null; // 연결 상태
        }

        private bool TryFindTarget(out EnemyController target) // 대상 탐색
        {
            float range = GetUpgrade().ApplyRange(AttackProfile.SearchRange); // 강화 사거리
            return EnemyController.TryFindNearest(transform.position, range, out target); // 가까운 몬스터
        }

        private bool Fire(EnemyController target) // 공격 실행
        {
            if (AttackProfile.MoveType == SegmentAttackMoveType.Laser)
            {
                Transform muzzle = ResolveMuzzle(); // 포구
                Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // 생성 위치
                DamageData damage = CreateDamageData(spawnPosition); // 피해값
                PlayMuzzleVfx(muzzle); // 발사 VFX
                FireLaser(target, damage); // 레이저
                return true;
            }

            if (ShouldFireProjectilesSequentially())
            {
                projectileSequenceRoutine = StartCoroutine(FireProjectileSequence(target)); // 순차 발사
                return false; // 코루틴 종료 후 쿨타임
            }

            Transform projectileMuzzle = ResolveMuzzle(); // 포구
            Vector3 projectileSpawnPosition = projectileMuzzle != null ? projectileMuzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // 생성 위치
            DamageData projectileDamage = CreateDamageData(projectileSpawnPosition); // 피해값
            PlayMuzzleVfx(projectileMuzzle); // 발사 VFX
            FireProjectiles(target, projectileSpawnPosition, projectileDamage); // 투사체
            return true;
        }

        private DamageData CreateDamageData(Vector3 position) // 피해값 생성
        {
            CoreStatData coreStats = CoreStatProvider.GetCurrentOrDefault(); // 코어 스탯
            // 건준 추가 시작 =====
            WeaponStatBonusData weaponBonus = CoreStatProvider.GetWeaponStatBonusOrDefault(GetEffectiveSegmentId()); // 무기 강화
            float baseDamage = AttackProfile.BaseDamage + weaponBonus.BaseDamageBonus; // 프로필 + 강화
            // 건준 추가 끝 =====
            float damage = GetUpgrade().ApplyDamage(coreStats.ApplyDamage(baseDamage)); // 최종 피해
            return DamageData.Create(damage, GetDamageType(), Segment.ChainIndex, position, gameObject); // 전달값
        }

        private DamageType GetDamageType() // 피해 종류
        {
            if (AttackProfile.MoveType == SegmentAttackMoveType.Laser)
            {
                return DamageType.Laser; // 레이저
            }

            return AttackProfile.ImpactType == SegmentAttackImpactType.ExplosionArea ? DamageType.Explosion : DamageType.Projectile; // 투사체/폭발
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
                PlayMuzzleVfx(muzzle); // 발사 VFX
                FireSingleProjectile(target, spawnPosition, damage, i, count, spread); // 개별 발사
                HideLoadedProjectileVisual(i); // 사용한 장전탄 숨김

                if (i < count - 1 && delay > 0f)
                {
                    yield return new WaitForSeconds(delay); // 다음 발 지연
                }
            }

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
            // 건준 추가 시작 =====
            WeaponStatBonusData weaponBonus = CoreStatProvider.GetWeaponStatBonusOrDefault(GetEffectiveSegmentId()); // 무기 강화
            SegmentProjectileRuntime.Spawn(Segment.Owner.GetProjectileRoot(), AttackProfile.ProjectilePrefab, spawnPosition, direction, target, AttackProfile, damage, weaponBonus); // 공통 투사체
            // 건준 추가 끝 =====
        }

        private EnemyController ResolveSequenceTarget(EnemyController initialTarget) // 순차 발사 대상
        {
            if (initialTarget != null)
            {
                float range = GetUpgrade().ApplyRange(AttackProfile.SearchRange); // 사거리
                if (Vector3.Distance(transform.position, initialTarget.transform.position) <= range)
                {
                    return initialTarget; // 기존 대상 유지
                }
            }

            return TryFindTarget(out EnemyController target) ? target : null; // 새 대상 fallback
        }

        private void FireLaser(EnemyController target, DamageData damage) // 레이저 공격
        {
            if (laserRoutine != null)
            {
                StopCoroutine(laserRoutine); // 이전 지속 피해 정리
            }

            laserRoutine = StartCoroutine(ApplyLaserDamage(target, damage)); // 지속 피해 시작
        }

        private IEnumerator ApplyLaserDamage(EnemyController target, DamageData damage) // 레이저 지속 피해
        {
            float timer = Mathf.Max(0.05f, AttackProfile.LaserDuration); // 지속 시간
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.02f, AttackProfile.LaserTickInterval)); // 피해 간격
            while (timer > 0f && target != null)
            {
                Vector3 hitPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 명중 위치
                if (Vector3.Distance(transform.position, target.transform.position) <= GetUpgrade().ApplyRange(AttackProfile.SearchRange))
                {
                    target.ApplyDamage(damage.WithHitPosition(hitPosition)); // 지속 피해
                    PlayHitVfx(hitPosition); // 명중 VFX
                }

                timer -= AttackProfile.LaserTickInterval; // 시간 감소
                yield return wait;
            }

            laserRoutine = null; // 종료
        }

        private Vector3 GetFireDirection(EnemyController target, Vector3 spawnPosition) // 발사 방향
        {
            if (target != null)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 목표 중심
                Vector3 direction = targetPosition - spawnPosition; // 포구 -> 목표
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized; // 목표 방향
                }
            }

            Transform muzzle = ResolveMuzzle(); // 포구 fallback
            return muzzle != null ? muzzle.forward : transform.forward; // 현재 방향
        }

        private bool AimHeadAtTarget(EnemyController target, float deltaTime) // 머리 조준
        {
            Transform pivot = ResolveHeadYawPivot(); // 회전축
            if (pivot == null || target == null)
            {
                return true; // 회전축 없음
            }

            Transform muzzle = ResolveMuzzle(); // 포구
            if (!TryGetHorizontalAim(target, pivot, muzzle, out Vector3 currentDirection, out Vector3 targetDirection))
            {
                return true; // 방향 없음
            }

            float signedAngle = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up); // 목표 각도
            float maxStep = AttackProfile.HeadTurnSpeed * deltaTime; // 회전량
            float step = Mathf.Clamp(signedAngle, -maxStep, maxStep); // 과회전 방지
            pivot.Rotate(Vector3.up, step, Space.World); // 회전

            if (!TryGetHorizontalAim(target, pivot, muzzle, out currentDirection, out targetDirection))
            {
                return true; // 방향 없음
            }

            return Mathf.Abs(Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up)) <= AttackProfile.FireAngleTolerance; // 조준 완료
        }

        private bool TryGetHorizontalAim(EnemyController target, Transform pivot, Transform muzzle, out Vector3 currentDirection, out Vector3 targetDirection) // 수평 조준
        {
            currentDirection = Vector3.zero; // 현재 방향
            targetDirection = Vector3.zero; // 목표 방향
            Vector3 aimOrigin = muzzle != null ? muzzle.position : pivot.position; // 포구 우선
            Vector3 targetPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 목표 중심
            targetDirection = targetPosition - aimOrigin; // 목표 벡터
            targetDirection.y = 0f; // 수평 회전만
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 방향 없음
            }

            currentDirection = GetCurrentMuzzleDirection(pivot, muzzle); // 현재 포신 방향
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 기준 없음
            }

            currentDirection.Normalize(); // 정규화
            targetDirection.Normalize(); // 정규화
            return true; // 계산 가능
        }

        private static Vector3 GetCurrentMuzzleDirection(Transform pivot, Transform muzzle) // 현재 포신 방향
        {
            if (muzzle != null)
            {
                Vector3 pivotToMuzzle = muzzle.position - pivot.position; // 피벗 -> 포구
                pivotToMuzzle.y = 0f; // 수평
                if (pivotToMuzzle.sqrMagnitude > 0.0001f)
                {
                    return pivotToMuzzle; // 모델 기준
                }

                Vector3 muzzleForward = muzzle.forward; // 포구 방향
                muzzleForward.y = 0f;
                if (muzzleForward.sqrMagnitude > 0.0001f)
                {
                    return muzzleForward;
                }
            }

            Vector3 pivotForward = pivot.forward; // 피벗 방향
            pivotForward.y = 0f;
            return pivotForward;
        }

        private Transform ResolveHeadYawPivot() // 회전축 찾기
        {
            if (HeadYawPivot != null)
            {
                return HeadYawPivot; // 수동 연결
            }

            Transform root = Segment != null ? Segment.transform : transform; // 검색 루트
            HeadYawPivot = FindChildRecursive(root, "YawPivot"); // 머리 프리팹 기준 회전축
            if (HeadYawPivot == null)
            {
                HeadYawPivot = FindChildRecursive(root, "Joint_HeadMount"); // 기존 조립 기준 fallback
            }

            if (HeadYawPivot == null)
            {
                HeadYawPivot = FindChildRecursive(root, "Joint"); // 구버전 fallback
            }

            return HeadYawPivot;
        }

        private Transform ResolveMuzzle() // 포구 찾기
        {
            if (Muzzle != null)
            {
                return Muzzle; // 수동 연결
            }

            Transform pivot = ResolveHeadYawPivot(); // 회전축
            Transform root = pivot != null ? pivot : (Segment != null ? Segment.transform : transform); // 검색 루트
            Muzzle = FindChildRecursive(root, "Muzzle"); // 포구
            return Muzzle;
        }

        private Transform ResolveMuzzleVfxSocket(Transform muzzle) // 발사 VFX 기준점
        {
            if (MuzzleVfxSocket != null)
            {
                return MuzzleVfxSocket; // 수동 연결
            }

            Transform root = muzzle != null ? muzzle : ResolveMuzzle(); // 포구 기준
            MuzzleVfxSocket = FindChildRecursive(root, "VFX_Muzzle"); // 정식 이름
            if (MuzzleVfxSocket == null)
            {
                MuzzleVfxSocket = FindChildRecursive(root, "MuzzleVFX"); // fallback
            }

            return MuzzleVfxSocket;
        }

        private bool ShouldFireProjectilesSequentially() // 순차 발사 여부
        {
            return AttackProfile.FireProjectilesSequentially || ShouldUseLoadedProjectileVisuals(); // 장전 표시 사용 시 순차 처리
        }

        private bool ShouldUseLoadedProjectileVisuals() // 장전 표시 사용 여부
        {
            return AttackProfile != null && AttackProfile.UseLoadedProjectileVisuals; // 프로필 설정
        }

        private Vector3 GetProjectileSpawnPosition(int projectileIndex, Transform muzzle) // 발사 위치
        {
            CacheLoadedProjectileVisuals(); // 표시 목록 보정
            if (projectileIndex >= 0 && projectileIndex < loadedProjectileVisuals.Count)
            {
                Transform visual = loadedProjectileVisuals[projectileIndex]; // 장전탄
                if (visual != null)
                {
                    return visual.position; // 장전 위치에서 발사
                }
            }

            return muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight; // fallback
        }

        private void UpdateLoadedProjectileReloadVisuals() // 장전 표시 복구
        {
            if (!ShouldUseLoadedProjectileVisuals() || loadedProjectilesRestored || fireIntervalDuration <= 0f)
            {
                return; // 복구 대상 없음
            }

            float progress = fireTimer <= 0f ? 1f : 1f - Mathf.Clamp01(fireTimer / fireIntervalDuration); // 쿨타임 진행률
            if (progress >= AttackProfile.LoadedProjectileReloadRatio)
            {
                RestoreLoadedProjectileVisuals(); // 50% 시점 복구
            }
        }

        private void RestoreLoadedProjectileVisuals() // 장전 표시 전체 복구
        {
            CacheLoadedProjectileVisuals(); // 목록 보정
            int count = AttackProfile != null ? Mathf.Max(1, AttackProfile.ProjectileCount) : loadedProjectileVisuals.Count; // 표시 수
            for (int i = 0; i < loadedProjectileVisuals.Count; i++)
            {
                Transform visual = loadedProjectileVisuals[i]; // 장전탄
                if (visual != null)
                {
                    visual.gameObject.SetActive(i < count); // 필요한 수만 표시
                }
            }

            loadedProjectilesRestored = true; // 복구 완료
        }

        private void HideLoadedProjectileVisual(int projectileIndex) // 사용한 장전 표시 숨김
        {
            if (!ShouldUseLoadedProjectileVisuals())
            {
                return; // 장전 표시 미사용
            }

            CacheLoadedProjectileVisuals(); // 목록 보정
            if (projectileIndex < 0 || projectileIndex >= loadedProjectileVisuals.Count)
            {
                return; // 슬롯 없음
            }

            Transform visual = loadedProjectileVisuals[projectileIndex]; // 대상
            if (visual != null)
            {
                visual.gameObject.SetActive(false); // 발사됨
            }
        }

        private void CacheLoadedProjectileVisuals() // 장전 표시 수집
        {
            Transform root = ResolveLoadedProjectileRoot(); // 표시 루트
            loadedProjectileVisuals.Clear(); // 재수집
            if (root == null)
            {
                return; // 표시 없음
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i); // 슬롯
                if (child != null)
                {
                    loadedProjectileVisuals.Add(child); // 순서 유지
                }
            }
        }

        private Transform ResolveLoadedProjectileRoot() // 장전 표시 루트 찾기
        {
            if (LoadedProjectileRoot != null)
            {
                return LoadedProjectileRoot; // 수동 연결
            }

            Transform pivot = ResolveHeadYawPivot(); // 머리 기준
            Transform root = pivot != null ? pivot : (Segment != null ? Segment.transform : transform); // 검색 루트
            LoadedProjectileRoot = FindChildRecursive(root, "LoadedProjectiles"); // 정식 이름
            if (LoadedProjectileRoot == null)
            {
                LoadedProjectileRoot = FindChildRecursive(root, "MissileList"); // fallback
            }

            if (LoadedProjectileRoot == null)
            {
                LoadedProjectileRoot = FindChildRecursive(root, "MisslieList"); // 오타 fallback
            }

            return LoadedProjectileRoot;
        }

        private void PlayMuzzleVfx(Transform muzzle) // 발사 VFX
        {
            if (AttackProfile.MuzzleVfxPrefab == null)
            {
                return; // 지정 없음
            }

            Transform socket = ResolveMuzzleVfxSocket(muzzle); // 기준점
            Vector3 position = socket != null ? socket.position : (muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight); // 위치
            Quaternion rotation = socket != null ? socket.rotation : (muzzle != null ? muzzle.rotation : transform.rotation); // 방향
            GameObject instance = Instantiate(AttackProfile.MuzzleVfxPrefab, position, rotation); // 생성
            if (AttackProfile.MuzzleVfxLifetime > 0f)
            {
                Destroy(instance, AttackProfile.MuzzleVfxLifetime); // 제거
            }
        }

        private void PlayHitVfx(Vector3 position) // 명중 VFX
        {
            if (AttackProfile.HitVfxPrefab == null)
            {
                return; // 지정 없음
            }

            GameObject instance = Instantiate(AttackProfile.HitVfxPrefab, position, Quaternion.identity); // 생성
            if (AttackProfile.HitVfxLifetime > 0f)
            {
                Destroy(instance, AttackProfile.HitVfxLifetime); // 제거
            }
        }

        private void ResetCooldown() // 쿨타임 재설정
        {
            float min = Mathf.Min(AttackProfile.MinAttackInterval, AttackProfile.MaxAttackInterval); // 최소
            float max = Mathf.Max(AttackProfile.MinAttackInterval, AttackProfile.MaxAttackInterval); // 최대
            float baseInterval = Random.Range(min, max); // 기본 쿨타임
            float coreInterval = CoreStatProvider.GetCurrentOrDefault().ApplyFireInterval(baseInterval); // 코어 공속
            fireTimer = GetUpgrade().ApplyFireInterval(coreInterval); // 세그먼트 공속
            fireIntervalDuration = fireTimer; // 진행률 계산 기준
            loadedProjectilesRestored = !ShouldUseLoadedProjectileVisuals(); // 장전 표시 복구 대기
        }

        private static Transform FindChildRecursive(Transform root, string childName) // 이름 검색
        {
            if (root == null)
            {
                return null; // 검색 불가
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i); // 하위
                if (child.name == childName)
                {
                    return child; // 발견
                }

                Transform found = FindChildRecursive(child, childName); // 재귀
                if (found != null)
                {
                    return found; // 발견
                }
            }

            return null; // 없음
        }
    }
}
