using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SegmentProjectileRuntime : MonoBehaviour // 데이터 기반 투사체
    {
        private readonly List<int> hitEnemyIds = new List<int>(8); // 관통 중복 방지
        private readonly List<int> explosionEnemyIds = new List<int>(16); // 폭발 중복 방지

        private SegmentAttackProfile profile; // 공격 데이터
        private EnemyController target; // 목표
        private DamageData damage; // 피해값
        private Transform hitVfxSocket; // 명중 VFX 기준점
        private Vector3 direction; // 직선 방향
        private Vector3 startPosition; // 곡사 시작
        private Vector3 endPosition; // 곡사 도착
        private float lifeTimer; // 남은 시간
        private float arcTimer; // 곡사 진행 시간
        private float arcDuration; // 곡사 전체 시간
        private int remainingPierces; // 남은 관통 수
        private float effectiveProjectileSpeed; // 강화 반영 속도
        private float effectiveExplosionRadius; // 강화 반영 폭발 반경

        public static SegmentProjectileRuntime Spawn(Transform root, GameObject prefab, Vector3 position, Vector3 direction, EnemyController target, SegmentAttackProfile profile, DamageData damage, WeaponStatBonusData weaponBonus = default) // 생성
        {
            GameObject instance;
            if (prefab != null)
            {
                Quaternion rotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : Quaternion.identity; // 방향
                instance = Instantiate(prefab, position, rotation, root); // 프리팹 생성
            }
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Sphere); // fallback 탄
                instance.name = "GenericProjectile";
                instance.transform.SetParent(root, false);
                instance.transform.position = position;
                instance.transform.localScale = Vector3.one * 0.35f;
                Destroy(instance.GetComponent<Collider>()); // 표시 전용
            }

            SegmentProjectileRuntime runtime = instance.GetComponent<SegmentProjectileRuntime>(); // 런타임
            if (runtime == null)
            {
                runtime = instance.AddComponent<SegmentProjectileRuntime>(); // 자동 보강
            }

            runtime.Configure(direction, target, profile, damage, weaponBonus); // 값 주입
            return runtime;
        }

        private void Configure(Vector3 fireDirection, EnemyController target, SegmentAttackProfile profile, DamageData damage, WeaponStatBonusData weaponBonus) // 값 주입
        {
            this.profile = profile; // 프로필
            this.target = target; // 목표
            this.damage = damage; // 피해
            direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : transform.forward; // 방향
            lifeTimer = profile != null ? Mathf.Max(0.1f, profile.ProjectileLifetime) : 0.1f; // 수명
            effectiveProjectileSpeed = profile != null
                ? Mathf.Max(0.1f, profile.ProjectileSpeed + weaponBonus.ProjectileSpeedBonus)
                : 0.1f; // 속도
            remainingPierces = profile != null
                ? Mathf.Max(1, profile.PierceCount + weaponBonus.PierceCountBonus)
                : 1; // 관통 수
            effectiveExplosionRadius = profile != null
                ? Mathf.Max(0.1f, profile.ExplosionRadius + weaponBonus.ExplosionRadiusBonus)
                : 0.1f; // 폭발 반경
            startPosition = transform.position; // 시작
            float targetAimHeight = profile != null ? profile.TargetAimHeight : 0.45f; // 조준 높이
            endPosition = target != null ? target.transform.position + Vector3.up * targetAimHeight : startPosition + direction * 8f; // 도착
            float distance = Vector3.Distance(startPosition, endPosition); // 거리
            arcDuration = profile != null ? Mathf.Max(0.05f, distance / effectiveProjectileSpeed) : 0.05f; // 곡사 시간
            arcTimer = 0f; // 진행 초기화
            hitEnemyIds.Clear(); // 중복 초기화
            explosionEnemyIds.Clear(); // 중복 초기화
        }

        private void Update() // 이동 루프
        {
            if (profile == null || !damage.IsValid)
            {
                Destroy(gameObject); // 데이터 없음
                return;
            }

            lifeTimer -= Time.deltaTime; // 수명 감소
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject); // 만료
                return;
            }

            switch (profile.MoveType)
            {
                case SegmentAttackMoveType.ArcProjectile:
                    UpdateArcProjectile(); // 곡사
                    break;
                case SegmentAttackMoveType.HomingProjectile:
                    UpdateHomingProjectile(); // 추적
                    break;
                default:
                    UpdateStraightProjectile(); // 직선/관통
                    break;
            }
        }

        private void UpdateStraightProjectile() // 직선 이동
        {
            transform.position += direction * (effectiveProjectileSpeed * Time.deltaTime); // 이동
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 방향
            }

            TryApplyHitAt(transform.position); // 명중 확인
        }

        private void UpdateHomingProjectile() // 추적 이동
        {
            if (target != null)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * profile.TargetAimHeight; // 목표 중심
                Vector3 offset = targetPosition - transform.position; // 목표 방향
                if (offset.sqrMagnitude > 0.0001f)
                {
                    direction = offset.normalized; // 방향 갱신
                }
            }

            UpdateStraightProjectile(); // 이동 공유
        }

        private void UpdateArcProjectile() // 곡사 이동
        {
            arcTimer += Time.deltaTime; // 진행
            float t = Mathf.Clamp01(arcTimer / arcDuration); // 비율
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t); // 직선 보간
            position.y += Mathf.Sin(t * Mathf.PI) * profile.ArcHeight; // 포물선 높이
            Vector3 previous = transform.position; // 이전 위치
            transform.position = position; // 이동
            Vector3 moveDirection = position - previous; // 이동 방향
            if (moveDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up); // 방향
            }

            if (t >= 1f)
            {
                ApplyImpactAt(endPosition, target); // 도착 처리
                return;
            }

            TryApplyHitAt(transform.position); // 비행 중 명중
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

                Vector3 hitPosition = enemy.transform.position + Vector3.up * profile.TargetAimHeight; // 명중 위치
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
                enemy.ApplyDamage(damage.WithHitPosition(position)); // 직접 피해
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
            PlayExplosionVfx(position); // 폭발 VFX
            DamageData explosionDamage = DamageData.Create(damage.Amount, DamageType.Explosion, damage.SourceSegmentIndex, position, damage.SourceObject); // 폭발 피해
            Collider[] hits = Physics.OverlapSphere(position, effectiveExplosionRadius); // 범위 검색
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController enemy = hits[i].GetComponentInParent<EnemyController>(); // 몬스터
                if (enemy == null || explosionEnemyIds.Contains(enemy.EnemyId))
                {
                    continue; // 대상 아님/중복
                }

                explosionEnemyIds.Add(enemy.EnemyId); // 중복 방지
                Vector3 hitPosition = enemy.transform.position + Vector3.up * profile.TargetAimHeight; // 명중 위치
                enemy.ApplyDamage(explosionDamage.WithHitPosition(hitPosition)); // 범위 피해
            }
        }

        private void PlayHitVfx(Vector3 position) // 명중 VFX
        {
            if (profile.HitVfxPrefab == null)
            {
                return; // 지정 없음
            }

            Transform socket = ResolveHitVfxSocket(); // 기준점
            Vector3 spawnPosition = socket != null ? socket.position : position; // 위치
            Quaternion rotation = socket != null ? socket.rotation : Quaternion.identity; // 방향
            GameObject instance = Instantiate(profile.HitVfxPrefab, spawnPosition, rotation); // 생성
            if (profile.HitVfxLifetime > 0f)
            {
                Destroy(instance, profile.HitVfxLifetime); // 제거
            }
        }

        private Transform ResolveHitVfxSocket() // 명중 VFX 기준점
        {
            if (hitVfxSocket != null)
            {
                return hitVfxSocket; // 캐시
            }

            hitVfxSocket = FindChildRecursive(transform, "VFX_Hit"); // 정식 이름
            if (hitVfxSocket == null)
            {
                hitVfxSocket = FindChildRecursive(transform, "HitVFX"); // fallback
            }

            return hitVfxSocket;
        }

        private void PlayExplosionVfx(Vector3 position) // 폭발 VFX
        {
            if (profile.ExplosionVfxPrefab == null)
            {
                return; // 지정 없음
            }

            GameObject instance = Instantiate(profile.ExplosionVfxPrefab, position, Quaternion.identity); // 생성
            instance.transform.localScale = Vector3.one * (effectiveExplosionRadius * 2f); // 범위 표시
            float lifetime = profile.ExplosionVfxLifetime > 0f ? profile.ExplosionVfxLifetime : profile.ExplosionLifetime; // 제거 시간
            if (lifetime > 0f)
            {
                Destroy(instance, lifetime); // 제거
            }
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
