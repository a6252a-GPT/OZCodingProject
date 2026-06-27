using UnityEngine;

namespace TeamProject01.Gameplay
{
    [RequireComponent(typeof(EnemyObstacleSummoner))]
    public sealed class EnemyObstacleSummonerAnimatorBridge : MonoBehaviour
    {
        private static readonly int SummonParameter = Animator.StringToHash("Summon"); // Animator의 Summon Trigger Parameter를 Hash 값으로 저장한다.

        [Header("Animator")]
        [SerializeField] private Animator animator; // Necromancer 모델에 붙은 Animator Component 참조

        private EnemyObstacleSummoner obstacleSummoner; // 장애물 소환 상태를 읽을 EnemyObstacleSummoner 참조
        private EnemyHealth enemyHealth; // 몬스터가 사망했는지 확인할 EnemyHealth 참조

        private bool wasSummoning; // 이전 프레임에 장애물을 소환 중이었는지 저장한다.

        private void Awake()
        {
            obstacleSummoner = GetComponent<EnemyObstacleSummoner>(); // 같은 GameObject에 붙은 EnemyObstacleSummoner를 찾는다.
            enemyHealth = GetComponent<EnemyHealth>(); // 같은 GameObject에 붙은 EnemyHealth를 찾는다.

            if (animator == null) // Inspector에 Animator가 연결되지 않았다면
            {
                animator = GetComponentInChildren<Animator>(true); // 자식 오브젝트에서 Animator를 자동으로 찾는다.
            }
        }

        private void OnEnable()
        {
            wasSummoning = obstacleSummoner != null && obstacleSummoner.IsSummoning; // 활성화 시 현재 소환 상태를 초기값으로 저장한다.
        }

        private void Update()
        {
            if (animator == null || obstacleSummoner == null) // 필요한 Component가 없다면
            {
                return; // 애니메이션을 갱신하지 않는다.
            }

            if (enemyHealth != null && enemyHealth.IsDead) // 몬스터가 사망했다면
            {
                wasSummoning = false; // 이전 소환 상태를 초기화한다.
                return; // 소환 애니메이션을 실행하지 않는다.
            }

            bool isSummoning = obstacleSummoner.IsSummoning; // 현재 장애물 소환 상태를 가져온다.

            if (isSummoning && !wasSummoning) // 이전에는 소환 중이 아니었고 현재 소환이 시작됐다면
            {
                animator.ResetTrigger(SummonParameter); // 남아 있을 수 있는 Summon Trigger를 초기화한다.
                animator.SetTrigger(SummonParameter); // Summon 애니메이션을 실행한다.
            }

            wasSummoning = isSummoning; // 현재 소환 상태를 다음 프레임 비교용으로 저장한다.
        }

        private void OnDisable()
        {
            wasSummoning = false; // 비활성화될 때 이전 소환 상태를 초기화한다.

            if (animator != null) // Animator가 연결되어 있다면
            {
                animator.ResetTrigger(SummonParameter); // 남아 있는 Summon Trigger를 초기화한다.
            }
        }
    }
}