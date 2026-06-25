using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BossChargeStunAttack : MonoBehaviour // Rage Phase에서 컨보이 머리를 일정 시간 추격하며 돌진하는 보스 패턴
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor"); // URP Material의 기본 색상 Property ID
        private static readonly int ColorProperty = Shader.PropertyToID("_Color"); // Standard Material의 기본 색상 Property ID

        [Header("Telegraph")]
        [SerializeField] private GameObject chargeTelegraphPrefab; // 보스 돌진 경로를 표시할 예고 Prefab

        [Min(0.1f)]
        [SerializeField] private float telegraphWidth = 2.5f; // 돌진 예고선의 가로 폭

        [Min(0.0f)]
        [SerializeField] private float telegraphGroundHeight = 0.03f; // 예고선이 지면에 묻히지 않도록 적용할 높이

        [Range(0.01f, 1.0f)]
        [SerializeField] private float telegraphStartAlpha = 0.07f; // 예고선이 처음 나타날 때의 투명도

        [Range(0.01f, 1.0f)]
        [SerializeField] private float telegraphEndAlpha = 1.0f; // 돌진 직전 예고선의 투명도

        [Header("Charge")]
        [Min(0.1f)]
        [SerializeField] private float chargeSpeed = 24.0f; // 보스의 돌진 이동속도

        [Min(1.0f)]
        [SerializeField] private float chargeTurnSpeed = 360.0f; // 돌진 중 컨보이 머리를 따라 회전하는 초당 각도

        [Min(0.1f)]
        [SerializeField] private float chargeDuration = 7.0f; // 컨보이 머리를 추격하며 돌진할 최대 시간

        [Min(0.1f)]
        [SerializeField] private float chargeCollisionRadius = 1.2f; // 고속 돌진 중 머리 충돌을 검사할 SphereCast 반경

        [Min(0.0f)]
        [SerializeField] private float chargeCollisionHeight = 0.8f; // Boss01 기준 충돌 검사 중심의 높이

        [SerializeField] private LayerMask collisionMask = ~0; // 돌진 충돌 검사에 사용할 Layer

        [Header("Charge Impact")]
        [Min(0.1f)]
        [SerializeField] private float impactRadius = 4.0f; // 머리 충돌 후 기존 충격파 API를 적용할 범위

        [Min(0.0f)]
        [SerializeField] private float pushDistance = 1.8f; // 충돌한 컨보이를 살짝 밀어낼 거리

        [Min(0.0f)]
        [SerializeField] private float impactSourceBackOffset = 1.0f; // 넉백 방향을 돌진 방향으로 만들기 위한 충격 중심 보정 거리

        [Header("Charge Time Based Stun")]
        [Min(0.01f)]
        [SerializeField] private float minimumStunDuration = 0.2f; // 돌진 시작 직후 명중했을 때 적용할 최소 스턴 시간

        [Min(0.01f)]
        [SerializeField] private float maximumStunDuration = 7.0f; // 최대 돌진 시간 가까이 추격한 뒤 명중했을 때 적용할 최대 스턴 시간

        [Header("Timing")]
        [Min(0.1f)]
        [SerializeField] private float attackInterval = 11.0f; // 다음 돌진 공격까지의 대기시간

        [Min(0.1f)]
        [SerializeField] private float telegraphDuration = 1.2f; // 돌진 전에 경로를 보여주는 시간

        [Min(0.0f)]
        [SerializeField] private float recoveryDuration = 0.7f; // 돌진 성공 또는 실패 후 다른 행동까지 기다리는 시간

        private readonly RaycastHit[] sphereCastHits = new RaycastHit[32]; // 고속 이동 중 충돌 결과를 저장할 배열
        private readonly Collider[] overlapHits = new Collider[32]; // 현재 위치에서 겹친 Collider를 저장할 배열

        private BossController bossController; // 보스 Phase와 행동 잠금을 관리하는 Script Component
        private Rigidbody bossRigidbody; // 돌진 중 물리 간섭을 막기 위한 Rigidbody
        private Transform convoyTarget; // 돌진 중 추격할 컨보이 머리 Transform

        private Coroutine attackCoroutine; // 현재 실행 중인 돌진 공격 Coroutine
        private GameObject activeTelegraph; // 현재 생성된 돌진 예고선

        private float nextAttackTime; // 다음 돌진 공격이 가능한 시간

        private bool ownsActionLock; // 이 Script가 보스 행동 잠금을 가지고 있는지 나타내는 값
        private bool previousIsKinematic; // 돌진 전 Rigidbody의 Is Kinematic 상태
        private bool previousUseGravity; // 돌진 전 Rigidbody의 Use Gravity 상태
        private bool rigidbodyStateStored; // Rigidbody의 이전 상태를 저장했는지 나타내는 값
        private bool chargeImpactApplied; // 이번 돌진에서 충돌 효과가 이미 적용됐는지 나타내는 값

        public bool IsAttacking { get; private set; } // 현재 보스 돌진 공격이 진행 중인지 나타내는 값

        private void Awake()
        {
            bossController = GetComponent<BossController>(); // 같은 Boss01에 붙어 있는 BossController를 가져온다.
            bossRigidbody = GetComponent<Rigidbody>(); // 같은 Boss01에 붙어 있는 Rigidbody를 가져온다.
            TryFindConvoyTarget(); // 등록된 컨보이 타겟을 미리 확인한다.
        }

        private void Start()
        {
            ScheduleNextAttack(); // 보스 생성 후 첫 돌진 공격 시간을 예약한다.
        }

        private void Update()
        {
            if (bossController == null || bossController.IsDead) // BossController가 없거나 보스가 사망했다면
            {
                return; // 새로운 돌진 공격을 시작하지 않는다.
            }

            if (bossController.CurrentPhase != BossPhase.Rage) // 현재 보스 Phase가 Rage가 아니라면
            {
                return; // 돌진 공격을 사용하지 않는다.
            }

            if (attackCoroutine != null) // 이미 돌진 공격이 진행 중이라면
            {
                return; // 돌진 공격을 중복 실행하지 않는다.
            }

            if (bossController.IsActionRunning) // 점프 등 다른 보스 행동이 진행 중이라면
            {
                return; // 동시에 돌진하지 않는다.
            }

            if (Time.time < nextAttackTime) // 아직 다음 돌진 시간이 되지 않았다면
            {
                return; // 공격 간격이 끝날 때까지 기다린다.
            }

            if (!TryFindConvoyTarget()) // 현재 컨보이 타겟을 찾지 못했다면
            {
                nextAttackTime = Time.time + 1.0f; // 1초 뒤 다시 타겟을 확인한다.
                return; // 돌진 공격을 시작하지 않는다.
            }

            if (chargeTelegraphPrefab == null) // 돌진 예고 Prefab이 연결되지 않았다면
            {
                nextAttackTime = Time.time + 1.0f; // 1초 뒤 다시 확인한다.
                return; // 돌진 공격을 시작하지 않는다.
            }

            if (!bossController.TryBeginAction()) // 보스 행동 잠금을 얻지 못했다면
            {
                return; // 다른 보스 행동이 끝날 때까지 기다린다.
            }

            ownsActionLock = true; // 이 Script가 행동 잠금을 소유한다고 저장한다.
            attackCoroutine = StartCoroutine(AttackRoutine()); // 돌진 공격 Coroutine을 시작한다.
        }

        private void OnDisable()
        {
            if (attackCoroutine != null) // 실행 중인 돌진 Coroutine이 있다면
            {
                StopCoroutine(attackCoroutine); // 현재 돌진 Coroutine을 중단한다.
                attackCoroutine = null; // Coroutine 참조를 비운다.
            }

            CleanupTelegraph(); // 남아 있는 돌진 예고선을 제거한다.
            RestoreRigidbodyState(); // 돌진 전 Rigidbody 상태로 복구한다.

            IsAttacking = false; // 공격 상태를 해제한다.
            chargeImpactApplied = false; // 충돌 효과 적용 상태를 초기화한다.

            ReleaseActionLock(); // 이 Script가 가진 행동 잠금을 해제한다.
        }

        private IEnumerator AttackRoutine() // 돌진 예고부터 머리 추격과 충돌 처리까지 담당하는 전체 흐름
        {
            IsAttacking = true; // 돌진 공격이 시작됐다고 저장한다.
            chargeImpactApplied = false; // 이번 돌진의 충돌 적용 상태를 초기화한다.

            if (!TryFindConvoyTarget()) // 공격 시작 시점에 컨보이를 찾지 못했다면
            {
                FinishAttack(); // 공격 상태를 정리한다.
                yield break; // 돌진하지 않고 종료한다.
            }

            SpawnChargeTelegraph(); // 현재 보스와 컨보이 머리 사이에 돌진 예고선을 생성한다.

            float telegraphTimer = 0.0f; // 돌진 예고가 진행된 시간을 저장한다.

            while (telegraphTimer < telegraphDuration) // 예고 시간이 끝날 때까지 반복한다.
            {
                if (!CanContinueAttack()) // 예고 중 보스가 죽거나 Rage Phase가 끝났거나 타겟이 사라졌다면
                {
                    FinishAttack(); // 예고선과 행동 잠금을 정리한다.
                    yield break; // 돌진하지 않고 종료한다.
                }

                telegraphTimer += Time.deltaTime; // 지난 프레임 시간을 예고 타이머에 더한다.

                float progress = Mathf.Clamp01(telegraphTimer / telegraphDuration); // 예고 진행도를 계산한다.
                float alpha = Mathf.Lerp(telegraphStartAlpha, telegraphEndAlpha, progress); // 예고선이 점점 진해지도록 투명도를 계산한다.

                UpdateChargeTelegraph(); // 이동하는 컨보이 머리 위치에 맞춰 예고선 방향과 길이를 갱신한다.
                SetTelegraphAlpha(activeTelegraph, alpha); // 현재 예고선의 투명도를 적용한다.

                yield return null; // 다음 프레임까지 기다린다.
            }

            CleanupTelegraph(); // 돌진 직전에 예고선을 제거한다.

            if (!CanContinueAttack()) // 돌진 직전에 보스나 컨보이 상태가 유효하지 않다면
            {
                FinishAttack(); // 공격 상태를 정리한다.
                yield break; // 돌진하지 않고 종료한다.
            }

            PrepareRigidbodyForCharge(); // 보스 위치를 Script가 직접 제어하도록 Rigidbody를 설정한다.

            yield return ChargeMove(); // 제한 시간 동안 컨보이 머리를 계속 추격하며 돌진한다.

            RestoreRigidbodyState(); // 돌진 전 Rigidbody 상태로 복구한다.

            if (bossController == null || bossController.IsDead) // 돌진 종료 시점에 보스가 사망했다면
            {
                FinishAttack(); // 공격 상태를 정리한다.
                yield break; // 회복시간 없이 종료한다.
            }

            yield return new WaitForSeconds(recoveryDuration); // 명중 또는 시간초과 후 짧은 회복시간 동안 기다린다.

            FinishAttack(); // 공격 상태와 행동 잠금을 정리한다.
        }

        private IEnumerator ChargeMove() // Charge Duration 동안 컨보이 머리를 실시간으로 추격하는 함수
        {
            float chargeTimer = 0.0f; // 이번 돌진이 실제로 진행된 시간을 저장한다.

            while (chargeTimer < chargeDuration) // 설정된 최대 돌진 시간이 끝날 때까지 반복한다.
            {
                if (!CanContinueAttack()) // 돌진 중 보스가 죽거나 Phase가 끝났거나 컨보이가 사라졌다면
                {
                    yield break; // 돌진을 즉시 취소한다.
                }

                float remainingChargeTime = chargeDuration - chargeTimer; // 남아 있는 돌진 시간을 계산한다.
                float deltaTime = Mathf.Min(Time.deltaTime, remainingChargeTime); // 마지막 프레임에 제한 시간을 넘지 않도록 프레임 시간을 제한한다.

                if (deltaTime <= 0.0f) // 유효한 프레임 시간이 없다면
                {
                    yield return null; // 다음 프레임까지 기다린다.
                    continue; // 이동 계산을 건너뛴다.
                }

                Vector3 currentPosition = transform.position; // 현재 보스 위치를 가져온다.
                Vector3 desiredDirection = convoyTarget.position - currentPosition; // 현재 보스 위치에서 현재 컨보이 머리 위치까지의 방향을 계산한다.
                desiredDirection.y = 0.0f; // 지면을 따라 추격하도록 Y축 방향을 제거한다.

                if (desiredDirection.sqrMagnitude <= 0.0001f) // 보스와 컨보이 머리의 평면 위치가 거의 같다면
                {
                    desiredDirection = transform.forward; // 현재 보스가 바라보는 방향을 임시 돌진 방향으로 사용한다.
                    desiredDirection.y = 0.0f; // Y축 방향을 제거한다.
                }

                if (desiredDirection.sqrMagnitude <= 0.0001f) // 현재 앞 방향도 사용할 수 없다면
                {
                    desiredDirection = Vector3.forward; // 월드 기준 앞 방향을 사용한다.
                }

                desiredDirection.Normalize(); // 목표 방향의 길이를 1로 만든다.

                Vector3 currentDirection = transform.forward; // 보스가 현재 바라보는 방향을 가져온다.
                currentDirection.y = 0.0f; // 지면을 따라 회전하도록 Y축을 제거한다.

                if (currentDirection.sqrMagnitude <= 0.0001f) // 현재 앞 방향이 유효하지 않다면
                {
                    currentDirection = desiredDirection; // 컨보이를 향한 목표 방향을 사용한다.
                }
                else
                {
                    currentDirection.Normalize(); // 현재 방향의 길이를 1로 만든다.
                }

                float maximumTurnRadians = chargeTurnSpeed * Mathf.Deg2Rad * deltaTime; // 이번 프레임에 회전할 수 있는 최대 각도를 라디안으로 계산한다.
                Vector3 chargeDirection = Vector3.RotateTowards(currentDirection, desiredDirection, maximumTurnRadians, 0.0f); // 현재 방향에서 컨보이 방향으로 제한된 속도로 회전한다.
                chargeDirection.y = 0.0f; // 계산 과정에서 생길 수 있는 Y축 방향을 제거한다.

                if (chargeDirection.sqrMagnitude <= 0.0001f) // 계산된 추격 방향이 유효하지 않다면
                {
                    chargeDirection = desiredDirection; // 컨보이를 향한 방향을 직접 사용한다.
                }
                else
                {
                    chargeDirection.Normalize(); // 추격 방향의 길이를 1로 만든다.
                }

                float frameDistance = chargeSpeed * deltaTime; // 이번 프레임에 이동할 돌진 거리를 계산한다.

                if (TryDetectConvoyHeadHit(currentPosition, chargeDirection, frameDistance, out float hitDistance)) // 이번 이동 구간에서 컨보이 머리와 충돌했는지 검사한다.
                {
                    float safeHitDistance = Mathf.Clamp(hitDistance, 0.0f, frameDistance); // 충돌 위치까지의 거리를 안전하게 제한한다.
                    float safeChargeSpeed = Mathf.Max(0.01f, chargeSpeed); // 0으로 나누는 상황을 막기 위해 안전한 돌진 속도를 계산한다.
                    float hitTravelTime = safeHitDistance / safeChargeSpeed; // 이번 프레임 시작부터 실제 충돌 위치까지 이동하는 데 걸린 시간을 계산한다.
                    float actualChargeTime = Mathf.Clamp(chargeTimer + hitTravelTime, 0.0f, chargeDuration); // 돌진 시작부터 실제 명중 순간까지의 전체 추격 시간을 계산한다.

                    transform.position = currentPosition + chargeDirection * safeHitDistance; // 보스를 실제 충돌 위치까지 이동시킨다.
                    transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up); // 충돌 당시의 돌진 방향을 바라보게 한다.

                    ApplyChargeImpact(chargeDirection, actualChargeTime); // 실제 추격 시간에 비례한 스턴과 넉백을 적용한다.

                    yield break; // 명중했으므로 이번 돌진을 종료한다.
                }

                transform.position = currentPosition + chargeDirection * frameDistance; // 충돌하지 않았다면 컨보이를 향해 이동한다.
                transform.rotation = Quaternion.LookRotation(chargeDirection, Vector3.up); // 갱신된 추격 방향을 바라보게 한다.

                chargeTimer += deltaTime; // 이번 프레임만큼 실제 돌진 시간을 증가시킨다.

                yield return null; // 다음 프레임까지 기다린다.
            }

            // Charge Duration이 끝날 때까지 머리에 충돌하지 못했다면 아무 효과 없이 돌진이 취소된다.
        }

        private bool TryDetectConvoyHeadHit(Vector3 currentPosition, Vector3 chargeDirection, float frameDistance, out float hitDistance) // 고속 이동 구간에서 컨보이 머리 충돌을 검사하는 함수
        {
            hitDistance = 0.0f; // 기본 충돌 거리를 0으로 초기화한다.

            Vector3 sphereCenter = currentPosition + Vector3.up * chargeCollisionHeight; // Boss01 기준 충돌 검사 중심을 계산한다.

            int overlapCount = Physics.OverlapSphereNonAlloc(sphereCenter, chargeCollisionRadius, overlapHits, collisionMask, QueryTriggerInteraction.Collide); // 현재 위치에서 이미 머리와 겹쳤는지 확인한다.

            for (int i = 0; i < overlapCount; i++) // 현재 겹친 모든 Collider를 확인한다.
            {
                Collider overlapCollider = overlapHits[i]; // 현재 검사할 Collider를 가져온다.

                if (overlapCollider == null) // Collider가 없다면
                {
                    continue; // 다음 Collider를 확인한다.
                }

                if (MonsterInteractionApi.IsConvoyHeadCollider(overlapCollider)) // 현재 Collider가 컨보이 머리라면
                {
                    hitDistance = 0.0f; // 현재 위치에서 이미 충돌했다고 저장한다.
                    return true; // 머리 충돌 성공을 반환한다.
                }
            }

            int hitCount = Physics.SphereCastNonAlloc(sphereCenter, chargeCollisionRadius, chargeDirection, sphereCastHits, frameDistance, collisionMask, QueryTriggerInteraction.Collide); // 이번 프레임 이동 경로 전체를 SphereCast로 검사한다.

            float closestHitDistance = frameDistance; // 가장 가까운 머리 충돌 거리를 이번 이동 거리로 초기화한다.
            bool foundHeadHit = false; // 컨보이 머리 충돌을 찾았는지 저장한다.

            for (int i = 0; i < hitCount; i++) // SphereCast에 감지된 모든 충돌을 확인한다.
            {
                RaycastHit hit = sphereCastHits[i]; // 현재 충돌 결과를 가져온다.

                if (hit.collider == null) // 충돌 Collider가 없다면
                {
                    continue; // 다음 충돌 결과를 확인한다.
                }

                if (!MonsterInteractionApi.IsConvoyHeadCollider(hit.collider)) // 현재 Collider가 컨보이 머리가 아니라면
                {
                    continue; // 몸통과 다른 오브젝트는 무시한다.
                }

                if (hit.distance > closestHitDistance) // 이미 찾은 머리 충돌보다 멀리 있다면
                {
                    continue; // 더 가까운 충돌 결과를 유지한다.
                }

                closestHitDistance = hit.distance; // 가장 가까운 머리 충돌 거리를 저장한다.
                foundHeadHit = true; // 머리 충돌을 찾았다고 저장한다.
            }

            hitDistance = closestHitDistance; // 최종 머리 충돌 거리를 반환값에 저장한다.

            return foundHeadHit; // 컨보이 머리와 충돌했는지 반환한다.
        }

        private void ApplyChargeImpact(Vector3 chargeDirection, float actualChargeTime) // 제한 시간 안에 머리와 충돌했을 때 실제 추격 시간에 따른 넉백과 스턴을 적용하는 함수
        {
            if (chargeImpactApplied) // 이번 돌진에서 이미 충돌 효과를 적용했다면
            {
                return; // 중복으로 넉백과 스턴을 적용하지 않는다.
            }

            chargeImpactApplied = true; // 이번 돌진의 충돌 효과가 적용됐다고 저장한다.

            float appliedStunDuration = CalculateStunDuration(actualChargeTime); // 돌진 시작부터 명중까지 걸린 시간으로 최종 스턴 시간을 계산한다.

            Vector3 impactCenter = transform.position - chargeDirection * impactSourceBackOffset; // 컨보이가 보스의 돌진 방향으로 밀리도록 충격 중심을 보스 뒤쪽으로 보정한다.
            impactCenter.y = 0.0f; // 충격파 계산을 지면 평면 기준으로 제한한다.

            MonsterInteractionApi.RequestSegmentShockwave(impactCenter, impactRadius, pushDistance, appliedStunDuration); // 기존 점프 충격파 API로 살짝 밀림과 시간 비례 스턴을 적용한다.
        }

        private float CalculateStunDuration(float actualChargeTime) // 실제 추격 시간을 최소·최대 스턴 시간 사이의 값으로 변환하는 함수
        {
            float safeChargeDuration = Mathf.Max(0.01f, chargeDuration); // 0으로 나누지 않도록 안전한 최대 돌진 시간을 계산한다.
            float minimumDuration = Mathf.Min(minimumStunDuration, maximumStunDuration); // Inspector 입력 순서와 관계없이 작은 시간을 최소값으로 사용한다.
            float maximumDuration = Mathf.Max(minimumStunDuration, maximumStunDuration); // Inspector 입력 순서와 관계없이 큰 시간을 최대값으로 사용한다.

            float chargeTimeRate = Mathf.Clamp01(actualChargeTime / safeChargeDuration); // 실제 추격 시간이 최대 돌진 시간에서 차지하는 비율을 계산한다.

            return Mathf.Lerp(minimumDuration, maximumDuration, chargeTimeRate); // 추격 시간이 길수록 최대값에 가까워지는 스턴 시간을 반환한다.
        }

        private bool TryFindConvoyTarget() // MonsterInteractionApi에 등록된 컨보이 머리 Transform을 가져오는 함수
        {
            if (MonsterInteractionApi.TryGetConvoyTarget(out Transform target)) // 활성화된 컨보이 타겟이 있다면
            {
                convoyTarget = target; // 컨보이 Transform을 저장한다.
                return true; // 컨보이를 찾았다고 반환한다.
            }

            convoyTarget = null; // 컨보이를 찾지 못했다면 기존 참조를 비운다.
            return false; // 컨보이를 찾지 못했다고 반환한다.
        }

        private bool CanContinueAttack() // 현재 돌진 공격을 계속 실행할 수 있는지 확인하는 함수
        {
            if (bossController == null || bossController.IsDead) // BossController가 없거나 보스가 사망했다면
            {
                return false; // 돌진 공격을 취소한다.
            }

            if (bossController.CurrentPhase != BossPhase.Rage) // 돌진 도중 Rage Phase가 끝났다면
            {
                return false; // 돌진 공격을 취소한다.
            }

            if (convoyTarget == null || !convoyTarget.gameObject.activeInHierarchy) // 저장된 컨보이가 없거나 비활성화됐다면
            {
                return TryFindConvoyTarget(); // 현재 활성화된 컨보이를 다시 찾는다.
            }

            return true; // 돌진 공격을 계속할 수 있다고 반환한다.
        }

        private void SpawnChargeTelegraph() // 현재 보스와 컨보이 머리 사이에 긴 예고선을 생성하는 함수
        {
            CleanupTelegraph(); // 이전 공격에서 남은 예고선을 먼저 제거한다.

            Transform runtimeRoot = MonsterRuntimeRoot.GetRootOrFallback(transform.parent); // Monsters Runtime Root를 가져온다.

            activeTelegraph = Instantiate(chargeTelegraphPrefab, transform.position, Quaternion.identity, runtimeRoot); // Monsters 아래에 돌진 예고선을 생성한다.

            UpdateChargeTelegraph(); // 생성 직후 현재 컨보이 머리 위치에 맞춰 예고선을 배치한다.
            SetTelegraphAlpha(activeTelegraph, telegraphStartAlpha); // 생성 직후의 시작 투명도를 적용한다.
        }

        private void UpdateChargeTelegraph() // 예고 중 움직이는 컨보이 머리 위치에 맞춰 예고선을 갱신하는 함수
        {
            if (activeTelegraph == null || convoyTarget == null) // 예고선이나 컨보이 타겟이 없다면
            {
                return; // 예고선을 갱신하지 않는다.
            }

            Vector3 startPosition = transform.position; // 현재 보스 위치를 예고선 시작점으로 사용한다.
            Vector3 targetPosition = convoyTarget.position; // 현재 컨보이 머리 위치를 예고선 끝점으로 사용한다.

            Vector3 telegraphDirection = targetPosition - startPosition; // 보스에서 머리까지의 방향을 계산한다.
            telegraphDirection.y = 0.0f; // 지면에 표시하도록 Y축 방향을 제거한다.

            float telegraphLength = telegraphDirection.magnitude; // 현재 보스와 머리 사이의 거리를 예고선 길이로 사용한다.

            if (telegraphDirection.sqrMagnitude <= 0.0001f) // 보스와 머리 위치가 거의 같다면
            {
                telegraphDirection = transform.forward; // 현재 보스의 앞 방향을 사용한다.
                telegraphDirection.y = 0.0f; // Y축 방향을 제거한다.
                telegraphLength = 0.1f; // Scale이 0이 되지 않도록 최소 길이를 사용한다.
            }

            if (telegraphDirection.sqrMagnitude <= 0.0001f) // 보스 앞 방향도 유효하지 않다면
            {
                telegraphDirection = Vector3.forward; // 월드 기준 앞 방향을 사용한다.
            }

            telegraphDirection.Normalize(); // 예고 방향의 길이를 1로 만든다.

            Vector3 telegraphPosition = (startPosition + targetPosition) * 0.5f; // 시작점과 끝점의 중간 위치를 계산한다.
            telegraphPosition.y = telegraphGroundHeight; // 예고선 전용 지면 높이를 적용한다.

            activeTelegraph.transform.position = telegraphPosition; // 현재 보스와 머리 사이의 중간 위치에 예고선을 배치한다.
            activeTelegraph.transform.rotation = Quaternion.LookRotation(telegraphDirection, Vector3.up); // 예고선이 현재 머리를 향하게 한다.

            Vector3 telegraphScale = activeTelegraph.transform.localScale; // 예고 Prefab의 현재 Scale을 가져온다.
            telegraphScale.x = telegraphWidth; // 예고선의 가로 폭을 적용한다.
            telegraphScale.z = Mathf.Max(0.1f, telegraphLength); // 예고선 길이를 현재 머리까지의 거리에 맞춘다.
            activeTelegraph.transform.localScale = telegraphScale; // 계산된 예고선 크기를 적용한다.
        }

        private void SetTelegraphAlpha(GameObject telegraph, float alpha) // 돌진 예고선의 투명도를 변경하는 함수
        {
            if (telegraph == null) // 예고선이 없거나 이미 제거됐다면
            {
                return; // Material을 수정하지 않는다.
            }

            Renderer[] renderers = telegraph.GetComponentsInChildren<Renderer>(true); // 예고선과 자식의 모든 Renderer를 가져온다.

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++) // 모든 Renderer를 순회한다.
            {
                Material[] materials = renderers[rendererIndex].materials; // 현재 Renderer의 Material 인스턴스를 가져온다.

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++) // 모든 Material을 순회한다.
                {
                    Material material = materials[materialIndex]; // 현재 수정할 Material을 가져온다.

                    if (material == null) // Material이 없다면
                    {
                        continue; // 다음 Material을 확인한다.
                    }

                    if (material.HasProperty(BaseColorProperty)) // URP 기본 색상 Property가 있다면
                    {
                        Color color = material.GetColor(BaseColorProperty); // 기존 색상을 가져온다.
                        color.a = alpha; // 기존 RGB는 유지하고 Alpha만 변경한다.
                        material.SetColor(BaseColorProperty, color); // 변경된 색상을 적용한다.
                    }

                    if (material.HasProperty(ColorProperty)) // Standard 기본 색상 Property가 있다면
                    {
                        Color color = material.GetColor(ColorProperty); // 기존 색상을 가져온다.
                        color.a = alpha; // 기존 RGB는 유지하고 Alpha만 변경한다.
                        material.SetColor(ColorProperty, color); // 변경된 색상을 적용한다.
                    }
                }
            }
        }

        private void CleanupTelegraph() // 현재 생성된 돌진 예고선을 제거하는 함수
        {
            if (activeTelegraph != null) // 예고선이 존재한다면
            {
                Destroy(activeTelegraph); // 예고선 GameObject를 제거한다.
                activeTelegraph = null; // 제거된 예고선 참조를 비운다.
            }
        }

        private void PrepareRigidbodyForCharge() // 돌진 중 보스 위치를 Script가 직접 제어하도록 Rigidbody를 설정하는 함수
        {
            if (bossRigidbody == null) // Boss01에 Rigidbody가 없다면
            {
                return; // Rigidbody 상태를 변경하지 않는다.
            }

            previousIsKinematic = bossRigidbody.isKinematic; // 돌진 전 Is Kinematic 값을 저장한다.
            previousUseGravity = bossRigidbody.useGravity; // 돌진 전 Use Gravity 값을 저장한다.
            rigidbodyStateStored = true; // Rigidbody 이전 상태가 저장됐다고 표시한다.

            bossRigidbody.isKinematic = true; // 돌진 중 물리 힘 대신 Script가 위치를 제어하게 한다.
            bossRigidbody.useGravity = false; // 돌진 중 중력이 중복 적용되지 않게 한다.
        }

        private void RestoreRigidbodyState() // 돌진 전에 사용하던 Rigidbody 상태로 되돌리는 함수
        {
            if (bossRigidbody == null || !rigidbodyStateStored) // Rigidbody가 없거나 이전 상태를 저장하지 않았다면
            {
                return; // 복구할 상태가 없다.
            }

            bossRigidbody.isKinematic = previousIsKinematic; // 돌진 전 Is Kinematic 값으로 복구한다.
            bossRigidbody.useGravity = previousUseGravity; // 돌진 전 Use Gravity 값으로 복구한다.

            rigidbodyStateStored = false; // Rigidbody 복구가 끝났다고 저장한다.
        }

        private void ScheduleNextAttack() // 다음 돌진 공격 시간을 예약하는 함수
        {
            nextAttackTime = Time.time + attackInterval; // 현재 시간에 설정된 공격 간격을 더한다.
        }

        private void FinishAttack() // 돌진 공격 상태를 정리하는 함수
        {
            CleanupTelegraph(); // 남아 있을 수 있는 돌진 예고선을 제거한다.
            RestoreRigidbodyState(); // 돌진 전 Rigidbody 상태로 복구한다.

            IsAttacking = false; // 공격 진행 상태를 해제한다.
            chargeImpactApplied = false; // 충돌 효과 적용 상태를 초기화한다.

            ReleaseActionLock(); // 다른 보스 패턴이 실행될 수 있도록 행동 잠금을 해제한다.
            ScheduleNextAttack(); // 다음 돌진 공격 시간을 예약한다.

            attackCoroutine = null; // 현재 공격 Coroutine 참조를 비운다.
        }

        private void ReleaseActionLock() // 이 Script가 소유한 BossController 행동 잠금을 해제하는 함수
        {
            if (!ownsActionLock) // 행동 잠금을 가지고 있지 않다면
            {
                return; // 다른 보스 패턴의 잠금에 영향을 주지 않는다.
            }

            if (bossController != null) // BossController가 존재한다면
            {
                bossController.EndAction(); // 보스 행동 잠금을 해제한다.
            }

            ownsActionLock = false; // 행동 잠금을 더 이상 소유하지 않는다고 저장한다.
        }
    }
}