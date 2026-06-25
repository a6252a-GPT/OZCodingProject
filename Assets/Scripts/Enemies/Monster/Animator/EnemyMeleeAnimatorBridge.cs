using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyMeleeAnimatorBridge : MonoBehaviour
    {
        public static readonly int IsMovingParameter = Animator.StringToHash("IsMoving");
        public static readonly int AttackParameter = Animator.StringToHash("Attack");
        public static readonly int HitParameter = Animator.StringToHash("Hit");

        [Header("Animator")]
        [SerializeField] private Animator animator;

        private EnemyMovement enemyMovement;
        private EnemyMeleeAttack enemyMeleeAttack;
        private EnemyHealth enemyHealth;
        private EnemySupportDebuffState supportDebuffState;

        private float previousHp;

        private void Awake()
        {
            enemyMovement = GetComponent<EnemyMovement>();
            enemyMeleeAttack = GetComponent<EnemyMeleeAttack>();
            enemyHealth = GetComponent<EnemyHealth>();
            supportDebuffState = GetComponent<EnemySupportDebuffState>();

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }

            if (enemyHealth != null)
            {
                previousHp = enemyHealth.CurrentHp;
            }
        }

        private void OnEnable()
        {
            if (enemyMeleeAttack != null)
            {
                enemyMeleeAttack.AttackPerformed += PlayAttack;
            }

            if (enemyHealth != null)
            {
                previousHp = enemyHealth.CurrentHp;
            }

            UpdateMovementAnimation();
        }

        private void Update()
        {
            UpdateMovementAnimation();
            UpdateHitAnimation();
        }

        private void OnDisable()
        {
            if (enemyMeleeAttack != null)
            {
                enemyMeleeAttack.AttackPerformed -= PlayAttack;
            }

            if (animator == null)
            {
                return;
            }

            animator.SetBool(IsMovingParameter, false);
            animator.ResetTrigger(AttackParameter);
            animator.ResetTrigger(HitParameter);
        }

        public void PlayAttack()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(AttackParameter);
            animator.SetTrigger(AttackParameter);
        }

        private void PlayHit()
        {
            if (animator == null)
            {
                return;
            }

            animator.ResetTrigger(HitParameter);
            animator.SetTrigger(HitParameter);
        }

        private void UpdateMovementAnimation()
        {
            if (animator == null || enemyMovement == null)
            {
                return;
            }

            bool isFrozen = supportDebuffState != null && supportDebuffState.IsFrozen;
            bool isMoving = enemyMovement.enabled && !enemyMovement.IsInStopRange && !isFrozen;

            animator.SetBool(IsMovingParameter, isMoving);
        }

        private void UpdateHitAnimation()
        {
            if (enemyHealth == null)
            {
                return;
            }

            float currentHp = enemyHealth.CurrentHp;

            if (currentHp < previousHp && !enemyHealth.IsDead)
            {
                PlayHit();
            }

            previousHp = currentHp;
        }
    }
}