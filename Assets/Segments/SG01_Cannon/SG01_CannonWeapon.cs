using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG01_CannonWeapon : SegmentWeaponBehaviour // SG01 캐논 세그먼트 무기
    {
        [Min(0f)] public float BaseDamage = 1f; // 기본 피해량
        [Min(0.1f)] public float MinAttackInterval = 3f; // 최소 공격 간격
        [Min(0.1f)] public float MaxAttackInterval = 5f; // 최대 공격 간격
        [Min(0.1f)] public float SearchRange = 24f; // 적 탐색 거리
        [Min(0f)] public float AttackSpawnHeight = 0.42f; // fallback 탄 생성 높이
        [Min(0f)] public float TargetAimHeight = 0.45f; // 몬스터 조준 높이
        public GameObject ProjectilePrefab; // 투사체 프리팹
        [Min(0.1f)] public float ProjectileSpeed = 20f; // 투사체 속도
        [Min(0.05f)] public float ProjectileHitRadius = 0.5f; // 명중 반경
        [Min(0.1f)] public float ProjectileLifetime = 5f; // 투사체 생존시간
        [Header("Head Aim")]
        public Transform HeadYawPivot; // Joint/YawPivot 회전축
        public Transform Muzzle; // 발사 위치
        [Min(1f)] public float HeadTurnSpeed = 540f; // 머리 회전 속도
        [Min(0f)] public float FireAngleTolerance = 8f; // 발사 허용 각도
        public bool RequireAimBeforeFire = true; // 조준 후 발사

        private float fireTimer; // 남은 쿨타임

        private void Reset() // 기본값 보정
        {
            SegmentId = "SG01_Cannon"; // 새 세그먼트 ID
        }

        public override void TickWeapon(float deltaTime) // 무기 갱신
        {
            if (!CanUseWeapon())
            {
                return; // 발사 불가
            }

            fireTimer -= deltaTime; // 쿨타임 감소
            if (!TryFindTarget(out EnemyController target)) // 중요!! 몬스터 탐색
            {
                return; // 대상 없음
            }

            CoreStatData coreStats = CoreStatProvider.GetCurrentOrDefault(); // 코어 스탯
            bool aimed = AimHeadAtTarget(target, deltaTime); // 머즐 기준 머리 회전
            if (fireTimer > 0f)
            {
                return; // 조준만 유지
            }

            if (RequireAimBeforeFire && !aimed)
            {
                return; // 아직 조준 중
            }

            Fire(target, coreStats); // 중요!! 실제 발사
            ResetCooldown(coreStats); // 공격 후 쿨타임
        }

        private bool CanUseWeapon() // 작동 가능 확인
        {
            return IsWeaponActive && Segment != null && Segment.Owner != null; // 연결 상태 확인
        }

        private bool TryFindTarget(out EnemyController target) // 대상 탐색
        {
            float range = GetUpgrade().ApplyRange(SearchRange); // 세그먼트 강화 거리 반영
            return EnemyController.TryFindNearest(transform.position, range, out target); // 중요!! 태그 기반 몬스터 탐색
        }

        private void Fire(EnemyController target, CoreStatData coreStats) // 발사 처리
        {
            float damage = CalculateDamage(coreStats); // 중요!! 피해 계산
            Transform muzzle = ResolveMuzzle(); // 포구 찾기
            Vector3 spawnPosition = muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackSpawnHeight; // 탄 위치
            DamageData damageData = DamageData.Create(damage, DamageType.Projectile, Segment.ChainIndex, spawnPosition, gameObject); // 중요!! 피해값 생성
            SG01_CannonProjectile.Spawn(Segment.Owner.GetProjectileRoot(), ProjectilePrefab, spawnPosition, target, ProjectileSpeed, ProjectileHitRadius, ProjectileLifetime, damageData); // 중요!! 무기 -> 투사체
        }

        private bool AimHeadAtTarget(EnemyController target, float deltaTime) // 머리 조준
        {
            Transform pivot = ResolveHeadYawPivot(); // 회전축
            if (pivot == null || target == null)
            {
                return true; // 회전축 없는 기존 프리팹 호환
            }

            Transform muzzle = ResolveMuzzle(); // 포구
            if (!TryGetHorizontalAim(target, pivot, muzzle, out Vector3 currentDirection, out Vector3 targetDirection))
            {
                return true; // 거의 같은 위치
            }

            float signedAngle = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up); // 현재 포구 방향 -> 몬스터 방향
            float maxStep = HeadTurnSpeed * deltaTime; // 이번 프레임 회전량
            float step = Mathf.Clamp(signedAngle, -maxStep, maxStep); // 과회전 방지
            pivot.Rotate(Vector3.up, step, Space.World); // 좌우 회전

            if (!TryGetHorizontalAim(target, pivot, muzzle, out currentDirection, out targetDirection))
            {
                return true; // 회전 후 같은 위치
            }

            return Mathf.Abs(Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up)) <= FireAngleTolerance; // 조준 완료
        }

        private bool TryGetHorizontalAim(EnemyController target, Transform pivot, Transform muzzle, out Vector3 currentDirection, out Vector3 targetDirection) // 포구 기준 조준 벡터
        {
            currentDirection = Vector3.zero; // 현재 포구 방향
            targetDirection = Vector3.zero; // 목표 방향
            Vector3 aimOrigin = muzzle != null ? muzzle.position : pivot.position; // 포구 위치 우선
            Vector3 targetPosition = target.transform.position + Vector3.up * TargetAimHeight; // 목표 중심
            targetDirection = targetPosition - aimOrigin; // 포구 -> 목표
            targetDirection.y = 0f; // 좌우 회전만
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 방향 없음
            }

            currentDirection = GetCurrentMuzzleDirection(pivot, muzzle); // 현재 포구/포신 방향
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 기준 방향 없음
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
                pivotToMuzzle.y = 0f; // 수평 방향
                if (pivotToMuzzle.sqrMagnitude > 0.0001f)
                {
                    return pivotToMuzzle; // 모델 포구 위치를 포신 방향으로 사용
                }

                Vector3 muzzleForward = muzzle.forward; // 포구 forward fallback
                muzzleForward.y = 0f;
                if (muzzleForward.sqrMagnitude > 0.0001f)
                {
                    return muzzleForward;
                }
            }

            Vector3 pivotForward = pivot.forward; // 피벗 forward fallback
            pivotForward.y = 0f;
            return pivotForward;
        }

        private Transform ResolveHeadYawPivot() // Joint/YawPivot 찾기
        {
            if (HeadYawPivot != null)
            {
                return HeadYawPivot; // 수동 연결 우선
            }

            Transform root = Segment != null ? Segment.transform : transform; // 세그먼트 루트
            HeadYawPivot = FindChildRecursive(root, "Joint"); // 사용자 배치 기준
            if (HeadYawPivot == null)
            {
                HeadYawPivot = FindChildRecursive(root, "YawPivot"); // 생성 프리팹 기준
            }

            return HeadYawPivot; // 없으면 기존 방식 유지
        }

        private Transform ResolveMuzzle() // 포구 찾기
        {
            if (Muzzle != null)
            {
                return Muzzle; // 수동 연결 우선
            }

            Transform pivot = ResolveHeadYawPivot(); // 회전축 기준
            Transform root = pivot != null ? pivot : (Segment != null ? Segment.transform : transform); // 검색 루트
            Muzzle = FindChildRecursive(root, "Muzzle"); // 포구
            return Muzzle; // 없으면 높이 fallback
        }

        private static Transform FindChildRecursive(Transform root, string childName) // 이름으로 하위 검색
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

                Transform found = FindChildRecursive(child, childName); // 재귀 검색
                if (found != null)
                {
                    return found; // 발견
                }
            }

            return null; // 없음
        }

        private float CalculateDamage(CoreStatData coreStats) // 피해 계산
        {
            return GetUpgrade().ApplyDamage(coreStats.ApplyDamage(BaseDamage)); // 코어 + 세그먼트 강화 반영
        }

        private void ResetCooldown(CoreStatData coreStats) // 쿨타임 재설정
        {
            float min = Mathf.Min(MinAttackInterval, MaxAttackInterval); // 최소값
            float max = Mathf.Max(MinAttackInterval, MaxAttackInterval); // 최대값
            float baseInterval = Random.Range(min, max); // 랜덤 간격
            float coreInterval = coreStats.ApplyFireInterval(baseInterval); // 코어 공속
            fireTimer = GetUpgrade().ApplyFireInterval(coreInterval); // 세그먼트 공속
        }
    }
}
