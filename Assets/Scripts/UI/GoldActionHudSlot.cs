using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class GoldActionHudSlot : MonoBehaviour
    {
        private const string RoundedIconMaskName = "ActionHudRoundedIconMask";
        private const float RoundedIconCornerRadius = 8f;
        private const int RoundedIconCornerSegments = 5;

        public Image BackgroundImage;
        public Image IconImage;
        public Image CooldownFill;
        public Image LockOverlay;
        public Text KeyText;
        public Text NameText;
        public Text LevelText;
        public Text StateText;
        public Text CooldownText;
        public Button ActionButton;
        public Text ActionButtonText;
        private HudTooltipTrigger slotTooltipTrigger;
        private HudTooltipTrigger iconTooltipTrigger;
        private HudTooltipTrigger buttonTooltipTrigger;
        private HudTooltipTrigger buttonGraphicTooltipTrigger;
        private Button iconButton;
        private RectTransform roundedIconMaskTransform;

        [Header("Colors")]
        public Color NormalTextColor = new Color(0.96f, 0.98f, 1f, 1f);
        public Color DisabledTextColor = new Color(0.55f, 0.6f, 0.68f, 1f);
        public Color CooldownTextColor = new Color(1f, 0.93f, 0.62f, 1f);
        public Color DimmedIconColor = new Color(0.34f, 0.38f, 0.44f, 0.82f);
        public Color CooldownOverlayColor = new Color(0f, 0f, 0f, 0.62f);

        private void Awake()
        {
            EnsureRoundedIconMask();
        }

        private void OnEnable()
        {
            EnsureRoundedIconMask();
        }

        public void BindButton(UnityEngine.Events.UnityAction action)
        {
            BindButton(action, action);
        }

        public void BindButton(UnityEngine.Events.UnityAction buttonAction, UnityEngine.Events.UnityAction iconAction)
        {
            if (ActionButton == null)
            {
                BindIconButton(iconAction);
                return;
            }

            EnsureButtonRaycastTarget();
            ActionButton.onClick.RemoveAllListeners();
            if (buttonAction != null)
            {
                ActionButton.onClick.AddListener(buttonAction);
            }

            BindIconButton(iconAction);
        }

        public void Refresh(
            Sprite icon,
            string keyLabel,
            string skillName,
            string levelLabel,
            string stateLabel,
            string cooldownLabel,
            float cooldownRatio,
            string buttonLabel,
            bool buttonEnabled,
            bool locked,
            bool iconActive,
            bool coolingDown)
        {
            EnsureRoundedIconMask();
            SetSprite(IconImage, icon);
            SetText(KeyText, keyLabel, NormalTextColor);
            SetText(NameText, string.Empty, NormalTextColor);
            SetText(LevelText, levelLabel, locked ? DisabledTextColor : NormalTextColor);
            SetText(StateText, locked ? stateLabel : string.Empty, DisabledTextColor);
            SetText(CooldownText, cooldownLabel, coolingDown ? CooldownTextColor : DisabledTextColor);
            SetText(ActionButtonText, buttonLabel, buttonEnabled ? NormalTextColor : DisabledTextColor);

            if (BackgroundImage != null)
            {
                BackgroundImage.color = Color.clear;
                BackgroundImage.raycastTarget = true;
            }

            if (IconImage != null)
            {
                IconImage.color = iconActive && !coolingDown ? Color.white : DimmedIconColor;
                IconImage.raycastTarget = true;
            }

            if (iconButton != null)
            {
                iconButton.interactable = iconActive && !coolingDown && !locked;
            }

            if (CooldownFill != null)
            {
                CooldownFill.color = CooldownOverlayColor;
                CooldownFill.type = Image.Type.Filled;
                CooldownFill.fillMethod = Image.FillMethod.Vertical;
                CooldownFill.fillOrigin = 0;
                CooldownFill.fillAmount = Mathf.Clamp01(cooldownRatio);
                CooldownFill.raycastTarget = false;
                CooldownFill.gameObject.SetActive(coolingDown && cooldownRatio > 0.001f);
            }

            if (LockOverlay != null)
            {
                LockOverlay.color = Color.white;
                LockOverlay.raycastTarget = false;
                LockOverlay.gameObject.SetActive(locked);
            }

            if (ActionButton != null)
            {
                EnsureButtonRaycastTarget();
                ActionButton.interactable = buttonEnabled;
            }

            if (ActionButtonText != null)
            {
                ActionButtonText.raycastTarget = false;
            }
        }

        public void SetTooltipContent(string title, string body, string footer)
        {
            ApplyTooltipContent(EnsureTooltipTrigger(gameObject, ref slotTooltipTrigger), title, body, footer);

            if (IconImage != null)
            {
                IconImage.raycastTarget = true;
                ApplyTooltipContent(EnsureTooltipTrigger(IconImage.gameObject, ref iconTooltipTrigger), title, body, footer);
            }

            if (ActionButton != null)
            {
                ApplyTooltipContent(EnsureTooltipTrigger(ActionButton.gameObject, ref buttonTooltipTrigger), title, body, footer);

                if (ActionButton.targetGraphic != null && ActionButton.targetGraphic.gameObject != ActionButton.gameObject)
                {
                    ApplyTooltipContent(EnsureTooltipTrigger(ActionButton.targetGraphic.gameObject, ref buttonGraphicTooltipTrigger), title, body, footer);
                }
            }
        }

        private void EnsureButtonRaycastTarget()
        {
            if (ActionButton == null || ActionButton.targetGraphic == null)
            {
                return;
            }

            ActionButton.targetGraphic.raycastTarget = true;
        }

        private void BindIconButton(UnityEngine.Events.UnityAction iconAction)
        {
            Button button = EnsureIconButton();
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (iconAction != null)
            {
                button.onClick.AddListener(iconAction);
            }
        }

        private Button EnsureIconButton()
        {
            if (IconImage == null)
            {
                return null;
            }

            if (iconButton == null || iconButton.gameObject != IconImage.gameObject)
            {
                iconButton = IconImage.GetComponent<Button>() ?? IconImage.gameObject.AddComponent<Button>();
            }

            IconImage.raycastTarget = true;
            iconButton.targetGraphic = IconImage;
            iconButton.transition = Selectable.Transition.None;
            iconButton.navigation = new Navigation { mode = Navigation.Mode.None };
            return iconButton;
        }

        private void EnsureRoundedIconMask()
        {
            if (IconImage == null)
            {
                return;
            }

            RectTransform iconRect = IconImage.rectTransform;
            if (iconRect == null)
            {
                return;
            }

            if (roundedIconMaskTransform == null)
            {
                Transform existingMask = transform.Find(RoundedIconMaskName);
                roundedIconMaskTransform = existingMask != null ? existingMask as RectTransform : null;
            }

            if (roundedIconMaskTransform == null)
            {
                roundedIconMaskTransform = CreateRoundedIconMask(iconRect);
            }

            if (roundedIconMaskTransform == null)
            {
                return;
            }

            ConfigureRoundedIconMask(roundedIconMaskTransform);
            ParentToRoundedIconMask(IconImage.rectTransform);
            ParentToRoundedIconMask(CooldownFill != null ? CooldownFill.rectTransform : null);

            IconImage.rectTransform.SetAsFirstSibling();
            if (CooldownFill != null)
            {
                CooldownFill.rectTransform.SetAsLastSibling();
            }
        }

        private RectTransform CreateRoundedIconMask(RectTransform sourceRect)
        {
            Transform sourceParent = sourceRect.parent;
            if (sourceParent == null)
            {
                return null;
            }

            int siblingIndex = sourceRect.GetSiblingIndex();
            GameObject maskObject = new GameObject(
                RoundedIconMaskName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(ActionHudRoundedRectMaskGraphic),
                typeof(Mask));

            maskObject.layer = sourceRect.gameObject.layer;
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.SetParent(sourceParent, false);
            CopyRectTransform(sourceRect, maskRect);
            maskRect.SetSiblingIndex(siblingIndex);

            return maskRect;
        }

        private static void ConfigureRoundedIconMask(RectTransform maskRect)
        {
            ActionHudRoundedRectMaskGraphic maskGraphic = maskRect.GetComponent<ActionHudRoundedRectMaskGraphic>();
            if (maskGraphic != null)
            {
                maskGraphic.CornerRadius = RoundedIconCornerRadius;
                maskGraphic.CornerSegments = RoundedIconCornerSegments;
                maskGraphic.color = Color.white;
                maskGraphic.raycastTarget = false;
            }

            Mask mask = maskRect.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = false;
            }
        }

        private void ParentToRoundedIconMask(RectTransform targetRect)
        {
            if (targetRect == null || targetRect == roundedIconMaskTransform || targetRect.parent == roundedIconMaskTransform)
            {
                return;
            }

            targetRect.SetParent(roundedIconMaskTransform, true);
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static HudTooltipTrigger EnsureTooltipTrigger(GameObject target, ref HudTooltipTrigger cache)
        {
            if (target == null)
            {
                return null;
            }

            if (cache == null || cache.gameObject != target)
            {
                cache = target.GetComponent<HudTooltipTrigger>() ?? target.AddComponent<HudTooltipTrigger>();
            }

            return cache;
        }

        private static void ApplyTooltipContent(HudTooltipTrigger trigger, string title, string body, string footer)
        {
            trigger?.SetContent(title, body, footer);
        }

        private static void SetSprite(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }

        private static void SetText(Text text, string value, Color color)
        {
            if (text == null)
            {
                return;
            }

            text.text = value;
            text.color = color;
        }
    }
}
