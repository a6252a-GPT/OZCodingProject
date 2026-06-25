using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyRangedAttack : MonoBehaviour // 원거리 몬스터의 Nexus 공격을 담당하는 Script Component
    {
        private enum RangedAttackType // 원거리 공격 방식
        {
            ProjectileArc, // 투사체를 발사하는 공격 방식
            TargetImpact // Nexus 겉면에 즉시 임팩트를 생성하는 공격 방식
        }

        private Transform nexus; // 공격 대상이 되는 Nexus Transform

        [SerializeField] private Transform firePoint; // 투사체가 발사될 위치

        [SerializeField] private RangedAttackType attackType = RangedAttackType.ProjectileArc; // 원거리 공격 방식

        [SerializeField] private EnemyProjectile projectilePrefab; // 투사체 공격에 사용할 EnemyProjectile Prefab

        [SerializeField] private GameObject impactPrefab; // 즉시 임팩트 공격에 사용할 Prefab

        [Min(0.1f)]
        [SerializeField] private float attackRange = 6.0f; // 원거리 몬스터가 공격할 수 있는 거리

        [Min(0.1f)]
        [SerializeField] private float attackDelay = 1.5f; // 공격 사이의 기본 대기 시간

        [Min(0.1f)]
        [SerializeField] private float impactLifeTimeMin = 0.5f; // 임팩트 오브젝트 최소 유지 시간

        [Min(0.1f)]
        [SerializeField] private float impactLifeTimeMax = 1.0f; // 임팩트 오브젝트 최대 유지 시간

        [Min(0.0f)]
        [SerializeField] private float impactSurfaceOffset = 1.2f; // Nexus 중심에서 겉면 방향으로 임팩트를 밀어낼 거리

        [SerializeField] private float impactHeightOffset = 0.5f; // 임팩트 생성 위치를 위로 올릴 높이

        [Range(0.0f, 1.0f)]
        [SerializeField] private float impactSideRandomAmount = 0.5f; // Nexus 정면 기준 좌우 랜덤 범위

        [Header("Animation Timing")]
        [SerializeField] private bool waitForAnimationEvent; // 조성원추가-0624 - 공격 애니메이션 이벤트가 올 때까지 실제 발사를 기다릴지 설정한다.

        // 조성원삭제-0624 - Animation Event 누락 시 2초 뒤 자동 발사하던 제한시간 설정을 사용하지 않는다.
        // [Min(0.1f)]
        // [SerializeField] private float animationEventTimeout = 2.0f;

        public float AttackRange // EnemyMovement가 원거리 공격 사거리를 읽기 위한 Property
        {
            get
            {
                return attackRange; // 원거리 공격 가능 거리를 반환한다.
            }
        }

        public event System.Action AttackPerformed; // 조성원추가-0624 - 원거리 공격 시작을 Animator Bridge에 전달하는 이벤트

        private float attackTimer; // 다음 공격까지 남은 시간

        private bool attackPending; // 조성원추가-0624 - 공격 애니메이션이 시작되고 실제 발사를 기다리는 상태
        private float pendingAttackPowerMultiplier = 1.0f; // 조성원추가-0624 - 발사 시 사용할 공격력 배율
        private float pendingAttackDelay; // 조성원추가-0624 - 실제 발사 후 적용할 다음 공격 대기시간

        // 조성원삭제-0624 - Animation Event 누락 시 자동 발사에 사용하던 제한시간 변수를 사용하지 않는다.
        // private float pendingAttackTimeout;

        private EnemyBuffReceiver buffReceiver; // 공격력/공격속도 버프를 읽기 위한 Script Component 참조

        private void Awake()
        {
            buffReceiver = GetComponent<EnemyBuffReceiver>(); // 같은 GameObject에 붙은 EnemyBuffReceiver를 찾는다.

            if (nexus == null) // 공격 대상 Nexus가 아직 없다면
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 씬에서 Nexus_Core 오브젝트를 찾는다.
                nexus = nexusObject != null ? nexusObject.transform : null; // 찾았다면 Transform을 저장한다.
            }
        }

        private void OnDisable()
        {
            CancelPendingAttack(); // 조성원추가-0624 - 사망하거나 비활성화될 때 대기 중인 공격을 취소한다.
        }

        private void Update()
        {
            if (nexus == null) // Nexus가 없다면
            {
                return; // 공격하지 않는다.
            }

            if (attackPending) // 조성원수정-0624 - Animation Event가 호출될 때까지 실제 발사를 계속 기다린다.
            {
                // 조성원삭제-0624 - 2초 뒤 자동으로 투사체를 발사하던 제한시간 처리를 사용하지 않는다.
                // pendingAttackTimeout -= Time.deltaTime;
                //
                // if (pendingAttackTimeout <= 0.0f)
                // {
                //     ReleasePendingAttack();
                // }

                return; // 발사를 기다리는 동안 새로운 공격을 시작하지 않는다.
            }

            attackTimer -= Time.deltaTime; // 지난 시간만큼 공격 대기 시간을 줄인다.

            if (attackTimer > 0.0f) // 아직 공격 대기 시간이 남아 있다면
            {
                return; // 이번 프레임에는 공격하지 않는다.
            }

            Vector3 offset = nexus.position - transform.position; // 몬스터에서 Nexus까지의 방향과 거리
            offset.y = 0.0f; // 높이 차이는 제거한다.

            if (offset.sqrMagnitude > attackRange * attackRange) // Nexus가 공격 사거리 밖이라면
            {
                return; // 공격하지 않는다.
            }

            float attackSpeedMultiplier = 1.0f; // 기본 공격속도 배율

            if (buffReceiver != null) // 버프를 받을 수 있는 몬스터라면
            {
                attackSpeedMultiplier = buffReceiver.GetAttackSpeedMultiplier(); // 현재 공격속도 버프 배율을 가져온다.
            }

            float finalAttackDelay = Mathf.Max(0.01f, attackDelay / attackSpeedMultiplier); // 공격속도 버프를 적용한 최종 공격 대기 시간
            float attackPowerMultiplier = GetAttackPowerMultiplier(); // 현재 공격력 버프 배율을 가져온다.

            BeginAttack(attackPowerMultiplier, finalAttackDelay); // 조성원수정-0624 - 애니메이션을 먼저 시작하고 설정에 따라 실제 발사를 기다린다.
        }

        private void BeginAttack(float attackPowerMultiplier, float finalAttackDelay) // 조성원추가-0624 - 공격 애니메이션과 실제 발사 과정을 시작하는 함수
        {
            if (waitForAnimationEvent) // Animation Event 발사 방식을 사용하는 몬스터라면
            {
                attackPending = true; // 실제 발사를 기다리는 상태로 변경한다.
                pendingAttackPowerMultiplier = attackPowerMultiplier; // 현재 공격력 배율을 저장한다.
                pendingAttackDelay = finalAttackDelay; // 실제 발사 후 사용할 공격 대기시간을 저장한다.

                // 조성원삭제-0624 - Animation Event 누락 시 2초 뒤 발사하던 제한시간 설정을 사용하지 않는다.
                // pendingAttackTimeout = animationEventTimeout;
            }

            AttackPerformed?.Invoke(); // 투사체보다 먼저 공격 애니메이션을 시작한다.

            if (waitForAnimationEvent) // 실제 발사를 애니메이션 시점까지 기다려야 한다면
            {
                return; // ReleaseProjectile Animation Event가 호출될 때까지 발사하지 않는다.
            }

            ExecuteAttack(attackPowerMultiplier); // Animation Event를 사용하지 않는 기존 몬스터는 즉시 발사한다.
            attackTimer = finalAttackDelay; // 실제 발사 후 다음 공격 대기시간을 설정한다.
        }

        public void ReleasePendingAttack() // 공격 애니메이션의 발사 프레임에서 호출할 함수
        {
            if (!attackPending) // 대기 중인 공격이 없다면
            {
                return; // 중복 발사를 막는다.
            }

            float attackPowerMultiplier = pendingAttackPowerMultiplier; // 저장한 공격력 배율을 가져온다.
            float finalAttackDelay = pendingAttackDelay; // 저장한 공격 대기시간을 가져온다.

            attackPending = false; // 발사 대기 상태를 해제한다.
            pendingAttackPowerMultiplier = 1.0f; // 저장한 공격력 배율을 초기화한다.
            pendingAttackDelay = 0.0f; // 저장한 공격 대기시간을 초기화한다.

            // 조성원삭제-0624 - 자동 발사 제한시간 초기화를 사용하지 않는다.
            // pendingAttackTimeout = 0.0f;

            if (nexus != null) // 공격 대상 Nexus가 아직 존재한다면
            {
                ExecuteAttack(attackPowerMultiplier); // 애니메이션 발사 시점에 실제 투사체나 임팩트를 생성한다.
            }

            attackTimer = finalAttackDelay; // 실제 발사 시점부터 다음 공격 대기시간을 시작한다.
        }

        private void CancelPendingAttack() // 대기 중인 공격을 실제 발사 없이 취소하는 함수
        {
            attackPending = false; // 발사 대기 상태를 해제한다.
            pendingAttackPowerMultiplier = 1.0f; // 저장한 공격력 배율을 초기화한다.
            pendingAttackDelay = 0.0f; // 저장한 공격 대기시간을 초기화한다.

            // 조성원삭제-0624 - 자동 발사 제한시간 초기화를 사용하지 않는다.
            // pendingAttackTimeout = 0.0f;
        }

        private void ExecuteAttack(float attackPowerMultiplier) // 선택된 원거리 공격을 실제로 생성하는 함수
        {
            if (attackType == RangedAttackType.ProjectileArc) // 투사체 공격 방식이라면
            {
                ShootProjectile(attackPowerMultiplier); // 투사체를 발사한다.
                return; // 공격 처리를 끝낸다.
            }

            if (attackType == RangedAttackType.TargetImpact) // 즉시 임팩트 공격 방식이라면
            {
                SpawnTargetImpact(attackPowerMultiplier); // Nexus 겉면에 임팩트를 생성한다.
            }
        }

        private float GetAttackPowerMultiplier() // 공격력 버프 배율을 가져오는 함수
        {
            if (buffReceiver == null) // EnemyBuffReceiver가 없다면
            {
                return 1.0f; // 기본 배율을 반환한다.
            }

            return buffReceiver.GetAttackPowerMultiplier(); // EnemyBuffReceiver에서 공격력 버프 배율을 가져온다.
        }

        private void ShootProjectile(float attackPowerMultiplier) // Nexus 방향으로 투사체를 발사하는 함수
        {
            if (projectilePrefab == null) // 발사할 투사체 Prefab이 없다면
            {
                return; // 투사체를 만들 수 없으므로 종료한다.
            }

            Vector3 spawnPosition = firePoint != null ? firePoint.position : transform.position; // FirePoint가 있으면 그 위치, 없으면 몬스터 위치에서 발사한다.

            Vector3 offset = nexus.position - spawnPosition; // 발사 위치에서 Nexus까지의 방향
            offset.y = 0.0f; // 높이 차이는 제거한다.

            Quaternion spawnRotation = transform.rotation; // 기본 회전값은 몬스터 회전값으로 둔다.

            if (offset.sqrMagnitude > 0.0001f) // Nexus 방향을 계산할 수 있다면
            {
                spawnRotation = Quaternion.LookRotation(offset.normalized, Vector3.up); // 투사체가 Nexus 방향을 바라보게 한다.
            }

            Transform runtimeRoot = MonsterRuntimeRoot.GetRootOrFallback(transform.parent); // Monsters를 찾고, 없으면 현재 몬스터의 부모를 사용한다.

            EnemyProjectile projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation, runtimeRoot); // 투사체 Prefab을 Monsters 밑에 생성한다.
            projectile.Configure(nexus, attackPowerMultiplier); // 목표 Nexus와 공격력 버프 배율을 투사체에 전달한다.
        }

        private void SpawnTargetImpact(float attackPowerMultiplier) // Nexus 겉면에 즉시 임팩트를 생성하는 함수
        {
            if (impactPrefab == null) // 임팩트 Prefab이 없다면
            {
                return; // 임팩트를 만들 수 없으므로 종료한다.
            }

            Vector3 directionToCaster = transform.position - nexus.position; // Nexus에서 공격 몬스터 쪽으로 향하는 방향
            directionToCaster.y = 0.0f; // 높이 차이는 제거한다.

            if (directionToCaster.sqrMagnitude <= 0.0001f) // 방향을 계산하기 어려울 정도로 가까우면
            {
                directionToCaster = -transform.forward; // 몬스터의 반대 방향을 임시 방향으로 사용한다.
                directionToCaster.y = 0.0f; // 높이 차이는 제거한다.
            }

            Vector3 centerDirection = directionToCaster.normalized; // Nexus 겉면 중앙 방향
            Vector3 sideDirection = Vector3.Cross(Vector3.up, centerDirection).normalized; // 중앙 방향 기준 좌우 방향

            float randomSide = Random.Range(-impactSideRandomAmount, impactSideRandomAmount); // 좌우 랜덤값

            Vector3 surfaceDirection = centerDirection + sideDirection * randomSide; // 중앙 방향에 좌우 랜덤 방향을 섞는다.
            surfaceDirection.y = 0.0f; // 높이 차이는 제거한다.
            surfaceDirection.Normalize(); // 최종 방향을 길이 1로 만든다.

            Vector3 impactPosition = nexus.position + surfaceDirection * impactSurfaceOffset; // Nexus 중심에서 겉면 방향으로 밀어낸 위치
            impactPosition.y += impactHeightOffset; // 임팩트 위치를 위로 올린다.

            Quaternion impactRotation = Quaternion.LookRotation(-surfaceDirection, Vector3.up); // 임팩트가 Nexus 쪽을 바라보게 한다.

            Transform runtimeRoot = MonsterRuntimeRoot.GetRootOrFallback(transform.parent); // Monsters를 찾고, 없으면 현재 몬스터의 부모를 사용한다.

            GameObject impactObject = Instantiate(impactPrefab, impactPosition, impactRotation, runtimeRoot); // 임팩트 오브젝트를 Monsters 밑에 생성한다.

            float randomLifeTime = GetRandomImpactLifeTime(); // 임팩트 유지 시간을 랜덤으로 정한다.

            EnemyImpactDebugVisual impactVisual = impactObject.GetComponent<EnemyImpactDebugVisual>(); // 임팩트 오브젝트에서 연출 Script Component를 찾는다.

            if (impactVisual != null) // 연출 Script Component가 있다면
            {
                impactVisual.Configure(nexus, randomLifeTime, attackPowerMultiplier); // 목표 Nexus, 유지 시간, 공격력 버프 배율을 전달한다.
            }
            else // 연출 Script Component가 없다면
            {
                Destroy(impactObject, randomLifeTime); // 일정 시간 뒤 제거한다.
            }
        }

        private float GetRandomImpactLifeTime() // 임팩트 유지 시간을 랜덤으로 정하는 함수
        {
            float minLifeTime = Mathf.Min(impactLifeTimeMin, impactLifeTimeMax); // 두 값 중 작은 값을 최소 시간으로 사용한다.
            float maxLifeTime = Mathf.Max(impactLifeTimeMin, impactLifeTimeMax); // 두 값 중 큰 값을 최대 시간으로 사용한다.

            return Random.Range(minLifeTime, maxLifeTime); // 최소~최대 사이의 랜덤 시간을 반환한다.
        }

        public void Configure(Transform nexus, EnemyProjectile projectilePrefab, float attackRange, float attackDelay) // 기존 호출부 호환용 초기화 함수
        {
            this.nexus = nexus; // 공격 대상 Nexus를 저장한다.
            this.projectilePrefab = projectilePrefab; // 발사할 투사체 Prefab을 저장한다.
            this.attackRange = attackRange; // 공격 사거리를 저장한다.
            this.attackDelay = attackDelay; // 공격 대기 시간을 저장한다.
        }

        public void Configure(Transform nexus, EnemyProjectile projectilePrefab, int unusedAttackDamage, float attackRange, float attackDelay) // 기존 호출부 호환용 초기화 함수
        {
            this.nexus = nexus; // 공격 대상 Nexus를 저장한다.
            this.projectilePrefab = projectilePrefab; // 발사할 투사체 Prefab을 저장한다.
            this.attackRange = attackRange; // 공격 사거리를 저장한다.
            this.attackDelay = attackDelay; // 공격 대기 시간을 저장한다.
        }
    }
}