using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG02_MissileWeapon : SegmentWeaponBehaviour // 폭발미사일 세그먼트 무기
    {
        [Min(0f)] public float BaseDamage = 1f; // 기본 피해량
        [Min(0.1f)] public float MinAttackInterval = 4f; // 최소 공격 간격
        [Min(0.1f)] public float MaxAttackInterval = 6f; // 최대 공격 간격
        [Min(0.1f)] public float SearchRange = 26f; // 적 탐색 거리
        [Min(0f)] public float AttackSpawnHeight = 0.48f; // 미사일 생성 높이
        public GameObject ProjectilePrefab; // 미사일 프리팹
        [Min(0.1f)] public float ProjectileSpeed = 14f; // 미사일 속도
        [Min(0.05f)] public float ProjectileHitRadius = 0.7f; // 명중 반경
        [Min(0.1f)] public float ProjectileLifetime = 6f; // 미사일 생존시간
        public GameObject ExplosionPrefab; // 폭발 프리팹
        [Min(0.1f)] public float ExplosionRadius = 3f; // 폭발 반경
        [Min(0.05f)] public float ExplosionLifetime = 0.35f; // 폭발 표시 시간

        private float fireTimer; // 남은 쿨타임

        public override void TickWeapon(float deltaTime) // 무기 갱신
        {
            if (!CanUseWeapon())
            {
                return; // 발사 불가
            }

            fireTimer -= deltaTime; // 쿨타임 감소
            if (fireTimer > 0f)
            {
                return; // 대기
            }

            if (!TryFindTarget(out EnemyController target)) // 중요!! 몬스터 탐색
            {
                return; // 대상 없음
            }

            CoreStatData coreStats = CoreStatProvider.GetCurrentOrDefault(); // 데이터를 받는 곳!! 코어 → 세그먼트
            Fire(target, coreStats); // 중요!! 실제 발사
            ResetCooldown(coreStats); // 공격 후 쿨타임
        }

        private bool CanUseWeapon() // 작동 가능 확인
        {
            return IsWeaponActive && Segment != null && Segment.Owner != null; // 연결 상태 확인
        }

        private bool TryFindTarget(out EnemyController target) // 대상 탐색
        {
            return EnemyController.TryFindNearest(transform.position, SearchRange, out target); // 중요!! 태그 기반 몬스터 탐색
        }

        private void Fire(EnemyController target, CoreStatData coreStats) // 발사 처리
        {
            float damage = CalculateDamage(coreStats); // 중요!! 피해 계산
            Vector3 spawnPosition = transform.position + Vector3.up * AttackSpawnHeight; // 생성 위치
            DamageData damageData = DamageData.Create(damage, DamageType.Explosion, Segment.ChainIndex, spawnPosition, gameObject); // 중요!! 피해값 생성
            SG02_MissileProjectile.Spawn(Segment.Owner.GetProjectileRoot(), ProjectilePrefab, ExplosionPrefab, spawnPosition, target, ProjectileSpeed, ProjectileHitRadius, ProjectileLifetime, ExplosionRadius, ExplosionLifetime, damageData); // 중요!! 무기 → 투사체
        }

        private float CalculateDamage(CoreStatData coreStats) // 피해 계산
        {
            return coreStats.ApplyDamage(BaseDamage); // 중요!! 기본피해 + 공격력능력치
        }

        private void ResetCooldown(CoreStatData coreStats) // 쿨타임 재설정
        {
            float min = Mathf.Min(MinAttackInterval, MaxAttackInterval); // 최소값
            float max = Mathf.Max(MinAttackInterval, MaxAttackInterval); // 최대값
            float baseInterval = Random.Range(min, max); // 랜덤 간격
            fireTimer = coreStats.ApplyFireInterval(baseInterval); // 공격 후 쿨타임
        }
    }
}
