using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class BonusChest : MonoBehaviour
    {
        [Header("상자 감지 설정")]
        [Tooltip("컨보이 머리가 이 거리 안으로 들어오면 상자가 열립니다.")]
        [InspectorName("열림 거리")]
        [Range(0.5f, 80.0f)]
        [SerializeField] private float openDistance = 10.0f; // 상자가 열리기 시작하는 거리입니다.

        [Header("애니메이션 설정")]
        [Tooltip("상자 열림 애니메이션을 재생할 Animator입니다. 비워두면 자식에서 자동으로 찾습니다.")]
        [InspectorName("상자 애니메이터")]
        [SerializeField] private Animator animator; // 상자 열림 애니메이션 담당입니다.

        [Tooltip("상자 열림 Trigger 이름입니다. 현재 상자처럼 Trigger가 없으면 비워둬도 됩니다.")]
        [InspectorName("열림 트리거 이름")]
        [SerializeField] private string openTriggerName = "Open"; // 선택적으로 사용할 Animator Trigger입니다.

        [Tooltip("상자 열림 애니메이션 재생 속도입니다. 2면 2배속입니다.")]
        [InspectorName("열림 애니메이션 속도")]
        [Range(0.1f, 5.0f)]
        [SerializeField] private float openAnimationSpeed = 2.0f; // 상자가 너무 느리게 열릴 때 조절합니다.

        [Tooltip("애니메이션을 몇 퍼센트 지점부터 재생할지 정합니다. 0이면 처음부터, 0.25면 25% 지점부터입니다.")]
        [InspectorName("열림 애니메이션 시작 지점")]
        [Range(0.0f, 0.95f)]
        [SerializeField] private float openAnimationStart = 0.0f; // 필요하면 초반 느린 구간을 건너뜁니다.

        [Tooltip("켜두면 생성 직후 Animator를 멈춰 상자가 자동으로 열리지 않게 합니다.")]
        [InspectorName("열리기 전 애니메이터 정지")]
        [SerializeField] private bool pauseAnimatorUntilOpen = true; // 스폰 직후 자동 재생을 막습니다.

        private bool opened; // 상자가 열렸는지 저장합니다.
        private ConvoyController cachedConvoy; // 컨보이 머리만 상자를 열 수 있게 하기 위한 캐시입니다.
        private BonusChestWaveSpawner ownerSpawner; // 같은 보너스 웨이브의 다른 상자를 정리하기 위한 스포너 참조입니다.
        private Transform choiceGroupRoot; // 같은 선택 그룹에 속한 상자들을 찾을 부모입니다.
        private bool allowOnlyOneChoice = true; // 한 상자만 열 수 있는지 저장합니다.
        private float unselectedChestDestroyDelay = 0.2f; // 선택되지 않은 상자 제거 대기 시간입니다.

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

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

        }

        public void ConfigureOwner(BonusChestWaveSpawner owner)
        {
            ownerSpawner = owner;
        }

        public void ConfigureChoiceGroup(Transform groupRoot, bool oneChoiceOnly, float removeDelay)
        {
            choiceGroupRoot = groupRoot;
            allowOnlyOneChoice = oneChoiceOnly;
            unselectedChestDestroyDelay = Mathf.Max(0.0f, removeDelay);
        }

        public void RemoveWithoutReward(float delay)
        {
            if (this == null)
            {
                return;
            }

            Destroy(gameObject, Mathf.Max(0.0f, delay));
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

            return convoyTarget; // 테스트 환경에서 HeadVisual이 없을 때만 사용하는 안전장치입니다.
        }

        private void OpenChest()
        {
            if (ownerSpawner != null && !ownerSpawner.TrySelectChest(this))
            {
                return;
            }

            RemoveOtherChoiceChests();

            opened = true;

            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.speed = Mathf.Max(0.1f, openAnimationSpeed);
            animator.Play(0, 0, Mathf.Clamp01(openAnimationStart));

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

        private void RemoveOtherChoiceChests()
        {
            if (!allowOnlyOneChoice)
            {
                return;
            }

            Transform root = choiceGroupRoot != null ? choiceGroupRoot : transform.parent;
            if (root == null)
            {
                return;
            }

            BonusChest[] chests = root.GetComponentsInChildren<BonusChest>(true);
            for (int i = 0; i < chests.Length; i++)
            {
                BonusChest chest = chests[i];
                if (chest == null || chest == this)
                {
                    continue;
                }

                chest.RemoveWithoutReward(unselectedChestDestroyDelay);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, openDistance);
        }
    }
}
