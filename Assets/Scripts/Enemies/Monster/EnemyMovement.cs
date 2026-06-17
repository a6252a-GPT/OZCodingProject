using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyMovement : MonoBehaviour //몬스터 이동
    {
        private const float FallbackStopRadius = 1.6f; // 공격 Script가 없을 때만 사용할 예비 정지 거리

        [SerializeField] private Transform nexus; // 이동 목표

        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 1.25f; // 몬스터 이동 속도

        [Min(0.05f)]
        [SerializeField] private float bodyRadius = 0.46f; // 몬스터가 이동할 때 세그먼트와 겹치거나 밀고 들어가는 것을 보정한다.

        [Min(0f)]
        [SerializeField] private float groundHeight = 0.72f; // 바닥 위에 몬스터를 올려둘 높이 오프셋

        public bool IsInStopRange { get; private set; } // 현재 Nexus가 멈춤 거리 안에 있는지 외부에서 읽는 값

        private EnemyMeleeAttack meleeAttack; // 같은 GameObject에 붙은 근거리 공격 Script Component 참조
        private EnemyRangedAttack rangedAttack; // 같은 GameObject에 붙은 원거리 공격 Script Component 참조

        private void Awake()
        {
            meleeAttack = GetComponent<EnemyMeleeAttack>(); // 같은 GameObject에 붙은 EnemyMeleeAttack Script Component를 찾는다.
            rangedAttack = GetComponent<EnemyRangedAttack>(); // 같은 GameObject에 붙은 EnemyRangedAttack Script Component를 찾는다.

            if (nexus == null) //Nexus가 연결되지 않았다면
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core");  //씬에서 이름이 Nexus_Core인 GameObject를 찾는다.
                nexus = nexusObject != null ? nexusObject.transform : null; //찾았다면 Transform을 저장하고, 못 찾았다면 null로 둔다.
            }
        }

        private void Update()
        {
            if (nexus == null) //이동 목표가 없으면
            {
                return; //종료한다.
            }

            Vector3 offset = nexus.position - transform.position; // 현재 몬스터 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.

            offset.y = 0f; //높이 차이는 제거한다.

            float stopDistance = GetStopDistance(); // 공격 Script의 AttackRange 또는 예비 정지 거리를 가져온다.

            if (offset.sqrMagnitude <= stopDistance * stopDistance) // Nexus와의 거리가 멈춤 거리 안이라면
            {
                IsInStopRange = true; // 현재 멈춤 거리 안에 있다고 상태를 저장한다.
                //전찬우 추가
                Vector3 resolvedPosition = SegmentBlocker.ResolveMonsterPosition(transform.position, transform.position, bodyRadius);
                transform.position = resolvedPosition;

                return; // 공격 사거리 안에서는 이동하지 않고 종료한다.
            }

            IsInStopRange = false; // 멈춤 거리 밖이라면 아직 이동해야 하는 상태로 저장한다.

            Vector3 direction = offset.normalized; // Nexus 방향 벡터를 길이 1짜리 방향으로 만든다.

            Vector3 desiredPosition = transform.position + direction * (moveSpeed * Time.deltaTime); // 이번 프레임에 이동하고 싶은 목표 위치를 계산한다.
            desiredPosition = GroundService.ProjectToGround(desiredPosition, groundHeight); // 목표 위치를 바닥 기준 높이에 맞게 보정한다.

            Vector3 position = SegmentBlocker.ResolveMonsterPosition(transform.position, desiredPosition, bodyRadius); // 세그먼트와 겹치지 않도록 이동 위치를 보정한다.
            transform.position = position; // 최종 보정된 위치를 몬스터 Transform에 적용한다.
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 몬스터가 이동 방향을 바라보게 회전시킨다.
        }

        private float GetStopDistance() // 몬스터가 이동을 멈출 거리를 결정하는 함수
        {
            if (meleeAttack != null) // 근거리 공격 Script Component가 있다면
            {
                return meleeAttack.AttackRange; // 근거리 공격 사거리를 멈춤 거리로 사용한다.
            }

            if (rangedAttack != null) // 원거리 공격 Script Component가 있다면
            {
                return rangedAttack.AttackRange; // 원거리 공격 사거리를 멈춤 거리로 사용한다.
            }

            return FallbackStopRadius; // 공격 Script가 없는 몬스터라면 예비 정지 거리를 사용한다.
        }

        public void Configure(Transform nexus, float moveSpeed, float groundHeight)// Spawner나 Controller가 이동 초기값을 넣어주는 함수
        {
            this.nexus = nexus; // 이동 목표 Nexus를 저장한다.
            this.moveSpeed = moveSpeed; // 이동 속도를 저장한다.
            this.groundHeight = groundHeight; // 바닥 높이 오프셋을 저장한다.
        }
    }
}