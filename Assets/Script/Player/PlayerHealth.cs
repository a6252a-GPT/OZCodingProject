using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Image[] lifeImages;
    [SerializeField] private int maxHp = 3;

    private int currentHp;
    private Vector3 spawnPosition;
    private Rigidbody2D rb;

    public bool IsDead => currentHp <= 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnPosition = transform.position;
    }

    void Start()
    {
        currentHp = maxHp;
        UpdateLifeUI();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead) return;

        currentHp -= damage;
        UpdateLifeUI();

        if (currentHp <= 0)
            Die();
    }

    private void Die()
    {
        gameObject.SetActive(false);
    }

    public void Respawn()
    {
        currentHp = maxHp;
        UpdateLifeUI();
        transform.position = spawnPosition;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        gameObject.SetActive(true);
    }

    private void UpdateLifeUI()
    {
        for (int i = 0; i < lifeImages.Length; i++)
            lifeImages[i].enabled = i < currentHp;
    }
}
