using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BossDiamondSiegeAttack : MonoBehaviour
    {
        [Header("Projectile")]
        [SerializeField] private BossDiamondProjectile projectilePrefab; // 생성할 보스 다이아몬드 투사체 Prefab

        [Header("Normal Magic Circle")]
        [SerializeField] private GameObject leftMagicCircleRoot; // Normal 공격에서 표시할 왼쪽 마법진 전체 오브젝트

        [SerializeField] private GameObject rightMagicCircleRoot; // Normal 공격에서 표시할 오른쪽 마법진 전체 오브젝트

        [SerializeField] private Transform leftFirePoint; // 왼쪽 마법진의 투사체 생성 위치

        [SerializeField] private Transform rightFirePoint; // 오른쪽 마법진의 투사체 생성 위치

        [Header("Berserk Formation")]
        [SerializeField] private Transform burstCenter; // Berserk 다이아몬드가 보스 뒤쪽에서 생성될 기준점

        [Min(1)]
        [SerializeField] private int berserkShotCount = 24; // Berserk 공격에서 생성할 전체 다이아몬드 수

        [Min(1)]
        [SerializeField] private int formationRowCount = 3; // 뒤쪽 대형의 세로 줄 수

        [Min(0.1f)]
        [SerializeField] private float formationHorizontalSpacing = 1.3f; // 대형에서 좌우 다이아몬드 사이의 간격

        [Min(0.1f)]
        [SerializeField] private float formationVerticalSpacing = 1.1f; // 대형에서 위아래 다이아몬드 사이의 간격

        [Min(0.0f)]
        [SerializeField] private float formationDepthSpacing = 0.6f; // 바깥쪽 열일수록 뒤로 물러나는 간격

        [Min(0.0f)]
        [SerializeField] private float formationWingRise = 0.35f; // 바깥쪽 열일수록 위로 펼쳐지는 높이

        [Min(0.0f)]
        [SerializeField] private float formationHoldDuration = 0.5f; // 대형을 완성한 뒤 첫 투사체가 출격하기까지의 시간

        [Min(0.01f)]
        [SerializeField] private float berserkLaunchInterval = 0.05f; // 대형에서 각 다이아몬드가 차례로 출격하는 시간 차이

        [Header("Berserk Target Spread")]
        [Min(0.0f)]
        [SerializeField] private float berserkTargetMinimumRadius = 3.0f; // Nexus 중심에서 가장 가까운 분산 목표 거리

        [Min(0.1f)]
        [SerializeField] private float berserkTargetMaximumRadius = 8.0f; // Nexus 중심에서 가장 먼 분산 목표 거리

        [Range(0.0f, 45.0f)]
        [SerializeField] private float berserkTargetAngleJitter = 8.0f; // 일정한 원형 배치가 너무 규칙적으로 보이지 않게 각도를 흔드는 값

        [Header("Normal Attack")]
        [Min(0.1f)]
        [SerializeField] private float normalAttackInterval = 6.0f; // Normal 연사 공격을 다시 사용할 때까지의 시간

        [Min(1)]
        [SerializeField] private int normalShotCount = 8; // Normal 공격에서 좌우 번갈아 발사할 전체 투사체 수

        [Min(0.01f)]
        [SerializeField] private float normalShotInterval = 0.12f; // Normal 공격의 각 투사체 사이 발사 간격

        [Header("Berserk Attack")]
        [Min(0.1f)]
        [SerializeField] private float berserkAttackInterval = 8.0f; // Berserk 대형 유도 공격을 다시 사용할 때까지의 시간

        [Header("Attack Timing")]
        [Min(0.0f)]
        [SerializeField] private float windupDuration = 1.0f; // 공격 전 준비 시간

        [Min(0.0f)]
        [SerializeField] private float recoveryDuration = 0.5f; // 공격 종료 후 다음 행동까지 기다리는 시간

        private BossController bossController; // 보스 상태와 행동 잠금을 관리하는 Script Component
        private Transform nexus; // 다이아몬드 투사체가 공격할 Nexus_Core

        private Coroutine attackCoroutine; // 현재 실행 중인 공격 Coroutine
        private float nextAttackTime; // 다음 다이아몬드 공격이 가능한 시간
        private bool ownsActionLock; // 이 Script가 행동 잠금을 사용하고 있는지 나타내는 값

        public bool IsAttacking { get; private set; } // 현재 다이아몬드 공격이 진행 중인지 나타내는 값

        private void Awake()
        {
            bossController = GetComponent<BossController>(); // 같은 Boss01에서 BossController를 가져온다.
            FindNexus(); // 씬에서 Nexus_Core를 찾아 저장한다.
            SetMagicCirclesActive(false); // 시작할 때 Normal용 좌우 마법진을 숨긴다.
        }

        private void Start()
        {
            ScheduleNextAttack(); // 보스 생성 후 첫 다이아몬드 공격 시간을 예약한다.
        }

        private void Update()
        {
            if (bossController == null || bossController.IsDead)
            {
                return;
            }

            if (!CanUseDiamondAttack(bossController.CurrentPhase))
            {
                return;
            }

            if (attackCoroutine != null)
            {
                return;
            }

            if (bossController.IsActionRunning)
            {
                return;
            }

            if (Time.time < nextAttackTime)
            {
                return;
            }

            if (nexus == null)
            {
                FindNexus();

                if (nexus == null)
                {
                    nextAttackTime = Time.time + 1.0f;
                    return;
                }
            }

            if (projectilePrefab == null)
            {
                nextAttackTime = Time.time + 1.0f;
                return;
            }

            BossPhase attackPhase = bossController.CurrentPhase;

            if (!HasRequiredAttackPoints(attackPhase))
            {
                nextAttackTime = Time.time + 1.0f;
                return;
            }

            if (!bossController.TryBeginAction())
            {
                return;
            }

            ownsActionLock = true;
            attackCoroutine = StartCoroutine(AttackRoutine(attackPhase));
        }

        private void OnDisable()
        {
            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            SetMagicCirclesActive(false);
            IsAttacking = false;
            ReleaseActionLock();
        }

        private IEnumerator AttackRoutine(BossPhase attackPhase)
        {
            IsAttacking = true;

            LookAtNexus();

            if (attackPhase == BossPhase.Normal)
            {
                SetMagicCirclesActive(true);
            }
            else
            {
                SetMagicCirclesActive(false);
            }

            yield return new WaitForSeconds(windupDuration);

            if (!CanContinueAttack(attackPhase))
            {
                FinishAttack();
                yield break;
            }

            if (attackPhase == BossPhase.Berserk)
            {
                SpawnBerserkFormation();
            }
            else
            {
                yield return StartCoroutine(FireNormalBurst());
            }

            if (bossController == null || bossController.IsDead)
            {
                FinishAttack();
                yield break;
            }

            yield return new WaitForSeconds(recoveryDuration);

            FinishAttack();
        }

        private IEnumerator FireNormalBurst()
        {
            int shotCount = Mathf.Max(1, normalShotCount);

            for (int i = 0; i < shotCount; i++)
            {
                if (!CanContinueAttack(BossPhase.Normal))
                {
                    yield break;
                }

                Transform selectedFirePoint = i % 2 == 0 ? leftFirePoint : rightFirePoint;

                SpawnStraightProjectile(selectedFirePoint);

                if (i < shotCount - 1)
                {
                    yield return new WaitForSeconds(normalShotInterval);
                }
            }
        }

        private void SpawnStraightProjectile(Transform selectedFirePoint)
        {
            Vector3 spawnPosition = selectedFirePoint.position;
            Quaternion spawnRotation = GetNexusRotation(spawnPosition);
            Transform runtimeRoot = MonsterRuntimeRoot.GetRootOrFallback(transform.parent);

            BossDiamondProjectile projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation, runtimeRoot);

            projectile.Configure(nexus);
        }

        private void SpawnBerserkFormation()
        {
            int shotCount = Mathf.Max(1, berserkShotCount);
            Transform runtimeRoot = MonsterRuntimeRoot.GetRootOrFallback(transform.parent);

            for (int i = 0; i < shotCount; i++)
            {
                Vector3 spawnPosition = burstCenter.position;
                Vector3 formationPosition = CalculateFormationPosition(i);
                Vector3 homingTargetOffset = CalculateHomingTargetOffset(i, shotCount);
                Vector3 formationDirection = formationPosition - spawnPosition;

                Quaternion spawnRotation = formationDirection.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(formationDirection.normalized, Vector3.up) : burstCenter.rotation;

                float standbyDuration = formationHoldDuration + i * berserkLaunchInterval;

                BossDiamondProjectile projectile = Instantiate(projectilePrefab, spawnPosition, spawnRotation, runtimeRoot);

                projectile.ConfigureFormationHoming(nexus, formationPosition, standbyDuration, homingTargetOffset);
            }
        }

        private Vector3 CalculateFormationPosition(int index)
        {
            int rows = Mathf.Max(1, formationRowCount);
            int pairIndex = index / 2;
            int side = index % 2 == 0 ? -1 : 1;
            int row = pairIndex % rows;
            int column = pairIndex / rows;

            float centeredRow = row - (rows - 1) * 0.5f;
            float localX = side * formationHorizontalSpacing * (column + 1);
            float localY = centeredRow * formationVerticalSpacing + column * formationWingRise;
            float localZ = -column * formationDepthSpacing;

            Vector3 localOffset = new Vector3(localX, localY, localZ);

            return burstCenter.TransformPoint(localOffset);
        }

        private Vector3 CalculateHomingTargetOffset(int index, int shotCount) // Nexus 주변의 360도 평면 목표점을 계산한다.
        {
            float minimumRadius = Mathf.Min(berserkTargetMinimumRadius, berserkTargetMaximumRadius); // 최소·최대 값이 뒤집혀도 작은 값을 최소 거리로 사용한다.
            float maximumRadius = Mathf.Max(berserkTargetMinimumRadius, berserkTargetMaximumRadius); // 최소·최대 값이 뒤집혀도 큰 값을 최대 거리로 사용한다.

            float angleStep = 360.0f / Mathf.Max(1, shotCount); // 전체 원을 다이아몬드 수만큼 동일하게 나눈다.
            float baseAngle = angleStep * index; // 현재 다이아몬드의 기본 원형 배치 각도를 계산한다.
            float angle = baseAngle + Random.Range(-berserkTargetAngleJitter, berserkTargetAngleJitter); // 너무 규칙적으로 보이지 않도록 각도에 무작위 흔들림을 더한다.
            float radius = Random.Range(minimumRadius, maximumRadius); // Nexus 중심에서 떨어질 거리를 무작위로 선택한다.

            Vector3 nexusForward = nexus.position - burstCenter.position; // 보스 뒤쪽 중심에서 Nexus로 향하는 평면 방향을 계산한다.
            nexusForward.y = 0.0f; // 상하 분산을 사용하지 않도록 Y축을 제거한다.

            if (nexusForward.sqrMagnitude <= 0.0001f)
            {
                nexusForward = transform.forward;
                nexusForward.y = 0.0f;
            }

            nexusForward.Normalize();

            Vector3 nexusRight = Vector3.Cross(Vector3.up, nexusForward).normalized; // Nexus 진행 방향을 기준으로 오른쪽 축을 계산한다.

            float angleRadians = angle * Mathf.Deg2Rad; // 원형 목표점 계산을 위해 각도를 라디안으로 변환한다.

            Vector3 planarDirection = nexusRight * Mathf.Cos(angleRadians) + nexusForward * Mathf.Sin(angleRadians); // 앞·뒤·좌·우·대각선을 포함하는 평면 방향을 만든다.

            return planarDirection.normalized * radius; // Y축 이동 없이 Nexus 주변의 서로 다른 평면 목표점을 반환한다.
        }

        private bool HasRequiredAttackPoints(BossPhase attackPhase)
        {
            if (attackPhase == BossPhase.Berserk)
            {
                return burstCenter != null;
            }

            return leftFirePoint != null && rightFirePoint != null;
        }

        private bool CanContinueAttack(BossPhase attackPhase)
        {
            if (bossController == null || bossController.IsDead)
            {
                return false;
            }

            if (nexus == null)
            {
                FindNexus();
            }

            if (nexus == null || projectilePrefab == null)
            {
                return false;
            }

            return HasRequiredAttackPoints(attackPhase);
        }

        private bool CanUseDiamondAttack(BossPhase phase)
        {
            return phase == BossPhase.Normal || phase == BossPhase.Berserk;
        }

        private Quaternion GetNexusRotation(Vector3 spawnPosition)
        {
            Vector3 direction = nexus.position - spawnPosition;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return transform.rotation;
            }

            return Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void FindNexus()
        {
            GameObject nexusObject = GameObject.Find("Nexus_Core");

            nexus = nexusObject != null ? nexusObject.transform : null;
        }

        private void LookAtNexus()
        {
            if (nexus == null)
            {
                return;
            }

            Vector3 direction = nexus.position - transform.position;
            direction.y = 0.0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void SetMagicCirclesActive(bool active)
        {
            if (leftMagicCircleRoot != null)
            {
                leftMagicCircleRoot.SetActive(active);
            }

            if (rightMagicCircleRoot != null)
            {
                rightMagicCircleRoot.SetActive(active);
            }
        }

        private void ScheduleNextAttack()
        {
            nextAttackTime = Time.time + GetAttackInterval();
        }

        private float GetAttackInterval()
        {
            if (bossController != null && bossController.CurrentPhase == BossPhase.Berserk)
            {
                return berserkAttackInterval;
            }

            return normalAttackInterval;
        }

        private void FinishAttack()
        {
            SetMagicCirclesActive(false);
            IsAttacking = false;
            ReleaseActionLock();
            ScheduleNextAttack();
            attackCoroutine = null;
        }

        private void ReleaseActionLock()
        {
            if (!ownsActionLock)
            {
                return;
            }

            if (bossController != null)
            {
                bossController.EndAction();
            }

            ownsActionLock = false;
        }
    }
}