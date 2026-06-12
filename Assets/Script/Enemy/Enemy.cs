using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private PatternType patternType;

    public PatternType Pattern => patternType;

    public void SetPattern(PatternType type) => patternType = type;

    [Header("애너미 이동 속도 설정")]
    [SerializeField] private float moveSpeed = 2.0f;
    [Header("애너미 최대 체력 설정")]
    [SerializeField] private float maxHealth = 3.0f;
    [Header("애너미 파괴 위치 설정")]
    [SerializeField] private float damageY = -6.0f;
    [Header("애너미 체력 UI 설정")]
    [SerializeField] private EnumyUI enemyhpUI;

    [Header("보스 설정")]
    [SerializeField] private float bossMoveSpeed = 1.0f;
    [SerializeField] private float bossStopY = 4f;
    [SerializeField] private float bossMaxHealth = 30f;
    [SerializeField] private EnemyfirePoint bossFirePoint;

    // Pattern2
    private static readonly Vector2[] Pattern2Waypoints =
    {
        new Vector2(2f, 3f),
        new Vector2(-2f, 0f),
        new Vector2(2f, -3f),
        new Vector2(-2f, -6f),
    };

    // Pattern3
    private static readonly Vector2[] Pattern3Waypoints =
    {
        new Vector2(3f, 4f),
        new Vector2(-3f, 4f),
        new Vector2(-3f, 2f),
        new Vector2(3f, 2f),
        new Vector2(3f, 0f),
        new Vector2(-3f, 0f),
        new Vector2(-3f, -2f),
        new Vector2(3f, -2f),
        new Vector2(3f, -4f),
        new Vector2(-3f, -4f),
        new Vector2(-3f, -6f),
    };

    private float currentHealth;
    private Rigidbody2D rb;
    private int waypointIndex;
    private bool isInvincible;
    private bool isBossArrived;
    private Slider bossHealthSlider;
    private const float WaypointReachDistance = 0.05f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (bossFirePoint == null)
            bossFirePoint = GetComponentInChildren<EnemyfirePoint>();
    }

    void Start()
    {
        if (patternType == PatternType.BossPattern)
        {
            maxHealth = bossMaxHealth;
            isInvincible = true;
            isBossArrived = false;

            if (bossFirePoint != null)
                bossFirePoint.enabled = false;
        }

        currentHealth = maxHealth;

        if (patternType != PatternType.BossPattern && enemyhpUI != null)
            enemyhpUI.Initialize((int)maxHealth);

        switch (patternType)
        {
            case PatternType.Pattern2:
            case PatternType.Pattern3:
                waypointIndex = 0;
                break;
        }
    }

    public void InitializeBoss(Slider healthSlider)
    {
        if (healthSlider == null)
            return;

        bossHealthSlider = healthSlider;
        bossHealthSlider.gameObject.SetActive(true);
        bossHealthSlider.minValue = 0;
        bossHealthSlider.maxValue = bossMaxHealth;
        bossHealthSlider.value = bossMaxHealth;
    }

    private void FixedUpdate()
    {
        switch (patternType)
        {
            case PatternType.Pattern1:
                MovePattern1();
                break;

            case PatternType.Pattern2:
                MovePattern2();
                break;

            case PatternType.Pattern3:
                MovePattern3();
                break;

            case PatternType.BossPattern:
                MoveBossPattern();
                break;
        }
    }

    // Pattern1: 수업때 진행한 방법
    private void MovePattern1()
    {
        rb.linearVelocity = Vector2.down * moveSpeed;
    }

    // Pattern2
    private void MovePattern2()
    {
        if (waypointIndex >= Pattern2Waypoints.Length)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 target = Pattern2Waypoints[waypointIndex];
        Vector2 current = rb.position;
        Vector2 direction = (target - current).normalized;

        rb.linearVelocity = direction * moveSpeed;

        if (Vector2.Distance(current, target) <= WaypointReachDistance)
        {
            rb.position = target;
            waypointIndex++;
        }
    }

    // Pattern3
    private void MovePattern3()
    {
        if (waypointIndex >= Pattern3Waypoints.Length)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 target = Pattern3Waypoints[waypointIndex];
        Vector2 current = rb.position;
        Vector2 direction = (target - current).normalized;

        rb.linearVelocity = direction * moveSpeed;

        if (Vector2.Distance(current, target) <= WaypointReachDistance)
        {
            rb.position = target;
            waypointIndex++;
        }
    }

    // BossPattern: (0,6) 스폰 → y4까지 하강 후 대기
    private void MoveBossPattern()
    {
        if (rb == null)
            return;

        if (isBossArrived)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (transform.position.y <= bossStopY)
        {
            rb.position = new Vector2(transform.position.x, bossStopY);
            rb.linearVelocity = Vector2.zero;
            isBossArrived = true;
            isInvincible = false;

            if (bossFirePoint != null)
                bossFirePoint.enabled = true;

            return;
        }

        rb.linearVelocity = Vector2.down * bossMoveSpeed;
    }

    void Update()
    {
        if (patternType != PatternType.BossPattern && transform.position.y <= damageY)
            Destroy(gameObject);
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible)
            return;

        currentHealth -= damage;

        if (patternType == PatternType.BossPattern && bossHealthSlider != null)
            bossHealthSlider.value = currentHealth;
        else if (enemyhpUI != null)
            enemyhpUI.SetHP((int)currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (patternType == PatternType.BossPattern && bossHealthSlider != null)
            bossHealthSlider.gameObject.SetActive(false);

        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Bullet>(out Bullet bullet))
        {
            TakeDamage((int)bullet.Damage);
            Destroy(bullet.gameObject);
        }
    }
}
