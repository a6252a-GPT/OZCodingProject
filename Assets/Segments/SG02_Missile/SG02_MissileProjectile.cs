using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG02_MissileProjectile : MonoBehaviour // 폭발미사일 투사체
    {
        public EnemyController Target; // 추적 대상
        public DamageData Damage; // 폭발 피해값
        public GameObject ExplosionPrefab; // 폭발 프리팹
        [Min(0.1f)] public float Speed = 14f; // 미사일 속도
        [Min(0.05f)] public float HitRadius = 0.7f; // 명중 반경
        [Min(0.1f)] public float ExplosionRadius = 3f; // 폭발 반경
        [Min(0.05f)] public float ExplosionLifetime = 0.35f; // 폭발 시간

        private float lifeTimer; // 남은 시간

        public static void Spawn(Transform root, GameObject projectilePrefab, GameObject explosionPrefab, Vector3 position, EnemyController target, float speed, float hitRadius, float lifetime, float explosionRadius, float explosionLifetime, DamageData damage) // 생성
        {
            SG02_MissileProjectile projectile = Instantiate(projectilePrefab, position, Quaternion.identity, root).GetComponent<SG02_MissileProjectile>(); // 프리팹 생성
            projectile.Target = target; // 대상
            projectile.Damage = damage; // 중요!! 무기 → 투사체 받기
            projectile.Speed = speed; // 속도
            projectile.HitRadius = hitRadius; // 명중 반경
            projectile.ExplosionPrefab = explosionPrefab; // 폭발 프리팹
            projectile.ExplosionRadius = explosionRadius; // 폭발 반경
            projectile.ExplosionLifetime = explosionLifetime; // 폭발 시간
            projectile.lifeTimer = lifetime; // 생존시간
        }

        private void Update() // 이동 루프
        {
            lifeTimer -= Time.deltaTime; // 시간 감소
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject); // 시간 만료
                return; // 종료
            }

            if (Target == null)
            {
                Destroy(gameObject); // 목표 없음
                return; // 종료
            }

            Vector3 targetPosition = Target.transform.position + Vector3.up * 0.45f; // 목표 중심
            Vector3 offset = targetPosition - transform.position; // 목표 방향
            float distance = offset.magnitude; // 거리
            if (distance <= HitRadius)
            {
                Explode(targetPosition); // 목표 폭발
                return; // 종료
            }

            Vector3 direction = offset.normalized; // 이동 방향
            transform.position += direction * (Speed * Time.deltaTime); // 추적 이동
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 방향 회전
        }

        private void Explode(Vector3 position) // 폭발 처리
        {
            SG02_MissileExplosion.Spawn(transform.parent, ExplosionPrefab, position, ExplosionRadius, ExplosionLifetime, Damage.WithHitPosition(position)); // 중요!! 투사체 → 폭발
            Destroy(gameObject); // 미사일 제거
        }
    }
}
