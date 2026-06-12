using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    [Header("총알 이동 속도")]
    [SerializeField] private float speed = 5.0f;

    [Header("Y축 도달 시 삭제 (일반 적 총알)")]
    [SerializeField] private bool useDestroyY = true;
    [SerializeField] private float destroyY = -6f;

    [Header("생존 시간 (보스 총알, 0이면 미사용)")]
    [SerializeField] private float lifeTime = 0f;

    [Header("총알 데미지")]
    [SerializeField] private int damage = 1;

    public int Damage => damage;

    private Vector2 direction = Vector2.down;

    public void SetDirection(Vector2 dir)
    {
        direction = dir.normalized;
    }

    void Start()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime);

        if (useDestroyY && transform.position.y <= destroyY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.TryGetComponent<PlayerHealth>(out PlayerHealth playerHealth))
        {
            playerHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
