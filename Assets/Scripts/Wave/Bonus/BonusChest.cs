using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BonusChest : MonoBehaviour
    {
        [Header("상자 감지 설정")]
        [Tooltip("맨 앞 지렁이 머리가 이 거리 안에 들어오면 상자가 열립니다.")]
        [InspectorName("열림 거리")]
        [Range(0.5f, 60.0f)]
        [SerializeField] private float openDistance = 5.0f; // Distance for playing the open animation.

        [Tooltip("맨 앞 지렁이 머리가 이 거리 안에 들어오면 보상이 생성됩니다.")]
        [InspectorName("보상 획득 거리")]
        [Range(0.2f, 20.0f)]
        [SerializeField] private float collectDistance = 2.0f; // Distance for dropping the reward.

        [Header("보상 설정")]
        [Tooltip("상자에서 생성할 경험치 보상입니다. 실제 획득 처리는 기존 보상 시스템이 담당합니다.")]
        [InspectorName("경험치 보상")]
        [Min(0)]
        [SerializeField] private int experienceReward = 20; // Experience reward amount.

        [Tooltip("상자에서 생성할 골드 보상입니다. 실제 획득 처리는 기존 보상 시스템이 담당합니다.")]
        [InspectorName("골드 보상")]
        [Min(0)]
        [SerializeField] private int goldReward = 20; // Gold reward amount.

        [Header("애니메이션 설정")]
        [Tooltip("상자 열림 애니메이션을 재생할 Animator입니다. 비워두면 자식 오브젝트에서 자동으로 찾습니다.")]
        [InspectorName("상자 애니메이터")]
        [SerializeField] private Animator animator; // Chest open animator.

        [Tooltip("상자 열림 Trigger 이름입니다. 현재 상자처럼 Trigger가 없는 컨트롤러라면 비워둬도 됩니다.")]
        [InspectorName("열림 트리거 이름")]
        [SerializeField] private string openTriggerName = "Open"; // Optional trigger name.

        [Tooltip("켜두면 생성 직후 Animator를 멈춰서 상자가 자동으로 열리지 않게 합니다.")]
        [InspectorName("열리기 전 애니메이터 정지")]
        [SerializeField] private bool pauseAnimatorUntilOpen = true; // Prevents auto-open on spawn.

        [Tooltip("켜두면 보상이 생성된 뒤 상자 오브젝트를 제거합니다.")]
        [InspectorName("보상 후 상자 제거")]
        [SerializeField] private bool destroyAfterReward = true; // Remove chest after reward.

        [Tooltip("보상이 생성된 뒤 상자를 제거하기 전까지 기다리는 시간입니다.")]
        [InspectorName("제거 대기 시간")]
        [Range(0.0f, 10.0f)]
        [SerializeField] private float destroyDelay = 2.0f; // Time to leave the opened chest visible.

        private bool opened; // True after the open animation starts.
        private bool rewarded; // True after reward has been dropped.
        private int rewardId; // Stable reward id for this chest instance.
        private ConvoyController cachedConvoy; // Only the front worm head can open and collect this chest.

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            rewardId = GetInstanceID();

            if (animator != null && pauseAnimatorUntilOpen)
            {
                animator.enabled = false;
            }
        }

        private void Update()
        {
            Transform headTarget = ResolveHeadTarget();
            if (headTarget == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, headTarget.position);

            if (!opened && distance <= openDistance)
            {
                OpenChest();
            }

            if (!rewarded && distance <= collectDistance)
            {
                DropReward();
            }
        }

        public void ConfigureReward(int experience, int gold)
        {
            experienceReward = Mathf.Max(0, experience);
            goldReward = Mathf.Max(0, gold);
        }

        private Transform ResolveHeadTarget()
        {
            if (!MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget))
            {
                return null;
            }

            if (cachedConvoy == null || cachedConvoy.transform != convoyTarget || !cachedConvoy.gameObject.activeInHierarchy)
            {
                cachedConvoy = convoyTarget.GetComponent<ConvoyController>();
            }

            if (cachedConvoy != null && cachedConvoy.HeadVisual != null && cachedConvoy.HeadVisual.gameObject.activeInHierarchy)
            {
                return cachedConvoy.HeadVisual;
            }

            return convoyTarget; // Fallback for test setups that do not have HeadVisual yet.
        }

        private void OpenChest()
        {
            opened = true;

            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.Play(0, 0, 0.0f); // Works with the current chest controller that has no trigger parameter.

            if (!string.IsNullOrWhiteSpace(openTriggerName) && HasAnimatorTrigger(openTriggerName))
            {
                animator.SetTrigger(openTriggerName);
            }
        }

        private bool HasAnimatorTrigger(string triggerName)
        {
            if (animator == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = animator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == triggerName)
                {
                    return true;
                }
            }

            return false;
        }

        private void DropReward()
        {
            rewarded = true;

            RewardData reward = RewardData.Create(experienceReward, goldReward, rewardId, transform.position);
            RewardDropService.SpawnReward(reward, transform.position);

            if (destroyAfterReward)
            {
                Destroy(gameObject, destroyDelay);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, openDistance);

            Gizmos.color = new Color(1.0f, 0.8f, 0.1f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, collectDistance);
        }
    }
}
