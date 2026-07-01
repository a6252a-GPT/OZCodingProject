using UnityEngine;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class AutoModeHudController : MonoBehaviour
    {
        [SerializeField] private ConvoyController convoy;
        [SerializeField] private CardUI cardUi;
        [SerializeField] private HudIconToggleButton speed2xButton;
        [SerializeField] private HudIconToggleButton autoCardButton;
        [SerializeField] private HudIconToggleButton autoOrbitButton;
        [SerializeField] private bool clearDoubleSpeedWhenAutoOrbitStops = true;

        private bool wired;
        private bool hasAutoOrbitSnapshot;
        private bool lastAutoOrbitActive;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            ResolveReferences();
            WireButtons();
            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
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

            if (cardUi == null)
            {
                cardUi = FindFirstObjectByType<CardUI>();
            }
        }

        private void WireButtons()
        {
            if (wired)
            {
                return;
            }

            AddClickListener(autoOrbitButton, HandleAutoOrbitClicked);
            AddClickListener(autoCardButton, HandleAutoCardClicked);
            AddClickListener(speed2xButton, HandleSpeed2xClicked);
            wired = true;
        }

        private void UnwireButtons()
        {
            if (!wired)
            {
                return;
            }

            RemoveClickListener(autoOrbitButton, HandleAutoOrbitClicked);
            RemoveClickListener(autoCardButton, HandleAutoCardClicked);
            RemoveClickListener(speed2xButton, HandleSpeed2xClicked);
            wired = false;
        }

        private static void AddClickListener(HudIconToggleButton target, UnityEngine.Events.UnityAction action)
        {
            if (target == null || target.Button == null)
            {
                return;
            }

            target.Button.onClick.RemoveListener(action);
            target.Button.onClick.AddListener(action);
        }

        private static void RemoveClickListener(HudIconToggleButton target, UnityEngine.Events.UnityAction action)
        {
            if (target == null || target.Button == null)
            {
                return;
            }

            target.Button.onClick.RemoveListener(action);
        }

        private void HandleAutoOrbitClicked()
        {
            ResolveReferences();
            if (convoy == null)
            {
                return;
            }

            AudioManager.EnsureExists()?.PlaySFX(SFXType.ClickButton);
            convoy.ToggleAutoOrbit();
            Refresh(true);
        }

        private void HandleAutoCardClicked()
        {
            ResolveReferences();
            if (!IsAutoOrbitActive() || cardUi == null)
            {
                return;
            }

            AudioManager.EnsureExists()?.PlaySFX(SFXType.ClickButton);
            cardUi.ToggleAutoSelectInAutoOrbit();
            Refresh(true);
        }

        private void HandleSpeed2xClicked()
        {
            ResolveReferences();
            if (!IsAutoOrbitActive())
            {
                return;
            }

            AudioManager.EnsureExists()?.PlaySFX(SFXType.ClickButton);
            GameSpeedController.SetDoubleSpeedPreferred(!GameSpeedController.IsDoubleSpeedPreferred());
            Refresh(true);
        }

        private void Refresh(bool force)
        {
            ResolveReferences();
            bool autoOrbitActive = IsAutoOrbitActive();
            NotifyAutoOrbitChanged(autoOrbitActive, force);

            bool cardAutoActive = cardUi != null && cardUi.AutoSelectInAutoOrbit;
            bool speedActive = autoOrbitActive && GameSpeedController.IsDoubleSpeedPreferred();

            if (autoOrbitButton != null)
            {
                autoOrbitButton.SetVisible(true);
                autoOrbitButton.SetVisualState(autoOrbitActive, convoy != null && convoy.EnableAutoOrbit);
            }

            if (autoCardButton != null)
            {
                autoCardButton.SetVisible(autoOrbitActive);
                autoCardButton.SetVisualState(cardAutoActive, autoOrbitActive && cardUi != null);
            }

            if (speed2xButton != null)
            {
                speed2xButton.SetVisible(autoOrbitActive);
                speed2xButton.SetVisualState(speedActive, autoOrbitActive);
            }

            if (force)
            {
                GameSpeedController.ApplyDesiredTimeScale();
            }
        }

        private void NotifyAutoOrbitChanged(bool autoOrbitActive, bool force)
        {
            if (!force && hasAutoOrbitSnapshot && lastAutoOrbitActive == autoOrbitActive)
            {
                return;
            }

            bool wasActive = hasAutoOrbitSnapshot && lastAutoOrbitActive;
            hasAutoOrbitSnapshot = true;
            lastAutoOrbitActive = autoOrbitActive;

            if (cardUi != null)
            {
                cardUi.NotifyAutoOrbitActiveChanged(autoOrbitActive);
            }

            if (wasActive && !autoOrbitActive && clearDoubleSpeedWhenAutoOrbitStops)
            {
                GameSpeedController.SetDoubleSpeedPreferred(false);
            }
            else
            {
                GameSpeedController.ApplyDesiredTimeScale();
            }
        }

        private bool IsAutoOrbitActive()
        {
            return convoy != null && convoy.IsAutoOrbitActive;
        }
    }
}
