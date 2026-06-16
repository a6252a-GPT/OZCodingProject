using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExpBarController : MonoBehaviour // 경험치 Slider (0~1)
    {
        public Slider ExpSlider; // UI Slider
        public TextMeshProUGUI LevelText; // 레벨 표시 (LevelTest)
        public ExpTest ExpTestSource; // 임시 테스트
        public LevelUpUi LevelUpUi; // 레벨업 패널
        public CardUI CardUi; // 레벨업 UI 호출 (LevelUpUi 없을 때)
        public bool PreferExpTest = true; // 테스트 우선

        private CoreStatProvider subscribedCore;
        private ExpTest subscribedExpTest;

        private void Awake()
        {
            if (ExpSlider == null)
            {
                ExpSlider = GetComponentInChildren<Slider>(true);
            }

            if (ExpSlider != null)
            {
                ExpSlider.minValue = 0f;
                ExpSlider.maxValue = 1f;
                ExpSlider.interactable = false;
            }
        }

        private void OnEnable()
        {
            if (UsesExpTest())
            {
                TrySubscribeExpTest();
                RefreshFromExpTest();
                return;
            }

            TrySubscribeCore();
            CoreStatData stats = CoreStatProvider.GetCurrentOrDefault();
            SetFillRatio(stats.ExperienceRatio);
            SetLevelDisplay(stats.Level);
        }

        private void OnDisable()
        {
            if (subscribedCore != null)
            {
                subscribedCore.StatsChanged -= OnStatsChanged;
                subscribedCore = null;
            }

            if (subscribedExpTest != null)
            {
                subscribedExpTest.Changed -= OnExpTestChanged;
                subscribedExpTest.LevelUpTriggered -= OnLevelUpTriggered;
                subscribedExpTest = null;
            }
        }

        private bool UsesExpTest()
        {
            return PreferExpTest && ResolveExpTest() != null;
        }

        private ExpTest ResolveExpTest()
        {
            return ExpTestSource != null ? ExpTestSource : ExpTest.Active;
        }

        private void TrySubscribeExpTest()
        {
            ExpTest expTest = ResolveExpTest();
            if (subscribedExpTest != null || expTest == null)
            {
                return;
            }

            subscribedExpTest = expTest;
            subscribedExpTest.Changed += OnExpTestChanged;
            subscribedExpTest.LevelUpTriggered += OnLevelUpTriggered;
        }

        private void OnLevelUpTriggered()
        {
            if (LevelUpUi != null)
            {
                LevelUpUi.Open();
                return;
            }

            CardUi?.PlayLevelUpTween();
        }

        private void TrySubscribeCore()
        {
            if (subscribedCore != null || CoreStatProvider.Active == null)
            {
                return;
            }

            subscribedCore = CoreStatProvider.Active;
            subscribedCore.StatsChanged += OnStatsChanged;
        }

        private void OnExpTestChanged()
        {
            RefreshFromExpTest();
        }

        private void OnStatsChanged(CoreStatData stats)
        {
            if (UsesExpTest())
            {
                RefreshFromExpTest();
                return;
            }

            SetFillRatio(stats.ExperienceRatio);
            SetLevelDisplay(stats.Level);
        }

        private void RefreshFromExpTest()
        {
            TrySubscribeExpTest();
            ExpTest expTest = ResolveExpTest();
            if (expTest == null)
            {
                return;
            }

            SetFillRatio(expTest.FillRatio);
            SetLevelDisplay(expTest.Level);
        }

        private void SetFillRatio(float ratio)
        {
            if (ExpSlider == null)
            {
                return;
            }

            ExpSlider.value = Mathf.Clamp01(ratio);
        }

        private void SetLevelDisplay(int level)
        {
            if (LevelText == null)
            {
                return;
            }

            LevelText.text = $"LV : {Mathf.Max(1, level)}";
        }
    }
}
