namespace TeamProject01.Gameplay
{
    public static class MetaWormIds // 타이틀 지렁이 ID
    {
        public const string Basic = "worm_basic"; // 기본형
        public const string Defense = "worm_defense"; // 방어형
        public const string Armed = "worm_armed"; // 무장형
        public const string Charge = "worm_charge"; // 돌격형
    }

    public static class MetaMapIds // 타이틀 맵 ID
    {
        public const string Map1 = "map_01"; // 현재 선택 가능
        public const string Map2 = "map_02"; // 업데이트 예정
        public const string Map3 = "map_03"; // 업데이트 예정
    }

    public enum MetaUpgradeId // 업그레이드 종류
    {
        GoldBonus,
        DiamondBonus,
        TurnBonus,
        CollisionForce,
        BaseAttack,
        AttackSpeed,
        NexusMaxHp,
        NexusRegen
    }
}
