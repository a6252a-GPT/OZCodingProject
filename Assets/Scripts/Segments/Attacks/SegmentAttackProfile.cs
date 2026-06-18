using UnityEngine;

namespace TeamProject01.Gameplay
{
    [CreateAssetMenu(menuName = "OZ/Segments/Attack Profile", fileName = "AP_SegmentAttack")]
    public sealed class SegmentAttackProfile : ScriptableObject // 세그먼트 공격 데이터
    {
        public string DisplayName; // 표시 이름
        [TextArea(2, 4)] public string Description; // 팀원 메모

        [Header("Pattern")]
        public SegmentAttackMoveType MoveType = SegmentAttackMoveType.StraightProjectile; // 이동 방식
        public SegmentAttackImpactType ImpactType = SegmentAttackImpactType.DirectDamage; // 명중 방식

        [Header("Target")]
        [Min(0.1f)] public float SearchRange = 24f; // 탐색 거리
        [Min(0f)] public float TargetAimHeight = 0.45f; // 조준 높이
        [Min(0f)] public float AttackSpawnHeight = 0.42f; // 포구 fallback 높이

        [Header("Attack")]
        [Min(0f)] public float BaseDamage = 1f; // 기본 피해량
        [Min(0.05f)] public float MinAttackInterval = 3f; // 최소 공격 간격
        [Min(0.05f)] public float MaxAttackInterval = 5f; // 최대 공격 간격

        [Header("Projectile")]
        public GameObject ProjectilePrefab; // 투사체 프리팹
        [Min(1)] public int ProjectileCount = 1; // 동시 발사 수
        [Min(0f)] public float SpreadAngle = 0f; // 산탄 각도
        public bool FireProjectilesSequentially; // 순차 발사 사용
        [Min(0f)] public float ProjectileFireDelay = 0.18f; // 순차 발사 지연
        public bool UseLoadedProjectileVisuals; // 장전 미사일 표시 사용
        [Range(0f, 1f)] public float LoadedProjectileReloadRatio = 0.5f; // 쿨타임 중 복구 시점
        [Min(0.1f)] public float ProjectileSpeed = 20f; // 투사체 속도
        [Min(0.05f)] public float ProjectileHitRadius = 0.5f; // 명중 반경
        [Min(0.1f)] public float ProjectileLifetime = 5f; // 생존 시간
        [Min(0)] public int PierceCount = 3; // 관통 가능 수
        [Min(0f)] public float ArcHeight = 3f; // 곡사 높이

        [Header("Explosion")]
        [Min(0.1f)] public float ExplosionRadius = 3f; // 폭발 반경
        [Min(0.05f)] public float ExplosionLifetime = 0.35f; // 폭발 표시 시간

        [Header("Laser")]
        [Min(0.05f)] public float LaserDuration = 0.5f; // 레이저 지속 시간
        [Min(0.02f)] public float LaserTickInterval = 0.15f; // 지속 피해 간격

        [Header("Aim")]
        public bool RequireAimBeforeFire = true; // 조준 후 발사
        [Min(1f)] public float HeadTurnSpeed = 540f; // 머리 회전 속도
        [Min(0f)] public float FireAngleTolerance = 8f; // 발사 허용 각도

        [Header("VFX Slots")]
        public GameObject MuzzleVfxPrefab; // 발사 VFX
        [Min(0f)] public float MuzzleVfxLifetime = 1.5f; // 발사 VFX 제거 시간
        public GameObject HitVfxPrefab; // 명중 VFX
        [Min(0f)] public float HitVfxLifetime = 2f; // 명중 VFX 제거 시간
        public GameObject ExplosionVfxPrefab; // 폭발 VFX
        [Min(0f)] public float ExplosionVfxLifetime = 2f; // 폭발 VFX 제거 시간
    }
}
