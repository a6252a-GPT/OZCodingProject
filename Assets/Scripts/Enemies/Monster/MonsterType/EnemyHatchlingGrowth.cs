using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyHatchlingGrowth : MonoBehaviour // 구슬을 흡수해서 성장하는 해츨링 몬스터
    {
        [Min(0.1f)]
        [SerializeField] private float absorbRange = 10.0f; // 이 범위 안에 있는 성장 구슬을 빨아들인다.

        [Min(0.1f)]
        [SerializeField] private float scanInterval = 0.25f; // 몇 초마다 주변 구슬을 찾을지

        [Min(1)]
        [SerializeField] private int maxGrowthStack = 100; // 최대 성장 스택

        [Min(1)]
        [SerializeField] private int maxAbsorbCountPerScan = 10; // 한 번 탐색할 때 최대 몇 개의 구슬을 흡수 대상으로 지정할지

        [Min(0.0f)]
        [SerializeField] private float maxHpIncreasePercentPerOrb = 0.05f; // 구슬 1개당 최대 HP 증가율

        [Min(0.0f)]
        [SerializeField] private float attackPowerIncreasePercentPerOrb = 0.05f; // 구슬 1개당 공격력 증가율

        [Min(0.0f)]
        [SerializeField] private float attackSpeedIncreasePercentPerOrb = 0.05f; // 구슬 1개당 공격속도 증가율

        [Min(0.0f)]
        [SerializeField] private float scaleIncreasePercentPerOrb = 0.05f; // 구슬 1개당 크기 증가율

        [SerializeField] private int growthStack; // 현재 먹은 구슬 수, Inspector 확인용 런타임 값

        private Vector3 baseScale; // 성장하기 전 원래 크기

        private EnemyHealth health; // 같은 GameObject에 붙은 EnemyHealth Script Component 참조

        private float scanTimer; // 다음 구슬 탐색까지 남은 시간

        public int GrowthStack // 외부에서 현재 성장 스택을 읽기 위한 property
        {
            get
            {
                return growthStack; // 현재 성장 스택을 반환한다.
            }
        }

        public bool CanGrow // 아직 더 성장할 수 있는지 확인하는 property
        {
            get
            {
                return growthStack < maxGrowthStack; // 성장 스택이 최대치보다 작으면 true를 반환한다.
            }
        }

        public float MaxHpIncreasePercentPerOrb // 다음 단계에서 EnemyHealth가 사용할 최대 HP 증가율 property
        {
            get
            {
                return maxHpIncreasePercentPerOrb; // 구슬 1개당 최대 HP 증가율을 반환한다.
            }
        }

        public float AttackPowerMultiplier // 공격 Script가 읽을 성장 공격력 배율
        {
            get
            {
                return 1.0f + growthStack * attackPowerIncreasePercentPerOrb; // 성장 스택에 따른 공격력 배율을 반환한다.
            }
        }

        public float AttackSpeedMultiplier // 공격 Script가 읽을 성장 공격속도 배율
        {
            get
            {
                return 1.0f + growthStack * attackSpeedIncreasePercentPerOrb; // 성장 스택에 따른 공격속도 배율을 반환한다.
            }
        }

        private void Awake()
        {
            baseScale = transform.localScale; // 성장 전 원래 크기를 저장한다.
            health = GetComponent<EnemyHealth>(); // 같은 GameObject에 붙은 EnemyHealth Script Component를 찾는다.
        }

        private void OnEnable()
        {
            scanTimer = scanInterval; // 처음 구슬 탐색까지의 시간을 설정한다.
        }

        private void Update()
        {
            if (!CanGrow) // 이미 최대 성장 상태라면
            {
                return; // 더 이상 구슬을 찾지 않는다.
            }

            scanTimer -= Time.deltaTime; // 지난 시간만큼 구슬 탐색 대기 시간을 줄인다.

            if (scanTimer > 0.0f) // 아직 탐색 시간이 남아 있다면
            {
                return; // 이번 프레임에는 구슬을 찾지 않는다.
            }

            scanTimer = scanInterval; // 다음 탐색 시간을 다시 설정한다.

            TryAbsorbNearbyOrbs(); // 주변 구슬을 찾아 흡수 대상으로 지정한다.
        }

        private void TryAbsorbNearbyOrbs() // 주변 성장 구슬을 찾아 흡수 명령을 보내는 함수
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, absorbRange); // 흡수 범위 안의 Collider들을 찾는다.

            int absorbCount = 0; // 이번 탐색에서 흡수 명령을 보낸 구슬 수

            for (int i = 0; i < colliders.Length; i++) // 찾은 Collider 목록을 순회한다.
            {
                if (absorbCount >= maxAbsorbCountPerScan) // 한 번에 지정할 수 있는 최대 구슬 수에 도달했다면
                {
                    return; // 더 이상 구슬을 지정하지 않는다.
                }

                EnemyGrowthOrb growthOrb = colliders[i].GetComponentInParent<EnemyGrowthOrb>(); // Collider가 속한 성장 구슬 Script Component를 찾는다.

                if (growthOrb == null) // 성장 구슬이 아니라면
                {
                    continue; // 제외한다.
                }

                if (growthOrb.IsAbsorbing) // 이미 다른 몬스터에게 빨려 들어가는 중이라면
                {
                    continue; // 중복 흡수하지 않는다.
                }

                growthOrb.StartAbsorb(this); // 구슬에게 이 몬스터 쪽으로 빨려 들어오라고 명령한다.
                absorbCount++; // 흡수 명령을 보낸 구슬 수를 증가시킨다.
            }
        }

        public void ConsumeGrowthOrb(EnemyGrowthOrb growthOrb) // 구슬이 몬스터에게 도착했을 때 호출되는 함수
        {
            if (growthOrb == null) // 구슬 정보가 없다면
            {
                return; // 성장하지 않는다.
            }

            if (!CanGrow) // 이미 최대 성장 상태라면
            {
                return; // 더 이상 성장하지 않는다.
            }

            growthStack++; // 성장 스택을 1 증가시킨다.

            if (health != null) // 체력 Script Component가 있다면
            {
                health.IncreaseMaxHpByPercentKeepingRatio(maxHpIncreasePercentPerOrb); // 현재 체력 비율을 유지하면서 최대 체력을 증가시킨다.
            }

            ApplyScaleGrowth(); // 현재 성장 스택에 맞게 크기를 갱신한다.
        }

        private void ApplyScaleGrowth() // 성장 스택에 따라 크기를 갱신하는 함수
        {
            float scaleMultiplier = 1.0f + growthStack * scaleIncreasePercentPerOrb; // 성장 스택에 따른 크기 배율을 계산한다.

            transform.localScale = baseScale * scaleMultiplier; // 원래 크기를 기준으로 최종 크기를 적용한다.
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, absorbRange); // Scene에서 선택했을 때 구슬 흡수 범위를 표시한다.
        }
    }
}