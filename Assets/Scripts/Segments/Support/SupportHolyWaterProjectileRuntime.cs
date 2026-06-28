using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SupportHolyWaterProjectileRuntime : MonoBehaviour
    {
        private static Material debugMaterial;
        private static Color debugMaterialColor;

        private Vector3 direction;
        private Transform muzzleInfluenceAnchor;
        private Vector3 lastMuzzleInfluenceAnchorPosition;
        private bool hasLastMuzzleInfluenceAnchorPosition;
        private float speed;
        private float lifetime;
        private float age;
        private float startRadius;
        private float endRadius;
        private float muzzleInfluenceStrength;

        public static void Spawn(
            Transform parent,
            Vector3 position,
            Vector3 direction,
            Transform muzzleInfluenceAnchor,
            float speed,
            float lifetime,
            float startRadius,
            float endRadius,
            float muzzleInfluenceStrength,
            Color projectileColor)
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "SG54_HolyWaterDebugSphere_Runtime";

            Collider collider = instance.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, true);
            }

            instance.transform.position = position;

            SupportHolyWaterProjectileRuntime runtime = instance.AddComponent<SupportHolyWaterProjectileRuntime>();
            runtime.Configure(
                direction,
                muzzleInfluenceAnchor,
                speed,
                lifetime,
                startRadius,
                endRadius,
                muzzleInfluenceStrength,
                projectileColor);
        }

        private void Configure(
            Vector3 fireDirection,
            Transform muzzleInfluenceAnchor,
            float speed,
            float lifetime,
            float startRadius,
            float endRadius,
            float muzzleInfluenceStrength,
            Color projectileColor)
        {
            direction = fireDirection.sqrMagnitude > 0.0001f ? fireDirection.normalized : transform.forward;
            this.muzzleInfluenceAnchor = muzzleInfluenceAnchor;
            this.speed = Mathf.Max(0.1f, speed);
            this.lifetime = Mathf.Max(0.05f, lifetime);
            this.startRadius = Mathf.Max(0.05f, startRadius);
            this.endRadius = Mathf.Max(this.startRadius, endRadius);
            this.muzzleInfluenceStrength = Mathf.Clamp01(muzzleInfluenceStrength);
            age = 0f;
            hasLastMuzzleInfluenceAnchorPosition = this.muzzleInfluenceAnchor != null;
            lastMuzzleInfluenceAnchorPosition = hasLastMuzzleInfluenceAnchorPosition ? this.muzzleInfluenceAnchor.position : Vector3.zero;

            ApplyVisual(projectileColor);
            ApplyScale(GetCurrentRadius());
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            transform.position += direction * (speed * Time.deltaTime);
            ApplyMuzzleInfluence();
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            float radius = GetCurrentRadius();
            ApplyScale(radius);
        }

        private void ApplyMuzzleInfluence()
        {
            if (muzzleInfluenceAnchor == null)
            {
                hasLastMuzzleInfluenceAnchorPosition = false;
                return;
            }

            if (muzzleInfluenceStrength <= 0f)
            {
                return;
            }

            Vector3 anchorPosition = muzzleInfluenceAnchor.position;
            if (!hasLastMuzzleInfluenceAnchorPosition)
            {
                lastMuzzleInfluenceAnchorPosition = anchorPosition;
                hasLastMuzzleInfluenceAnchorPosition = true;
                return;
            }

            Vector3 anchorDelta = anchorPosition - lastMuzzleInfluenceAnchorPosition;
            lastMuzzleInfluenceAnchorPosition = anchorPosition;
            anchorDelta.y = 0f;
            if (anchorDelta.sqrMagnitude <= 0.000001f)
            {
                return;
            }

            Vector3 offset = transform.position - anchorPosition;
            offset.y = 0f;
            float influenceRange = Mathf.Max(0.5f, speed * lifetime);
            float nearFactor = Mathf.Clamp01(1f - offset.magnitude / influenceRange);
            float influence = muzzleInfluenceStrength * nearFactor * nearFactor;
            transform.position += anchorDelta * influence;
        }

        private float GetCurrentRadius()
        {
            float progress = lifetime > 0f ? Mathf.Clamp01(age / lifetime) : 1f;
            progress = Mathf.SmoothStep(0f, 1f, progress);
            return Mathf.Lerp(startRadius, endRadius, progress);
        }

        private void ApplyScale(float radius)
        {
            float diameter = Mathf.Max(0.05f, radius) * 2f;
            transform.localScale = new Vector3(diameter, diameter, diameter);
        }

        private void ApplyVisual(Color projectileColor)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            Material material = GetDebugMaterial(projectileColor);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }

        private static Material GetDebugMaterial(Color color)
        {
            if (debugMaterial != null && debugMaterialColor == color)
            {
                return debugMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            debugMaterial = new Material(shader);
            debugMaterial.name = "Runtime_SG54_HolyWaterDebugSphere";
            debugMaterialColor = color;

            if (debugMaterial.HasProperty("_BaseColor"))
            {
                debugMaterial.SetColor("_BaseColor", color);
            }

            if (debugMaterial.HasProperty("_Color"))
            {
                debugMaterial.SetColor("_Color", color);
            }

            if (debugMaterial.HasProperty("_Surface"))
            {
                debugMaterial.SetFloat("_Surface", 1f);
            }

            if (debugMaterial.HasProperty("_Mode"))
            {
                debugMaterial.SetFloat("_Mode", 3f);
            }

            if (debugMaterial.HasProperty("_Blend"))
            {
                debugMaterial.SetFloat("_Blend", 0f);
            }

            debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            debugMaterial.SetInt("_ZWrite", 0);
            debugMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            debugMaterial.EnableKeyword("_ALPHABLEND_ON");
            debugMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return debugMaterial;
        }
    }
}
