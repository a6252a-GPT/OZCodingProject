using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DebugPanelVisibilityController : MonoBehaviour
    {
        [SerializeField] private Button toggleButton;
        [SerializeField] private TextMeshProUGUI toggleLabel;
        [SerializeField] private Text legacyToggleLabel;
        [SerializeField] private GameObject[] debugTargets = System.Array.Empty<GameObject>();
        [SerializeField] private bool visibleOnStart;
        [SerializeField] private string shownLabel = "DBG";
        [SerializeField] private string hiddenLabel = "DBG";

        private bool visible;
        private bool wired;

        private void Awake()
        {
            ResolveReferences();
            WireButton();
            SetVisible(visibleOnStart);
        }

        private void OnEnable()
        {
            ResolveReferences();
            WireButton();
            ApplyVisibility();
        }

        private void OnDisable()
        {
            UnwireButton();
        }

        private void ResolveReferences()
        {
            if (toggleButton == null)
            {
                toggleButton = GetComponentInChildren<Button>(true);
            }

            if (toggleButton != null)
            {
                if (toggleLabel == null)
                {
                    toggleLabel = toggleButton.GetComponentInChildren<TextMeshProUGUI>(true);
                }

                if (legacyToggleLabel == null)
                {
                    legacyToggleLabel = toggleButton.GetComponentInChildren<Text>(true);
                }
            }
        }

        private void WireButton()
        {
            if (wired || toggleButton == null)
            {
                return;
            }

            toggleButton.onClick.RemoveListener(Toggle);
            toggleButton.onClick.AddListener(Toggle);
            wired = true;
        }

        private void UnwireButton()
        {
            if (!wired || toggleButton == null)
            {
                return;
            }

            toggleButton.onClick.RemoveListener(Toggle);
            wired = false;
        }

        private void Toggle()
        {
            SetVisible(!visible);
        }

        public void SetVisible(bool value)
        {
            visible = value;
            ApplyVisibility();
        }

        private void ApplyVisibility()
        {
            for (int i = 0; i < debugTargets.Length; i++)
            {
                GameObject target = debugTargets[i];
                if (target == null)
                {
                    continue;
                }

                CanvasGroup group = target.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    group = target.AddComponent<CanvasGroup>();
                }

                group.alpha = visible ? 1f : 0f;
                group.interactable = visible;
                group.blocksRaycasts = visible;
            }

            string label = visible ? shownLabel : hiddenLabel;
            if (toggleLabel != null)
            {
                toggleLabel.text = label;
            }

            if (legacyToggleLabel != null)
            {
                legacyToggleLabel.text = label;
            }
        }
    }
}
