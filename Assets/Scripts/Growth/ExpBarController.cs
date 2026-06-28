using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ExpBarController : MonoBehaviour // 경험치 바 + 레벨/골드 HUD 연동
    {
        [Header("경험치 바")]
        public Slider ExpSlider; // UI Slider (fillImage 미연결 시 fallback)
        [SerializeField] private Image expFillImage; // Fill 이미지 — fillAmount로 게이지 표시

        [Header("레벨업 UI")]
        public TextMeshProUGUI LevelText; // 레벨 숫자 표시
        public LevelUpUi LevelUpUi; // 레벨업 패널
        public CardUI CardUi; // LevelUpUi 없을 때 카드 UI 호출

        [Header("경험치 텍스트")]
        [SerializeField] private TextMeshProUGUI expNumText; // 현재/최대 경험치 

        [Header("골드 텍스트")]
        [SerializeField] private TextMeshProUGUI goldText; // 현재 골드 수량

        [Header("보석 텍스트")]
        [SerializeField] private TextMeshProUGUI gemText; // 보석 수량 (변수 미정)

        private CoreStatProvider subscribedCore; // StatsChanged 구독 대상
        private bool levelUpUiOpened; // 레벨업 UI 중복 오픈 방지
        private bool wasLevelUpChoicePending; // 카드 선택 완료 감지용 이전 상태
        private bool cancelingFailedLevelUpOpen; // 오픈 실패 복구 중 재진입 방지

        private void Awake() // Slider·Fill 참조 초기화
        {
            if (ExpSlider == null)
            {
                ExpSlider = GetComponentInChildren<Slider>(true);
            }

            if (ExpSlider != null)
            {
                ExpSlider.minValue = 0f;
                ExpSlider.maxValue = 1f;
                ExpSlider.interactable = false; // 플레이어 조작 불가
            }

            // 인스펙터 미연결 시 Slider 하위 Fill Image 자동 탐색
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

            ConfigureExpFillImage(); // 게이지는 이미지 크기 축소가 아니라 fillAmount로 자르기
        }

        private void ConfigureExpFillImage() // EXPBar 원본 비율 유지 + 좌측 기준 크롭
        {
            if (expFillImage == null)
            {
                return;
            }

            expFillImage.type = Image.Type.Filled; // fillAmount가 실제로 잘라내도록 설정
            expFillImage.fillMethod = Image.FillMethod.Horizontal; // 좌우 게이지
            expFillImage.fillOrigin = (int)Image.OriginHorizontal.Left; // 왼쪽부터 채움
            expFillImage.preserveAspect = false; // RectTransform 비율은 씬에서 원본 비율로 관리
        }

        private void OnEnable() // 활성화 시 코어 구독 + 즉시 표시
        {
            TrySubscribeCore();
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault());
        }

        private void Start() // Awake/OnEnable 순서 보정
        {
            TrySubscribeCore();
            RefreshFromCore(CoreStatProvider.GetCurrentOrDefault());
        }

        private void Update() // 코어가 늦게 생성된 경우 연결 재시도
        {
            if (CoreStatProvider.Active == null)
            {
                return;
            }

            if (subscribedCore == null && TrySubscribeCore())
            {
                RefreshFromCore(CoreStatProvider.GetCurrentOrDefault());
            }

            TryOpenPendingLevelUp(CoreStatProvider.GetCurrentOrDefault()); // UI가 바빴던 레벨업 재시도
        }

        private void OnDisable() // 구독 해제
        {
            if (subscribedCore != null)
            {
                subscribedCore.StatsChanged -= OnStatsChanged;
                subscribedCore = null;
            }
        }

        private bool TrySubscribeCore() // CoreStatProvider.StatsChanged 구독
        {
            if (subscribedCore != null || CoreStatProvider.Active == null)
            {
                return false;
            }

            subscribedCore = CoreStatProvider.Active;
            subscribedCore.StatsChanged += OnStatsChanged;
            return true;
        }

        private void OnStatsChanged(CoreStatData stats) // 코어 스탯 변경 시 HUD 갱신 + 레벨업 처리
        {
            RefreshFromCore(stats);
            TryOpenPendingLevelUp(stats);
        }

        private void TryOpenPendingLevelUp(CoreStatData stats) // 경험치 충족 시 카드 UI가 열릴 때만 pending 처리
        {
            if (cancelingFailedLevelUpOpen)
            {
                return;
            }

            CoreStatProvider core = CoreStatProvider.Active;
            if (core == null)
            {
                return;
            }

            bool choicePending = core.IsLevelUpChoicePending;
            if (wasLevelUpChoicePending && !choicePending)
            {
                levelUpUiOpened = false; // 카드 선택 완료 → 다음 레벨업 허용
            }

            wasLevelUpChoicePending = choicePending;
            if (choicePending)
            {
                return; // 카드 선택 중 — 경험치는 아직 미소비
            }

            if (!stats.CanLevelUp)
            {
                levelUpUiOpened = false;
                return;
            }

            if (levelUpUiOpened)
            {
                return;
            }

            CardUI cardUi = ResolveCardUi();
            if (cardUi == null || !cardUi.CanOpenLevelUpPanel())
            {
                levelUpUiOpened = false; // 보상/선택권 패널이 닫힌 뒤 Update에서 재시도
                return;
            }

            if (!core.TryBeginLevelUpChoice())
            {
                levelUpUiOpened = false; // 레벨 반영 실패 시 재시도 허용
                return;
            }

            if (cardUi.TryOpenLevelUpPanel())
            {
                levelUpUiOpened = true;
                return;
            }

            cancelingFailedLevelUpOpen = true;
            try
            {
                core.CancelLevelUpChoice(); // UI 오픈 실패 시 pending만 남기지 않음
            }
            finally
            {
                cancelingFailedLevelUpOpen = false;
                levelUpUiOpened = false;
            }
        }

        private CardUI ResolveCardUi() // 씬 연결 누락 시 런타임 보강
        {
            if (CardUi != null)
            {
                return CardUi;
            }

            CardUi = FindFirstObjectByType<CardUI>();
            return CardUi;
        }

        private void RefreshFromCore(CoreStatData stats) // 코어 데이터 → UI 일괄 반영
        {
            SetFillRatio(stats.ExperienceRatio);
            SetLevelDisplay(stats.Level);
            SetExpNumDisplay(stats.CurrentExperience, stats.ExperienceToNextLevel);
            SetGoldDisplay(stats.Gold);
            SetGemDisplay();
        }

        private void SetFillRatio(float ratio) // 경험치 게이지 0~1 갱신
        {
            float clamped = Mathf.Clamp01(ratio);

            if (expFillImage != null)
            {
                expFillImage.enabled = true;
                expFillImage.fillAmount = clamped; // Image Filled 방식
                return;
            }

            if (ExpSlider != null)
            {
                ExpSlider.value = clamped; // fillImage 없을 때 Slider fallback
            }
        }

        private void SetLevelDisplay(int level) // 레벨 텍스트 갱신
        {
            if (LevelText == null)
            {
                return;
            }

            LevelText.text = $"{Mathf.Max(1, level)}";
        }

        private void SetExpNumDisplay(int current, int max) // 경험치 수치 텍스트 (현재/최대)
        {
            if (expNumText == null)
            {
                return;
            }

            expNumText.text = $"{current}/{max}";
        }

        private void SetGoldDisplay(int gold) // 골드 수량 텍스트 갱신
        {
            if (goldText == null)
            {
                return;
            }

            goldText.text = gold.ToString();
        }

        // TODO: CoreStatData에 보석 변수 추가되면 RefreshFromCore에서 stats.Gem 전달
        private void SetGemDisplay() // 보석 수량 텍스트 갱신 (변수 미정)
        {
            if (gemText == null)
            {
                return;
            }

            gemText.text = "0"; // 임시값 — 보석 변수 연결 후 교체
        }
    }
}
