using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
}

public class Bullet : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float damage = 1f;
    public float Damage => damage;
    [SerializeField] private float lifeTime = 3f;

    private Rigidbody2D rb;
    private Vector2 direction = Vector2.up;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;

        var collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;
    }

    public void SetDirection(Vector2 direction)
    {
        this.direction = direction.normalized;
    }

    private void Start()
    {
        rb.linearVelocity = direction * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
            return;

        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
