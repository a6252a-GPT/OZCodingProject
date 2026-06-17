using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG03_RapidShotProjectile : MonoBehaviour // 속사 세그먼트 투사체
    {
        public EnemyController Target; // 따라갈 몬스터
        public DamageData Damage; // 무기에서 받은 피해 데이터
        [Min(0.1f)] public float Speed = 26f; // 이동 속도
        [Min(0.05f)] public float HitRadius = 0.45f; // 명중 판정 반경
        [Min(0.1f)] public float Lifetime = 4f; // 탄 생존 시간

        public static void Spawn(Transform root, GameObject prefab, Vector3 position, EnemyController target, float speed, float hitRadius, float lifetime, DamageData damage) // 탄 생성 입구
        {
            SG03_RapidShotProjectile projectile = Instantiate(prefab, position, Quaternion.identity, root).GetComponent<SG03_RapidShotProjectile>();
            projectile.Target = target; // 무기가 찾은 몬스터를 저장한다.
            projectile.Damage = damage; // 세그먼트가 만든 DamageData를 들고 이동한다.
            projectile.Speed = speed; // 무기 설정값 반영
            projectile.HitRadius = hitRadius; // 무기 설정값 반영
            projectile.Lifetime = lifetime; // 무기 설정값 반영
        }

        private void Update() // 투사체 이동 루프
        {
            Lifetime -= Time.deltaTime; // 시간이 지나면 탄을 제거한다.
            if (Lifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (Target == null)
            {
                Destroy(gameObject); // 목표가 이미 죽었거나 사라지면 탄도 제거한다.
                return;
            }

            Vector3 targetPosition = Target.transform.position + Vector3.up * 0.45f; // 몬스터 몸통 중심을 겨냥한다.
            Vector3 offset = targetPosition - transform.position;
            float distance = offset.magnitude;

            if (distance <= HitRadius)
            {
                Target.ApplyDamage(Damage.WithHitPosition(targetPosition)); // 투사체 -> 몬스터: DamageData를 전달한다.
                Destroy(gameObject); // 맞은 탄은 제거한다.
                return;
            }

            Vector3 direction = offset.normalized; // 목표 방향으로 직선 이동한다.
            transform.position += direction * (Speed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
