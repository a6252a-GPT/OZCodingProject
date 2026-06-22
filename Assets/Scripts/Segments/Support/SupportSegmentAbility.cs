using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SupportSegmentAbility : SegmentWeaponBehaviour
    {
        private const float MinimumPickupMagnetPullStrength = 225f;
        private const float MinimumPickupMagnetMaxPullSpeed = 110f;

        public SegmentSupportAbilityProfile Profile;
        public Transform ActiveVfxRoot;
        public Transform RangeVfxRoot;
        public Transform MuzzleVfxRoot;
        public Transform TargetBodyVfxSocket;

        [Header("Temporary VFX")]
        public bool UseTemporarySupportVfx = true;

        [Header("Active Head Spin")]
        public Transform ActiveHeadRotationRoot;
        [Min(0f)] public float ActiveHeadSpinSpeed = 180f;

        [Header("Reward Pickup Magnet")]
        [Min(0f)] public float PickupMagnetPullStrength = MinimumPickupMagnetPullStrength;
        [Min(0.1f)] public float PickupMagnetMaxPullSpeed = MinimumPickupMagnetMaxPullSpeed;
        [Min(0.05f)] public float PickupMagnetCollectDistance = 0.65f;

        [Header("Holy Water Spray")]
        [Range(1f, 180f)] public float HolyWaterSprayAngle = 55f;
        [Min(1)] public int HolyWaterProjectileCount = 14;
        [Min(0.02f)] public float HolyWaterProjectileInterval = 0.08f;
        [Min(0.1f)] public float HolyWaterProjectileSpeed = 9f;
        [Min(0.05f)] public float HolyWaterProjectileLifetime = 1.05f;
        [Min(0.05f)] public float HolyWaterProjectileStartRadius = 0.32f;
        [Min(0.05f)] public float HolyWaterProjectileEndRadius = 1.75f;
        [Min(0.02f)] public float HolyWaterProjectileTickInterval = 0.25f;
        [Range(0f, 1f)] public float HolyWaterMuzzleInfluenceStrength = 0.3f;
        public Color HolyWaterProjectileColor = new Color(0.86f, 0.96f, 1f, 0.34f);

        private float cooldownTimer;
        private float activeTimer;
        private float holyWaterProjectileTimer;
        private int holyWaterProjectilesRemaining;
        private float activeHeadSpinAngle;
        private Quaternion activeHeadBaseLocalRotation;
        private bool hasActiveHeadBaseRotation;
        private bool isActive;
        private readonly List<EnemyController> activeEnemyBuffer = new List<EnemyController>(32);

        public bool IsAbilityActive => isActive;

        public override void Configure(ConvoySegmentRuntime segment)
        {
            bool segmentChanged = Segment != segment;
            base.Configure(segment);
            CacheActiveHeadRotationRoot();
            if (segmentChanged)
            {
                ResetCycle();
            }
        }

        public override void SetWeaponActive(bool active)
        {
            base.SetWeaponActive(active);
            if (!active)
            {
                isActive = false;
                activeTimer = 0f;
                holyWaterProjectileTimer = 0f;
                holyWaterProjectilesRemaining = 0;
                activeHeadSpinAngle = 0f;
                RestoreActiveHeadRotation();
                SupportSegmentRuntimeBuffs.ClearSource(this);
                SetVfxRootsActive(false);
            }
        }

        public override void TickWeapon(float deltaTime)
        {
            if (!IsWeaponActive || Profile == null)
            {
                return;
            }

            if (isActive)
            {
                TickActiveSupportEffect(deltaTime);
                TickActiveHeadSpin(deltaTime);
                activeTimer -= deltaTime;
                if (activeTimer <= 0f)
                {
                    EndActivation();
                }

                return;
            }

            cooldownTimer -= deltaTime;
            if (cooldownTimer <= 0f)
            {
                BeginActivation();
            }
        }

        private void TickActiveSupportEffect(float deltaTime)
        {
            if (Profile == null)
            {
                return;
            }

            switch (Profile.AbilityKind)
            {
                case SegmentSupportAbilityKind.FinalDamageBuff:
                case SegmentSupportAbilityKind.FinalAttackSpeedBuff:
                    SupportSegmentRuntimeBuffs.RefreshAllyBuff(this, Profile);
                    break;
                case SegmentSupportAbilityKind.PickupMagnet:
                    ApplyPickupMagnet(deltaTime);
                    break;
                case SegmentSupportAbilityKind.FreezeArea:
                    ApplyFreezeArea();
                    break;
                case SegmentSupportAbilityKind.HolyWaterVulnerabilitySpray:
                    ApplyHolyWaterSpray(deltaTime);
                    break;
            }

            RefreshTemporarySupportVfx();
        }

        private void ResetCycle()
        {
            isActive = false;
            activeTimer = 0f;
            holyWaterProjectileTimer = 0f;
            holyWaterProjectilesRemaining = 0;
            activeHeadSpinAngle = 0f;
            RestoreActiveHeadRotation();
            SupportSegmentRuntimeBuffs.ClearSource(this);
            cooldownTimer = Profile != null && Profile.StartsReady ? 0f : GetCooldown();
            SetVfxRootsActive(false);
        }

        private void BeginActivation()
        {
            isActive = true;
            activeTimer = GetActiveDurationSeconds();
            holyWaterProjectileTimer = 0f;
            holyWaterProjectilesRemaining = GetHolyWaterProjectileCount();
            activeHeadSpinAngle = 0f;
            CacheActiveHeadRotationRoot();
            SetVfxRootsActive(true);
            TickActiveSupportEffect(0f);
        }

        private void EndActivation()
        {
            isActive = false;
            activeTimer = 0f;
            holyWaterProjectileTimer = 0f;
            holyWaterProjectilesRemaining = 0;
            activeHeadSpinAngle = 0f;
            RestoreActiveHeadRotation();
            SupportSegmentRuntimeBuffs.ClearSource(this);
            cooldownTimer = GetCooldown();
            SetVfxRootsActive(false);
        }

        private void OnDisable()
        {
            SupportSegmentRuntimeBuffs.ClearSource(this);
        }

        private float GetCooldown()
        {
            return Profile != null ? GetRandomizedCooldown(Profile.Cooldown) : 0f;
        }

        private float GetActiveDurationSeconds()
        {
            if (Profile == null)
            {
                return 0.05f;
            }

            float duration = Mathf.Max(0.05f, Profile.ActiveDurationSeconds);
            if (Profile.AbilityKind == SegmentSupportAbilityKind.HolyWaterVulnerabilitySpray)
            {
                float sequenceDuration = Mathf.Max(0.05f, (GetHolyWaterProjectileCount() - 1) * Mathf.Max(0.02f, HolyWaterProjectileInterval) + 0.01f);
                duration = Mathf.Max(duration, sequenceDuration);
            }

            return duration;
        }

        private float GetEffectivePickupMagnetPullStrength()
        {
            return Mathf.Max(PickupMagnetPullStrength, MinimumPickupMagnetPullStrength);
        }

        private float GetEffectivePickupMagnetMaxPullSpeed()
        {
            return Mathf.Max(PickupMagnetMaxPullSpeed, MinimumPickupMagnetMaxPullSpeed);
        }

        private void ApplyPickupMagnet(float deltaTime)
        {
            WorldRewardPickup.AttractInRange(
                transform.position,
                Profile.Range,
                GetEffectivePickupMagnetPullStrength(),
                GetEffectivePickupMagnetMaxPullSpeed(),
                PickupMagnetCollectDistance,
                deltaTime);
        }

        private void ApplyFreezeArea()
        {
            float range = Mathf.Max(0.1f, Profile.Range);
            EnemyController.CollectActiveInRange(transform.position, range, activeEnemyBuffer);

            float duration = GetEffectDurationSeconds();
            for (int i = 0; i < activeEnemyBuffer.Count; i++)
            {
                EnemySupportDebuffState state = EnemySupportDebuffState.GetOrAdd(activeEnemyBuffer[i]);
                if (state != null)
                {
                    state.ApplyFreeze(duration);
                }
            }
        }

        private void ApplyHolyWaterSpray(float deltaTime)
        {
            if (holyWaterProjectilesRemaining <= 0)
            {
                return;
            }

            holyWaterProjectileTimer -= deltaTime;
            if (holyWaterProjectileTimer > 0f)
            {
                return;
            }

            Transform sprayRoot = MuzzleVfxRoot != null ? MuzzleVfxRoot : transform;
            Vector3 origin = sprayRoot.position;
            Vector3 forward = sprayRoot.forward;
            forward.y = 0f;

            if (forward.sqrMagnitude <= 0.0001f)
            {
                forward = transform.forward;
                forward.y = 0f;
            }

            if (forward.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            forward.Normalize();

            Transform projectileRoot = Segment != null && Segment.Owner != null ? Segment.Owner.GetProjectileRoot() : null;
            SupportHolyWaterProjectileRuntime.Spawn(
                projectileRoot,
                origin,
                forward,
                sprayRoot,
                HolyWaterProjectileSpeed,
                HolyWaterProjectileLifetime,
                HolyWaterProjectileStartRadius,
                HolyWaterProjectileEndRadius,
                HolyWaterProjectileTickInterval,
                HolyWaterMuzzleInfluenceStrength,
                Mathf.Max(1f, Profile.IncomingDamageMultiplier),
                GetEffectDurationSeconds(),
                HolyWaterProjectileColor);

            holyWaterProjectilesRemaining--;
            holyWaterProjectileTimer = Mathf.Max(0.02f, HolyWaterProjectileInterval);
        }

        private int GetHolyWaterProjectileCount()
        {
            return Mathf.Max(1, HolyWaterProjectileCount);
        }

        private void RefreshTemporarySupportVfx()
        {
            if (!UseTemporarySupportVfx || Profile == null || Segment == null)
            {
                return;
            }

            Transform sourceVfxRoot = ActiveVfxRoot != null ? ActiveVfxRoot : transform;
            SupportTemporaryVfx.ShowSource(sourceVfxRoot, Profile.AbilityKind);

            if (ShouldShowTemporaryRangeVfx())
            {
                Transform rangeRoot = RangeVfxRoot != null ? RangeVfxRoot : transform;
                SupportTemporaryVfx.ShowRange(rangeRoot, Profile.AbilityKind, Profile.Range);
            }

            if (IsAllyBuffProfile())
            {
                RefreshTemporaryAllyBuffVfx();
            }
        }

        private void RefreshTemporaryAllyBuffVfx()
        {
            if (Segment == null || Segment.Owner == null || Segment.Owner.SegmentRoot == null)
            {
                return;
            }

            Transform root = Segment.Owner.SegmentRoot;
            for (int i = 0; i < root.childCount; i++)
            {
                ConvoySegmentRuntime runtime = root.GetChild(i).GetComponent<ConvoySegmentRuntime>();
                if (runtime == null || runtime == Segment || !runtime.IsAttached)
                {
                    continue;
                }

                if (!IsSegmentInsideAllyBuffRange(runtime.ChainIndex))
                {
                    continue;
                }

                Transform targetRoot = FindChildRecursive(runtime.transform, "VFX_BuffBodyRoot");
                SupportTemporaryVfx.ShowBuffTarget(targetRoot != null ? targetRoot : runtime.transform, Profile.AbilityKind);
            }
        }

        private bool IsAllyBuffProfile()
        {
            return Profile != null
                && (Profile.AbilityKind == SegmentSupportAbilityKind.FinalDamageBuff
                    || Profile.AbilityKind == SegmentSupportAbilityKind.FinalAttackSpeedBuff);
        }

        private bool ShouldShowTemporaryRangeVfx()
        {
            return Profile != null
                && (Profile.AbilityKind == SegmentSupportAbilityKind.PickupMagnet
                    || Profile.AbilityKind == SegmentSupportAbilityKind.FreezeArea);
        }

        private bool IsSegmentInsideAllyBuffRange(int chainIndex)
        {
            if (Segment == null || Profile == null)
            {
                return false;
            }

            int offset = chainIndex - Segment.ChainIndex;
            if (offset == 0)
            {
                return false;
            }

            if (offset > 0)
            {
                return offset <= Mathf.Max(0, Profile.FrontSegmentCount);
            }

            return -offset <= Mathf.Max(0, Profile.BackSegmentCount);
        }

        private float GetEffectDurationSeconds()
        {
            return Profile != null ? Mathf.Max(0.05f, Profile.EffectDurationSeconds) : 0.05f;
        }

        private void TickActiveHeadSpin(float deltaTime)
        {
            if (!ShouldSpinHeadDuringActive())
            {
                return;
            }

            CacheActiveHeadRotationRoot();
            if (ActiveHeadRotationRoot == null)
            {
                return;
            }

            activeHeadSpinAngle = Mathf.Repeat(activeHeadSpinAngle + ActiveHeadSpinSpeed * deltaTime, 360f);
            ActiveHeadRotationRoot.localRotation = activeHeadBaseLocalRotation * Quaternion.Euler(0f, activeHeadSpinAngle, 0f);
        }

        private bool ShouldSpinHeadDuringActive()
        {
            if (Profile == null || Profile.AbilityKind == SegmentSupportAbilityKind.None)
            {
                return false;
            }

            return Profile.AbilityKind != SegmentSupportAbilityKind.HolyWaterVulnerabilitySpray && ActiveHeadSpinSpeed > 0f;
        }

        private void CacheActiveHeadRotationRoot()
        {
            if (ActiveHeadRotationRoot == null)
            {
                ActiveHeadRotationRoot = ResolveHeadRotationRoot();
            }

            if (ActiveHeadRotationRoot != null && !hasActiveHeadBaseRotation)
            {
                activeHeadBaseLocalRotation = ActiveHeadRotationRoot.localRotation;
                hasActiveHeadBaseRotation = true;
            }
        }

        private void RestoreActiveHeadRotation()
        {
            if (ActiveHeadRotationRoot != null && hasActiveHeadBaseRotation)
            {
                ActiveHeadRotationRoot.localRotation = activeHeadBaseLocalRotation;
            }
        }

        private Transform ResolveHeadRotationRoot()
        {
            Transform directHead = transform.Find("Head");
            return directHead != null ? directHead : FindChildRecursive(transform, "Head");
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child.name == childName)
                {
                    return child;
                }

                Transform nested = FindChildRecursive(child, childName);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private void SetVfxRootsActive(bool active)
        {
            if (ActiveVfxRoot != null)
            {
                ActiveVfxRoot.gameObject.SetActive(active);
            }

            if (RangeVfxRoot != null)
            {
                RangeVfxRoot.gameObject.SetActive(active);
            }

            if (MuzzleVfxRoot != null)
            {
                MuzzleVfxRoot.gameObject.SetActive(active);
            }
        }
    }
}
