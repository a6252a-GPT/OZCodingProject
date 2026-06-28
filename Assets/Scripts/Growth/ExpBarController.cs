using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExpBarController : MonoBehaviour // 경험치 바 + 레벨/골드 HUD 연동
    {
        [Header("경험치 바")]
        public Slider ExpSlider;
        [SerializeField] private Image expFillImage;

        [Header("레벨업 UI")]
        public TextMeshProUGUI LevelText;
        public LevelUpUi LevelUpUi;
        public CardUI CardUi;

        [Header("경험치 텍스트")]
        [SerializeField] private TextMeshProUGUI expNumText;

        [Header("골드 텍스트")]
        [SerializeField] private TextMeshProUGUI goldText;

        [Header("보석 텍스트")]
        [SerializeField] private TextMeshProUGUI gemText;

        private CoreStatProvider subscribedCore;
        private bool levelUpUiOpened;
        private bool wasLevelUpChoicePending;

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

            if (expFillImage == null && ExpSlider != null)
            {
                foreach (Image img in ExpSlider.GetComponentsInChildren<Image>(true))
                {
                    if (img.gameObject.name.Contains("Fill") && img.gameObject.name != "Fill Area")
                    {
                        expFillImage = img;
                        break;
                    }
                }
            }

            ConfigureExpFillImage();
        }

        private void ConfigureExpFillImage()
        {
            if (expFillImage == null)
            {
                return;
            }

            expFillImage.type = Image.Type.Filled;
            expFillImage.fillMethod = Image.FillMethod.Horizontal;
            expFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            expFillImage.preserveAspect = false;
        }

        private void OnEnable()
        {
            TrySubscribeCore();
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault());
            TryProcessLevelUp();
        }

        private void Start()
        {
            TrySubscribeCore();
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault());
            TryProcessLevelUp();
        }

        private void Update()
        {
            if (subscribedCore == null && CoreStatProvider.Active != null)
            {
                TrySubscribeCore();
            }

            TryProcessLevelUp(); // 경험치 초과 여부 상시 확인 //안건준 수정 - 0628
        }

        private void OnDisable()
        {
            if (subscribedCore != null)
            {
                subscribedCore.StatsChanged -= OnStatsChanged;
                subscribedCore = null;
            }
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

        private void OnStatsChanged(CoreStatData stats)
        {
            RefreshFromCore(stats);
            TryProcessLevelUp();
        }

        private void TryProcessLevelUp()
        {
            CoreStatProvider core = CoreStatProvider.Active;
            if (core == null)
            {
                return;
            }

            CoreStatData stats = core.CurrentStats;
            RefreshFromCore(stats);

            bool choicePending = core.IsLevelUpChoicePending;
            bool panelOpen = IsLevelUpPanelOpen();
            bool panelVisible = IsLevelUpPanelVisible();

            if (choicePending && !panelOpen)
            {
                core.CancelLevelUpChoice(); // UI 없이 pending만 남은 stuck 상태 복구 //안건준 수정 - 0628
                choicePending = false;
                levelUpUiOpened = false;
            }

            if (wasLevelUpChoicePending && !choicePending)
            {
                levelUpUiOpened = false;
            }

            wasLevelUpChoicePending = choicePending;

            if (choicePending)
            {
                return;
            }

            if (!stats.CanLevelUp)
            {
                levelUpUiOpened = false;
                return;
            }

            if (levelUpUiOpened && panelVisible)
            {
                return;
            }

            if (!core.TryBeginLevelUpChoice())
            {
                levelUpUiOpened = false;
                return;
            }

            levelUpUiOpened = true;
            OnLevelUpTriggered();
        }

        private bool IsLevelUpPanelOpen()
        {
            return LevelUpUi != null && LevelUpUi.IsPanelOpen;
        }

        private bool IsLevelUpPanelVisible()
        {
            return LevelUpUi != null && LevelUpUi.IsPanelVisible;
        }

        private void RefreshFromCore(CoreStatData stats)
        {
            SetFillRatio(stats.ExperienceRatio);
            SetLevelDisplay(stats.Level);
            SetExpNumDisplay(stats.CurrentExperience, stats.ExperienceToNextLevel);
            SetGoldDisplay(stats.Gold);
            SetGemDisplay();
        }

        private void SetFillRatio(float ratio)
        {
            float clamped = Mathf.Clamp01(ratio);

            if (expFillImage != null)
            {
                expFillImage.enabled = true;
                expFillImage.fillAmount = clamped;
                return;
            }

            if (ExpSlider != null)
            {
                ExpSlider.value = clamped;
            }
        }

        private void SetLevelDisplay(int level)
        {
            if (LevelText == null)
            {
                return;
            }

            LevelText.text = $"{Mathf.Max(1, level)}";
        }

        private void SetExpNumDisplay(int current, int max)
        {
            if (expNumText == null)
            {
                return;
            }

            expNumText.text = $"{current}/{max}";
        }

        private void SetGoldDisplay(int gold)
        {
            if (goldText == null)
            {
                return;
            }

            goldText.text = gold.ToString();
        }

        private void SetGemDisplay()
        {
            if (gemText == null)
            {
                return;
            }

            gemText.text = "0";
        }
    }
}
