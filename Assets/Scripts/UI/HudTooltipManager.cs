using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class HudTooltipManager : MonoBehaviour
    {
        public static HudTooltipManager Instance { get; private set; }

        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text footerText;
        [SerializeField] private Vector2 screenOffset = new Vector2(18f, -18f);
        [SerializeField, Min(180f)] private float panelWidth = 300f;

        private RectTransform tooltipRect;
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private Vector2 lastPointerPosition;

        private void Awake()
        {
            RegisterInstance(this);
            EnsureRuntimeView();
            HideTooltip();
        }

        private void OnEnable()
        {
            RegisterInstance(this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (tooltipPanel == null || !tooltipPanel.activeSelf)
            {
                return;
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                MoveTo(mouse.position.ReadValue());
            }
            else
            {
                MoveTo(lastPointerPosition);
            }
        }

        public static HudTooltipManager ResolveFor(Transform owner)
        {
            if (Instance != null)
            {
                return Instance;
            }

            Canvas ownerCanvas = owner != null ? owner.GetComponentInParent<Canvas>(true) : null;
            if (ownerCanvas == null)
            {
                ownerCanvas = FindFirstObjectByType<Canvas>();
            }

            if (ownerCanvas == null)
            {
                return null;
            }

            GameObject root = new GameObject("HudTooltipRoot", typeof(RectTransform), typeof(HudTooltipManager));
            root.transform.SetParent(ownerCanvas.transform, false);
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            HudTooltipManager manager = root.GetComponent<HudTooltipManager>();
            manager.canvas = ownerCanvas;
            manager.EnsureRuntimeView();
            manager.HideTooltip();
            return manager;
        }

        public void ShowTooltip(HudTooltipContent content, Vector2 pointerPosition)
        {
            EnsureRuntimeView();
            if (content == null || !content.HasAnyText || tooltipPanel == null)
            {
                HideTooltip();
                return;
            }

            SetText(titleText, content.Title);
            SetText(bodyText, content.Body);
            SetText(footerText, content.Footer);

            tooltipPanel.SetActive(true);
            tooltipPanel.transform.SetAsLastSibling();
            RebuildLayout();
            MoveTo(pointerPosition);
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null)
            {
                tooltipPanel.SetActive(false);
            }
        }

        public void MoveTo(Vector2 pointerPosition)
        {
            lastPointerPosition = pointerPosition;
            if (tooltipRect == null || tooltipPanel == null || !tooltipPanel.activeSelf)
            {
                return;
            }

            tooltipRect.position = ClampToScreen(pointerPosition + screenOffset);
        }

        private static void RegisterInstance(HudTooltipManager candidate)
        {
            if (candidate == null)
            {
                return;
            }

            Instance = candidate;
        }

        private void EnsureRuntimeView()
        {
            if (canvas == null)
            {
                canvas = GetComponentInParent<Canvas>(true);
            }

            if (tooltipPanel == null)
            {
                Transform existingPanel = transform.Find("TooltipPanel");
                tooltipPanel = existingPanel != null ? existingPanel.gameObject : CreatePanel();
            }

            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            if (tooltipRect == null)
            {
                tooltipRect = tooltipPanel.AddComponent<RectTransform>();
            }

            tooltipRect.pivot = new Vector2(0f, 1f);
            tooltipRect.sizeDelta = new Vector2(panelWidth, tooltipRect.sizeDelta.y);

            Image background = tooltipPanel.GetComponent<Image>();
            if (background == null)
            {
                background = tooltipPanel.AddComponent<Image>();
            }

            background.color = new Color(0.03f, 0.035f, 0.045f, 0.96f);
            background.raycastTarget = false;

            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }

            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            VerticalLayoutGroup layout = tooltipPanel.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = tooltipPanel.AddComponent<VerticalLayoutGroup>();
            }

            layout.padding = new RectOffset(12, 12, 10, 10);
            layout.spacing = 4f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = tooltipPanel.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = tooltipPanel.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            titleText = EnsureText("TitleText", titleText, 17, FontStyle.Bold, new Color(1f, 0.9f, 0.55f, 1f));
            bodyText = EnsureText("BodyText", bodyText, 14, FontStyle.Normal, new Color(0.9f, 0.94f, 1f, 1f));
            footerText = EnsureText("FooterText", footerText, 13, FontStyle.Bold, new Color(0.55f, 0.78f, 1f, 1f));
            ConfigureRaycastBlocking();
        }

        private GameObject CreatePanel()
        {
            GameObject panel = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(panelWidth, 120f);
            return panel;
        }

        private Text EnsureText(string childName, Text current, int fontSize, FontStyle style, Color color)
        {
            if (current == null)
            {
                Transform child = tooltipPanel.transform.Find(childName);
                current = child != null ? child.GetComponent<Text>() : null;
            }

            if (current == null)
            {
                GameObject textObject = new GameObject(childName, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                textObject.transform.SetParent(tooltipPanel.transform, false);
                current = textObject.GetComponent<Text>();
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            current.font = font;
            current.fontSize = fontSize;
            current.fontStyle = style;
            current.color = color;
            current.supportRichText = true;
            current.alignment = TextAnchor.UpperLeft;
            current.horizontalOverflow = HorizontalWrapMode.Wrap;
            current.verticalOverflow = VerticalWrapMode.Overflow;
            current.raycastTarget = false;

            LayoutElement layout = current.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = current.gameObject.AddComponent<LayoutElement>();
            }

            layout.preferredWidth = panelWidth - 24f;
            layout.flexibleWidth = 1f;
            return current;
        }

        private void RebuildLayout()
        {
            if (tooltipRect == null)
            {
                return;
            }

            tooltipRect.sizeDelta = new Vector2(panelWidth, tooltipRect.sizeDelta.y);
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltipRect);
        }

        private Vector2 ClampToScreen(Vector2 desired)
        {
            float scale = canvas != null ? Mathf.Max(0.001f, canvas.scaleFactor) : 1f;
            float width = tooltipRect != null ? tooltipRect.rect.width * scale : panelWidth;
            float height = tooltipRect != null ? tooltipRect.rect.height * scale : 120f;
            const float margin = 10f;

            float x = Mathf.Clamp(desired.x, margin, Mathf.Max(margin, Screen.width - width - margin));
            float y = Mathf.Clamp(desired.y, Mathf.Min(Screen.height - margin, height + margin), Screen.height - margin);
            return new Vector2(x, y);
        }

        private void ConfigureRaycastBlocking()
        {
            if (tooltipPanel == null)
            {
                return;
            }

            Graphic[] graphics = tooltipPanel.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }

        private static void SetText(Text text, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;
            text.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }
    }
}
