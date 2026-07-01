using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class HudIconToggleButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color inactiveColor = new Color(0.38f, 0.33f, 0.2f, 0.82f);
        [SerializeField] private Color disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.55f);

        public Button Button => ResolveButton();

        private void Awake()
        {
            ResolveButton();
            ResolveIconImage();
            ApplySprite();
        }

        private void OnValidate()
        {
            ResolveButton();
            ResolveIconImage();
            ApplySprite();
        }

        public void SetVisualState(bool active, bool available)
        {
            Image image = ResolveIconImage();
            if (image != null)
            {
                ApplySprite();
                image.color = available ? (active ? activeColor : inactiveColor) : disabledColor;
            }

            Button resolvedButton = ResolveButton();
            if (resolvedButton != null)
            {
                resolvedButton.interactable = available;
            }
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        private Button ResolveButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            return button;
        }

        private Image ResolveIconImage()
        {
            if (iconImage == null)
            {
                Button resolvedButton = ResolveButton();
                iconImage = resolvedButton != null ? resolvedButton.targetGraphic as Image : null;
            }

            if (iconImage == null)
            {
                iconImage = GetComponent<Image>();
            }

            return iconImage;
        }

        private void ApplySprite()
        {
            Image image = ResolveIconImage();
            if (image == null || iconSprite == null)
            {
                return;
            }

            image.sprite = iconSprite;
            image.preserveAspect = true;
        }
    }
}
