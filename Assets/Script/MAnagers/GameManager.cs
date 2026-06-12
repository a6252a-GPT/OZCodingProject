using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindAnyObjectByType<EnemySpawner>();

        if (playerHealth == null)
            playerHealth = FindAnyObjectByType<PlayerHealth>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (enemySpawner != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                enemySpawner.ChangePattern(PatternType.Pattern1);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                enemySpawner.ChangePattern(PatternType.Pattern2);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                enemySpawner.ChangePattern(PatternType.Pattern3);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                enemySpawner.ChangePattern(PatternType.BossPattern);
        }

        if (Keyboard.current.digit5Key.wasPressedThisFrame && playerHealth != null)
            playerHealth.Respawn();
    }
}
