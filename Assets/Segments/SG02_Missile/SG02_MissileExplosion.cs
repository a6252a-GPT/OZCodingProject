using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG02_MissileExplosion : MonoBehaviour // 폭발 범위
    {
        public DamageData Damage; // 피해값
        [Min(0.1f)] public float ExplosionRadius = 3f; // 폭발 반경

        private float lifeTimer; // 남은 표시 시간

        public static void Spawn(Transform root, GameObject explosionPrefab, Vector3 position, float explosionRadius, float explosionLifetime, DamageData damage) // 생성
        {
            SG02_MissileExplosion area = Instantiate(explosionPrefab, position, Quaternion.identity, root).GetComponent<SG02_MissileExplosion>(); // 프리팹 생성
            area.transform.localScale = Vector3.one * (explosionRadius * 2f); // 범위 표시
            area.ExplosionRadius = explosionRadius; // 폭발 반경
            area.Damage = damage; // 중요!! 투사체 → 폭발 받기
            area.lifeTimer = explosionLifetime; // 표시 시간
            area.ApplyDamageInRange(); // 데이터를 보내는 곳!! 폭발 → 몬스터
        }

        private void Update() // 폭발 루프
        {
            lifeTimer -= Time.deltaTime; // 시간 감소
            if (lifeTimer <= 0f)
            {
                Destroy(gameObject); // 표시 제거
            }
        }

        private void ApplyDamageInRange() // 범위 피해
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, ExplosionRadius); // 범위 검색
            for (int i = 0; i < hits.Length; i++)
            {
                EnemyController monster = hits[i].GetComponentInParent<EnemyController>(); // 몬스터 확인
                if (monster == null)
                {
                    continue; // 대상 아님
                }

                monster.ApplyDamage(Damage.WithHitPosition(monster.transform.position)); // 중요!! 폭발 → 몬스터
            }
        }
    }
}
