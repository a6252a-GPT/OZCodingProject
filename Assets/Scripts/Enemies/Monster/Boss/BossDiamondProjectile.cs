using UnityEngine;

namespace TeamProject01.Gameplay
{
    public enum BossDiamondProjectileMode
    {
        Straight = 0, // Normal 상태에서 Nexus 중심으로 직선 이동하는 모드
        FormationHoming = 1 // Berserk 상태에서 대형, 원호, 돌진 순서로 이동하는 모드
    }

    public sealed class BossDiamondProjectile : MonoBehaviour
    {
        private enum FormationHomingState
        {
            MovingToFormation = 0, // 생성 위치에서 등 뒤 대형 위치로 이동하는 상태
            Standby = 1, // 대형을 유지하며 자신의 출격 순서를 기다리는 상태
            ArcFlight = 2, // Nexus를 중심으로 원호를 그리며 이동하는 상태
            DiveAttack = 3 // 원호 이동을 끝내고 Nexus로 고속 돌진하는 상태
        }

        [Header("Projectile")]
        [Min(0)]
        [SerializeField] private int nexusDamage = 1; // Nexus에 도착했을 때 적용할 피해량

        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 6.0f; // Normal 투사체의 직선 이동 속도

        [Min(0.1f)]
        [SerializeField] private float hitDistance = 0.7f; // Normal 투사체의 Nexus 도착 판정 거리

        [Min(0.1f)]
        [SerializeField] private float lifeTime = 20.0f; // 투사체가 존재할 수 있는 최대 시간

        [Header("Formation")]
        [Min(0.0f)]
        [SerializeField] private float formationDuration = 0.6f; // 생성 위치에서 대형 위치까지 이동하는 시간

        [Header("Berserk Arc Flight")]
        [Min(1.0f)]
        [SerializeField] private float arcAngularSpeed = 140.0f; // Nexus 주변을 회전하는 초당 각도

        [Min(0.0f)]
        [SerializeField] private float minimumArcDuration = 0.6f; // 돌진 전 최소 원호 이동 시간

        [Min(0.0f)]
        [SerializeField] private float maximumArcDuration = 1.4f; // 돌진 전 최대 원호 이동 시간

        [Header("Berserk Dive Attack")]
        [Min(0.1f)]
        [SerializeField] private float diveSpeed = 18.0f; // 원호 이동 후 Nexus로 돌진하는 속도

        [Min(0.0f)]
        [SerializeField] private float diveImpactRadius = 0.8f; // Nexus 중심에서 조금 떨어진 충돌 지점의 반지름

        [Min(0.1f)]
        [SerializeField] private float diveHitDistance = 0.5f; // 돌진 목표점 도착 판정 거리

        private Transform target; // 실제 피해를 받을 Nexus Transform

        private BossDiamondProjectileMode projectileMode; // 현재 투사체의 이동 방식

        private FormationHomingState formationHomingState; // Berserk 투사체의 현재 이동 단계

        private Vector3 formationStartPosition; // 대형 이동을 시작한 월드 위치

        private Vector3 formationTargetPosition; // 대형에서 대기할 월드 위치

        private Vector3 homingTargetOffset; // Nexus 중심을 기준으로 전달받은 평면 방향값

        private Vector3 movementDirection; // 현재 투사체가 이동하는 방향

        private Vector3 diveTargetPosition; // 원호 이동 후 최종적으로 돌진할 Nexus 주변 위치

        private float standbyDuration; // 대형에서 자신의 출격 순서를 기다릴 시간

        private float lifeTimer; // 투사체가 생성된 후 지난 시간

        private float modeTimer; // 현재 이동 상태에서 지난 시간

        private float arcCurrentAngle; // Nexus를 기준으로 한 현재 원호 각도

        private float arcRadius; // Nexus를 기준으로 한 현재 원호 반지름

        private float arcFlightDuration; // 이번 투사체가 원호 이동을 유지할 시간

        private float arcFlightHeight; // 원호 이동 중 유지할 Y축 높이

        private float arcDirectionSign; // 시계 또는 반시계 방향을 나타내는 값

        private bool isConfigured; // 목표와 이동 방식이 설정됐는지 나타내는 값

        private bool isDestroyed; // 제거 처리가 시작됐는지 나타내는 값

        public bool IsDestroyed
        {
            get
            {
                return isDestroyed; // 외부에서 투사체의 제거 상태를 확인할 수 있게 반환한다.
            }
        }

        private void Update()
        {
            if (isDestroyed) // 이미 제거 처리가 시작됐다면
            {
                return; // 이동과 피해 처리를 반복하지 않는다.
            }

            lifeTimer += Time.deltaTime; // 투사체가 존재한 시간을 증가시킨다.

            if (lifeTimer >= lifeTime) // 최대 유지 시간을 넘었다면
            {
                DestroyProjectile(); // 목표에 도착하지 못했더라도 제거한다.
                return;
            }

            if (!isConfigured) // 아직 Nexus 목표가 설정되지 않았다면
            {
                return; // 이동하지 않는다.
            }

            if (target == null) // Nexus가 제거됐거나 참조를 잃었다면
            {
                DestroyProjectile(); // 이동할 목표가 없으므로 제거한다.
                return;
            }

            if (projectileMode == BossDiamondProjectileMode.FormationHoming) // Berserk 투사체라면
            {
                MoveFormationHoming(); // 대형, 원호, 돌진 이동을 처리한다.
                return;
            }

            if (TryHitNormalTarget()) // Normal 투사체가 이미 Nexus 도착 범위 안이라면
            {
                return; // 피해 처리가 끝났으므로 이동하지 않는다.
            }

            MoveStraight(); // Normal 투사체를 Nexus 중심으로 이동시킨다.

            TryHitNormalTarget(); // 이동 후 Nexus 도착 여부를 다시 확인한다.
        }

        public void Configure(Transform target) // Normal 직선 투사체를 설정하는 함수
        {
            InitializeProjectile(target); // 공통 투사체 상태를 초기화한다.

            projectileMode = BossDiamondProjectileMode.Straight; // 직선 이동 모드로 설정한다.

            if (target == null) // 유효한 Nexus가 없다면
            {
                return; // 이동 방향을 계산하지 않는다.
            }

            movementDirection = target.position - transform.position; // 현재 위치에서 Nexus까지의 방향을 계산한다.

            if (movementDirection.sqrMagnitude <= 0.0001f) // 방향을 계산할 수 없다면
            {
                return; // 현재 회전을 유지한다.
            }

            movementDirection.Normalize(); // 이동 방향의 길이를 1로 만든다.
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up); // Nexus 방향을 바라보게 한다.
        }

        public void ConfigureFormationHoming(Transform target, Vector3 formationPosition, float standbyDuration) // 기존 3개 매개변수 연결용 함수
        {
            ConfigureFormationHoming(target, formationPosition, standbyDuration, Vector3.zero); // 별도 방향이 없으면 자동으로 방향을 계산한다.
        }

        public void ConfigureFormationHoming(Transform target, Vector3 formationPosition, float standbyDuration, Vector3 homingTargetOffset) // Berserk 투사체를 설정하는 함수
        {
            InitializeProjectile(target); // 공통 투사체 상태를 초기화한다.

            projectileMode = BossDiamondProjectileMode.FormationHoming; // Berserk 이동 모드로 설정한다.
            formationHomingState = FormationHomingState.MovingToFormation; // 대형 이동 상태부터 시작한다.

            formationStartPosition = transform.position; // 현재 생성 위치를 대형 이동 시작점으로 저장한다.
            formationTargetPosition = formationPosition; // 공격 Script가 계산한 대형 위치를 저장한다.
            this.standbyDuration = Mathf.Max(0.0f, standbyDuration); // 대기시간을 0 이상으로 저장한다.

            this.homingTargetOffset = homingTargetOffset; // Nexus 주변에서 사용할 충돌 방향을 저장한다.
            this.homingTargetOffset.y = 0.0f; // 충돌 방향의 상하 분산을 제거한다.

            movementDirection = formationTargetPosition - formationStartPosition; // 생성점에서 대형 위치까지의 방향을 계산한다.

            if (movementDirection.sqrMagnitude <= 0.0001f) // 대형 이동 방향을 계산할 수 없다면
            {
                movementDirection = transform.forward; // 현재 바라보는 방향을 대신 사용한다.
            }

            movementDirection.Normalize(); // 이동 방향의 길이를 1로 만든다.
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up); // 대형 위치 방향을 바라보게 한다.
        }

        private void InitializeProjectile(Transform target) // 모든 발사 방식에서 사용하는 공통 초기화 함수
        {
            this.target = target; // 전달받은 Nexus Transform을 저장한다.

            lifeTimer = 0.0f; // 유지시간을 초기화한다.
            modeTimer = 0.0f; // 상태 타이머를 초기화한다.
            standbyDuration = 0.0f; // 이전 대기시간을 초기화한다.
            homingTargetOffset = Vector3.zero; // 이전 목표 방향을 초기화한다.
            diveTargetPosition = Vector3.zero; // 이전 돌진 목표점을 초기화한다.
            isDestroyed = false; // 제거되지 않은 상태로 초기화한다.
            isConfigured = target != null; // 유효한 Nexus가 있다면 설정 완료 상태로 저장한다.
        }

        private void MoveStraight() // Normal 투사체를 Nexus 중심으로 직선 이동시키는 함수
        {
            Vector3 offset = target.position - transform.position; // 현재 위치에서 Nexus까지의 방향을 계산한다.

            if (offset.sqrMagnitude <= 0.0001f) // 이동 방향을 계산할 수 없다면
            {
                return; // 현재 위치를 유지한다.
            }

            movementDirection = offset.normalized; // Nexus 방향의 길이를 1로 만든다.

            transform.position = Vector3.MoveTowards(transform.position, target.position, moveSpeed * Time.deltaTime); // Nexus 중심으로 직선 이동한다.
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up); // 이동 방향을 바라보게 한다.
        }

        private void MoveFormationHoming() // Berserk 투사체의 전체 이동 단계를 처리하는 함수
        {
            if (formationHomingState == FormationHomingState.MovingToFormation) // 대형 위치로 이동 중이라면
            {
                MoveToFormation(); // 지정된 대형 위치로 이동한다.
                return;
            }

            if (formationHomingState == FormationHomingState.Standby) // 대형에서 대기 중이라면
            {
                WaitInFormation(); // 대형 위치를 유지한다.
                return;
            }

            if (formationHomingState == FormationHomingState.ArcFlight) // Nexus 주변을 회전 중이라면
            {
                MoveArcFlight(); // 원호 이동을 처리한다.
                return;
            }

            MoveDiveAttack(); // 원호 이동 후 Nexus 돌진을 처리한다.
        }

        private void MoveToFormation() // 생성 위치에서 대형 위치로 펼쳐지는 함수
        {
            modeTimer += Time.deltaTime; // 대형 이동 시간을 증가시킨다.

            float progress = formationDuration <= 0.0f ? 1.0f : Mathf.Clamp01(modeTimer / formationDuration); // 이동 진행률을 계산한다.
            float easedProgress = Mathf.SmoothStep(0.0f, 1.0f, progress); // 시작과 끝을 부드럽게 만든다.

            Vector3 nextPosition = Vector3.Lerp(formationStartPosition, formationTargetPosition, easedProgress); // 이번 프레임 위치를 계산한다.
            Vector3 nextDirection = nextPosition - transform.position; // 실제 이동 방향을 계산한다.

            transform.position = nextPosition; // 계산된 위치로 이동한다.

            if (nextDirection.sqrMagnitude > 0.0001f) // 유효한 이동 방향이 있다면
            {
                transform.rotation = Quaternion.LookRotation(nextDirection.normalized, Vector3.up); // 이동 방향을 바라보게 한다.
            }

            if (progress < 1.0f) // 아직 대형 위치에 도착하지 않았다면
            {
                return; // 대기 상태로 넘어가지 않는다.
            }

            transform.position = formationTargetPosition; // 최종 대형 위치를 정확히 맞춘다.
            formationHomingState = FormationHomingState.Standby; // 대형 대기 상태로 변경한다.
            modeTimer = 0.0f; // 대기시간 계산을 위해 타이머를 초기화한다.
        }

        private void WaitInFormation() // 대형을 유지하며 자신의 출격 시간을 기다리는 함수
        {
            transform.position = formationTargetPosition; // 대기 중에는 지정된 대형 위치를 유지한다.

            modeTimer += Time.deltaTime; // 대기시간을 증가시킨다.

            if (modeTimer < standbyDuration) // 아직 자신의 출격 시간이 되지 않았다면
            {
                return; // 대형을 계속 유지한다.
            }

            BeginArcFlight(); // Nexus 주변 원호 이동을 시작한다.
        }

        private void BeginArcFlight() // 원호 이동을 시작하기 위한 값을 계산하는 함수
        {
            formationHomingState = FormationHomingState.ArcFlight; // 원호 이동 상태로 변경한다.
            modeTimer = 0.0f; // 원호 이동 시간을 초기화한다.

            Vector3 startOffset = transform.position - target.position; // Nexus 중심에서 투사체까지의 방향을 계산한다.
            startOffset.y = 0.0f; // 원호 이동을 XZ 평면으로 제한한다.

            if (startOffset.sqrMagnitude <= 0.0001f) // 원호 시작 방향을 계산할 수 없다면
            {
                startOffset = -transform.forward; // 현재 앞 방향의 반대를 사용한다.
                startOffset.y = 0.0f; // Y축을 제거한다.
            }

            arcRadius = Mathf.Max(0.1f, startOffset.magnitude); // 현재 Nexus와의 거리를 원호 반지름으로 저장한다.
            arcCurrentAngle = Mathf.Atan2(startOffset.z, startOffset.x) * Mathf.Rad2Deg; // 현재 Nexus 기준 각도를 저장한다.
            arcDirectionSign = Random.value < 0.5f ? -1.0f : 1.0f; // 시계 또는 반시계 방향을 무작위로 선택한다.

            float minimumDuration = Mathf.Min(minimumArcDuration, maximumArcDuration); // 두 시간 중 작은 값을 최소 시간으로 사용한다.
            float maximumDuration = Mathf.Max(minimumArcDuration, maximumArcDuration); // 두 시간 중 큰 값을 최대 시간으로 사용한다.

            arcFlightDuration = Random.Range(minimumDuration, maximumDuration); // 각 투사체마다 서로 다른 원호 유지 시간을 선택한다.
            arcFlightHeight = transform.position.y; // 대형에서 출격한 높이를 원호 이동 중 유지한다.
        }

        private void MoveArcFlight() // Nexus를 중심으로 원호를 그리며 이동하는 함수
        {
            modeTimer += Time.deltaTime; // 원호 이동 시간을 증가시킨다.
            arcCurrentAngle += arcDirectionSign * arcAngularSpeed * Time.deltaTime; // 시계 또는 반시계 방향으로 각도를 증가시킨다.

            float angleRadians = arcCurrentAngle * Mathf.Deg2Rad; // 원호 위치 계산을 위해 각도를 라디안으로 변환한다.

            Vector3 planarOffset = new Vector3(Mathf.Cos(angleRadians), 0.0f, Mathf.Sin(angleRadians)) * arcRadius; // Nexus 기준 원형 위치를 계산한다.
            Vector3 nextPosition = target.position + planarOffset; // Nexus 위치에 원형 오프셋을 더해 월드 위치를 계산한다.

            nextPosition.y = arcFlightHeight; // 원호 이동 중에는 Y축 높이를 유지한다.

            Vector3 nextDirection = nextPosition - transform.position; // 원호의 실제 접선 이동 방향을 계산한다.

            transform.position = nextPosition; // 계산된 원호 위치로 이동한다.

            if (nextDirection.sqrMagnitude > 0.0001f) // 실제 이동 방향이 있다면
            {
                nextDirection.y = 0.0f; // 위아래로 기울어지지 않도록 Y축을 제거한다.

                if (nextDirection.sqrMagnitude > 0.0001f) // 평면 이동 방향이 유효하다면
                {
                    transform.rotation = Quaternion.LookRotation(nextDirection.normalized, Vector3.up); // 원호의 접선 방향을 바라보게 한다.
                }
            }

            if (modeTimer < arcFlightDuration) // 아직 선택된 원호 이동 시간이 지나지 않았다면
            {
                return; // 계속 Nexus 주변을 회전한다.
            }

            BeginDiveAttack(); // 원호 이동을 끝내고 Nexus 돌진을 시작한다.
        }

        private void BeginDiveAttack() // Nexus 주변의 서로 다른 지점으로 돌진을 시작하는 함수
        {
            formationHomingState = FormationHomingState.DiveAttack; // 돌진 상태로 변경한다.
            modeTimer = 0.0f; // 돌진 상태 타이머를 초기화한다.

            Vector3 impactDirection = homingTargetOffset; // 공격 Script가 전달한 좌·우·앞·뒤·대각선 방향을 가져온다.
            impactDirection.y = 0.0f; // 충돌 방향에서 Y축을 제거한다.

            if (impactDirection.sqrMagnitude <= 0.0001f) // 전달받은 방향이 없다면
            {
                impactDirection = transform.position - target.position; // 현재 원호 위치 방향을 사용한다.
                impactDirection.y = 0.0f; // Y축을 제거한다.
            }

            if (impactDirection.sqrMagnitude <= 0.0001f) // 현재 위치 방향도 계산할 수 없다면
            {
                impactDirection = Vector3.forward; // 기본 앞 방향을 사용한다.
            }

            impactDirection.Normalize(); // 충돌 방향의 길이를 1로 만든다.

            diveTargetPosition = target.position + impactDirection * diveImpactRadius; // Nexus 중심 주변의 서로 다른 충돌 지점을 계산한다.

            movementDirection = diveTargetPosition - transform.position; // 현재 원호 위치에서 돌진 목표점까지의 방향을 계산한다.

            if (movementDirection.sqrMagnitude > 0.0001f) // 돌진 방향이 유효하다면
            {
                movementDirection.Normalize(); // 돌진 방향의 길이를 1로 만든다.
                transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up); // 즉시 돌진 방향으로 급선회한다.
            }
        }

        private void MoveDiveAttack() // 원호 이동 후 Nexus로 고속 돌진하는 함수
        {
            Vector3 offset = diveTargetPosition - transform.position; // 현재 위치에서 돌진 목표점까지의 방향과 거리를 계산한다.

            if (offset.sqrMagnitude <= diveHitDistance * diveHitDistance) // 돌진 목표점의 도착 범위 안이라면
            {
                HitNexus(); // Nexus에 피해를 적용하고 투사체를 제거한다.
                return;
            }

            movementDirection = offset.normalized; // 돌진 목표점 방향의 길이를 1로 만든다.

            transform.position = Vector3.MoveTowards(transform.position, diveTargetPosition, diveSpeed * Time.deltaTime); // Nexus 주변 충돌 지점으로 빠르게 돌진한다.
            transform.rotation = Quaternion.LookRotation(movementDirection, Vector3.up); // 돌진 방향을 바라보게 한다.

            Vector3 remainingOffset = diveTargetPosition - transform.position; // 이동 후 남은 거리를 다시 계산한다.

            if (remainingOffset.sqrMagnitude <= diveHitDistance * diveHitDistance) // 이동 후 도착 범위 안에 들어왔다면
            {
                HitNexus(); // Nexus에 피해를 적용하고 투사체를 제거한다.
            }
        }

        private bool TryHitNormalTarget() // Normal 투사체의 Nexus 도착 여부를 확인하는 함수
        {
            Vector3 offset = target.position - transform.position; // 투사체와 Nexus 중심 사이의 거리를 계산한다.

            if (offset.sqrMagnitude > hitDistance * hitDistance) // 아직 Nexus 도착 범위보다 멀다면
            {
                return false; // 도착하지 않았다고 반환한다.
            }

            HitNexus(); // Nexus 피해와 투사체 제거를 처리한다.
            return true; // Nexus에 도착했다고 반환한다.
        }

        private void HitNexus() // Nexus 피해를 처리하는 함수
        {
            if (target != null) // Nexus가 아직 존재한다면
            {
                NexusController.TryApplyDamage(target, nexusDamage); // Nexus 공통 피해 API로 피해를 적용한다.
            }

            DestroyProjectile(); // 피해 처리 후 투사체를 제거한다.
        }

        private void DestroyProjectile() // 투사체 제거를 한 곳에서 처리하는 함수
        {
            if (isDestroyed) // 이미 제거 처리가 시작됐다면
            {
                return; // Destroy를 중복 실행하지 않는다.
            }

            isDestroyed = true; // 제거 상태로 저장한다.
            Destroy(gameObject); // 투사체 GameObject를 제거한다.
        }
    }
}