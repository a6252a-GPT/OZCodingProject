using UnityEngine;

public class EnemyfirePoint : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private EnemyBullet bulletPrefab;
    [SerializeField] private float fireInterval = 1.5f;

    [Header("방사형 발사 (보스용)")]
    [SerializeField] private bool useSpread;
    [SerializeField] private int spreadCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    [Header("회전 발사 (보스용)")]
    [SerializeField] private bool useRotate;
    [SerializeField] private float maxRotateAngle = 30f;
    [SerializeField] private float rotateSpeed = 2f;

    private float fireTimer;

    void Update()
    {
        if (useRotate && firePoint != null)
        {
            float angle = Mathf.Sin(Time.time * rotateSpeed) * maxRotateAngle;
            firePoint.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            Fire();
            fireTimer = 0.0f;
        }
    }

    private void Fire()
    {
        Vector2 baseDirection = GetFireDirection();

        if (useSpread)
        {
            for (int i = 0; i < spreadCount; i++)
            {
                float offset = i - (spreadCount - 1) / 2f;
                float angle = offset * spreadAngle;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * baseDirection;
                SpawnBullet(direction);
            }
        }
        else
        {
            SpawnBullet(baseDirection);
        }
    }

    // firePoint 회전 방향 기준 (아래 = -up)
    private Vector2 GetFireDirection()
    {
        if (firePoint != null)
            return -firePoint.up;

        return Vector2.down;
    }

    private void SpawnBullet(Vector2 direction)
    {
        EnemyBullet bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.SetDirection(direction);
    }
}
