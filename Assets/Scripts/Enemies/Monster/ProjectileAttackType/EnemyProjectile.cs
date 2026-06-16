using UnityEngine;
namespace TeamProject01.Gameplay
{
    public sealed class EnemyProjectile : MonoBehaviour //원거리 투사체(투사체 자체에 데미지가 있어, 다른 원거리 몬스터 만들면 이곳에서 데미지 수정)
    {
        [SerializeField] private Transform target; // 투사체가 날아갈 목표, 보통 Nexus

        [Min(0)]
        [SerializeField] private int damage = 1; // Nexus에 줄 피해량

        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 8f; // 투사체 이동 속도

        [Min(0.1f)]
        [SerializeField] private float hitRadius = 0.6f; // 목표에 닿았다고 판단할 거리

        [Min(0.1f)]
        [SerializeField] private float lifeTime = 5f; // 투사체 최대 생존 시간

        private float lifeTimer; // 투사체가 생성된 뒤 지난 시간

        private void Update() 
        {
            lifeTimer += Time.deltaTime; // 지난 시간만큼 생존 시간을 증가시킨다.

            if (lifeTimer >= lifeTime) // 생존 시간이 제한 시간을 넘었다면
            {
                Destroy(gameObject); // 투사체를 제거한다.
                return; // 더 이상 처리하지 않는다.
            }

            if (target == null) // 목표가 없다면
            {
                Destroy(gameObject); // 갈 곳이 없으므로 투사체를 제거한다.
                return; // 더 이상 처리하지 않는다.
            }

            Vector3 offset = target.position - transform.position; // 투사체 위치에서 목표까지의 방향과 거리
            offset.y = 0f; // 3D 평면 기준으로 계산하기 위해 높이 차이를 제거한다.

            if (offset.sqrMagnitude <= hitRadius * hitRadius) // 목표와의 거리가 충돌 거리 안이라면
            {
                NexusController.TryApplyDamage(target, damage); // Nexus에 피해를 요청한다.
                Destroy(gameObject); // 충돌 후 투사체를 제거한다.
                return; // 더 이상 이동하지 않는다.
            }

            Vector3 direction = offset.normalized; // 목표 방향을 길이 1짜리 방향 벡터로 만든다.
            transform.position += direction * (moveSpeed * Time.deltaTime); // 목표 방향으로 이동한다.
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 이동 방향을 바라보게 회전한다.
        }

        public void Configure(Transform target) // EnemyRangedAttack이 투사체 목표 초기값을 넣어주는 함수
        {
            this.target = target; // 매개변수 target을 내부 target field에 저장한다.
            lifeTimer = 0f; // 생존 시간을 0으로 초기화한다.
        }
    }
}