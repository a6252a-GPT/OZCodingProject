using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyMeleeAttack : MonoBehaviour //근거리 몬스터
    {
        [SerializeField] private Transform nexus; // 공격 타겟이 되는 Nexus Transform

        [Min(0)]
        [SerializeField] private int attackDamage = 1; // Nexus에 줄 피해량

        [Min(0.1f)]
        [SerializeField] private float attackRange = 1.6f; // Nexus를 공격할 수 있는 거리

        [Min(0.1f)]
        [SerializeField] private float attackDelay = 1.0f; // 공격 사이의 대기 시간, 공격속도 역할

        public float AttackRange // EnemyMovement가 근거리 공격 사거리를 읽기 위한 property
        {
            get
            {
                return attackRange; // 근거리 공격 가능 거리를 반환한다.
            }
        }

        private float attackTimer; // 다음 공격까지 남은 시간을 저장하는 변수

        private void Awake()
        {
            if (nexus == null) // Inspector에서 Nexus가 연결되지 않았다면
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

            attackTimer -= Time.deltaTime; // 지난 시간만큼 공격 대기 시간을 줄인다.

            if (attackTimer > 0f) // 아직 공격 대기 시간이 남아 있다면
            {
                return; // 이번 프레임에는 공격하지 않는다.
            }

            Vector3 offset = nexus.position - transform.position; // 현재 몬스터 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.
            offset.y = 0f; // 3D 평면 거리만 사용할 것이므로 높이 차이는 제거한다.

            if (offset.sqrMagnitude > attackRange * attackRange) // Nexus가 공격 사거리 밖이라면
            {
                return; // 공격하지 않고 종료한다.
            }

            NexusController.TryApplyDamage(nexus, attackDamage); // Nexus에 피해를 요청한다.
            attackTimer = attackDelay; // 공격 후 다음 공격까지 대기 시간을 다시 설정한다.
        }

        public void Configure(Transform nexus, int attackDamage, float attackRange, float attackDelay) // Spawner나 Controller가 공격 초기값을 넣어주는 함수
        {
            this.nexus = nexus; // 매개변수 nexus를 내부 nexus field에 저장한다.
            this.attackDamage = attackDamage; // 매개변수 attackDamage를 내부 attackDamage field에 저장한다.
            this.attackRange = attackRange; // 매개변수 attackRange를 내부 attackRange field에 저장한다.
            this.attackDelay = attackDelay; // 매개변수 attackDelay를 내부 attackDelay field에 저장한다.
        }
    }
}