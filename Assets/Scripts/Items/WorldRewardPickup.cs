using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class WorldRewardPickup : MonoBehaviour // 필드 경험치/골드 픽업
    {
        private static readonly List<WorldRewardPickup> ActivePickups = new List<WorldRewardPickup>(256);
        private static readonly Quaternion GoldModelUprightRotation = Quaternion.Euler(90f, 0f, 0f);
        private static readonly Color GoldOrbColor = new Color(1f, 0.78f, 0.12f, 1f);
        private static readonly Color GoldOrbEmissionColor = new Color(0.75f, 0.38f, 0.04f, 1f);
        private static readonly Color ExperienceOrbColor = new Color(0.18f, 1f, 0.36f, 1f);
        private static readonly Color ExperienceOrbEmissionColor = new Color(0.04f, 0.65f, 0.2f, 1f);

        public RewardPickupKind Kind = RewardPickupKind.Experience; // 보상 종류
        [Min(0)] public int Amount = 1; // 보상 수치
        public Transform ModelRoot; // 회전/둥둥 표시 루트
        public Transform IdleVfxRoot; // 대기 VFX 자리
        public Transform CollectVfxRoot; // 획득 VFX 자리
        public Transform MagnetTrailVfxRoot; // 자석 흡수 VFX 자리

        [Header("Motion")]
        [Min(0f)] public float HoverHeight = 0.62f;
        [Min(0f)] public float HoverAmplitude = 0.12f;
        [Min(0f)] public float HoverSpeed = 2.4f;
        [Min(0f)] public float RotationSpeed = 100f;
        [Min(0f)] public float GroundHeightOffset = 0.02f;
        [Min(0f)] public float DropPopHeight = 1.15f;
        [Min(0.05f)] public float DropPopDuration = 0.42f;

        [Header("Collect")]
        [Min(0.05f)] public float CollectDistance = 0.55f;

        private int enemyId;
        private bool collected;
        private bool attractedThisFrame;
        private bool isDropping;
        private float dropTimer;
        private float hoverPhase;
        private Vector3 velocity;
        private Vector3 dropStartPosition;
        private Vector3 dropLandingPosition;
        private MaterialPropertyBlock visualPropertyBlock;

        public static int ActiveCount => ActivePickups.Count; // 디버그용 활성 수

        private void OnEnable()
        {
            if (!ActivePickups.Contains(this))
            {
                ActivePickups.Add(this); // 흡수 검색 등록
            }

            hoverPhase = Random.Range(0f, Mathf.PI * 2f); // 같은 타이밍으로 흔들리지 않게 분산
            SetVfxRootActive(IdleVfxRoot, true);
            SetVfxRootActive(CollectVfxRoot, false);
            SetVfxRootActive(MagnetTrailVfxRoot, false);
            ApplyKindVisualPose();
        }

        private void OnDisable()
        {
            ActivePickups.Remove(this); // 비활성 픽업 제거
        }

        private void Update()
        {
            if (collected)
            {
                return;
            }

            UpdateVisualMotion();
            SetVfxRootActive(MagnetTrailVfxRoot, attractedThisFrame);
            attractedThisFrame = false;
        }

        public void Configure(RewardPickupKind kind, int amount, int sourceEnemyId, Vector3 position)
        {
            Configure(kind, amount, sourceEnemyId, position, position);
        }

        public void Configure(RewardPickupKind kind, int amount, int sourceEnemyId, Vector3 landingPosition, Vector3 spawnPosition)
        {
            Kind = kind;
            Amount = Mathf.Max(0, amount);
            enemyId = sourceEnemyId;
            collected = false;
            attractedThisFrame = false;
            velocity = Vector3.zero;
            hoverPhase = Random.Range(0f, Mathf.PI * 2f);
            BeginDropMotion(spawnPosition, landingPosition);
            SetVfxRootActive(IdleVfxRoot, true);
            SetVfxRootActive(CollectVfxRoot, false);
            SetVfxRootActive(MagnetTrailVfxRoot, false);
            ApplyKindVisualPose();
        }

        public static bool AttractInRange(Vector3 center, float radius, float pullStrength, float maxSpeed, float collectDistance, float deltaTime)
        {
            if (radius <= 0f || deltaTime <= 0f)
            {
                return false;
            }

            bool hasCandidate = false;
            float safeRadius = Mathf.Max(0.05f, radius);
            float safePullStrength = Mathf.Max(0f, pullStrength);
            float safeMaxSpeed = Mathf.Max(0.1f, maxSpeed);
            float safeCollectDistance = Mathf.Max(0.05f, collectDistance);

            for (int i = ActivePickups.Count - 1; i >= 0; i--)
            {
                WorldRewardPickup pickup = ActivePickups[i];
                if (pickup == null)
                {
                    ActivePickups.RemoveAt(i);
                    continue;
                }

                if (pickup.TryAttract(center, safeRadius, safePullStrength, safeMaxSpeed, safeCollectDistance, deltaTime))
                {
                    hasCandidate = true;
                }
            }

            return hasCandidate;
        }

        private bool TryAttract(Vector3 center, float radius, float pullStrength, float maxSpeed, float collectDistance, float deltaTime)
        {
            if (collected || Amount <= 0 || isDropping)
            {
                return false;
            }

            Vector3 target = center;
            target.y = transform.position.y; // 루트는 바닥면을 따라 이동
            Vector3 offset = target - transform.position;
            offset.y = 0f;

            float radiusSqr = radius * radius;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > radiusSqr)
            {
                return false;
            }

            attractedThisFrame = true;

            float finalCollectDistance = Mathf.Max(CollectDistance, collectDistance);
            if (distanceSqr <= finalCollectDistance * finalCollectDistance)
            {
                TryCollect();
                return true;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            if (distance <= 0.001f)
            {
                return true;
            }

            Vector3 direction = offset / distance;
            float rangeFactor = 1f - Mathf.Clamp01(distance / radius);
            float targetSpeed = Mathf.Min(maxSpeed, velocity.magnitude + pullStrength * (0.45f + rangeFactor) * deltaTime);
            velocity = Vector3.Lerp(velocity, direction * targetSpeed, 1f - Mathf.Exp(-12f * deltaTime));

            Vector3 nextPosition = transform.position + velocity * deltaTime;
            nextPosition.y = transform.position.y;
            transform.position = nextPosition;
            return true;
        }

        private void TryCollect()
        {
            if (collected)
            {
                return;
            }

            RewardData reward = Kind == RewardPickupKind.Experience
                ? RewardData.Create(Amount, 0, enemyId, transform.position)
                : RewardData.Create(0, Amount, enemyId, transform.position);

            if (!RewardGateway.SubmitReward(reward))
            {
                return;
            }

            DamageFloatingSpawner.SpawnRewardGain(Kind, Amount, ResolveRewardFloatingFallbackPosition());
            collected = true;
            SetVfxRootActive(IdleVfxRoot, false);
            SetVfxRootActive(CollectVfxRoot, true);
            Destroy(gameObject);
        }

        private void UpdateVisualMotion()
        {
            UpdateDropMotion();
            Transform visual = ModelRoot != null ? ModelRoot : transform;

            if (ModelRoot != null)
            {
                float bob = HoverHeight + Mathf.Sin(Time.time * HoverSpeed + hoverPhase) * HoverAmplitude;
                ModelRoot.localPosition = Vector3.up * Mathf.Max(0f, bob);
            }

            if (ShouldRotateVisual())
            {
                visual.Rotate(0f, RotationSpeed * Time.deltaTime, 0f, Space.Self);
            }
        }

        private bool ShouldRotateVisual()
        {
            return Kind != RewardPickupKind.Experience && RotationSpeed > 0f;
        }

        private void BeginDropMotion(Vector3 spawnPosition, Vector3 landingPosition)
        {
            dropStartPosition = GroundService.ProjectToGround(spawnPosition, GroundHeightOffset);
            dropLandingPosition = GroundService.ProjectToGround(landingPosition, GroundHeightOffset);
            dropTimer = 0f;
            isDropping = DropPopHeight > 0f && DropPopDuration > 0f;
            transform.position = dropStartPosition;
        }

        private void UpdateDropMotion()
        {
            if (!isDropping)
            {
                return;
            }

            dropTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(dropTimer / Mathf.Max(0.05f, DropPopDuration));
            float smoothProgress = progress * progress * (3f - 2f * progress);
            Vector3 position = Vector3.Lerp(dropStartPosition, dropLandingPosition, smoothProgress);
            position.y += Mathf.Sin(progress * Mathf.PI) * DropPopHeight;
            transform.position = position;

            if (progress >= 1f)
            {
                isDropping = false;
                velocity = Vector3.zero;
                transform.position = dropLandingPosition;
            }
        }

        private void ApplyKindVisualPose()
        {
            if (Kind == RewardPickupKind.Experience)
            {
                ApplyExperienceVisualColor();
                return;
            }

            if (Kind != RewardPickupKind.Gold || ModelRoot == null)
            {
                if (Kind == RewardPickupKind.Gold)
                {
                    ApplyGoldVisualColor();
                }

                return;
            }

            ApplyGoldVisualColor();
            Transform model = FindPrimaryModelTransform();
            if (model != null)
            {
                model.localRotation = GoldModelUprightRotation;
            }
        }

        private Transform FindPrimaryModelTransform()
        {
            MeshRenderer renderer = ModelRoot.GetComponentInChildren<MeshRenderer>(true);
            return renderer != null && renderer.transform != ModelRoot ? renderer.transform : ModelRoot;
        }

        private void ApplyExperienceVisualColor()
        {
            ApplyVisualColor(ExperienceOrbColor, ExperienceOrbEmissionColor);
        }

        private void ApplyGoldVisualColor()
        {
            ApplyVisualColor(GoldOrbColor, GoldOrbEmissionColor);
        }

        private void ApplyVisualColor(Color baseColor, Color emissionColor)
        {
            MeshRenderer[] renderers = ModelRoot != null
                ? ModelRoot.GetComponentsInChildren<MeshRenderer>(true)
                : GetComponentsInChildren<MeshRenderer>(true);

            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            visualPropertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(visualPropertyBlock);
                visualPropertyBlock.SetColor("_Color", baseColor);
                visualPropertyBlock.SetColor("_BaseColor", baseColor);
                visualPropertyBlock.SetColor("_EmissionColor", emissionColor);
                renderer.SetPropertyBlock(visualPropertyBlock);
            }
        }

        private Vector3 ResolveRewardFloatingFallbackPosition()
        {
            return transform.position + Vector3.up * 1.2f;
        }

        private static void SetVfxRootActive(Transform root, bool active)
        {
            if (root != null && root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }
    }
}
