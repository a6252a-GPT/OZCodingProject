using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class SegmentProjectileRuntime : MonoBehaviour // 데이터 기반 투사체
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
        // 투석기 돌이 곡사 착지 후 바닥을 구르는 상태인지 저장
        private bool isRollingAfterArcLanding; // 착지 후 구르기 진행 중
        // 착지 후 굴러가기 시작/끝 위치
        private Vector3 landingRollStartPosition; // 구르기 시작 위치
        private Vector3 landingRollEndPosition; // 구르기 종료 위치
        // 착지 후 굴러가는 방향과 회전축
        private Vector3 landingRollDirection; // 바닥 구르기 방향
        private Vector3 landingRollSpinAxis; // 돌이 굴러가는 회전축
        // 착지 후 굴러가기 시간 계산값
        private float landingRollTimer; // 구르기 진행 시간
        private float landingRollDuration; // 구르기 전체 시간
        private int remainingSawBounces; // 남은 톱날 연쇄 수
        private int currentSawTargetId; // 현재 톱날 목표 ID
        private float sawSpinAngle; // 톱날 회전 누적 각도
        private float effectiveProjectileSpeed; // 강화 반영 속도
        private float effectiveExplosionRadius; // 강화 반영 폭발 반경

        public static SegmentProjectileRuntime Spawn(Transform root, GameObject prefab, Vector3 position, Vector3 direction, EnemyController target, SegmentAttackProfile profile, DamageData damage, WeaponStatBonusData weaponBonus = default) // 생성 (weaponBonus=카드 강화 누적값)
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

        private void Configure(Vector3 fireDirection, EnemyController target, SegmentAttackProfile profile, DamageData damage, WeaponStatBonusData weaponBonus) // 값 주입 (프로필+강화 합산)
        {
            this.profile = profile; // 프로필
            this.target = target; // 목표
            this.damage = damage; // 피해
            direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : transform.forward; // 방향
            ApplyProjectileScale(); // 프로필 크기 적용
            lifeTimer = profile != null ? Mathf.Max(0.1f, profile.ProjectileLifetime) : 0.1f; // 수명
            effectiveProjectileSpeed = profile != null
                ? Mathf.Max(0.1f, profile.ProjectileSpeed + weaponBonus.ProjectileSpeedBonus) // 기본+강화 속도
                : 0.1f;
            remainingPierces = profile != null
                ? Mathf.Max(1, profile.PierceCount + weaponBonus.PierceCountBonus) // 기본+강화 관통
                : 1;
            effectiveExplosionRadius = profile != null
                ? Mathf.Max(0.1f, profile.ExplosionRadius + weaponBonus.ExplosionRadiusBonus) // 기본+강화 폭발
                : 0.1f;
            startPosition = transform.position; // 시작
            float targetAimHeight = profile != null ? profile.TargetAimHeight : 0.45f; // 조준 높이
            endPosition = target != null ? target.transform.position + Vector3.up * targetAimHeight : startPosition + direction * 8f; // 도착
            float distance = Vector3.Distance(startPosition, endPosition); // 거리
            arcDuration = profile != null ? Mathf.Max(0.05f, distance / effectiveProjectileSpeed) : 0.05f; // 곡사 시간
            arcTimer = 0f; // 진행 초기화
            isRollingAfterArcLanding = false; // 착지 후 구르기 초기화
            landingRollTimer = 0f; // 구르기 진행 초기화
            hitEnemyIds.Clear(); // 중복 초기화
            explosionEnemyIds.Clear(); // 중복 초기화
            remainingSawBounces = profile != null ? Mathf.Max(0, profile.MaxChainDepth) : 0; // 톱날 연쇄 초기화
            currentSawTargetId = target != null ? target.EnemyId : 0; // 최초 목표 저장
            sawSpinAngle = 0f; // 톱날 회전 초기화
        }

        private float GetProjectileSpeed() // 강화 반영 속도
        {
            float speed = effectiveProjectileSpeed > 0f ? effectiveProjectileSpeed : (profile != null ? profile.ProjectileSpeed : 0.1f); // fallback
            return Mathf.Max(0.1f, speed); // 최소 속도
        }

        private float GetExplosionRadius() // 강화 반영 폭발 반경
        {
            float radius = effectiveExplosionRadius > 0f ? effectiveExplosionRadius : (profile != null ? profile.ExplosionRadius : 0.1f); // fallback
            return Mathf.Max(0.1f, radius); // 최소 반경
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

            if (isRollingAfterArcLanding)
            {
                UpdateLandingRoll(); // 투석기 돌 착지 후 구르기
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
                case SegmentAttackMoveType.SawBounceProjectile:
                    UpdateSawBounceProjectile(); // 톱날 관통 연쇄
                    break;
                default:
                    UpdateStraightProjectile(); // 직선/관통
                    break;
            }
        }


        private void ApplyProjectileScale() // 프로필 투사체 크기 적용
        {
            if (profile == null)
            {
                return;
            }

            Vector3 scale = profile.ProjectileScale; // 프로필 크기
            if (scale.x <= 0f || scale.y <= 0f || scale.z <= 0f)
            {
                return; // 기본 프리팹 크기 유지
            }

            transform.localScale = scale; // 런타임 투사체 크기
        }
    }
}
