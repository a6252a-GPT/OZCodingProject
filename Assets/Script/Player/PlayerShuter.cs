using UnityEngine;

public class PlayerShuter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireDelay = 0.2f;

    private float fireTimer = 0.0f;

    
    // Update is called once per frame
    void Update()
    {
        //경과시간 누적
        fireTimer += Time.deltaTime;

        if(InputManager.IsFire)
        {
            Fire();
        }

        
    }
    private void Fire()
    {
        if(fireTimer < fireDelay) return;

        fireTimer = 0.0f;

        switch (InputManager.CurrentAttackType)
        {
            case AttackType.Single:
                SpawnBullet(Vector2.up);
                break;

            case AttackType.Spread:
                for (int i = -2; i <= 2; i++)
                {
                    float angle = i * 20f;
                    Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.up;
                    SpawnBullet(direction);
                }
                break;
        }
    }

    private void SpawnBullet(Vector2 direction)
    {
        var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        bullet.GetComponent<Bullet>().SetDirection(direction);
    }
}
