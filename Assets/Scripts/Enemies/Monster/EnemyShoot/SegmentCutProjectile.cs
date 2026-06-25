using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SegmentCutProjectile : MonoBehaviour // 세그먼트 분리 마법 구체
    {
        [Header("Movement")]
        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 8.0f; // 마법 구체 이동속도

        [Min(1.0f)]
        [SerializeField] private float turnSpeed = 220.0f; // 초당 회전속도

        [Min(0.1f)]
        [SerializeField] private float lifeTime = 5.0f; // 최대 생존시간

        [Header("Effect Reference")]
        [SerializeField] private ParticleSystem impactEffectPrefab; // 머리 방어 또는 세그먼트 적중 시 생성할 충돌 효과 Prefab

        private Transform targetSegment; // 추적할 무기 세그먼트
        private float lifeTimer; // 남은 생존시간
        private bool resolved; // 방어 또는 적중 결과가 이미 처리됐는지 확인
        private bool targetReservationReleased;

        public void Initialize(Transform target)
        {
            targetSegment = target; // 마법사가 선택한 무기 세그먼트를 저장한다.
            lifeTimer = lifeTime; // 투사체 생존시간을 초기화한다.
            resolved = false; // 아직 충돌 결과가 처리되지 않은 상태로 초기화한다.
            targetReservationReleased = false;
        }

        private void Update()
        {
            if (resolved)
            {
                return; // 이미 방어 또는 적중 처리가 끝났다면 더 이상 이동하지 않는다.
            }

            if (targetSegment == null)
            {
                ReleaseTargetReservation();
                Destroy(gameObject); // 선택된 세그먼트가 사라졌다면 투사체를 제거한다.
                return;
            }

            if (!MonsterInteractionApi.IsAttachedSegmentCutTarget(targetSegment))
            {
                ReleaseTargetReservation();
                Destroy(gameObject);
                return;
            }

            lifeTimer -= Time.deltaTime; // 지난 시간만큼 생존시간을 감소시킨다.

            if (lifeTimer <= 0.0f)
            {
                ReleaseTargetReservation();
                Destroy(gameObject); // 제한시간 안에 적중하지 못했다면 투사체를 제거한다.
                return;
            }

            Vector3 targetPosition = targetSegment.position; // 선택된 세그먼트의 현재 위치를 가져온다.
            Vector3 direction = targetPosition - transform.position; // 투사체에서 대상까지의 방향을 계산한다.

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return; // 방향을 계산할 수 없을 정도로 가까우면 이번 프레임에는 이동하지 않는다.
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up); // 대상 세그먼트를 바라보는 회전을 만든다.

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime); // 회전속도 제한을 적용해 대상 방향으로 회전한다.

            transform.position += transform.forward * (moveSpeed * Time.deltaTime); // 현재 앞 방향으로 마법 구체를 이동시킨다.
        }

        private void OnTriggerEnter(Collider other)
        {
            if (resolved)
            {
                return; // 이미 결과가 처리됐다면 추가 충돌을 무시한다.
            }

            if (MonsterInteractionApi.IsConvoyHeadCollider(other))
            {
                resolved = true; // 머리가 마법 구체를 막았으므로 결과 처리를 완료한다.

                PlayImpactEffect(transform.position); // 머리가 마법을 막은 위치에 충돌 효과를 재생한다.

                ReleaseTargetReservation();

                Destroy(gameObject); // 세그먼트를 분리하지 않고 투사체만 제거한다.
                return;
            }

            if (!MonsterInteractionApi.IsTargetWeaponSegmentCollider(other, targetSegment))
            {
                return; // 선택된 대상이 아닌 다른 Collider와 세그먼트는 무시한다.
            }

            resolved = true; // 선택된 무기 세그먼트 적중 결과를 한 번만 처리한다.

            PlayImpactEffect(transform.position); // 선택된 세그먼트에 적중한 위치에 충돌 효과를 재생한다.

            MonsterInteractionApi.RequestSegmentCut(targetSegment); // 선택된 세그먼트를 기준으로 실제 분리를 요청한다.

            ReleaseTargetReservation();

            Destroy(gameObject); // 적중 처리가 끝났으므로 투사체를 제거한다.
        }

        private void OnDestroy()
        {
            ReleaseTargetReservation();
        }

        private void ReleaseTargetReservation()
        {
            if (targetReservationReleased)
            {
                return;
            }

            MonsterInteractionApi.ReleaseSegmentCutTarget(targetSegment);
            targetReservationReleased = true;
        }

        private void PlayImpactEffect(Vector3 effectPosition)
        {
            if (impactEffectPrefab == null)
            {
                return; // 연결된 충돌 효과 Prefab이 없다면 효과를 생성하지 않는다.
            }

            ParticleSystem impactEffect = Instantiate(impactEffectPrefab, effectPosition, Quaternion.identity); // 충돌 위치에 별도의 파티클 인스턴스를 생성한다.

            impactEffect.Play(); // Play On Awake 설정과 관계없이 충돌 파티클을 재생한다.

            ParticleSystem.MainModule main = impactEffect.main; // 생성된 파티클의 Main 설정을 가져온다.
            float destroyDelay = main.duration + main.startLifetime.constantMax; // 방출시간과 파티클 생존시간을 더해 제거시간을 계산한다.

            Destroy(impactEffect.gameObject, destroyDelay); // 파티클 재생이 끝난 뒤 충돌 효과 인스턴스를 제거한다.
        }
    }
}