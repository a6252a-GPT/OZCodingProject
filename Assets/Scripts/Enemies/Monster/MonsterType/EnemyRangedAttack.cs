using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyRangedAttack : MonoBehaviour // 원거리 몬스터
    {
        private enum RangedAttackType // 원거리 공격 방식
        {
            ProjectileArc, // 포물선 투사체 공격
            TargetImpact // 타겟 겉면에 즉시 임팩트 공격
        }

        [SerializeField] private Transform nexus; // 공격 타겟 Nexus 설정
        [SerializeField] private Transform firePoint; // 투사체가 발사될 위치

        [SerializeField] private RangedAttackType attackType = RangedAttackType.ProjectileArc; // Inspector에서 선택할 원거리 공격 방식

        [SerializeField] private EnemyProjectile projectilePrefab; // 포물선 공격에 사용할 EnemyProjectile Prefab
        [SerializeField] private GameObject impactPrefab; // 타겟 임팩트 공격에 사용할 임시 표시 오브젝트

        [Min(0)]
        [SerializeField] private int attackDamage = 1; // Nexus에 줄 피해량

        [Min(0.1f)]
        [SerializeField] private float attackRange = 6.0f; // 원거리 몬스터가 공격할 수 있는 거리

        [Min(0.1f)]
        [SerializeField] private float attackDelay = 1.5f; // 공격 사이의 대기 시간, 공격속도 역할

        [Min(0.1f)]
        [SerializeField] private float impactLifeTimeMin = 0.5f; // 임팩트 오브젝트 최소 유지 시간

        [Min(0.1f)]
        [SerializeField] private float impactLifeTimeMax = 1.0f; // 임팩트 오브젝트 최대 유지 시간

        [Min(0.0f)]
        [SerializeField] private float impactSurfaceOffset = 1.2f; // 타겟 중심에서 바깥쪽으로 임팩트를 밀어낼 거리

        [SerializeField] private float impactHeightOffset = 0.5f; // 임팩트를 위로 올릴 높이

        [Range(0.0f, 1.0f)]
        [SerializeField] private float impactSideRandomAmount = 0.5f; // 타겟 정면 중앙을 기준으로 좌우로 조금 흔들 범위

        public float AttackRange // EnemyMovement가 원거리 공격 사거리를 읽기 위한 property
        {
            get
            {
                return attackRange; // 원거리 공격 가능 거리를 반환한다.
            }
        }

        private float attackTimer; // 다음 공격까지 남은 시간을 저장하는 변수

        private EnemyBuffReceiver buffReceiver; // 같은 GameObject에 붙은 버프 상태 Script Component 참조

        private void Awake()
        {
            buffReceiver = GetComponent<EnemyBuffReceiver>(); // 같은 GameObject에 붙은 EnemyBuffReceiver Script Component를 찾는다.

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

            attackTimer -= Time.deltaTime; // 지난 시간만큼 공격 대기 시간을 줄인다.

            if (attackTimer > 0.0f) // 아직 공격 대기 시간이 남아 있다면
            {
                return; // 이번 프레임에는 공격하지 않는다.
            }

            Vector3 offset = nexus.position - transform.position; // 현재 몬스터 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.
            offset.y = 0.0f; // 높이 차이는 제거한다.

            if (offset.sqrMagnitude > attackRange * attackRange) // Nexus가 공격 사거리 밖이라면
            {
                return; // 공격하지 않고 종료한다.
            }

            float attackSpeedMultiplier = 1.0f; // 기본 공격속도 버프 배율

            if (buffReceiver != null) // 버프 상태 Script Component가 있다면
            {
                attackSpeedMultiplier = buffReceiver.GetAttackSpeedMultiplier(); // 현재 공격속도 버프 배율을 가져온다.
            }

            float finalAttackDelay = Mathf.Max(0.01f, attackDelay / attackSpeedMultiplier); // 공격속도 배율을 적용한 최종 공격 대기 시간을 계산한다.

            Attack(); // 선택된 공격 방식으로 공격한다.
            attackTimer = finalAttackDelay; // 공격 후 다음 공격까지 대기 시간을 다시 설정한다.
        }

        private void Attack() // Inspector에서 선택한 공격 방식에 따라 공격을 실행하는 함수
        {
            int finalAttackDamage = GetFinalAttackDamage(); // 버프 배율까지 적용한 최종 피해량을 계산한다.

            if (attackType == RangedAttackType.ProjectileArc) // 포물선 투사체 공격 방식이라면
            {
                ShootProjectile(finalAttackDamage); // 포물선 투사체를 발사한다.
                return; // 공격 처리를 끝낸다.
            }

            if (attackType == RangedAttackType.TargetImpact) // 타겟 임팩트 공격 방식이라면
            {
                SpawnTargetImpact(finalAttackDamage); // 타겟 겉면에 임팩트를 생성하고 피해를 준다.
                return; // 공격 처리를 끝낸다.
            }
        }

        private int GetFinalAttackDamage() // 버프를 반영한 최종 피해량을 계산하는 함수
        {
            float attackPowerMultiplier = 1.0f; // 기본 공격력 버프 배율

            if (buffReceiver != null) // 버프 상태 Script Component가 있다면
            {
                attackPowerMultiplier = buffReceiver.GetAttackPowerMultiplier(); // 현재 공격력 버프 배율을 가져온다.
            }

            return Mathf.Max(0, Mathf.RoundToInt(attackDamage * attackPowerMultiplier)); // 공격력 버프 배율을 적용한 피해량을 반환한다.
        }

        private void ShootProjectile(int finalAttackDamage) // Nexus 방향으로 포물선 투사체를 발사하는 함수
        {
            if (projectilePrefab == null) // 발사할 투사체 Prefab이 없다면
            {
                return; // 투사체를 만들 수 없으므로 종료한다.
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position; // FirePoint가 있으면 그 위치에서, 없으면 몬스터 위치에서 발사한다.

            Vector3 offset = nexus.position - spawnPosition; // 발사 위치에서 Nexus까지의 방향과 거리 벡터를 구한다.
            offset.y = 0.0f; // 높이 차이는 제거한다.

            Quaternion spawnRotation = transform.rotation; // 기본 회전값은 현재 몬스터 회전값으로 둔다.

            if (offset.sqrMagnitude > 0.0f) // Nexus 방향을 계산할 수 있다면
            {
                spawnRotation = Quaternion.LookRotation(offset.normalized, Vector3.up); // 투사체가 Nexus 방향을 바라보게 회전값을 만든다.
            }

            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation); // 투사체 Prefab을 생성한다.
            projectile.Configure(nexus, finalAttackDamage); // 생성된 투사체에 목표 Nexus와 최종 피해량을 전달한다.
        }

        private void SpawnTargetImpact(int finalAttackDamage) // 타겟 겉면 좌우 랜덤 위치에 임팩트를 생성하고 피해를 주는 함수
        {
            Vector3 directionToCaster = transform.position - nexus.position; // Nexus에서 공격 몬스터 쪽으로 향하는 방향을 구한다.
            directionToCaster.y = 0.0f; // 평면 방향만 사용할 것이므로 높이는 제거한다.

            if (directionToCaster.sqrMagnitude <= 0.0001f) // 방향을 계산하기 어려울 정도로 가까운 위치라면
            {
                directionToCaster = -transform.forward; // 몬스터의 반대 방향을 임시 방향으로 사용한다.
            }

            Vector3 centerDirection = directionToCaster.normalized; // Nexus 겉면 중앙 방향을 구한다.
            Vector3 sideDirection = Vector3.Cross(Vector3.up, centerDirection).normalized; // 중앙 방향을 기준으로 좌우 방향을 구한다.

            float randomSide = Random.Range(-impactSideRandomAmount, impactSideRandomAmount); // 중앙을 기준으로 왼쪽, 가운데 근처, 오른쪽 중 랜덤 값을 구한다.

            Vector3 surfaceDirection = centerDirection + sideDirection * randomSide; // 중앙 방향에 좌우 랜덤 방향을 섞는다.
            surfaceDirection.y = 0.0f; // 혹시 생길 수 있는 높이 값을 제거한다.
            surfaceDirection.Normalize(); // 최종 겉면 방향을 길이 1로 만든다.

            Vector3 impactPosition = nexus.position + surfaceDirection * impactSurfaceOffset; // Nexus 중심에서 겉면 방향으로 밀어낸 위치를 계산한다.
            impactPosition.y += impactHeightOffset; // 임팩트가 바닥이나 중심에 묻히지 않도록 위로 올린다.

            Quaternion impactRotation = Quaternion.LookRotation(-surfaceDirection, Vector3.up); // 임팩트가 Nexus 쪽을 바라보게 회전값을 만든다.

            if (impactPrefab != null) // 임팩트 표시용 Prefab이 있다면
            {
                GameObject impactObject = Instantiate(impactPrefab, impactPosition, impactRotation); // Nexus 겉면의 좌우 랜덤 위치에 임팩트 오브젝트를 생성한다.

                float randomLifeTime = GetRandomImpactLifeTime(); // 임팩트 오브젝트가 유지될 랜덤 시간을 구한다.

                EnemyImpactDebugVisual impactVisual = impactObject.GetComponent<EnemyImpactDebugVisual>(); // 생성된 임팩트 오브젝트에서 연출 Script Component를 찾는다.

                if (impactVisual != null) // 연출 Script Component가 있다면
                {
                    impactVisual.Configure(randomLifeTime); // 연출 Script에게 유지 시간을 전달한다.
                }
                else // 연출 Script Component가 없다면
                {
                    Destroy(impactObject, randomLifeTime); // 그냥 일정 시간 뒤 제거한다.
                }
            }

            NexusController.TryApplyDamage(nexus, finalAttackDamage); // 실제 피해는 Nexus에 즉시 적용한다.
        }

        private float GetRandomImpactLifeTime() // 임팩트 오브젝트 유지 시간을 랜덤으로 정하는 함수
        {
            float minLifeTime = Mathf.Min(impactLifeTimeMin, impactLifeTimeMax); // 두 값 중 작은 값을 최소 시간으로 사용한다.
            float maxLifeTime = Mathf.Max(impactLifeTimeMin, impactLifeTimeMax); // 두 값 중 큰 값을 최대 시간으로 사용한다.

            return Random.Range(minLifeTime, maxLifeTime); // 최소 시간과 최대 시간 사이의 랜덤 값을 반환한다.
        }

        public void Configure(Transform nexus, EnemyProjectile projectilePrefab, float attackRange, float attackDelay) // 기존 호출부를 유지하기 위한 원거리 공격 초기값 함수
        {
            this.nexus = nexus; // 공격 대상 Nexus를 저장한다.
            this.projectilePrefab = projectilePrefab; // 발사할 투사체 Prefab을 저장한다.
            this.attackRange = attackRange; // 공격 사거리를 저장한다.
            this.attackDelay = attackDelay; // 공격 딜레이를 저장한다.
        }

        public void Configure(Transform nexus, EnemyProjectile projectilePrefab, int attackDamage, float attackRange, float attackDelay) // Spawner나 Controller가 원거리 공격 초기값을 넣어주는 함수
        {
            this.nexus = nexus; // 공격 대상 Nexus를 저장한다.
            this.projectilePrefab = projectilePrefab; // 발사할 투사체 Prefab을 저장한다.
            this.attackDamage = attackDamage; // 공격 피해량을 저장한다.
            this.attackRange = attackRange; // 공격 사거리를 저장한다.
            this.attackDelay = attackDelay; // 공격 딜레이를 저장한다.
        }
    }
}