using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyRangedAttack : MonoBehaviour //원거리 몬스터
    {
        [SerializeField] private Transform nexus; // 공격 타겟 Nexus설정
        [SerializeField] private Transform firePoint; // 투사체가 발사될 위치

        [SerializeField] private EnemyProjectile projectilePrefab; // 발사할 EnemyProjectile Prefab

        [Min(0.1f)]
        [SerializeField] private float attackRange = 6f; // 원거리 몬스터가 공격할 수 있는 거리

        [Min(0.1f)]
        [SerializeField] private float attackDelay = 1.5f; // 공격 사이의 대기 시간, 공격속도 역할

        public float AttackRange // EnemyMovement가 원거리 공격 사거리를 읽기 위한 property
        {
            get
            {
                return attackRange; // 원거리 공격 가능 거리를 반환한다.
            }
        }

        private float attackTimer; // 다음 공격까지 남은 시간을 저장하는 변수

        private void Awake()
        {
            if (nexus == null) // Nexus가 연결되지 않았다면
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 씬에서 이름이 Nexus_Core인 GameObject를 찾는다.
                nexus = nexusObject != null ? nexusObject.transform : null; // 찾았다면 Transform을 저장하고, 못 찾았다면 null로 둔다.
            }
        }

        private void Update()
        {
            if (nexus == null) // 공격 대상이 없다면
            {
                return; // 공격하지 않고 종료한다.
            }

            if (projectilePrefab == null) // 발사할 투사체 Prefab이 없다면
            {
                return; // 투사체를 만들 수 없으므로 종료한다.
            }

            attackTimer -= Time.deltaTime; // 지난 시간만큼 공격 대기 시간을 줄인다.

            if (attackTimer > 0f) // 아직 공격 대기 시간이 남아 있다면
            {
                return; // 이번 프레임에는 공격하지 않는다.
            }

            Vector3 offset = nexus.position - transform.position; // 현재 몬스터 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.
            offset.y = 0f; // 높이 차이는 제거한다.

            if (offset.sqrMagnitude > attackRange * attackRange) // Nexus가 공격 사거리 밖이라면
            {
                return; // 공격하지 않고 종료한다.
            }

            Shoot(); // 투사체를 발사한다.
            attackTimer = attackDelay; // 공격 후 다음 공격까지 대기 시간을 다시 설정한다.
        }

        private void Shoot()
        {
            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position; // FirePoint가 있으면 그 위치에서, 없으면 몬스터 위치에서 발사한다.

            Vector3 offset = nexus.position - spawnPosition; // 발사 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.
            offset.y = 0f; // 높이 차이는 제거한다.

            Quaternion spawnRotation = transform.rotation; // 기본 회전값은 현재 몬스터 회전값으로 둔다.

            if (offset.sqrMagnitude > 0f) // Nexus 방향을 계산할 수 있다면
            {
                spawnRotation = Quaternion.LookRotation(offset.normalized, Vector3.up); // 투사체가 Nexus 방향을 바라보게 회전값을 만든다.
            }

            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation); // 투사체 Prefab을 생성한다.
            projectile.Configure(nexus); // 생성된 투사체에 목표 Nexus만 전달한다.
        }

        public void Configure(Transform nexus, EnemyProjectile projectilePrefab, float attackRange, float attackDelay) // Spawner나 Controller가 원거리 공격 초기값을 넣어주는 함수
        {
            this.nexus = nexus; // 공격 대상 Nexus를 저장한다.
            this.projectilePrefab = projectilePrefab; // 발사할 투사체 Prefab을 저장한다.
            this.attackRange = attackRange; // 공격 사거리를 저장한다.
            this.attackDelay = attackDelay; // 공격 딜레이를 저장한다.
        }
    }
}