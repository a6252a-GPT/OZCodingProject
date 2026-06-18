using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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
        // 투석기처럼 발사 전에 별도 무기 모션을 재생하는 컴포넌트
        private SegmentTrebuchetFireMotion trebuchetFireMotion; // SG03 투석기 숟가락 모션
        // 발사 반동 대상 임시 목록
        private readonly List<Transform> fireRecoilTargets = new List<Transform>(3); // Visual/Head 동시 반동
        // 반동 복귀 기준 pose 목록
        private readonly List<RecoilTargetPose> recoilTargetPoses = new List<RecoilTargetPose>(3); // 원래 위치/회전
        // 중복 반동 연출 제어용 DOTween 시퀀스
        private Sequence recoilSequence; // 현재 반동 트윈
        // SG02 미사일 프로필처럼 반동값이 아직 저장되지 않은 로켓 계열에 줄 아주 약한 기본 반동
        private const float DefaultLightMissileRecoilDistance = 0.045f; // 미사일 발사 순간 살짝 밀림
        // SG02 미사일 프로필처럼 반동값이 아직 저장되지 않은 로켓 계열에 줄 아주 약한 기본 기울기
        private const float DefaultLightMissileRecoilTiltAngle = 1.1f; // 캐논보다 훨씬 약한 기울기

        public override void Configure(ConvoySegmentRuntime segment) // 세그먼트 연결
        {
            base.Configure(segment); // 공통 저장
            CacheTrebuchetFireMotion(); // SG03 투석기 모션 연결
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

            if (!active)
            {
                StopTrebuchetFireMotion(); // 분리/비활성 시 숟가락 모션 복구
                ResetFireRecoilPose(); // 분리/비활성 시 반동 중간 pose 복구
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
            return EnemyController.TryFindNearest(transform.position, range, IsTargetInAttackArea, out target); // 데이터에셋의 공격 범위 형태까지 통과한 가까운 몬스터
        }

        // 원형/양옆 부채꼴 공격 범위 조건 확인
        private bool IsTargetInAttackArea(EnemyController target)
        {
            if (target == null)
            {
                return false; // 대상 없음
            }

            if (AttackProfile == null || AttackProfile.AttackAreaMode == SegmentAttackAreaMode.FullCircle)
            {
                return true; // 기존 원형 범위는 추가 각도 제한 없음
            }

            if (AttackProfile.AttackAreaMode == SegmentAttackAreaMode.SideCones)
            {
                return IsPositionInSideCones(target.transform.position); // 양옆 부채꼴 판정
            }

            return true; // 새 모드가 추가됐는데 아직 처리 전이면 기존 방식 유지
        }

        // 세그먼트 바디 기준 좌우 각각 SideConeAngle 안에 있는지 확인
        private bool IsPositionInSideCones(Vector3 worldPosition)
        {
            Transform reference = Segment != null ? Segment.transform : transform; // 머리 회전축이 아니라 세그먼트 바디 기준
            Vector3 toTarget = worldPosition - reference.position; // 세그먼트 -> 몬스터
            toTarget.y = 0f; // 수평 판정
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return true; // 거의 같은 위치면 공격 가능
            }

            Vector3 right = GetHorizontalDirection(reference.right, transform.right, Vector3.right); // 오른쪽 부채꼴 중심
            Vector3 left = -right; // 왼쪽 부채꼴 중심
            Vector3 targetDirection = toTarget.normalized; // 몬스터 방향
            float halfAngle = Mathf.Clamp(AttackProfile.SideConeAngle, 1f, 180f) * 0.5f; // 100도면 좌우 50도씩
            float rightAngle = Vector3.Angle(right, targetDirection); // 오른쪽 중심과의 각도
            float leftAngle = Vector3.Angle(left, targetDirection); // 왼쪽 중심과의 각도
            return rightAngle <= halfAngle || leftAngle <= halfAngle; // 양쪽 중 하나라도 들어오면 공격 가능
        }

        // 수평 방향 벡터 fallback 정리
        private static Vector3 GetHorizontalDirection(Vector3 primary, Vector3 secondary, Vector3 fallback)
        {
            primary.y = 0f; // 수평화
            if (primary.sqrMagnitude > 0.0001f)
            {
                return primary.normalized; // 1순위 방향
            }

            secondary.y = 0f; // 수평화
            if (secondary.sqrMagnitude > 0.0001f)
            {
                return secondary.normalized; // 2순위 방향
            }

            fallback.y = 0f; // 수평화
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.right; // 최종 fallback
        }

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

        // 투석기 전용 모션이 있으면 즉시 발사 대신 숟가락 회전 후 발사
        private bool TryStartTrebuchetFireMotion(EnemyController target)
        {
            SegmentTrebuchetFireMotion motion = ResolveTrebuchetFireMotion(); // 모션 컴포넌트
            if (motion == null || !motion.CanPlayMotion || motion.IsPlaying)
            {
                return false; // 투석기 모션 없음
            }

            projectileSequenceRoutine = StartCoroutine(FireTrebuchetMotionSequence(target, motion)); // 회전 후 발사
            return true; // 시작 성공
        }

        // 숟가락이 90도 발사 지점에 도달했을 때 실제 투사체를 생성
        private IEnumerator FireTrebuchetMotionSequence(EnemyController initialTarget, SegmentTrebuchetFireMotion motion)
        {
            isFiringProjectileSequence = true; // 중복 발사 방지
            CacheLoadedProjectileVisuals(); // 표시 목록 갱신
            bool releasedProjectile = false; // 발사 콜백 실행 여부
            bool queuedThrowSway = false; // 팔로우스루 이후 실행할 흔들림 예약 여부
            Vector3 queuedThrowSwayDirection = Vector3.zero; // 예약된 던지는 방향
            Transform queuedThrowSwayMuzzle = null; // 예약된 포구

            yield return motion.PlayReleaseMotion(() =>
            {
                if (!CanUseWeapon())
                {
                    return; // 분리/비활성
                }

                EnemyController target = ResolveSequenceTarget(initialTarget); // 현재 대상
                Transform muzzle = ResolveMuzzle(); // 포구
                int count = Mathf.Max(1, AttackProfile.ProjectileCount); // 발사 수
                float spread = Mathf.Max(0f, AttackProfile.SpreadAngle); // 산탄 각도

                for (int i = 0; i < count; i++)
                {
                    Vector3 spawnPosition = GetProjectileSpawnPosition(i, muzzle); // 숟가락 돌 위치 우선
                    DamageData damage = CreateDamageData(spawnPosition); // 피해값
                    Vector3 fireDirection = GetFireDirection(target, spawnPosition); // 실제 발사 방향
                    PlayMuzzleVfx(muzzle); // 발사 VFX
                    if (i == 0)
                    {
                        queuedThrowSway = true; // 팔로우스루가 끝난 뒤 한 번만 흔들림
                        queuedThrowSwayDirection = fireDirection; // 발사 순간 방향 저장
                        queuedThrowSwayMuzzle = muzzle; // 발사 순간 포구 저장
                    }

                    FireSingleProjectile(target, spawnPosition, damage, i, count, spread); // 투사체 생성
                    HideLoadedProjectileVisual(i); // 숟가락 위 돌 숨김
                }

                ResetCooldown(); // 발사 순간부터 쿨타임 진행
                releasedProjectile = true; // 발사 완료
            }, () =>
            {
                if (queuedThrowSway && CanUseWeapon())
                {
                    PlayTrebuchetThrowSway(queuedThrowSwayDirection, queuedThrowSwayMuzzle, motion); // 끝까지 휘두른 뒤 던진 방향으로 흔들림
                }
            });

            if (!releasedProjectile && CanUseWeapon())
            {
                ResetCooldown(); // 예외적으로 발사 콜백이 못 돌았을 때 무한 대기 방지
            }

            projectileSequenceRoutine = null; // 코루틴 해제
            isFiringProjectileSequence = false; // 발사 완료
        }

        private DamageData CreateDamageData(Vector3 position) // 피해값 생성
        {
            CoreStatData coreStats = CoreStatProvider.GetCurrentOrDefault(); // 코어 스탯
            float damage = GetUpgrade().ApplyDamage(coreStats.ApplyDamage(AttackProfile.BaseDamage)); // 최종 피해
            return DamageData.Create(damage, GetDamageType(), Segment.ChainIndex, position, gameObject); // 전달값
        }

        private DamageType GetDamageType() // 피해 종류
        {
            if (AttackProfile.MoveType == SegmentAttackMoveType.Laser)
            {
                return DamageType.Laser; // 레이저
            }

            if (AttackProfile.MoveType == SegmentAttackMoveType.ChainLightning)
            {
                return DamageType.Electric; // 전기
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
                if (Vector3.Distance(transform.position, target.transform.position) <= GetUpgrade().ApplyRange(AttackProfile.SearchRange) && IsTargetInAttackArea(target)) // 레이저 지속 피해도 공격 범위 형태 유지
                {
                    target.ApplyDamage(damage.WithHitPosition(hitPosition)); // 지속 피해
                    PlayHitVfx(hitPosition); // 명중 VFX
                }

                timer -= AttackProfile.LaserTickInterval; // 시간 감소
                yield return wait;
            }

            laserRoutine = null; // 종료
        }

        private void FireChainLightning(EnemyController firstTarget, Transform startAnchor, Vector3 startPosition, DamageData damage) // 즉시 체인 번개
        {
            if (firstTarget == null)
            {
                return; // 첫 대상 없음
            }

            HashSet<int> hitIds = new HashSet<int>(); // 한 번의 체인 안에서 중복 타격 방지
            Vector3 firstHitPosition = GetEnemyHitPosition(firstTarget); // 첫 명중 위치
            hitIds.Add(firstTarget.EnemyId); // 첫 대상 기록
            SegmentLightningChainVfx.Spawn(startAnchor, startPosition, firstHitPosition, AttackProfile.ChainLineVfxLifetime); // 머즐 -> 첫 대상
            firstTarget.ApplyDamage(damage.WithHitPosition(firstHitPosition)); // 첫 대상은 전체 피해
            PlayHitVfx(firstHitPosition); // 첫 명중 VFX
            StartCoroutine(ChainLightningRoutine(firstHitPosition, 1, damage, hitIds)); // 첫 대상 위치에서 확산
        }

        private IEnumerator ChainLightningRoutine(Vector3 fromPosition, int depth, DamageData baseDamage, HashSet<int> hitIds) // 체인 확산
        {
            if (depth > Mathf.Max(0, AttackProfile.MaxChainDepth))
            {
                yield break; // 최대 체인 단계 도달
            }

            float delay = Mathf.Max(0f, AttackProfile.ChainDelay); // 단계 지연
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            if (!CanUseWeapon())
            {
                yield break; // 세그먼트가 분리/비활성화됨
            }

            List<ChainCandidate> targets = SelectChainTargets(fromPosition, hitIds); // 다음 후보
            if (targets.Count == 0)
            {
                yield break; // 더 퍼질 대상 없음
            }

            float damageAmount = CalculateChainDamage(baseDamage.Amount, depth); // 단계별 감쇠 피해
            DamageData chainDamage = DamageData.Create(damageAmount, DamageType.Electric, baseDamage.SourceSegmentIndex, fromPosition, baseDamage.SourceObject); // 체인 피해
            for (int i = 0; i < targets.Count; i++)
            {
                ChainCandidate target = targets[i];
                if (target.Enemy == null)
                {
                    continue; // 사라진 대상
                }

                hitIds.Add(target.Id); // 중복 방지
                Vector3 hitPosition = GetEnemyHitPosition(target.Enemy); // 명중 위치
                SegmentLightningChainVfx.Spawn(fromPosition, hitPosition, AttackProfile.ChainLineVfxLifetime); // 몬스터 -> 몬스터
                target.Enemy.ApplyDamage(chainDamage.WithHitPosition(hitPosition)); // 피해 적용
                PlayHitVfx(hitPosition); // 명중 VFX
                StartCoroutine(ChainLightningRoutine(hitPosition, depth + 1, baseDamage, hitIds)); // 다음 단계 확산
            }
        }

        private List<ChainCandidate> SelectChainTargets(Vector3 fromPosition, HashSet<int> hitIds) // 주변 체인 후보 선택
        {
            List<ChainCandidate> candidates = new List<ChainCandidate>(); // 전체 후보
            float range = GetUpgrade().ApplyRange(Mathf.Max(0.1f, AttackProfile.ChainRange)); // 체인 거리
            Collider[] hits = Physics.OverlapSphere(fromPosition, range, ~0, QueryTriggerInteraction.Collide); // 주변 콜라이더
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                {
                    continue; // 빈 콜라이더
                }

                EnemyController enemy = hit.GetComponentInParent<EnemyController>(); // 몬스터 확인
                if (enemy == null || hitIds.Contains(enemy.EnemyId) || ContainsChainCandidate(candidates, enemy.EnemyId))
                {
                    continue; // 이미 맞았거나 중복 후보
                }

                Vector3 center = GetEnemyHitPosition(enemy); // 후보 중심
                float distance = Vector3.Distance(fromPosition, center); // 거리
                candidates.Add(new ChainCandidate(enemy, enemy.EnemyId, center, 1f / Mathf.Max(0.1f, distance))); // 가까운 대상 우선
            }

            List<ChainCandidate> selected = new List<ChainCandidate>(); // 최종 선택
            int count = Mathf.Min(Mathf.Max(1, AttackProfile.ChainBranchCount), candidates.Count); // 분기 수
            for (int i = 0; i < count; i++)
            {
                int index = PickWeightedChainCandidate(candidates); // 거리 가중 랜덤
                if (index < 0)
                {
                    break; // 후보 없음
                }

                selected.Add(candidates[index]); // 선택
                candidates.RemoveAt(index); // 중복 선택 방지
            }

            return selected;
        }

        private static bool ContainsChainCandidate(List<ChainCandidate> candidates, int enemyId) // 후보 중복 확인
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Id == enemyId)
                {
                    return true; // 이미 후보에 있음
                }
            }

            return false;
        }

        private static int PickWeightedChainCandidate(List<ChainCandidate> candidates) // 가중 랜덤 선택
        {
            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += Mathf.Max(0f, candidates[i].Weight); // 가중치 합산
            }

            if (totalWeight <= 0f)
            {
                return candidates.Count > 0 ? 0 : -1; // fallback
            }

            float roll = Random.value * totalWeight;
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(0f, candidates[i].Weight);
                if (roll <= 0f)
                {
                    return i; // 선택
                }
            }

            return candidates.Count - 1; // 부동소수 오차 fallback
        }

        private float CalculateChainDamage(float baseAmount, int depth) // 체인 단계별 피해
        {
            float falloff = Mathf.Clamp01(AttackProfile.ChainDamageFalloff); // 감쇠율
            float multiplier = Mathf.Pow(falloff, Mathf.Max(1, depth)); // depth 1부터 감쇠
            return Mathf.Max(0f, baseAmount * multiplier); // 최종 피해
        }

        private Vector3 GetEnemyHitPosition(EnemyController enemy) // 몬스터 중심 위치
        {
            if (enemy == null)
            {
                return transform.position; // fallback
            }

            Collider targetCollider = enemy.GetComponentInChildren<Collider>();
            if (targetCollider != null)
            {
                return targetCollider.bounds.center; // 콜라이더 중심
            }

            return enemy.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 높이 보정
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

            currentDirection = GetCurrentMuzzleDirection(pivot, muzzle); // 투석기는 머즐 위치가 아닌 머즐 방향 기준 조준
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 기준 없음
            }

            currentDirection.Normalize(); // 정규화
            targetDirection.Normalize(); // 정규화
            return true; // 계산 가능
        }

        private Vector3 GetCurrentMuzzleDirection(Transform pivot, Transform muzzle) // 현재 포신 방향
        {
            if (ShouldAimByMuzzleForward())
            {
                Vector3 muzzleForwardDirection = GetTrebuchetAimDirection(muzzle, pivot); // SG03은 머즐 X축을 정면 조준축으로 사용
                if (muzzleForwardDirection.sqrMagnitude > 0.0001f)
                {
                    return muzzleForwardDirection; // 머즐 Z+ 방향을 조준 기준으로 사용
                }
            }

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

        // 투석기처럼 머즐 위치와 조준 정면이 다른 무기는 머즐 방향으로 조준한다.
        private bool ShouldAimByMuzzleForward()
        {
            return ResolveTrebuchetFireMotion() != null; // SG03 투석기 전용 보정
        }

        // 투석기 머즐 X+ 방향을 수평 조준 벡터로 변환
        private static Vector3 GetTrebuchetAimDirection(Transform primary, Transform fallback)
        {
            if (primary != null)
            {
                Vector3 direction = primary.right; // 투석기 머즐의 빨간 X축
                direction.y = 0f; // 좌우 회전만 사용
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction; // 머즐 X축 방향
                }
            }

            Vector3 fallbackDirection = fallback != null ? fallback.forward : Vector3.zero; // 피벗 fallback
            fallbackDirection.y = 0f; // 수평화
            return fallbackDirection; // 결과
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

        // SG03 투석기 모션 컴포넌트 캐싱
        private void CacheTrebuchetFireMotion()
        {
            trebuchetFireMotion = GetComponentInChildren<SegmentTrebuchetFireMotion>(true); // 세그먼트 안에서 검색
            if (trebuchetFireMotion != null)
            {
                trebuchetFireMotion.CaptureBasePoseIfNeeded(); // 현재 프리팹 기준 자세 저장
            }
        }

        // 필요할 때 투석기 모션 컴포넌트 재검색
        private SegmentTrebuchetFireMotion ResolveTrebuchetFireMotion()
        {
            if (trebuchetFireMotion == null)
            {
                CacheTrebuchetFireMotion(); // 런타임 교체 후 재검색
            }

            return trebuchetFireMotion;
        }

        // 비활성/분리 때 투석기 숟가락 회전 상태를 복구
        private void StopTrebuchetFireMotion()
        {
            SegmentTrebuchetFireMotion motion = ResolveTrebuchetFireMotion(); // 모션 컴포넌트
            if (motion != null)
            {
                motion.StopMotion(true); // 기준 자세로 복구
            }
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

        // 발사 순간 캐논 비주얼 반동 재생
        private void PlayFireRecoil(Vector3 fireDirection, Transform muzzle)
        {
            if (AttackProfile == null)
            {
                return; // 프로필 없음
            }

            float distance = AttackProfile.RecoilDistance; // 데이터에 지정된 이동 반동
            float tiltAngle = AttackProfile.RecoilTiltAngle; // 데이터에 지정된 회전 반동
            ApplyDefaultLightMissileRecoilIfNeeded(ref distance, ref tiltAngle); // SG02 미사일 기본 약반동

            PlayFireRecoil(
                fireDirection,
                muzzle,
                false,
                distance,
                tiltAngle,
                AttackProfile.RecoilKickDuration,
                AttackProfile.RecoilReturnDuration,
                AttackProfile.RecoilSettleDistanceRatio,
                AttackProfile.RecoilSettleTiltRatio,
                AttackProfile.RecoilSettleDuration); // 기존 캐논 반동은 발사 반대 방향
        }

        // 투석기 돌 발사 후에는 반동 반대가 아니라 던진 방향으로 몸체가 쏠리게 재생
        private void PlayTrebuchetThrowSway(Vector3 fireDirection, Transform muzzle, SegmentTrebuchetFireMotion motion)
        {
            if (motion == null || !motion.UseThrowSway)
            {
                return; // 투석기 전용 흔들림 미사용
            }

            PlayFireRecoil(
                fireDirection,
                muzzle,
                true,
                motion.ThrowSwayDistance,
                motion.ThrowSwayTiltAngle,
                motion.ThrowSwayKickDuration,
                motion.ThrowSwayReturnDuration,
                motion.ThrowSwaySettleDistanceRatio,
                motion.ThrowSwaySettleTiltRatio,
                motion.ThrowSwaySettleDuration); // 던진 방향으로 흔들림
        }

        // 캐논 반동과 투석기 전방 흔들림이 같은 트윈 시스템을 공유하도록 방향/값을 외부에서 받음
        private void PlayFireRecoil(
            Vector3 fireDirection,
            Transform muzzle,
            bool pushTowardFireDirection,
            float recoilDistance,
            float recoilTiltAngle,
            float recoilKickDuration,
            float recoilReturnDuration,
            float recoilSettleDistanceRatio,
            float recoilSettleTiltRatio,
            float recoilSettleDuration)
        {
            float distance = Mathf.Max(0f, recoilDistance); // 이동 반동
            float tiltAngle = Mathf.Max(0f, recoilTiltAngle); // 회전 반동
            if (distance <= 0f && tiltAngle <= 0f)
            {
                return; // 미사일처럼 반동 없는 공격
            }

            ResetFireRecoilTweenOnly(); // 기존 트윈 정리
            RestoreFireRecoilPose(); // 이전 반동 중간 pose를 먼저 복구
            CollectFireRecoilTargets(); // 흔들 대상 수집
            if (fireRecoilTargets.Count == 0)
            {
                return; // 대상 없음
            }

            CacheFireRecoilPoses(); // 원래 pose 저장

            Vector3 worldRecoilDirection = GetWorldRecoilDirection(fireDirection, muzzle, pushTowardFireDirection); // 캐논은 반대, 투석기는 던진 방향
            float kickDuration = Mathf.Max(0.01f, recoilKickDuration); // 밀림 시간
            float returnDuration = Mathf.Max(0.01f, recoilReturnDuration); // 복귀 시간
            float settleDistance = distance * Mathf.Max(0f, recoilSettleDistanceRatio); // 원점 반대쪽 되받음 거리
            float settleTilt = tiltAngle * Mathf.Max(0f, recoilSettleTiltRatio); // 원점 반대쪽 되받음 회전
            float settleDuration = Mathf.Max(0.01f, recoilSettleDuration); // 마지막 자리 잡는 시간
            recoilSequence = DOTween.Sequence(); // 새 반동 시퀀스

            for (int i = 0; i < recoilTargetPoses.Count; i++)
            {
                RecoilTargetPose pose = recoilTargetPoses[i]; // 대상 pose
                if (pose.Target == null)
                {
                    continue; // 삭제된 대상
                }

                Vector3 localDirection = GetLocalRecoilDirection(pose.Target, worldRecoilDirection); // 대상 부모 기준 방향
                Vector3 targetPosition = pose.LocalPosition + localDirection * distance; // 반동 위치
                Quaternion targetRotation = GetRecoilRotation(pose.LocalRotation, localDirection, tiltAngle); // 반동 회전
                recoilSequence.Join(pose.Target.DOLocalMove(targetPosition, kickDuration).SetEase(Ease.OutQuad)); // 뒤로 밀림
                recoilSequence.Join(pose.Target.DOLocalRotateQuaternion(targetRotation, kickDuration).SetEase(Ease.OutQuad)); // 살짝 젖음
            }

            bool appendedReturn = false; // 되받음 구간 시작 여부
            for (int i = 0; i < recoilTargetPoses.Count; i++)
            {
                RecoilTargetPose pose = recoilTargetPoses[i]; // 대상 pose
                if (pose.Target == null)
                {
                    continue; // 삭제된 대상
                }

                Vector3 localDirection = GetLocalRecoilDirection(pose.Target, worldRecoilDirection); // 대상 부모 기준 방향
                Vector3 settlePosition = pose.LocalPosition - localDirection * settleDistance; // 원점을 살짝 지나친 위치
                Quaternion settleRotation = GetRecoilRotation(pose.LocalRotation, -localDirection, settleTilt); // 반대 방향으로 살짝 되받는 회전
                Tween moveBack = pose.Target.DOLocalMove(settlePosition, returnDuration).SetEase(Ease.InOutSine); // 바로 원점 대신 되받는 지점으로 복귀
                Tween rotateBack = pose.Target.DOLocalRotateQuaternion(settleRotation, returnDuration).SetEase(Ease.InOutSine); // 회전도 살짝 되받음
                if (!appendedReturn)
                {
                    recoilSequence.Append(moveBack); // 첫 되받음 트윈으로 구간 생성
                    recoilSequence.Join(rotateBack); // 첫 회전 되받음
                    appendedReturn = true; // 되받음 구간 시작됨
                }
                else
                {
                    recoilSequence.Join(moveBack); // 다른 대상도 같은 타이밍에 되받음
                    recoilSequence.Join(rotateBack); // 다른 대상 회전 되받음
                }
            }

            bool appendedSettle = false; // 최종 자리 잡기 구간 시작 여부
            for (int i = 0; i < recoilTargetPoses.Count; i++)
            {
                RecoilTargetPose pose = recoilTargetPoses[i]; // 대상 pose
                if (pose.Target == null)
                {
                    continue; // 삭제된 대상
                }

                Tween moveSettle = pose.Target.DOLocalMove(pose.LocalPosition, settleDuration).SetEase(Ease.OutSine); // 원래 위치로 둔하게 정착
                Tween rotateSettle = pose.Target.DOLocalRotateQuaternion(pose.LocalRotation, settleDuration).SetEase(Ease.OutSine); // 원래 회전으로 둔하게 정착
                if (!appendedSettle)
                {
                    recoilSequence.Append(moveSettle); // 최종 정착 구간 생성
                    recoilSequence.Join(rotateSettle); // 첫 회전 정착
                    appendedSettle = true; // 정착 구간 시작됨
                }
                else
                {
                    recoilSequence.Join(moveSettle); // 다른 대상도 같은 타이밍에 정착
                    recoilSequence.Join(rotateSettle); // 다른 대상 회전 정착
                }
            }

            recoilSequence.OnComplete(() =>
            {
                RestoreFireRecoilPose(); // 미세 오차 보정
                recoilSequence = null; // 시퀀스 해제
            });
        }

        // 기존 반동 트윈만 정리
        private void ResetFireRecoilTweenOnly()
        {
            if (recoilSequence == null)
            {
                return; // 정리할 트윈 없음
            }

            recoilSequence.Kill(false); // 콜백 없이 중단
            recoilSequence = null; // 참조 해제
        }

        // 반동 중간 상태를 원래 위치로 되돌림
        private void ResetFireRecoilPose()
        {
            ResetFireRecoilTweenOnly(); // 트윈 정리
            RestoreFireRecoilPose(); // pose 복구
        }

        // 저장된 반동 대상 pose 복구
        private void RestoreFireRecoilPose()
        {
            for (int i = 0; i < recoilTargetPoses.Count; i++)
            {
                RecoilTargetPose pose = recoilTargetPoses[i]; // 저장 pose
                if (pose.Target == null)
                {
                    continue; // 삭제된 대상
                }

                pose.Target.localPosition = pose.LocalPosition; // 위치 복구
                pose.Target.localRotation = pose.LocalRotation; // 회전 복구
            }
        }

        // 반동 대상의 현재 pose를 기준으로 저장
        private void CacheFireRecoilPoses()
        {
            recoilTargetPoses.Clear(); // 이전 저장값 제거
            for (int i = 0; i < fireRecoilTargets.Count; i++)
            {
                Transform target = fireRecoilTargets[i]; // 반동 대상
                if (target == null)
                {
                    continue; // 삭제된 대상
                }

                recoilTargetPoses.Add(new RecoilTargetPose(target, target.localPosition, target.localRotation)); // 현재 pose 저장
            }
        }

        // 바디와 헤드가 같이 쏠리도록 반동 대상 수집
        private void CollectFireRecoilTargets()
        {
            fireRecoilTargets.Clear(); // 이전 대상 제거
            Transform segmentRoot = Segment != null ? Segment.transform : transform; // 세그먼트 루트
            Transform explicitRoot = FindChildRecursive(segmentRoot, "VisualRecoilRoot"); // 전용 루트
            if (explicitRoot != null)
            {
                fireRecoilTargets.Add(explicitRoot); // 전용 루트가 있으면 하나만 흔듦
                return; // 수집 완료
            }

            Transform visual = FindDirectChild(segmentRoot, "Visual"); // 바디/모델 비주얼
            AddFireRecoilTargetIfNeeded(visual); // 바디 비주얼 추가

            Transform pivot = ResolveHeadYawPivot(); // 헤드 회전축
            Transform headRoot = FindDirectChildContaining(segmentRoot, pivot); // 헤드 직계 루트
            AddFireRecoilTargetIfNeeded(headRoot); // 헤드가 Visual 밖이면 추가

            if (fireRecoilTargets.Count == 0 && pivot != null)
            {
                fireRecoilTargets.Add(pivot); // 최후 fallback: 헤드 피벗만
            }
        }

        // 이미 상위 대상이 흔들리면 중복 추가하지 않음
        private void AddFireRecoilTargetIfNeeded(Transform candidate)
        {
            if (candidate == null)
            {
                return; // 대상 없음
            }

            for (int i = 0; i < fireRecoilTargets.Count; i++)
            {
                Transform existing = fireRecoilTargets[i]; // 기존 대상
                if (existing == null)
                {
                    continue; // 삭제됨
                }

                if (candidate == existing || candidate.IsChildOf(existing))
                {
                    return; // 기존 대상이 이미 후보를 포함
                }
            }

            fireRecoilTargets.Add(candidate); // 새 대상 추가
        }

        // 캐논은 포구 반대 방향, 투석기는 던진 방향을 수평 흔들림 방향으로 변환
        private Vector3 GetWorldRecoilDirection(Vector3 fireDirection, Transform muzzle, bool pushTowardFireDirection)
        {
            Vector3 direction = pushTowardFireDirection ? fireDirection : -fireDirection; // 투석기는 던진 쪽, 캐논은 반대쪽
            direction.y = 0f; // 바디가 땅에서 들리지 않도록 수평화
            if (direction.sqrMagnitude <= 0.0001f && muzzle != null)
            {
                direction = pushTowardFireDirection ? muzzle.forward : -muzzle.forward; // 타겟 방향을 못 구한 경우 포구축 fallback
            }

            direction.y = 0f; // 바디가 땅에서 들리지 않도록 수평화
            if (direction.sqrMagnitude <= 0.0001f)
            {
                Transform segmentRoot = Segment != null ? Segment.transform : transform; // fallback 기준
                direction = pushTowardFireDirection ? segmentRoot.forward : -segmentRoot.forward; // 최종 fallback
                direction.y = 0f; // 수평화
            }

            return direction.sqrMagnitude > 0.0001f ? direction.normalized : (pushTowardFireDirection ? Vector3.forward : -Vector3.forward); // 최종 방향
        }

        // 미사일 데이터에 반동값을 따로 저장하지 않았을 때만 아주 약한 기본 반동을 적용
        private void ApplyDefaultLightMissileRecoilIfNeeded(ref float distance, ref float tiltAngle)
        {
            if (distance > 0f || tiltAngle > 0f)
            {
                return; // 데이터에서 직접 설정한 값 우선
            }

            if (!ShouldUseDefaultLightMissileRecoil())
            {
                return; // 미사일 계열 아님
            }

            distance = DefaultLightMissileRecoilDistance; // 약한 위치 반동
            tiltAngle = DefaultLightMissileRecoilTiltAngle; // 약한 회전 반동
        }

        // SG02처럼 곡사 폭발 로켓이며 장전 투사체 표시를 쓰는 공격만 기본 약반동 적용
        private bool ShouldUseDefaultLightMissileRecoil()
        {
            return AttackProfile != null
                && AttackProfile.MoveType == SegmentAttackMoveType.ArcProjectile
                && AttackProfile.ImpactType == SegmentAttackImpactType.ExplosionArea
                && AttackProfile.UseLoadedProjectileVisuals
                && ResolveTrebuchetFireMotion() == null; // SG03 투석기는 전용 ThrowSway를 사용
        }

        // 월드 반동 방향을 대상 부모 로컬 방향으로 변환
        private static Vector3 GetLocalRecoilDirection(Transform target, Vector3 worldDirection)
        {
            Transform parent = target != null ? target.parent : null; // 부모 기준
            Vector3 localDirection = parent != null ? parent.InverseTransformDirection(worldDirection) : worldDirection; // 로컬 변환
            localDirection.y = 0f; // 로컬에서도 수직 이동 제거
            return localDirection.sqrMagnitude > 0.0001f ? localDirection.normalized : -Vector3.forward; // fallback
        }

        // 반동 방향으로 윗부분이 살짝 밀리는 회전 계산
        private static Quaternion GetRecoilRotation(Quaternion baseRotation, Vector3 localDirection, float tiltAngle)
        {
            if (tiltAngle <= 0f)
            {
                return baseRotation; // 회전 반동 없음
            }

            Vector3 tiltAxis = Vector3.Cross(Vector3.up, localDirection); // 반동 방향으로 젖는 축
            if (tiltAxis.sqrMagnitude <= 0.0001f)
            {
                tiltAxis = Vector3.right; // fallback 축
            }

            return baseRotation * Quaternion.AngleAxis(tiltAngle, tiltAxis.normalized); // 기준 회전에 반동 회전 추가
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

        // 세그먼트 직계 자식 이름 검색
        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null; // 검색 불가
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i); // 직계 자식
                if (child.name == childName)
                {
                    return child; // 발견
                }
            }

            return null; // 없음
        }

        // 특정 하위 오브젝트를 포함하는 직계 자식 찾기
        private static Transform FindDirectChildContaining(Transform root, Transform descendant)
        {
            if (root == null || descendant == null)
            {
                return null; // 검색 불가
            }

            Transform current = descendant; // 시작점
            while (current != null && current.parent != null && current.parent != root)
            {
                current = current.parent; // 직계 자식까지 상승
            }

            return current != null && current.parent == root ? current : null; // 직계 자식이면 반환
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

        private readonly struct ChainCandidate // 체인 번개 후보
        {
            public readonly EnemyController Enemy; // 대상 몬스터
            public readonly int Id; // 중복 방지 ID
            public readonly Vector3 Center; // 중심 위치
            public readonly float Weight; // 선택 가중치

            public ChainCandidate(EnemyController enemy, int id, Vector3 center, float weight)
            {
                Enemy = enemy;
                Id = id;
                Center = center;
                Weight = weight;
            }
        }

        // 반동 대상의 원래 로컬 pose 저장값
        private readonly struct RecoilTargetPose
        {
            public readonly Transform Target; // 반동 대상
            public readonly Vector3 LocalPosition; // 원래 위치
            public readonly Quaternion LocalRotation; // 원래 회전

            public RecoilTargetPose(Transform target, Vector3 localPosition, Quaternion localRotation)
            {
                Target = target; // 대상 저장
                LocalPosition = localPosition; // 위치 저장
                LocalRotation = localRotation; // 회전 저장
            }
        }
    }
}
