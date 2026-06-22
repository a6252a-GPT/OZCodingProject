using UnityEngine;

namespace TeamProject01.Gameplay
{
    public enum SegmentSupportAbilityKind
    {
        None = 0,
        FinalDamageBuff = 1,
        PickupMagnet = 2,
        FreezeArea = 3,
        FinalAttackSpeedBuff = 4,
        HolyWaterVulnerabilitySpray = 5
    }

    [CreateAssetMenu(menuName = "OZ/Segments/Support Ability Profile", fileName = "SP_SG##_Support")]
    public sealed class SegmentSupportAbilityProfile : ScriptableObject
    {
        public SegmentSupportAbilityKind AbilityKind;

        [Header("Timing")]
        public bool StartsReady = true;
        [Min(0f)] public float Cooldown = 5f;
        [Min(0f)] public float ActiveDurationSeconds = 5f;
        [Min(0f)] public float EffectDurationSeconds = 5f;

        [Header("Targeting")]
        [Min(0f)] public float Range = 6f;
        [Min(0)] public int FrontSegmentCount;
        [Min(0)] public int BackSegmentCount;

        [Header("Multipliers")]
        [Min(0f)] public float FinalDamageMultiplier = 1f;
        [Min(0f)] public float FinalAttackSpeedMultiplier = 1f;
        [Min(0f)] public float IncomingDamageMultiplier = 1f;

        [Header("VFX")]
        public GameObject ActiveVfxPrefab;
        public GameObject RangeVfxPrefab;
        public GameObject TargetBodyVfxPrefab;
        public GameObject EnemyDebuffVfxPrefab;

        [TextArea(2, 5)] public string Memo;
    }
}
