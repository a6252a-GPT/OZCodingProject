using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SG03_RapidShotWeapon : SegmentWeaponBehaviour // 속사 세그먼트 무기
    {
        [Min(0f)] public float BaseDamage = 0.5f; // 한 발 기본 피해량. 속사형이라 SG01보다 낮게 시작한다.
        [Min(0.1f)] public float MinAttackInterval = 1f; // 가장 빠른 공격 간격
        [Min(0.1f)] public float MaxAttackInterval = 1.6f; // 가장 느린 공격 간격
        [Min(0.1f)] public float SearchRange = 20f; // 가까운 몬스터를 찾는 거리
        [Min(0f)] public float AttackSpawnHeight = 0.42f; // 탄이 세그먼트 중심보다 살짝 위에서 나오게 하는 높이
        public GameObject ProjectilePrefab; // 발사할 속사 탄 프리팹
        [Min(0.1f)] public float ProjectileSpeed = 26f; // 속사 느낌을 주기 위해 SG01보다 빠른 탄속
        [Min(0.05f)] public float ProjectileHitRadius = 0.45f; // 몬스터에 닿았다고 볼 반경
        [Min(0.1f)] public float ProjectileLifetime = 4f; // 탄이 너무 오래 남지 않도록 짧게 유지

        private float fireTimer; // 다음 발사까지 남은 시간

        public override void TickWeapon(float deltaTime) // ConvoySegmentRuntime이 붙어 있는 세그먼트만 호출한다.
        {
            if (!CanUseWeapon())
            {
                return; // 분리 꼬리 상태거나 컨보이에 연결되지 않았으면 공격하지 않는다.
            }

            fireTimer -= deltaTime; // 매 프레임 쿨타임을 줄인다.
            if (fireTimer > 0f)
            {
                return; // 아직 다음 발사 시간이 아니다.
            }

            if (!TryFindTarget(out EnemyController target))
            {
                return; // 사거리 안에 몬스터가 없으면 쿨타임을 소비하지 않고 대기한다.
            }

            CoreStatData coreStats = CoreStatProvider.GetCurrentOrDefault(); // 코어 -> 세그먼트: 현재 공격력/공격속도 배율을 받는다.
            Fire(target, coreStats); // 세그먼트 -> 투사체: DamageData를 만들어 탄에 넘긴다.
            ResetCooldown(coreStats); // 코어 공격속도 배율을 반영해 다음 발사 시간을 정한다.
        }

        private bool CanUseWeapon() // 세그먼트 무기가 작동 가능한지 확인한다.
        {
            return IsWeaponActive && Segment != null && Segment.Owner != null && ProjectilePrefab != null;
        }

        private bool TryFindTarget(out EnemyController target) // 몬스터 담당 API를 통해 가장 가까운 적을 찾는다.
        {
            return EnemyController.TryFindNearest(transform.position, SearchRange, out target);
        }

        private void Fire(EnemyController target, CoreStatData coreStats) // 실제 발사 처리
        {
            float damage = CalculateDamage(coreStats); // CoreStatData의 공격력 배율을 적용한 최종 피해량
            Vector3 spawnPosition = transform.position + Vector3.up * AttackSpawnHeight; // 탄 생성 위치

            DamageData damageData = DamageData.Create(
                damage,
                DamageType.Projectile,
                Segment.ChainIndex,
                spawnPosition,
                gameObject
            ); // 세그먼트는 몬스터를 직접 죽이지 않고 DamageData만 다음 단계로 넘긴다.

            SG03_RapidShotProjectile.Spawn(
                Segment.Owner.GetProjectileRoot(),
                ProjectilePrefab,
                spawnPosition,
                target,
                ProjectileSpeed,
                ProjectileHitRadius,
                ProjectileLifetime,
                damageData
            ); // 무기 -> 투사체: 탄이 DamageData를 들고 몬스터까지 이동한다.
        }

        private float CalculateDamage(CoreStatData coreStats) // 기본 피해량에 코어 공격력 배율을 적용한다.
        {
            return coreStats.ApplyDamage(BaseDamage);
        }

        private void ResetCooldown(CoreStatData coreStats) // 다음 발사까지 기다릴 시간을 정한다.
        {
            float min = Mathf.Min(MinAttackInterval, MaxAttackInterval);
            float max = Mathf.Max(MinAttackInterval, MaxAttackInterval);
            float baseInterval = Random.Range(min, max); // 세그먼트마다 약간 다른 리듬으로 쏘게 한다.
            fireTimer = coreStats.ApplyFireInterval(baseInterval); // 코어 공격속도 배율을 적용한다.
        }
    }
}
