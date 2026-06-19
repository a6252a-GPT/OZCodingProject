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
        // public ExpTest ExpTestSource; // 임시 테스트
        public LevelUpUi LevelUpUi; // 레벨업 패널
        public CardUI CardUi; // 레벨업 UI 호출 (LevelUpUi 없을 때)
        // public bool PreferExpTest = true; // 테스트 우선

        private CoreStatProvider subscribedCore;
        // private ExpTest subscribedExpTest;
        private bool levelUpUiOpened; // 코어 레벨업 UI 중복 호출 방지
        private bool wasLevelUpChoicePending; // 카드 선택 완료 감지

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
            // if (UsesExpTest())
            // {
            //     TrySubscribeExpTest();
            //     RefreshFromExpTest();
            //     return;
            // }

            TrySubscribeCore();
            CoreStatData stats = CoreStatProvider.GetCurrentOrDefault();
            RefreshFromCore(stats); // 
        }

        private void Start()
        {
            TrySubscribeCore(); // Awake/OnEnable 순서 보정
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault()); // 최신 코어값 재반영
        }

        private void Update()
        {
            if (subscribedCore != null || CoreStatProvider.Active == null)
            {
                return; // 이미 연결 또는 코어 없음
            }

            TrySubscribeCore(); // 늦게 생성된 코어 연결
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault()); // 연결 즉시 표시 보정
        }

        private void OnDisable()
        {
            if (subscribedCore != null)
            {
                subscribedCore.StatsChanged -= OnStatsChanged;
                subscribedCore = null;
            }

            // if (subscribedExpTest != null)
            // {
            //     subscribedExpTest.Changed -= OnExpTestChanged;
            //     subscribedExpTest.LevelUpTriggered -= OnLevelUpTriggered;
            //     subscribedExpTest = null;
            // }
        }

        // private bool UsesExpTest()
        // {
        //     return PreferExpTest && ResolveExpTest() != null;
        // }
        //
        // private ExpTest ResolveExpTest()
        // {
        //     return ExpTestSource != null ? ExpTestSource : ExpTest.Active;
        // }
        //
        // private void TrySubscribeExpTest()
        // {
        //     ExpTest expTest = ResolveExpTest();
        //     if (subscribedExpTest != null || expTest == null)
        //     {
        //         return;
        //     }
        //
        //     subscribedExpTest = expTest;
        //     subscribedExpTest.Changed += OnExpTestChanged;
        //     subscribedExpTest.LevelUpTriggered += OnLevelUpTriggered;
        // }

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

        // private void OnExpTestChanged()
        // {
        //     RefreshFromExpTest();
        // }

        private void OnStatsChanged(CoreStatData stats)
        {
            // if (UsesExpTest())
            // {
            //     RefreshFromExpTest();
            //     return;
            // }

            RefreshFromCore(stats); // 

            CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
            if (core == null)
            {
                return; // 코어 없음
            }

            bool choicePending = core.IsLevelUpChoicePending; // 카드 선택 UI 표시 중
            if (wasLevelUpChoicePending && !choicePending)
            {
                levelUpUiOpened = false; // 카드 선택 완료 → 다음 레벨업 UI 허용
            }

            wasLevelUpChoicePending = choicePending; // 이전 프레임 상태 저장
            if (choicePending)
            {
                return; // 카드 선택 중 (경험치는 아직 미소비)
            }

            if (stats.CanLevelUp && !levelUpUiOpened) // 
            {
                levelUpUiOpened = true; // 
                if (core.TryBeginLevelUpChoice()) // 조건 확인 후 패널 오픈 (경험치는 선택 시 소비)
                {
                    OnLevelUpTriggered(); // 
                }
                else
                {
                    levelUpUiOpened = false; // 레벨 반영 실패 시 재시도 허용
                }
            }
            else if (!stats.CanLevelUp) // 
            {
                levelUpUiOpened = false; // 
            }
        }

        // private void RefreshFromExpTest()
        // {
        //     TrySubscribeExpTest();
        //     ExpTest expTest = ResolveExpTest();
        //     if (expTest == null)
        //     {
        //         return;
        //     }
        //
        //     SetFillRatio(expTest.FillRatio);
        //     SetLevelDisplay(expTest.Level);
        // }

        private void RefreshFromCore(CoreStatData stats)
        {
            SetFillRatio(stats.ExperienceRatio); // 코어 경험치 비율
            SetLevelDisplay(stats.Level); // 코어 레벨
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
