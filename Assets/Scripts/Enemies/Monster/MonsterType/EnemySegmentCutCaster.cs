using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySegmentCutCaster : MonoBehaviour // 컨보이 무기 세그먼트 절단 마법 몬스터
    {
        private Transform target; // 사거리 판정에 사용할 컨보이 대표 Transform

        [Header("Cast Reference")]
        [SerializeField] private Transform castPoint; // 마법 투사체가 생성되는 위치

        [SerializeField]
        private SegmentCutMagicEffect magicEffectPrefab; // 선택된 무기 세그먼트에 표시할 경고 효과 Prefab

        [SerializeField]
        private SegmentCutProjectile projectilePrefab; // 선택된 무기 세그먼트를 추적할 마법 구체 Prefab

        [Header("Cast Setting")]
        [Min(0.0f)]
        [SerializeField] private float firstCastDelay = 3.0f; // 몬스터 생성 후 첫 번째 마법까지의 대기시간

        [Min(1.0f)]
        [SerializeField] private float castRange = 10.0f; // 새로운 마법 시전을 시작할 수 있는 거리

        [Min(1.0f)]
        [SerializeField] private float castInterval = 8.0f; // 마법 발사 후 다음 시전까지의 대기시간

        [Min(0.1f)]
        [SerializeField] private float castDelay = 2.0f; // 선택된 세그먼트를 표시한 뒤 발사하기까지의 준비시간

        private EnemyController ownerController; // 이 Script Component가 붙은 몬스터의 EnemyController

        private Transform selectedTargetSegment; // 이번 마법에서 선택된 무기 세그먼트

        private float castTimer; // 다음 마법 시도까지 남은 시간

        private Coroutine castCoroutine; // 현재 진행 중인 마법 시전 Coroutine

        private SegmentCutMagicEffect currentMagicEffect; // 현재 선택된 세그먼트에 표시된 경고 효과

        public float CastRange
        {
            get
            {
                return castRange;
            }
        }

        public bool IsCasting
        {
            get
            {
                return castCoroutine != null;
            }
        }

        private void Awake()
        {
            ownerController = GetComponent<EnemyController>(); // 같은 GameObject의 EnemyController Script Component를 찾는다.

            TryFindTarget(); // MonsterInteractionApi에서 컨보이 대표 대상을 찾는다.
        }

        private void OnEnable()
        {
            castTimer = firstCastDelay; // 몬스터가 생성된 후 첫 시전까지 3초를 기다린다.
        }

        private void OnDisable()
        {
            CancelCast(); // 몬스터가 죽거나 비활성화되면 진행 중인 시전과 경고 효과를 취소한다.
        }

        private void Update()
        {
            if (target == null)
            {
                TryFindTarget(); // 컨보이 대표 대상이 없다면 다시 찾는다.
            }

            if (target == null)
            {
                return; // 컨보이가 등록되어 있지 않다면 시전하지 않는다.
            }

            if (projectilePrefab == null)
            {
                return; // 발사할 절단 마법 구체 Prefab이 없다면 시전하지 않는다.
            }

            if (magicEffectPrefab == null)
            {
                return; // 선택된 세그먼트를 알려줄 경고 효과가 없다면 시전하지 않는다.
            }

            if (EnemySupportDebuffState.IsEnemyFrozen(ownerController))
            {
                return; // 동결 중에는 첫 시전과 재사용 대기시간을 진행하지 않는다.
            }

            if (castCoroutine != null)
            {
                return; // 이미 마법을 준비 중이라면 새로운 시전을 시작하지 않는다.
            }

            castTimer -= Time.deltaTime; // 지난 시간만큼 다음 시전 대기시간을 감소시킨다.

            if (castTimer > 0.0f)
            {
                return; // 아직 시전 대기시간이 남았다면 실행하지 않는다.
            }

            if (!IsTargetInCastRange())
            {
                return; // 시전을 시작하는 순간 컨보이가 사거리 밖이라면 실행하지 않는다.
            }

            if (!MonsterInteractionApi.TryGetRandomAttachedWeaponSegment(out Transform weaponSegment))
            {
                return; // 현재 컨보이에 절단할 수 있는 부착 무기 세그먼트가 없다면 시전하지 않는다.
            }

            selectedTargetSegment = weaponSegment; // 무작위로 선택된 부착 무기 세그먼트를 이번 마법의 대상으로 저장한다.

            castCoroutine = StartCoroutine(CastRoutine()); // 2초 경고 표시와 투사체 발사 과정을 시작한다.
        }

        private void TryFindTarget()
        {
            if (MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget))
            {
                target = convoyTarget; // 등록된 컨보이 대표 Transform을 사거리 판정 대상으로 저장한다.
                return;
            }

            target = null; // 등록된 컨보이가 없다면 대상을 비운다.
        }

        public bool IsTargetInCastRange()
        {
            if (target == null)
            {
                TryFindTarget(); // 대상이 없다면 MonsterInteractionApi에서 다시 찾아본다.
            }

            if (target == null)
            {
                return false; // 컨보이가 없다면 사거리 안에 있지 않다고 반환한다.
            }

            Vector3 offset = target.position - transform.position; // 몬스터에서 컨보이 대표 위치까지의 거리 벡터를 구한다.
            offset.y = 0.0f; // 지면 기준 거리만 사용하기 위해 높이를 제거한다.

            return offset.sqrMagnitude <= castRange * castRange; // 제곱 거리를 사용하여 시전 사거리 안인지 반환한다.
        }

        private IEnumerator CastRoutine()
        {
            CreateMagicEffect(); // 선택된 무기 세그먼트에 2초 동안 경고 효과를 표시한다.

            float timer = 0.0f; // 현재까지 진행된 시전 준비시간

            while (timer < castDelay)
            {
                if (target == null ||
                    selectedTargetSegment == null ||
                    !MonsterInteractionApi.IsAttachedSegmentCutTarget(selectedTargetSegment))
                {
                    ReleaseSelectedTargetReservation();
                    CancelCurrentMagicEffect(); // 컨보이 또는 선택된 세그먼트가 사라졌다면 경고 효과를 제거한다.
                    FinishCast(); // 현재 시전을 종료하고 다음 재사용 대기시간을 설정한다.
                    yield break;
                }

                if (EnemySupportDebuffState.IsEnemyFrozen(ownerController))
                {
                    yield return null; // 동결 중에는 시전 준비시간을 증가시키지 않고 다음 프레임까지 기다린다.
                    continue;
                }

                timer += Time.deltaTime; // 동결이 아닐 때만 시전 준비시간을 증가시킨다.

                yield return null; // 다음 프레임까지 기다린다.
            }

            LaunchProjectile(); // 시전 시작 후 사거리 밖으로 나가더라도 마법 구체를 발사한다.

            FinishCast(); // 투사체 발사 후 현재 시전을 끝내고 8초 재사용 대기시간을 시작한다.
        }

        private void CreateMagicEffect()
        {
            CancelCurrentMagicEffect(); // 이전 경고 효과가 남아 있다면 먼저 제거한다.

            if (magicEffectPrefab == null)
            {
                return; // 경고 효과 Prefab이 없다면 생성하지 않는다.
            }

            if (selectedTargetSegment == null)
            {
                return; // 선택된 무기 세그먼트가 없다면 생성하지 않는다.
            }

            Vector3 effectPosition = selectedTargetSegment.position + Vector3.up * 0.05f; // 선택된 세그먼트보다 약간 위에 표시할 위치를 계산한다.

            currentMagicEffect = Instantiate(
                magicEffectPrefab,
                effectPosition,
                Quaternion.identity,
                selectedTargetSegment); // 선택된 세그먼트의 자식으로 경고 효과를 생성하여 함께 움직이게 한다.

            currentMagicEffect.ShowWarning(); // 경고 표시를 활성화한다.
        }

        private void LaunchProjectile()
        {
            if (projectilePrefab == null)
            {
                ReleaseSelectedTargetReservation();
                CancelCurrentMagicEffect(); // 발사할 Prefab이 없다면 남아 있는 경고 효과를 제거한다.
                return;
            }

            if (selectedTargetSegment == null ||
                !MonsterInteractionApi.IsAttachedSegmentCutTarget(selectedTargetSegment))
            {
                ReleaseSelectedTargetReservation();
                CancelCurrentMagicEffect(); // 선택된 세그먼트가 사라졌다면 남아 있는 경고 효과를 제거한다.
                return;
            }

            Vector3 spawnPosition = castPoint != null ? castPoint.position : transform.position; // CasterPoint가 있으면 사용하고, 없다면 몬스터 위치를 사용한다.

            Vector3 direction = selectedTargetSegment.position - spawnPosition; // 발사 위치에서 선택된 세그먼트까지의 방향을 계산한다.

            Quaternion spawnRotation = direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction.normalized, Vector3.up) : transform.rotation; // 유효한 방향이 있다면 대상을 바라보고, 없다면 몬스터의 현재 회전을 사용한다.

            SegmentCutProjectile projectile = Instantiate(
                projectilePrefab,
                spawnPosition,
                spawnRotation); // CasterPoint 위치에 절단 마법 구체를 생성한다.

            projectile.Initialize(selectedTargetSegment); // 생성된 마법 구체에 추적할 무기 세그먼트를 전달한다.

            CancelCurrentMagicEffect(); // 투사체를 발사했으므로 대상 세그먼트의 경고 표시를 제거한다.
        }

        private void FinishCast()
        {
            castCoroutine = null; // 현재 실행 중인 Coroutine 참조를 비운다.

            selectedTargetSegment = null; // 이번 시전에 사용했던 무기 세그먼트 참조를 비운다.

            castTimer = castInterval; // 다음 마법까지 8초의 재사용 대기시간을 설정한다.
        }

        private void CancelCast()
        {
            if (castCoroutine != null)
            {
                StopCoroutine(castCoroutine); // 현재 진행 중인 시전 Coroutine을 중지한다.

                castCoroutine = null; // 중지한 Coroutine 참조를 비운다.
            }

            ReleaseSelectedTargetReservation();

            selectedTargetSegment = null; // 선택했던 무기 세그먼트 참조를 비운다.

            CancelCurrentMagicEffect(); // 진행 중이던 경고 효과도 함께 제거한다.
        }

        private void ReleaseSelectedTargetReservation()
        {
            if (selectedTargetSegment == null)
            {
                return;
            }

            MonsterInteractionApi.ReleaseSegmentCutTarget(selectedTargetSegment);
        }

        private void CancelCurrentMagicEffect()
        {
            if (currentMagicEffect == null)
            {
                return; // 제거할 경고 효과가 없다면 실행하지 않는다.
            }

            currentMagicEffect.Cancel(); // 현재 생성된 경고 효과 인스턴스를 제거한다.

            currentMagicEffect = null; // 제거한 효과의 참조를 비운다.
        }
    }
}