using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG01_MachineGunProjectile : MonoBehaviour // 기관총 투사체
    {
        public EnemyController Target; // 추적 대상
        public DamageData Damage; // 피해 전달값
        [Min(0.1f)] public float Speed = 20f; // 탄속
        [Min(0.05f)] public float HitRadius = 0.5f; // 명중 반경
        [Min(0.1f)] public float Lifetime = 5f; // 생존 시간

        public static void Spawn(Transform root, GameObject prefab, Vector3 position, EnemyController target, float speed, float hitRadius, float lifetime, DamageData damage) // 탄 생성
        {
            SG01_MachineGunProjectile projectile = Instantiate(prefab, position, Quaternion.identity, root).GetComponent<SG01_MachineGunProjectile>(); // 프리팹 생성
            projectile.Target = target; // 목표 설정
            projectile.Damage = damage; // 중요!! 무기 → 투사체 받기
            projectile.Speed = speed; // 탄속 설정
            projectile.HitRadius = hitRadius; // 명중 반경
            projectile.Lifetime = lifetime; // 수명 설정
        }

        private void Update() // 이동 루프
        {
            Lifetime -= Time.deltaTime; // 수명 감소
            if (Lifetime <= 0f)
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
                Target.ApplyDamage(Damage.WithHitPosition(targetPosition)); // 중요!! 투사체 → 몬스터
                Destroy(gameObject); // 탄 제거
                return; // 종료
            }

            Vector3 direction = offset.normalized; // 이동 방향
            transform.position += direction * (Speed * Time.deltaTime); // 목표 이동
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 진행 방향
        }
    }
}

