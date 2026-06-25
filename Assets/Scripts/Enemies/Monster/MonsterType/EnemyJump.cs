using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace TeamProject01.Gameplay
{
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class EnemyJump : MonoBehaviour // 세그먼트를 감지하면 앞으로 점프한다.
    {
        [Header("Jump Setting")]
        [Min(0.0f)]
        [SerializeField] private float jumpHeight = 2.0f; // 점프 높이

        [Min(0.05f)]
        [SerializeField] private float jumpDuration = 0.5f; // 점프 시간

        [Header("Segment Detection")]
        [Min(0.1f)]
        [SerializeField] private float segmentDetectDistance = 2.0f; // 앞쪽 세그먼트 감지 거리

        [Min(0.1f)]
        [SerializeField] private float jumpLandingDistance = 5.0f; // 세그먼트를 넘은 뒤 추가 착지 거리

        [Min(0.0f)]
        [SerializeField] private float jumpCooldown = 1.5f; // 착지 후 재점프 대기시간

        [Header("Landing Shockwave")]
        [Min(0.1f)]
        [SerializeField] private float shockwaveRadius = 7.0f; // 착지 충격파 범위

        [Min(0.0f)]
        [SerializeField] private float shockwavePushDistance = 3.0f; // 세그먼트 밀림 거리

        [Min(0.01f)]
        [SerializeField] private float shockwaveRecoveryDuration = 1.5f; // 원래 경로로 복구되는 시간

        ////// 안건준추가-0622 - EnemyJumpTest의 이동 Script Component 참조 구조를 가져온다.
        private EnemyMovement enemyMovement;
        private NavMeshAgent navAgent;
        private Coroutine jumpRoutine;
        private float cooldownTimer;

        ////// 안건준추가-0622 - SegmentBlocker의 활성 목록을 읽기 위한 참조를 가져온다.
        private static FieldInfo activeBlockersField;

        private void Awake()
        {
            ////// 안건준추가-0622 - 같은 GameObject의 이동과 AI Navigation Component를 찾는다.
            enemyMovement = GetComponent<EnemyMovement>();
            navAgent = GetComponent<NavMeshAgent>();

            ////// 안건준추가-0622 - SegmentBlocker의 활성 세그먼트 목록을 찾는다.
            if (activeBlockersField == null)
            {
                activeBlockersField = typeof(SegmentBlocker).GetField(
                    "ActiveBlockers",
                    BindingFlags.NonPublic | BindingFlags.Static);
            }

            ////// 안건준추가-0622 - Off-Mesh Link 이동은 EnemyJump가 직접 처리한다.
            if (navAgent != null)
            {
                navAgent.autoTraverseOffMeshLink = false;
            }
        }

        private void Update()
        {
            if (jumpRoutine != null) // 이미 점프 중이라면
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;

            ////// 안건준추가-0622 - NavMeshAgent가 Off-Mesh Link에 도착하면 지형 점프를 시작한다.
            if (navAgent != null &&
                navAgent.enabled &&
                navAgent.isOnNavMesh &&
                navAgent.isOnOffMeshLink)
            {
                jumpRoutine = StartCoroutine(JumpOffMeshLink());
                return;
            }

            if (cooldownTimer > 0.0f) // 재점프 대기시간이 남았다면
            {
                return;
            }

            ////// 안건준추가-0622 - 앞쪽 세그먼트를 감지하면 세그먼트 너머로 점프한다.
            if (IsSegmentAhead(out Vector3 landingPoint))
            {
                jumpRoutine = StartCoroutine(JumpOverSegment(landingPoint));
            }
        }

        private void OnDisable()
        {
            // 점프 도중 비활성화되어도 이동 상태가 남지 않도록 복구한다.
            if (jumpRoutine != null)
            {
                StopCoroutine(jumpRoutine);
                jumpRoutine = null;
            }

            SetEnemyMovementEnabled(true);

            if (navAgent != null &&
                navAgent.enabled &&
                navAgent.isOnNavMesh)
            {
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
            }
        }

        ////// 안건준추가-0622 - Off-Mesh Link의 시작점에서 끝점까지 포물선으로 이동한다.
        private IEnumerator JumpOffMeshLink()
        {
            SetEnemyMovementEnabled(false);

            navAgent.isStopped = true;
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;

            OffMeshLinkData link = navAgent.currentOffMeshLinkData;
            Vector3 from = transform.position;
            Vector3 to = link.endPos + Vector3.up * navAgent.baseOffset;

            yield return ArcMove(
                from,
                to,
                jumpHeight,
                jumpDuration);

            transform.position = to;

            if (navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.CompleteOffMeshLink();
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
            }

            cooldownTimer = jumpCooldown;
            jumpRoutine = null;

            SetEnemyMovementEnabled(true);
        }

        ////// 안건준추가-0622 - 몬스터 앞쪽에 점프할 세그먼트가 있는지 확인한다.
        private bool IsSegmentAhead(out Vector3 landingPoint)
        {
            landingPoint = Vector3.zero;

            var blockers = activeBlockersField?.GetValue(null)
                as System.Collections.Generic.List<SegmentBlocker>;

            if (blockers == null || blockers.Count == 0)
            {
                return false;
            }

            Vector3 myPosition = transform.position;
            Vector3 forward;

            ////// 안건준추가-0622 - NavMeshAgent 속도가 있으면 이동 방향으로 사용한다.
            if (navAgent != null &&
                navAgent.velocity.sqrMagnitude > 0.01f)
            {
                forward = navAgent.velocity;
            }
            else
            {
                forward = transform.forward;
            }

            forward.y = 0.0f;

            if (forward.sqrMagnitude < 0.001f)
            {
                return false;
            }

            forward.Normalize();

            for (int i = 0; i < blockers.Count; i++)
            {
                SegmentBlocker blocker = blockers[i];

                if (blocker == null)
                {
                    continue;
                }

                Vector3 toBlocker = blocker.transform.position - myPosition;

                toBlocker.y = 0.0f;

                float forwardDistance = Vector3.Dot(toBlocker, forward);

                if (forwardDistance < 0.0f)
                {
                    continue;
                }

                Vector3 lateralOffset = toBlocker - forward * forwardDistance;

                float monsterRadius = navAgent != null ? navAgent.radius : 0.5f;

                float combinedRadius = blocker.BlockRadius + monsterRadius;

                if (forwardDistance > segmentDetectDistance || lateralOffset.magnitude >= combinedRadius)
                {
                    continue;
                }

                landingPoint = blocker.transform.position + forward * (combinedRadius + jumpLandingDistance);

                landingPoint.y = myPosition.y;

                return true;
            }

            return false;
        }

        ////// 안건준추가-0622 - EnemyMovement를 멈추고 세그먼트 너머로 점프한다.
        private IEnumerator JumpOverSegment(Vector3 landingPoint)
        {
            SetEnemyMovementEnabled(false);

            bool canControlAgent = navAgent != null && navAgent.enabled && navAgent.isOnNavMesh;

            if (canControlAgent)
            {
                navAgent.isStopped = true;
                navAgent.updatePosition = false;
                navAgent.updateRotation = false;
            }

            Vector3 from = transform.position;

            yield return ArcMove(from, landingPoint, jumpHeight, jumpDuration);

            transform.position = landingPoint;

            ////// 안건준추가-0622 - 착지 위치 근처의 NavMesh 위치로 Agent를 맞춘다.
            if (canControlAgent)
            {
                if (NavMesh.SamplePosition(landingPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    navAgent.Warp(hit.position);
                }

                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
            }

            ApplyLandingShockwave(); // 세그먼트 점프 착지 지점에 충격파 발생

            cooldownTimer = jumpCooldown;
            jumpRoutine = null;

            SetEnemyMovementEnabled(true);
        }

        private void ApplyLandingShockwave() // 착지 주변의 연결 세그먼트를 바깥쪽으로 민다.
        {
            MonsterInteractionApi.RequestSegmentShockwave(transform.position, shockwaveRadius, shockwavePushDistance, shockwaveRecoveryDuration);
        }

        ////// 안건준추가-0622 - 시작점과 착지점 사이를 포물선으로 이동한다.
        private IEnumerator ArcMove(Vector3 from, Vector3 to, float height, float duration)
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);

                Vector3 position = Vector3.Lerp(from, to, progress);

                position.y += Mathf.Sin(progress * Mathf.PI) * height;

                transform.position = position;

                Vector3 direction = to - from;
                direction.y = 0.0f;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                }

                yield return null;
            }

            transform.position = to;
        }

        ////// 안건준추가-0622 - 점프 중에는 EnemyMovement를 끄고 착지 후 다시 켠다.
        private void SetEnemyMovementEnabled(bool enabled)
        {
            if (enemyMovement != null)
            {
                enemyMovement.enabled = enabled;
            }
        }
    }
}