using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyDeathAnimatorBridge : MonoBehaviour
    {
        private static readonly int DeathParameter = Animator.StringToHash("Death");

        [Header("Animator")]
        [SerializeField] private Animator animator;

        [Header("Death")]
        [Min(0.1f)]
        [SerializeField] private float deathDuration = 2.0f;

        [SerializeField] private GameObject hpBarRoot;

        private bool deathStarted;

        public bool IsDeathPlaying
        {
            get
            {
                return deathStarted;
            }
        }

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        public bool TryBeginDeath()
        {
            if (deathStarted)
            {
                return true;
            }

            if (animator == null)
            {
                return false;
            }

            deathStarted = true;

            EnemyMovement enemyMovement = GetComponent<EnemyMovement>();

            if (enemyMovement != null)
            {
                enemyMovement.enabled = false;
            }

            EnemyMeleeAttack enemyMeleeAttack = GetComponent<EnemyMeleeAttack>();

            if (enemyMeleeAttack != null)
            {
                enemyMeleeAttack.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            Rigidbody enemyRigidbody = GetComponent<Rigidbody>();

            if (enemyRigidbody != null)
            {
                if (!enemyRigidbody.isKinematic) // 조성원수정-0625 - Kinematic Rigidbody에는 Velocity를 설정하지 않는다.
                {
                    enemyRigidbody.linearVelocity = Vector3.zero;
                    enemyRigidbody.angularVelocity = Vector3.zero;
                }

                enemyRigidbody.useGravity = false;
                enemyRigidbody.isKinematic = true;
            }

            if (hpBarRoot != null)
            {
                hpBarRoot.SetActive(false);
            }

            animator.SetTrigger(DeathParameter);

            Destroy(gameObject, deathDuration);

            return true;
        }
    }
}