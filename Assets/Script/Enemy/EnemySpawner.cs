using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public enum PatternType
{
    Pattern1, // 수업에서 진행한거 위에서 x값 랜덤 위치에서 내려오는 패턴
    Pattern2, // 'z' 모양 지그재그 패턴
    Pattern3, // 'ㄹ'모양 지그재그 패턴
    BossPattern, // 보스 패턴
}

public class EnemySpawner : MonoBehaviour
{
    [Header("애너미 프리팹 설정")]
    [SerializeField] private Enemy enemyPrefab;

    [Header("보스 프리팹 설정")]
    [SerializeField] private Enemy bossPrefab;

    [Header("보스 체력 UI 설정")]
    [SerializeField] private Slider bossHealthUI;

    [Header("스폰 패턴 설정")]
    [SerializeField] private PatternType patternType;

    [Header("스폰 간격 설정")]
    [SerializeField] private float spawnInterval = 1.0f;

    [Header("스폰 위치 설정 (Pattern1)")]
    [SerializeField] private float spawnY = 5.5f;
    [SerializeField] private float spawnXMin = -2.5f;
    [SerializeField] private float spawnXMax = 2.5f;

    private bool bossSpawned;
    private PatternType lastPatternType;
    private Coroutine spawnCoroutine;

    void Start()
    {
        if (bossHealthUI != null)
            bossHealthUI.gameObject.SetActive(false);

        lastPatternType = patternType;
        spawnCoroutine = StartCoroutine(SpawnCo());
    }

    // Play 중 Inspector에서 패턴 변경 시 재시작
    void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (patternType == lastPatternType)
            return;

        RestartSpawn();
    }

    public void ChangePattern(PatternType type)
    {
        if (patternType == type)
            return;

        patternType = type;
        RestartSpawn();
    }

    private void RestartSpawn()
    {
        lastPatternType = patternType;

        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);

        ClearAllEnemies();

        Debug.Log($"[EnemySpawner] 패턴 변경 → {patternType}");
        spawnCoroutine = StartCoroutine(SpawnCo());
    }

    private void ClearAllEnemies()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
            Destroy(enemy.gameObject);

        bossSpawned = false;

        if (bossHealthUI != null)
            bossHealthUI.gameObject.SetActive(false);
    }

    IEnumerator SpawnCo()
    {
        if (patternType == PatternType.BossPattern)
        {
            SpawnBoss();
            yield break;
        }

        while (true)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(GetSpawnInterval(patternType));
        }
    }

    private void SpawnEnemy()
    {
        if (patternType == PatternType.BossPattern)
            return;

        Vector2 spawnPosition = GetSpawnPosition(patternType);
        Enemy enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        enemy.SetPattern(patternType);
    }

    private void SpawnBoss()
    {
        if (bossSpawned)
            return;

        if (bossPrefab == null)
        {
            Debug.LogError("[EnemySpawner] Boss Prefab이 비어 있습니다.");
            return;
        }

        bossSpawned = true;

        Vector2 spawnPosition = new Vector2(0f, 6f);
        Enemy boss = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        boss.SetPattern(PatternType.BossPattern);

        if (bossHealthUI != null)
            boss.InitializeBoss(bossHealthUI);
    }

    private Vector2 GetSpawnPosition(PatternType type)
    {
        switch (type)
        {
            case PatternType.Pattern1:
                return new Vector2(Random.Range(spawnXMin, spawnXMax), spawnY);

            case PatternType.Pattern2:
                return new Vector2(-2f, 6f);

            case PatternType.Pattern3:
                return new Vector2(3f, 6f);

            default:
                return new Vector2(Random.Range(spawnXMin, spawnXMax), spawnY);
        }
    }

    private float GetSpawnInterval(PatternType type)
    {
        switch (type)
        {
            case PatternType.Pattern2:
            case PatternType.Pattern3:
                return 0.5f;

            default:
                return spawnInterval;
        }
    }
}
