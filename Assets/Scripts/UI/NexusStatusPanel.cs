using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class NexusStatusPanel : MonoBehaviour // 넥서스 체력/실드 전용 패널
    {
        private const string ShieldBarName = "Shield";
        private const string HealthBarName = "Health";
        private const string FillName = "Fill";
        private const string TextName = "Text";

        [Header("Binding")]
        [SerializeField] private NexusController nexus; // 표시 대상 넥서스
        [SerializeField] private RectTransform statusRoot; // 패널 루트
        [SerializeField] private Image shieldFillImage; // 실드 Fill
        [SerializeField] private Image healthFillImage; // 체력 Fill
        [SerializeField] private Text shieldText; // 실드 수치
        [SerializeField] private Text healthText; // 체력 수치

        [Header("Visual")]
        [SerializeField] private bool autoResolveChildren = true; // 하위 Shield/Health 자동 연결
        [SerializeField] private bool configureVisuals = true; // Fill/Text 표시 속성 보정
        [SerializeField] private Color shieldColor = new Color(0.22f, 0.72f, 1f, 0.92f);
        [SerializeField] private Color healthColor = new Color(0.3f, 0.95f, 0.48f, 0.94f);
        [SerializeField] private Color lowHealthColor = new Color(1f, 0.23f, 0.18f, 0.94f);
        [SerializeField, Range(0.01f, 1f)] private float lowHealthRatio = 0.3f;

        private NexusController subscribedNexus;

        private void Awake()
        {
            ResolveReferences();
            ConfigureVisuals();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindNexusEvents();
            RefreshNow();
        }

        private void OnDisable()
        {
            UnbindNexusEvents();
        }

        private void LateUpdate()
        {
            if (nexus == null)
            {
                ResolveReferences();
                BindNexusEvents();
            }

            RefreshNow();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveReferences();
            ConfigureVisuals();
            RefreshNow();
        }
#endif

        public void RefreshNow()
        {
            if (nexus == null)
            {
                SetFillAmount(shieldFillImage, 0f);
                SetFillAmount(healthFillImage, 0f);
                SetText(shieldText, "실드 0/0");
                SetText(healthText, "체력 0/0");
                return;
            }

            SetFillAmount(shieldFillImage, nexus.ShieldRatio);
            SetFillAmount(healthFillImage, nexus.HealthRatio);

            if (healthFillImage != null)
            {
                healthFillImage.color = nexus.HealthRatio <= lowHealthRatio ? lowHealthColor : healthColor;
            }

            SetText(shieldText, $"실드 {nexus.CurrentShield}/{nexus.MaxShield}");
            string healthValue = nexus.IsInvincible
                ? $"체력 {nexus.CurrentHealth}/{nexus.MaxHealth} 무적"
                : $"체력 {nexus.CurrentHealth}/{nexus.MaxHealth}";
            SetText(healthText, healthValue);
        }

        private void ResolveReferences()
        {
            if (statusRoot == null)
            {
                statusRoot = transform as RectTransform;
            }

            if (autoResolveChildren && statusRoot != null)
            {
                shieldFillImage = shieldFillImage != null ? shieldFillImage : FindStatusChild<Image>(ShieldBarName, FillName);
                healthFillImage = healthFillImage != null ? healthFillImage : FindStatusChild<Image>(HealthBarName, FillName);
                shieldText = shieldText != null ? shieldText : FindStatusChild<Text>(ShieldBarName, TextName);
                healthText = healthText != null ? healthText : FindStatusChild<Text>(HealthBarName, TextName);
            }

            if (nexus == null)
            {
                nexus = NexusController.Active != null ? NexusController.Active : FindFirstObjectByType<NexusController>();
            }
        }

        private T FindStatusChild<T>(string barName, string childName) where T : Component
        {
            Transform child = statusRoot.Find($"{barName}/{childName}");
            return child != null ? child.GetComponent<T>() : null;
        }

        private void BindNexusEvents()
        {
            if (subscribedNexus == nexus)
            {
                return;
            }

            UnbindNexusEvents();
            if (nexus == null)
            {
                return;
            }

            subscribedNexus = nexus;
            subscribedNexus.HealthChanged += OnNexusHealthChanged;
            subscribedNexus.ShieldChanged += OnNexusShieldChanged;
            subscribedNexus.StateChanged += OnNexusStateChanged;
        }

        private void UnbindNexusEvents()
        {
            if (subscribedNexus == null)
            {
                return;
            }

            subscribedNexus.HealthChanged -= OnNexusHealthChanged;
            subscribedNexus.ShieldChanged -= OnNexusShieldChanged;
            subscribedNexus.StateChanged -= OnNexusStateChanged;
            subscribedNexus = null;
        }

        private void OnNexusHealthChanged(int current, int max)
        {
            RefreshNow();
        }

        private void OnNexusShieldChanged(int current, int max)
        {
            RefreshNow();
        }

        private void OnNexusStateChanged(NexusController changedNexus)
        {
            RefreshNow();
        }

        private void ConfigureVisuals()
        {
            if (!configureVisuals)
            {
                return;
            }

            ConfigureFill(shieldFillImage, shieldColor);
            ConfigureFill(healthFillImage, healthColor);
            ConfigureText(shieldText);
            ConfigureText(healthText);
        }

        private static void ConfigureFill(Image image, Color color)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = null;
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.raycastTarget = false;
        }

        private static void ConfigureText(Text text)
        {
            if (text == null)
            {
                return;
            }

            if (text.font == null)
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(0.92f, 1f, 1f, 1f);
            text.raycastTarget = false;
        }

        private static void SetFillAmount(Image image, float amount)
        {
            if (image != null)
            {
                image.fillAmount = amount;
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
