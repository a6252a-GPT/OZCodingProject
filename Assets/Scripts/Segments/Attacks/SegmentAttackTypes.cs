namespace TeamProject01.Gameplay
{
    public enum SegmentAttackMoveType // 공격 이동 방식
    {
        StraightProjectile = 0, // 직선 투사체
        PiercingProjectile = 1, // 관통 투사체
        ArcProjectile = 2, // 곡사 투사체
        HomingProjectile = 3, // 추적 투사체
        Laser = 4 // 레이저
    }

    public enum SegmentAttackImpactType // 명중 처리 방식
    {
        DirectDamage = 0, // 직접 피해
        PierceDamage = 1, // 관통 피해
        ExplosionArea = 2, // 폭발 범위 피해
        ContinuousDamage = 3 // 지속 피해
    }
}
