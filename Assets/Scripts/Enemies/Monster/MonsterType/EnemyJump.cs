using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace TeamProject01.Gameplay
{
    [RequireComponent(typeof(EnemyMovement))]
    public sealed class EnemyJump : MonoBehaviour // ���׸�Ʈ�� �����ϸ� ������ �����Ѵ�.
    {
        [Header("Jump Setting")]
        [Min(0.0f)]
        [SerializeField] private float jumpHeight = 2.0f; // ���� ����

        [Min(0.05f)]
        [SerializeField] private float jumpDuration = 0.5f; // ���� �ð�

        // �������߰�-0626 - ���� �� ��ũ�� �ڼ��� �����ϸ� �̵��� ������ ��� ���� �ð�
        [Min(0.0f)]
        [SerializeField] private float landingRecoveryDuration = 0.25f;

        [Header("Segment Detection")]
        [Min(0.1f)]
        [SerializeField] private float segmentDetectDistance = 2.0f; // ���� ���׸�Ʈ ���� �Ÿ�

        [Min(0.1f)]
        [SerializeField] private float jumpLandingDistance = 5.0f; // ���׸�Ʈ�� ���� �� �߰� ���� �Ÿ�

        [Min(0.0f)]
        [SerializeField] private float jumpCooldown = 1.5f; // ���� �� ������ ���ð�

        [Header("Landing Shockwave")]
        [Min(0.1f)]
        [SerializeField] private float shockwaveRadius = 7.0f; // ���� ����� ����

        [Min(0.0f)]
        [SerializeField] private float shockwavePushDistance = 3.0f; // ���׸�Ʈ �и� �Ÿ�

        [Min(0.01f)]
        [SerializeField] private float shockwaveRecoveryDuration = 1.5f; // ���� ��η� �����Ǵ� �ð�

        // �������߰�-0630 - ���׸�Ʈ ���� ���� ���� �������� VFX�� �����ϱ� ���� ����
        [Header("Landing Crack VFX")]
        [SerializeField] private GameObject landingCrackVfxPrefab; // �������߰�-0630 - ���� ���� ������ �������� VFX Prefab

        [Min(0.0f)]
        [SerializeField] private float landingCrackGroundHeight = 0.03f; // �������߰�-0630 - VFX�� �ٴڿ� ������ �ʵ��� �ø� ����

        [Min(0.1f)]
        [SerializeField] private float landingCrackScale = 0.65f; // �������߰�-0630 - ����Ʈ ���Ϳ� �������� VFX ũ�� ����

        [Min(0.01f)]
        [SerializeField] private float landingCrackLifeTime = 2.0f; // �������߰�-0630 - ������ �������� VFX ���� �ð�

        ////// �Ȱ����߰�-0622 - EnemyJumpTest�� �̵� Script Component ���� ������ �����´�.
        private EnemyMovement enemyMovement;
        private NavMeshAgent navAgent;
        private Coroutine jumpRoutine;
        private float cooldownTimer;

        // �������߰�-0626 - ���� �ִϸ��̼� Bridge�� ���� ���� ���¸� ���� �� �ֵ��� �����Ѵ�.
        public bool IsJumping { get; private set; }

        ////// �Ȱ����߰�-0622 - SegmentBlocker�� Ȱ�� ����� �б� ���� ������ �����´�.
        private static FieldInfo activeBlockersField;

        private void Awake()
        {
            ////// �Ȱ����߰�-0622 - ���� GameObject�� �̵��� AI Navigation Component�� ã�´�.
            enemyMovement = GetComponent<EnemyMovement>();
            navAgent = GetComponent<NavMeshAgent>();

            ////// �Ȱ����߰�-0622 - SegmentBlocker�� Ȱ�� ���׸�Ʈ ����� ã�´�.
            if (activeBlockersField == null)
            {
                activeBlockersField = typeof(SegmentBlocker).GetField("ActiveBlockers", BindingFlags.NonPublic | BindingFlags.Static);
            }

            ////// �Ȱ����߰�-0622 - Off-Mesh Link �̵��� EnemyJump�� ���� ó���Ѵ�.
            if (navAgent != null)
            {
                navAgent.autoTraverseOffMeshLink = false;
            }
        }

        private void Update()
        {
            if (jumpRoutine != null) // �̹� ���� ���̶��
            {
                return;
            }

            cooldownTimer -= Time.deltaTime;

            ////// �Ȱ����߰�-0622 - NavMeshAgent�� Off-Mesh Link�� �����ϸ� ���� ������ �����Ѵ�.
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh && navAgent.isOnOffMeshLink)
            {
                jumpRoutine = StartCoroutine(JumpOffMeshLink());
                return;
            }

            if (cooldownTimer > 0.0f) // ������ ���ð��� ���Ҵٸ�
            {
                return;
            }

            ////// �Ȱ����߰�-0622 - ���� ���׸�Ʈ�� �����ϸ� ���׸�Ʈ �ʸӷ� �����Ѵ�.
            if (IsSegmentAhead(out Vector3 landingPoint))
            {
                jumpRoutine = StartCoroutine(JumpOverSegment(landingPoint));
            }
        }

        private void OnDisable()
        {
            // ���� ���� ��Ȱ��ȭ�Ǿ �̵� ���°� ���� �ʵ��� �����Ѵ�.
            if (jumpRoutine != null)
            {
                StopCoroutine(jumpRoutine);
                jumpRoutine = null;
            }

            // �������߰�-0626 - ��Ȱ��ȭ�� �� ���� �ִϸ��̼� ���°� ���� �ʵ��� �����Ѵ�.
            IsJumping = false;

            SetEnemyMovementEnabled(true);

            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
            }
        }

        ////// �Ȱ����߰�-0622 - Off-Mesh Link�� ���������� �������� ���������� �̵��Ѵ�.
        private IEnumerator JumpOffMeshLink()
        {
            // �������߰�-0626 - ���� ������ ���۵ƴٰ� �����Ѵ�.
            IsJumping = true;

            SetEnemyMovementEnabled(false);

            navAgent.isStopped = true;
            navAgent.updatePosition = false;
            navAgent.updateRotation = false;

            OffMeshLinkData link = navAgent.currentOffMeshLinkData;
            Vector3 from = transform.position;
            Vector3 to = link.endPos + Vector3.up * navAgent.baseOffset;

            yield return ArcMove(from, to, jumpHeight, jumpDuration);

            transform.position = to;

            if (navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.CompleteOffMeshLink();
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;

                // ����������-0626 - ���� ȸ���� ���� ������ NavMeshAgent�� ���� ���¸� �����Ѵ�.
                navAgent.isStopped = true;
            }

            // �������߰�-0626 - ���� �� ���� ��ũ�� �ִϸ��̼� ���� �̵����� �ʴ´�.
            if (landingRecoveryDuration > 0.0f)
            {
                yield return new WaitForSeconds(landingRecoveryDuration);
            }

            cooldownTimer = jumpCooldown;

            // �������߰�-0626 - ���� ȸ������ ���� �� ���� ���¸� �����Ѵ�.
            IsJumping = false;

            SetEnemyMovementEnabled(true);

            // �������߰�-0626 - ���� ȸ���� ���� �� NavMeshAgent �̵��� �ٽ� ����Ѵ�.
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }

            jumpRoutine = null;
        }

        ////// �Ȱ����߰�-0622 - ���� ���ʿ� ������ ���׸�Ʈ�� �ִ��� Ȯ���Ѵ�.
        private bool IsSegmentAhead(out Vector3 landingPoint)
        {
            landingPoint = Vector3.zero;

            var blockers = activeBlockersField?.GetValue(null) as System.Collections.Generic.List<SegmentBlocker>;

            if (blockers == null || blockers.Count == 0)
            {
                return false;
            }

            Vector3 myPosition = transform.position;
            Vector3 forward;

            ////// �Ȱ����߰�-0622 - NavMeshAgent �ӵ��� ������ �̵� �������� ����Ѵ�.
            if (navAgent != null && navAgent.velocity.sqrMagnitude > 0.01f)
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

        ////// �Ȱ����߰�-0622 - EnemyMovement�� ���߰� ���׸�Ʈ �ʸӷ� �����Ѵ�.
        private IEnumerator JumpOverSegment(Vector3 landingPoint)
        {
            // �������߰�-0626 - ���׸�Ʈ ������ ���۵ƴٰ� �����Ѵ�.
            IsJumping = true;

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

            ////// �Ȱ����߰�-0622 - ���� ��ġ ��ó�� NavMesh ��ġ�� Agent�� �����.
            if (canControlAgent)
            {
                if (NavMesh.SamplePosition(landingPoint, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    navAgent.Warp(hit.position);
                }

                navAgent.updatePosition = true;
                navAgent.updateRotation = true;

                // ����������-0626 - ���� ȸ���� ���� ������ NavMeshAgent�� ���� ���¸� �����Ѵ�.
                navAgent.isStopped = true;
            }

            SpawnLandingCrackVfx(transform.position); // �������߰�-0630 - ���� ��ġ�� Ȯ���� �� �������� VFX�� �����Ѵ�.

            ApplyLandingShockwave(); // ���׸�Ʈ ���� ���� ������ ����� �߻�

            // �������߰�-0626 - ���� �� ���� ��ũ�� �ִϸ��̼� ���� �̵����� �ʴ´�.
            if (landingRecoveryDuration > 0.0f)
            {
                yield return new WaitForSeconds(landingRecoveryDuration);
            }

            cooldownTimer = jumpCooldown;

            // �������߰�-0626 - ���� ȸ������ ���� �� ���� ���¸� �����Ѵ�.
            IsJumping = false;

            SetEnemyMovementEnabled(true);

            // �������߰�-0626 - ���� ȸ���� ���� �� NavMeshAgent �̵��� �ٽ� ����Ѵ�.
            if (canControlAgent && navAgent.enabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
            }

            jumpRoutine = null;
        }

        // �������߰�-0630 - ���׸�Ʈ ���� ���� ��ġ�� �������� VFX�� �����Ѵ�.
        private void SpawnLandingCrackVfx(Vector3 position)
        {
            SpawnOneShotVfx(landingCrackVfxPrefab, position, landingCrackGroundHeight, landingCrackScale, landingCrackLifeTime);
        }

        // �������߰�-0630 - �ܹ߼� VFX�� �����ϰ� ���� �ð� �� �����Ѵ�.
        private void SpawnOneShotVfx(GameObject prefab, Vector3 position, float groundHeight, float scaleMultiplier, float lifeTime)
        {
            if (prefab == null)
            {
                return;
            }

            Vector3 spawnPosition = position;
            spawnPosition.y += groundHeight;

            GameObject vfx = Instantiate(prefab, spawnPosition, Quaternion.identity, transform.parent);
            vfx.transform.localScale = vfx.transform.localScale * scaleMultiplier;
            Destroy(vfx, lifeTime);
        }

        private void ApplyLandingShockwave() // ���� �ֺ��� ���� ���׸�Ʈ�� �ٱ������� �δ�.
        {
            MonsterInteractionApi.RequestSegmentShockwave(transform.position, shockwaveRadius, shockwavePushDistance, shockwaveRecoveryDuration);
        }

        ////// �Ȱ����߰�-0622 - �������� ������ ���̸� ���������� �̵��Ѵ�.
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

        ////// �Ȱ����߰�-0622 - ���� �߿��� EnemyMovement�� ���� ���� �� �ٽ� �Ҵ�.
        private void SetEnemyMovementEnabled(bool enabled)
        {
            if (enemyMovement != null)
            {
                enemyMovement.enabled = enabled;
            }
        }
    }
}