using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [RequireComponent(typeof(EnemyHealth))]
    public sealed class BossHpUIBinder : MonoBehaviour
    {
        private EnemyHealth health;
        private Coroutine bindCoroutine;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            bindCoroutine = StartCoroutine(BindRoutine());
        }

        private void OnDisable()
        {
            if (bindCoroutine != null)
            {
                StopCoroutine(bindCoroutine);
                bindCoroutine = null;
            }

            if (BossHpUI.Instance != null)
            {
                BossHpUI.Instance.Unbind();
            }
        }

        private IEnumerator BindRoutine()
        {
            while (BossHpUI.Instance == null)
            {
                yield return null;
            }

            BossHpUI.Instance.Bind(health);
            bindCoroutine = null;
        }
    }
}