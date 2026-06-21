using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SupportTemporaryVfx : MonoBehaviour
    {
        private const int RingSegments = 72;
        private const float DefaultVisibleDuration = 0.16f;
        private static readonly Dictionary<int, Material> MaterialsByColor = new Dictionary<int, Material>(16);

        private LineRenderer lineRenderer;
        private Color color;
        private float baseRadius = 0.7f;
        private float yOffset = 0.05f;
        private float visibleUntil;
        private float pulseAmount = 0.04f;
        private float pulseSpeed = 5.5f;
        private float spinSpeed = 18f;
        private float localSpinAngle;

        public static void ShowSource(Transform parent, SegmentSupportAbilityKind kind)
        {
            ShowRing(parent, $"Runtime_TempSupportVfx_Source_{kind}", kind, 0.58f, 0.2f, DefaultVisibleDuration, 0.9f, 0.035f, 36f);
        }

        public static void ShowRange(Transform parent, SegmentSupportAbilityKind kind, float range)
        {
            float radius = Mathf.Clamp(range, 0.4f, 24f);
            ShowRing(parent, $"Runtime_TempSupportVfx_Range_{kind}", kind, radius, 0.06f, DefaultVisibleDuration, 0.26f, 0.08f, 5f);
        }

        public static void ShowBuffTarget(Transform parent, SegmentSupportAbilityKind kind)
        {
            ShowRing(parent, $"Runtime_TempSupportVfx_Target_{kind}", kind, 0.83f, 0.14f, DefaultVisibleDuration, 0.72f, 0.055f, 24f);
        }

        private static void ShowRing(Transform parent, string childName, SegmentSupportAbilityKind kind, float radius, float yOffset, float duration, float alphaScale, float pulseAmount, float spinSpeed)
        {
            if (parent == null)
            {
                return;
            }

            Transform child = parent.Find(childName);
            if (child == null)
            {
                GameObject instance = new GameObject(childName);
                child = instance.transform;
                child.SetParent(parent, false);
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
            }

            SupportTemporaryVfx vfx = child.GetComponent<SupportTemporaryVfx>();
            if (vfx == null)
            {
                vfx = child.gameObject.AddComponent<SupportTemporaryVfx>();
            }

            Color color = GetColor(kind);
            color.a = Mathf.Clamp01(color.a * alphaScale);
            vfx.Refresh(radius, yOffset, color, duration, pulseAmount, spinSpeed);
        }

        private void Refresh(float radius, float yOffset, Color color, float duration, float pulseAmount, float spinSpeed)
        {
            EnsureRenderer();
            baseRadius = Mathf.Max(0.05f, radius);
            this.yOffset = Mathf.Max(0f, yOffset);
            this.color = color;
            this.pulseAmount = Mathf.Max(0f, pulseAmount);
            this.spinSpeed = spinSpeed;
            visibleUntil = Time.time + Mathf.Max(0.02f, duration);
            lineRenderer.enabled = true;
            lineRenderer.sharedMaterial = GetMaterial(color);
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            DrawRing(baseRadius);
        }

        private void Update()
        {
            if (lineRenderer == null)
            {
                return;
            }

            if (Time.time > visibleUntil)
            {
                lineRenderer.enabled = false;
                return;
            }

            localSpinAngle = Mathf.Repeat(localSpinAngle + spinSpeed * Time.deltaTime, 360f);
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            DrawRing(baseRadius + pulse);
        }

        private void EnsureRenderer()
        {
            if (lineRenderer != null)
            {
                return;
            }

            lineRenderer = gameObject.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = RingSegments;
            lineRenderer.widthMultiplier = 0.035f;
            lineRenderer.numCapVertices = 2;
            lineRenderer.numCornerVertices = 2;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }

        private void DrawRing(float radius)
        {
            EnsureRenderer();
            float safeRadius = Mathf.Max(0.05f, radius);
            Quaternion spin = Quaternion.Euler(0f, localSpinAngle, 0f);
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = (Mathf.PI * 2f * i) / RingSegments;
                Vector3 point = new Vector3(Mathf.Cos(angle) * safeRadius, yOffset, Mathf.Sin(angle) * safeRadius);
                lineRenderer.SetPosition(i, spin * point);
            }
        }

        private static Color GetColor(SegmentSupportAbilityKind kind)
        {
            switch (kind)
            {
                case SegmentSupportAbilityKind.FinalDamageBuff:
                    return new Color(1f, 0.72f, 0.22f, 0.42f);
                case SegmentSupportAbilityKind.PickupMagnet:
                    return new Color(0.5f, 0.9f, 1f, 0.36f);
                case SegmentSupportAbilityKind.FreezeArea:
                    return new Color(0.62f, 0.96f, 1f, 0.38f);
                case SegmentSupportAbilityKind.FinalAttackSpeedBuff:
                    return new Color(0.55f, 1f, 0.62f, 0.4f);
                case SegmentSupportAbilityKind.HolyWaterVulnerabilitySpray:
                    return new Color(0.88f, 0.96f, 1f, 0.34f);
                default:
                    return new Color(1f, 1f, 1f, 0.3f);
            }
        }

        private static Material GetMaterial(Color color)
        {
            int key = ColorUtility.ToHtmlStringRGBA(color).GetHashCode();
            if (MaterialsByColor.TryGetValue(key, out Material material) && material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader);
            material.name = "Runtime_TempSupportVfxLine";
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            MaterialsByColor[key] = material;
            return material;
        }
    }
}
