using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class SettingsControlModePanel : MonoBehaviour
    {
        [SerializeField] private ConvoyController convoy;
        [SerializeField] private Button relativeTurnButton;
        [SerializeField] private Button wasdDirectionButton;
        [SerializeField] private Button mousePointerButton;
        [SerializeField] private Image relativeTurnIndicator;
        [SerializeField] private Image wasdDirectionIndicator;
        [SerializeField] private Image mousePointerIndicator;
        [SerializeField] private Sprite selectedIndicatorSprite;
        [SerializeField] private Sprite normalIndicatorSprite;
        [SerializeField] private Color normalColor = new Color(0.1f, 0.12f, 0.13f, 0.9f);
        [SerializeField] private Color selectedColor = new Color(0.96f, 0.76f, 0.24f, 0.95f);
        [SerializeField] private Color normalTextColor = new Color(0.92f, 0.9f, 0.82f, 1f);
        [SerializeField] private Color selectedTextColor = new Color(0.08f, 0.07f, 0.04f, 1f);

        private bool wired;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            ResolveReferences();
            WireButtons();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void ResolveReferences()
        {
            if (convoy == null)
            {
                convoy = FindFirstObjectByType<ConvoyController>();
            }

            if (relativeTurnButton == null)
            {
                relativeTurnButton = FindButton("RelativeTurnButton");
            }

            if (wasdDirectionButton == null)
            {
                wasdDirectionButton = FindButton("WasdDirectionButton");
            }

            if (mousePointerButton == null)
            {
                mousePointerButton = FindButton("MousePointerButton");
            }
        }

        private Button FindButton(string objectName)
        {
            Transform target = FindDeep(transform, objectName);
            return target != null ? target.GetComponent<Button>() : null;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), objectName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void WireButtons()
        {
            if (wired)
            {
                return;
            }

            AddButtonListener(relativeTurnButton, HandleRelativeTurnClicked);
            AddButtonListener(wasdDirectionButton, HandleWasdDirectionClicked);
            AddButtonListener(mousePointerButton, HandleMousePointerClicked);
            wired = true;
        }

        private void UnwireButtons()
        {
            if (!wired)
            {
                return;
            }

            RemoveButtonListener(relativeTurnButton, HandleRelativeTurnClicked);
            RemoveButtonListener(wasdDirectionButton, HandleWasdDirectionClicked);
            RemoveButtonListener(mousePointerButton, HandleMousePointerClicked);
            wired = false;
        }

        private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(action);
        }

        private void HandleRelativeTurnClicked()
        {
            SetControlMode(ConvoyControlMode.RelativeTurn);
        }

        private void HandleWasdDirectionClicked()
        {
            SetControlMode(ConvoyControlMode.WasdDirection);
        }

        private void HandleMousePointerClicked()
        {
            SetControlMode(ConvoyControlMode.MousePointer);
        }

        private void SetControlMode(ConvoyControlMode mode)
        {
            if (convoy == null)
            {
                ResolveReferences();
            }

            if (convoy == null)
            {
                return;
            }

            AudioManager.EnsureExists()?.PlaySFX(SFXType.ClickButton);
            convoy.SetControlMode(mode);
            Refresh();
        }

        private void Refresh()
        {
            if (convoy == null)
            {
                ResolveReferences();
            }

            ConvoyControlMode mode = convoy != null ? convoy.CurrentControlMode : ConvoyControlMode.RelativeTurn;
            bool autoOrbitActive = convoy != null && convoy.IsAutoOrbitActive;
            RefreshButton(relativeTurnButton, !autoOrbitActive && mode == ConvoyControlMode.RelativeTurn);
            RefreshButton(wasdDirectionButton, !autoOrbitActive && mode == ConvoyControlMode.WasdDirection);
            RefreshButton(mousePointerButton, !autoOrbitActive && mode == ConvoyControlMode.MousePointer);
            RefreshIndicator(relativeTurnIndicator, !autoOrbitActive && mode == ConvoyControlMode.RelativeTurn);
            RefreshIndicator(wasdDirectionIndicator, !autoOrbitActive && mode == ConvoyControlMode.WasdDirection);
            RefreshIndicator(mousePointerIndicator, !autoOrbitActive && mode == ConvoyControlMode.MousePointer);
        }

        private void RefreshButton(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Image image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected ? selectedColor : normalColor;
            }

            TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = selected ? selectedTextColor : normalTextColor;
            }

            Text legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.color = selected ? selectedTextColor : normalTextColor;
            }
        }

        private void RefreshIndicator(Image indicator, bool selected)
        {
            if (indicator == null)
            {
                return;
            }

            Sprite sprite = selected ? selectedIndicatorSprite : normalIndicatorSprite;
            if (sprite != null)
            {
                indicator.sprite = sprite;
            }

            indicator.color = Color.white;
            indicator.preserveAspect = true;
        }
    }
}
