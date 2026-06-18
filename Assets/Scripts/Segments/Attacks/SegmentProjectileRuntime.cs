using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

        public static SegmentProjectileRuntime Spawn(Transform root, GameObject prefab, Vector3 position, Vector3 direction, EnemyController target, SegmentAttackProfile profile, DamageData damage) // 생성
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

            runtime.Configure(direction, target, profile, damage); // 값 주입
            return runtime;
        }

        private void Configure(Vector3 fireDirection, EnemyController target, SegmentAttackProfile profile, DamageData damage) // 값 주입
        {
            this.profile = profile; // 프로필
            this.target = target; // 목표
            this.damage = damage; // 피해
            direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : transform.forward; // 방향
            lifeTimer = profile != null ? Mathf.Max(0.1f, profile.ProjectileLifetime) : 0.1f; // 수명
            remainingPierces = profile != null ? Mathf.Max(1, profile.PierceCount) : 1; // 관통 수
            startPosition = transform.position; // 시작
            float targetAimHeight = profile != null ? profile.TargetAimHeight : 0.45f; // 조준 높이
            endPosition = target != null ? target.transform.position + Vector3.up * targetAimHeight : startPosition + direction * 8f; // 도착
            float distance = Vector3.Distance(startPosition, endPosition); // 거리
            arcDuration = profile != null ? Mathf.Max(0.05f, distance / Mathf.Max(0.1f, profile.ProjectileSpeed)) : 0.05f; // 곡사 시간
            arcTimer = 0f; // 진행 초기화
            isRollingAfterArcLanding = false; // 착지 후 구르기 초기화
            landingRollTimer = 0f; // 구르기 진행 초기화
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
                default:
                    UpdateStraightProjectile(); // 직선/관통
                    break;
            }
        }

        private void UpdateStraightProjectile() // 직선 이동
        {
            transform.position += direction * (profile.ProjectileSpeed * Time.deltaTime); // 이동
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
                if (ShouldRollAfterArcLanding())
                {
                    BeginLandingRoll(endPosition); // SG03 돌은 착지 후 일정 거리 굴러간 뒤 처리
                    return;
                }

                ApplyImpactAt(endPosition, target); // 도착 처리
                return;
            }

            if (ShouldRollAfterArcLanding())
            {
                return; // 투석기 돌은 비행 중 충돌하지 않고 바닥 착지 후 처리
            }

            TryApplyHitAt(transform.position); // 비행 중 명중
        }

        // 프로필에서 착지 후 굴러가기를 켠 곡사 투사체인지 확인
        private bool ShouldRollAfterArcLanding()
        {
            return profile != null
                && profile.MoveType == SegmentAttackMoveType.ArcProjectile
                && profile.RollAfterArcLanding
                && profile.LandingRollDistance > 0f
                && profile.LandingRollDuration > 0f;
        }

        // 곡사 도착 지점에서 바닥 구르기 상태로 전환
        private void BeginLandingRoll(Vector3 landingPosition)
        {
            isRollingAfterArcLanding = true; // 구르기 상태 시작
            landingRollTimer = 0f; // 진행 시간 초기화
            landingRollDuration = Mathf.Max(0.01f, profile.LandingRollDuration); // 프로필 시간 보정
            landingRollDirection = ResolveLandingRollDirection(); // 수평 구르기 방향
            landingRollSpinAxis = Vector3.Cross(Vector3.up, landingRollDirection); // 굴러가는 축
            if (landingRollSpinAxis.sqrMagnitude <= 0.0001f)
            {
                landingRollSpinAxis = Vector3.right; // 회전축 fallback
            }

            landingRollSpinAxis.Normalize(); // 회전축 정규화
            landingRollStartPosition = landingPosition; // 착지 위치
            landingRollEndPosition = landingRollStartPosition + landingRollDirection * profile.LandingRollDistance; // 종료 위치
            transform.position = landingRollStartPosition; // 바닥 위치 보정
            ApplyLandingImpactDamage(landingRollStartPosition); // 착지 순간 작은 범위 피해
            if (landingRollDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(landingRollDirection, Vector3.up); // 굴러갈 방향으로 정렬
            }
        }

        // 착지 후 돌이 일정 거리 굴러가며 지나가는 적에게 피해를 준다
        private void UpdateLandingRoll()
        {
            landingRollTimer += Time.deltaTime; // 진행 시간 증가
            float t = Mathf.Clamp01(landingRollTimer / landingRollDuration); // 진행률
            float eased = 1f - (1f - t) * (1f - t); // 살짝 감속하는 느낌
            transform.position = Vector3.Lerp(landingRollStartPosition, landingRollEndPosition, eased); // 바닥 이동
            float spinAmount = profile.LandingRollSpinSpeed * Time.deltaTime; // 이번 프레임 회전량
            if (spinAmount > 0f)
            {
                transform.Rotate(landingRollSpinAxis, spinAmount, Space.World); // 돌 굴러가는 회전
            }

            ApplyLandingRollDamage(transform.position); // 구르는 동안 접촉 피해
            if (t >= 1f)
            {
                isRollingAfterArcLanding = false; // 구르기 종료
                Destroy(gameObject); // 끝 폭발 없이 제거
            }
        }

        // 돌이 착지 후 어느 방향으로 굴러갈지 수평 방향 계산
        private Vector3 ResolveLandingRollDirection()
        {
            Vector3 rollDirection = endPosition - startPosition; // 곡사가 날아온 수평 방향 우선
            rollDirection.y = 0f; // 바닥 이동만 사용
            if (rollDirection.sqrMagnitude > 0.0001f)
            {
                return rollDirection.normalized; // 진행 방향 사용
            }

            rollDirection = direction; // 발사 방향 fallback
            rollDirection.y = 0f; // 바닥 이동만 사용
            if (rollDirection.sqrMagnitude > 0.0001f)
            {
                return rollDirection.normalized; // 발사 방향 사용
            }

            rollDirection = transform.forward; // 모델 방향 fallback
            rollDirection.y = 0f; // 바닥 이동만 사용
            return rollDirection.sqrMagnitude > 0.0001f ? rollDirection.normalized : Vector3.forward; // 최종 fallback
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
            ApplyExplosion(position, profile.ExplosionRadius, explosionEnemyIds, true); // 일반 폭발
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
                Vector3 hitPosition = enemy.transform.position + Vector3.up * profile.TargetAimHeight; // 명중 위치
                enemy.ApplyDamage(explosionDamage.WithHitPosition(hitPosition)); // 범위 피해
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
                Vector3 hitPosition = enemy.transform.position + Vector3.up * profile.TargetAimHeight; // 명중 위치
                enemy.ApplyDamage(rollDamage.WithHitPosition(hitPosition)); // 구르기 피해 적용
                PlayHitVfx(hitPosition); // 명중 VFX
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
            PlayExplosionVfx(position, profile.ExplosionRadius); // 기본 폭발 반경 사용
        }

        private void PlayExplosionVfx(Vector3 position, float radius) // 지정 반경으로 폭발 VFX
        {
            if (profile.ExplosionVfxPrefab == null)
            {
                return; // 지정 없음
            }

            GameObject instance = Instantiate(profile.ExplosionVfxPrefab, position, Quaternion.identity); // 생성
            instance.transform.localScale = Vector3.one * (Mathf.Max(0f, radius) * 2f); // 범위 표시
            ApplyExplosionVfxTransparency(instance); // 임시 구체 반투명 처리
            float lifetime = profile.ExplosionVfxLifetime > 0f ? profile.ExplosionVfxLifetime : profile.ExplosionLifetime; // 제거 시간
            if (lifetime > 0f)
            {
                Destroy(instance, lifetime); // 제거
            }
        }

        private void ApplyExplosionVfxTransparency(GameObject instance) // 임시 폭발 범위 구체를 반투명하게 보정
        {
            if (instance == null)
            {
                return; // 대상 없음
            }

            float alpha = profile != null ? Mathf.Clamp01(profile.ExplosionVfxAlpha) : 0.28f; // 프로필 투명도
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true); // 하위 렌더러 포함
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i]; // 렌더러
                renderer.shadowCastingMode = ShadowCastingMode.Off; // 투명 범위 표시는 그림자 제거
                renderer.receiveShadows = false; // 그림자 수신 제거
                Material[] materials = renderer.materials; // 인스턴스 재질
                for (int j = 0; j < materials.Length; j++)
                {
                    ConfigureTransparentMaterial(materials[j], alpha); // 재질 투명 설정
                }
            }
        }

        private static void ConfigureTransparentMaterial(Material material, float alpha) // URP/기본 셰이더 투명 설정
        {
            if (material == null)
            {
                return; // 재질 없음
            }

            if (material.HasProperty("_BaseColor"))
            {
                Color color = material.GetColor("_BaseColor"); // URP 기본 색
                color.a = alpha; // 알파 적용
                material.SetColor("_BaseColor", color); // 저장
            }

            if (material.HasProperty("_Color"))
            {
                Color color = material.GetColor("_Color"); // 기본 색
                color.a = alpha; // 알파 적용
                material.SetColor("_Color", color); // 저장
            }

            material.SetOverrideTag("RenderType", "Transparent"); // 투명 렌더 타입
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f); // URP Transparent
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); // 알파 블렌드
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); // 알파 블렌드
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f); // 투명 정렬용 ZWrite Off
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP 투명 키워드
            material.EnableKeyword("_ALPHABLEND_ON"); // 기본 셰이더 투명 키워드
            material.renderQueue = (int)RenderQueue.Transparent; // 투명 큐
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
