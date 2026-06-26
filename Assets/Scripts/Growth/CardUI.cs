using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using TeamProject01.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    private const string DescriptionValueToken = "(N)";
    private const string DescriptionValueColor = "#2F6BFF";
    private const string CardUiPrefabReferencesResourcePath = "LevelCard/CardUiPrefabReferences";
    private const string TierFrameNormalResourcePath = "LevelCard/TierFrames/CardFrame_Normal";
    private const string TierFrameRareResourcePath = "LevelCard/TierFrames/CardFrame_Rare";
    private const string TierFrameUniqueResourcePath = "LevelCard/TierFrames/CardFrame_Unique";

    [Header("Stat Upgrade")]
    [SerializeField] private GameObject[] statUpgradeCards = System.Array.Empty<GameObject>(); // 스탯 강화 카드 프리팹

    [Header("Add Segment")]
    // 안건준 수정 - 0623 : 세그먼트 카드 공통 기본 프리팹 (SegmentUpgradeCard 드래그)
    [Header("세그먼트 카드 공통 기본 프리팹")]
    [SerializeField] private GameObject segmentCardBasePrefab; // 세그먼트 카드 공통 프리팹 (SegmentUpgradeCard)
    [Header("세그먼트 선택카드 전용 프리팹")]
    [SerializeField] private GameObject segmentChoiceCardPrefab; // 후보 선택 1단계 전용 프리팹
    [SerializeField] private CardUiPrefabReferences prefabReferences; // Resources 중앙 참조
    // 안건준 추가 - 0623 : 세그먼트 카드 아이콘 크기 조절 (0=원본, -100=절반, +100=두배)
    [Header("세그먼트 카드 아이콘 크기 조절")]
    [Range(-100f, 100f)][SerializeField] private float segmentCardIconSizeOffset = 0f; // 세그먼트 아이콘 크기

    // 안건준 추가 - 0623 : 카드 등급별 VFX 이팩트 (같은 오브젝트의 CardEffect 컴포넌트)
    [SerializeField] private CardEffect cardEffect; // 카드 등급 이팩트 컴포넌트

    // 안건준 추가 - 0624 : 카드 사운드 매니저
    [SerializeField] private CardSoundManager cardSound;

    [Header("세그먼트 강화 카탈로그")]
    [SerializeField] private WeaponCatalogAsset weaponCatalogAsset; // 무기 강화 2단계 카탈로그

    [Header("레어 카드 등장 확률")]
    [Tooltip("레어: 보너스 2배, 노란색 — 스탯 카드·세그먼트 무기 강화 카드 모두 적용")]
    [Range(0f, 100f)][SerializeField] private float rareCardChancePercent = 30f; // 레어 등장 확률(%)
    [Header("유니크 카드 등장 확률")]
    [Tooltip("유니크: 보너스 3배, 초록색 — 스탯 카드·세그먼트 무기 강화 카드 모두 적용")]
    [Range(0f, 100f)][SerializeField] private float uniqueCardChancePercent = 10f; // 유니크 등장 확률(%)

    [Header("스탯 카드 선택 가중치")]
    [Tooltip("직전에 선택한 카드 프리팹이 다음 선택지에 더 자주 나오도록 추가 가중치")]
    [Min(0f)][SerializeField] private float baseCardSpawnWeight = 100f; // 모든 카드 기본 가중치
    [Min(0f)][SerializeField] private float selectedCardWeightBonus = 50f; // 직전 선택 카드 추가 가중치

    private GameObject lastSelectedStatCardPrefab; // 직전 선택한 스탯 카드 프리팹 (다음 뽑기 가중치용)

    [Header("카드 생성 슬롯")]
    [SerializeField] private RectTransform[] cardSlots = System.Array.Empty<RectTransform>(); // 카드 생성 위치
    [Min(1)][SerializeField] private int cardsToSpawn = 3; // 한 번에 생성할 카드 수

    [Header("카드 연출")]
    [SerializeField] private float startYOffset = -80.0f; // 등장 시작 Y 오프셋
    [SerializeField] private float hoverScale = 1.09f; // 마우스 오버 배율

    [Header("카드 선택 후 닫기 (A/B 공통)")]
    [Tooltip("선택 카드만 보여준 뒤 패널을 닫기까지 대기(초). 기존 0.5")]
    [SerializeField] private float selectionCloseHoldSeconds = 0.15f; // 선택 후 홀드
    [Tooltip("선택 카드 강조 트윈 — 커지는 시간")]
    [SerializeField] private float selectionSelectUpSeconds = 0.15f; // 기존 0.2
    [Tooltip("선택 카드 강조 트윈 — 원래 크기 복귀")]
    [SerializeField] private float selectionSelectDownSeconds = 0.1f; // 기존 0.15
    [Tooltip("레벨업 패널 페이드 아웃 (일시정지 해제 직전)")]
    [SerializeField] private float selectionPanelCloseFadeSeconds = 0.15f; // 기존 LevelUpUi 0.25

    [Header("레벨업 패널 감지")]
    [SerializeField] private CanvasGroup levelUpPanelCanvasGroup; // LevelUpPanel Canvas Group

    [Header("레벨업 UI")]
    [SerializeField] private LevelUpUi levelUpUi; // 비워두면 자동 검색

    [Header("세그먼트 무기 강화선택 조건")]
    [Tooltip("A모드 (체크): 세그먼트 선택 → 선택한 세그먼트의 강화 카드 선택 / B모드 (해제): 보유 세그먼트 강화만 랜덤 3장 (미보유 제외)")]
    [SerializeField] private bool useSegmentSelectWeaponEnhanceFlow = true; // A 기준 — 세그먼트 선택 후 강화
    [Header("A 모드 (체크) 세그먼트 기본 가중치 ")]
    [Min(0f)][SerializeField] private float weaponEnhanceSegmentBaseWeight = 100f; // 보유 0개도 이 가중치로 후보 가능
    [Header("A 모드 (체크) 세그먼트 개수 비례 가중치 증가 ")]
    [Min(0f)][SerializeField] private float weaponEnhanceSegmentWeightPerOwned = 50f; // 세그먼트 개수 비례 가중치

    [Header("디버그 세그먼트 갯수 표기")]
    [SerializeField] private bool logPlayerSegmentCounts = true; // 세그먼트 추가/레벨업 후 현재 구성 출력

    // 건춘추가 - 0621 ======
    [Header("세그먼트 무기 스탯 UI (TMP)")]
    [Tooltip("켜면 아래 Stat Text에 선택한 세그먼트 기본+강화 합산 스탯 표시")]
    [SerializeField] private bool showSegmentWeaponStatUi = true; // 스탯 UI 갱신
    [SerializeField] private TextMeshProUGUI segmentWeaponStatText; // 스탯 표시용 TMP 1개
    [SerializeField] private SegmentWeaponStatViewTarget segmentWeaponStatViewTarget = SegmentWeaponStatViewTarget.Cannon; // 초기 표시 세그먼트

    // 안건준 추가 - 0622 ======
    [Header("자동 카드 선택 (자동궤도 모드 연동)")]
    [Tooltip("켜면 자동궤도(AutoOrbit) 중 카드 선택지가 열릴 때 자동으로 1장을 선택합니다")]
    [SerializeField] private bool autoSelectInAutoOrbit = true; // 자동궤도 중 자동선택 활성화
    [Tooltip("카드가 펼쳐진 뒤 자동선택까지 대기 시간(초)")]
    [Min(0f)]
    [SerializeField] private float autoSelectDelay = 1f; // 자동선택 대기 시간
    private Coroutine autoSelectRoutine; // 자동선택 코루틴 참조
    // 안건준 추가 - 0622 ======

    // 안건준 추가 - 0622 ======
    [Header("세그먼트 리스트 호버 UI")]
    [Tooltip("카드 패널이 열릴 때 함께 활성화되는 트리거 바 (Hierarchy의 Segment List Popup)")]
    [SerializeField] private GameObject segmentListPopup; // 호버 트리거
    [Tooltip("Popup 호버 시 표시되는 세그먼트 목록 (Hierarchy의 Segment List)")]
    [SerializeField] private GameObject segmentList; // 호버 시 표시
    [Tooltip("Segment List 안 Scroll View 텍스트 — 장착 세그먼트 이름 : 개수 표시")]
    [SerializeField] private TextMeshProUGUI segmentListText; // 장착 세그먼트 이름:개수 TMP
    [Tooltip("Segment List > Viewport > Content RectTransform — 스크롤 높이 자동 조정용")]
    [SerializeField] private RectTransform segmentListContent; // 스크롤 Content RT
    // 안건준 추가 - 0622 ======

    [Header("마법책 리롤 UI")]
    [SerializeField] private GameObject rerollUiRoot; // 씬에 배치된 리롤 UI 루트
    [SerializeField] private Button rerollButton; // 정사각형 리롤 버튼
    [SerializeField] private Image rerollButtonImage; // 버튼 배경 이미지
    [SerializeField] private Sprite rerollButtonActiveSprite; // 리롤 가능 상태 이미지
    [SerializeField] private Sprite rerollButtonDisabledSprite; // 리롤 불가 상태 이미지
    [SerializeField] private TextMeshProUGUI rerollCountText; // 남은 리롤 횟수

    //전찬우 수정-0622
    private enum SegmentWeaponStatViewTarget // 스탯 UI 표시 대상
    {
        Cannon = 0, // SG01_Cannon
        Missile = 1, // SG02_Missile
        Trebuchet = 2, // SG03_Trebuchet
        SawLauncher = 3, // SG04_SawLauncher
        Flamethrower = 4, // SG05_Flamethrower
        LightningObelisk = 5, // SG20_LightningObelisk
        FireballTower = 6 // SG21_FireballTower
    }

    [System.Flags]
    private enum SegmentWeaponStatDisplayFlags
    {
        None = 0,
        BaseDamage = 1 << 0,
        ProjectileSpeed = 1 << 1,
        SearchRange = 1 << 2,
        Cooldown = 1 << 3,
        ProjectileCount = 1 << 4,
        PierceCount = 1 << 5,
        ExplosionRadius = 1 << 6,
        MaxChainDepth = 1 << 7,
        ChainRange = 1 << 8,
        ChainDamageFalloff = 1 << 9,
        SideConeAngle = 1 << 10,
        LaserDuration = 1 << 11,
        LaserTickInterval = 1 << 12,
        LandingRollDistance = 1 << 13,
        LandingRollDuration = 1 << 14,
        SawPierceDamageRatio = 1 << 15
    }

    private readonly struct SegmentWeaponStatDebugContext
    {
        public readonly string SegmentId;
        public readonly string Title;
        public readonly int Level;
        public readonly SegmentAttackProfile Profile;
        public readonly WeaponStatBonusData Bonus;
        public readonly SegmentWeaponStatDisplayFlags DisplayFlags;

        public SegmentWeaponStatDebugContext(
            string segmentId,
            string title,
            int level,
            SegmentAttackProfile profile,
            WeaponStatBonusData bonus,
            SegmentWeaponStatDisplayFlags displayFlags)
        {
            SegmentId = segmentId;
            Title = title;
            Level = level;
            Profile = profile;
            Bonus = bonus;
            DisplayFlags = displayFlags;
        }

        public bool HasProfile => Profile != null;
    }
    // 건춘추가 - 0621 ======

    private readonly List<SpawnedCardEntry> spawnedCards = new List<SpawnedCardEntry>(); // 생성된 카드 목록
    private readonly Dictionary<string, int> rerollCountsBySegmentId = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); // 마법책 개수 집계용
    private const string MagicBookRerollSegmentId = "SG55_MagicBook"; // 마법책 세그먼트 ID
    private bool spawnedForCurrentOpen; // 이번 패널 오픈에서 생성 완료 여부
    private bool isProcessingSelection; // 선택 처리 중
    private bool rerollAllowedForCurrentChoices; // 현재 카드 묶음 리롤 가능 여부
    private int remainingRerollCount; // 이번 카드 선택창 남은 리롤
    private static bool loggedWeaponEnhancementInitial; // 무기 강화 초기 디버그 1회
    private LevelUpCardPhase currentSpawnPhase = LevelUpCardPhase.StatUpgrade; // 이번 레벨업 카드 종류
    private string selectedSegmentWeaponStatId; // 카드 선택으로 갱신되는 디버그 표시 대상
    private CoreStatProvider segmentWeaponStatSubscribedCore; // 스탯 변경 구독 대상
    private Coroutine hideSegmentListCoroutine; // 안건준 추가 - 0622 — 코루틴 참조 (혹시 중복 방지용)
    private CardUiPrefabReferences cachedPrefabReferences; // Resources fallback 캐시
    private Sprite cachedTierFrameNormalSprite; // 일반 등급 카드 프레임
    private Sprite cachedTierFrameRareSprite; // 레어 등급 카드 프레임
    private Sprite cachedTierFrameUniqueSprite; // 유니크 등급 카드 프레임

    private enum LevelUpCardPhase
    {
        StatUpgrade = 0, // 스탯 강화
        WeaponEnhance = 1, // 세그먼트 무기 강화 (A: 세그먼트 선택→강화 / B: 랜덤 강화)
        SegmentAction = 2 // 세그먼트 추가/레벨업
    }

    // 세그먼트 ADD 풀에서 후보/액션 카드 구분
    private enum SegmentCardRole
    {
        None = 0, // 일반 카드
        Candidate = 1, // 세그먼트 후보 카드
        AddAction = 2, // 세그먼트 추가 카드
        LevelUpAction = 3, // 세그먼트 레벨업 카드
        Empty = 4, // 후보 없음 카드
        EnhanceChoice = 5 // 무기 강화 선택 카드 (2단계)
    }

    private void Awake()
    {
        ResolveManagerReferences(); // 참조 보강
        SetupSegmentListHoverUi(); // 안건준 추가 - 0622 — 호버 브릿지 연결 + 기본 비활성
        SetupRerollUi(); // 마법책 리롤 버튼 연결

        // TMP 줄바꿈 재귀 오류 방지 — 긴 텍스트가 들어가는 TMP에 word wrap 비활성
        if (segmentListText       != null) segmentListText.enableWordWrapping       = false;
        if (segmentWeaponStatText != null) segmentWeaponStatText.enableWordWrapping = false;
        if (rerollCountText       != null) rerollCountText.textWrappingMode         = TextWrappingModes.NoWrap;

        // 안건준 추가 - 0624 : CardSoundManager 자동 연결 (없으면 자동 생성)
        if (cardSound == null)
            cardSound = GetComponent<CardSoundManager>();
        if (cardSound == null)
            cardSound = FindFirstObjectByType<CardSoundManager>();
        if (cardSound == null)
            cardSound = gameObject.AddComponent<CardSoundManager>();

        // 안건준 추가 - 0623 : 같은 오브젝트에 CardEffect가 있으면 자동 연결
        if (cardEffect == null)
        {
            cardEffect = GetComponent<CardEffect>();
            if (cardEffect != null)
            {
                Debug.Log("[CardUI] CardEffect 자동 연결 완료", this);
            }
        }
    }

    private void Start()
    {
        LogWeaponEnhancementInitialOnce(); // 시작 시 무기 강화 초기값 1회 출력
        TrySubscribeSegmentCountDebug(); // [임시] 세그먼트 추가/제거 시 디버그
        TrySubscribeSegmentWeaponStatDebug(); // 코어 스탯 변경 시 디버그 갱신
        RefreshSegmentWeaponStatUi(); // 건춘추가 - 0621 ====== 세그먼트 스탯 TMP 초기 표시
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(HandleRerollButtonClicked); // 리스너 정리
        }

        UnsubscribeSegmentCountDebug(); // [임시] 구독 해제
        UnsubscribeSegmentWeaponStatDebug(); // 스탯 변경 구독 해제
    }

    // 건춘추가 - 0621 ======
    private void OnValidate() // Inspector에서 세그먼트 변경 시 플레이 중 미리보기
    {
        if (Application.isPlaying)
        {
            RefreshSegmentWeaponStatUi();
            RefreshRerollUi();
        }
    }
    // 건춘추가 - 0621 ======

    private void Update()
    {
        TrySubscribeSegmentCountDebug(); // Convoy 연결 늦을 때 재시도
        TrySubscribeSegmentWeaponStatDebug(); // Core 연결 늦을 때 재시도
        bool panelOpen = IsLevelUpPanelOpen(); // 패널 열림 여부
        if (panelOpen && !spawnedForCurrentOpen)
        {
            BeginRerollForPanelOpen(); // 마법책 개수만큼 이번 선택창 리롤 충전
            SpawnLevelUpCards(); // 순환 순서에 맞는 카드 생성
            spawnedForCurrentOpen = true;
            ShowSegmentListPopupOnPanelOpen(); // 안건준 추가 - 0622 — 트리거 바만 표시
            TryStartAutoSelect(); // 안건준 추가 - 0622 : 자동모드면 자동선택 코루틴 시작
            return;
        }

        if (!panelOpen && spawnedForCurrentOpen)
        {
            ClearSpawnedCards(); // 패널 닫힘 → 카드 정리
            spawnedForCurrentOpen = false;
            isProcessingSelection = false;
            currentSpawnPhase = LevelUpCardPhase.StatUpgrade; // 다음 오픈 시 재계산
            remainingRerollCount = 0; // 패널 닫힘 → 리롤 소멸
            rerollAllowedForCurrentChoices = false; // 다음 오픈 전까지 비활성
            RefreshRerollUi(); // 버튼 숨김/비활성 갱신
            HideSegmentListUi(); // 안건준 추가 - 0622 — 팝업·리스트 모두 숨김
            StopAutoSelect(); // 안건준 추가 - 0622 : 패널 닫힐 때 자동선택 코루틴 정리
            if (CoreStatProvider.Active != null && CoreStatProvider.Active.IsLevelUpChoicePending)
            {
                CoreStatProvider.Active.CancelLevelUpChoice(); // 선택 없이 닫힘 → 경험치 유지
            }
        }
    }

    public void PlayLevelUpTween()
    {
        // 안건준 추가 - 0622 : 자동궤도 모드이고 자동선택이 켜져 있으면 일시정지 없이 열기
        if (autoSelectInAutoOrbit && IsAutoOrbitActive())
        {
            ResolveLevelUpUi()?.OpenWithoutPause();
        }
        else
        {
            ResolveLevelUpUi()?.Open();
        }
    }

    private void SetupRerollUi()
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveListener(HandleRerollButtonClicked); // 중복 등록 방지
            rerollButton.onClick.AddListener(HandleRerollButtonClicked); // 씬 배치 버튼 클릭 연결
            if (rerollButtonImage == null)
            {
                rerollButtonImage = rerollButton.targetGraphic as Image;
            }

            if (rerollButtonImage == null)
            {
                rerollButtonImage = rerollButton.GetComponent<Image>();
            }
        }

        RefreshRerollUi(); // 초기 닫힘 상태 반영
    }

    private void BeginRerollForPanelOpen()
    {
        remainingRerollCount = ResolveMagicBookRerollCount(); // 현재 장착 마법책 수량
        rerollAllowedForCurrentChoices = false; // 카드 생성 전에는 비활성
        RefreshRerollUi();
    }

    private int ResolveMagicBookRerollCount()
    {
        rerollCountsBySegmentId.Clear(); // 이전 집계 제거
        ConvoyController convoy = CoreStatProvider.Active != null ? CoreStatProvider.Active.Convoy : null;
        if (convoy == null)
        {
            return 0; // 컨보이 없음
        }

        convoy.CollectAttachedSegmentCounts(rerollCountsBySegmentId); // 장착 세그먼트 ID별 수량
        return rerollCountsBySegmentId.TryGetValue(MagicBookRerollSegmentId, out int count)
            ? Mathf.Max(0, count)
            : 0;
    }

    private void HandleRerollButtonClicked()
    {
        if (!CanRerollCurrentChoices())
        {
            RefreshRerollUi(); // 클릭 불가 상태 재반영
            return;
        }

        // 안건준 추가 - 0626 : 리롤 버튼 클릭 사운드 + DOTween 효과
        cardSound?.PlayRerollClick();
        PlayRerollButtonClickTween();

        remainingRerollCount = Mathf.Max(0, remainingRerollCount - 1); // 리롤 1회 소비
        StopAutoSelect(); // 재생성 중 자동 선택 중지
        ClearSpawnedCards(); // 현재 후보 제거
        SpawnCardsForCurrentPhase(); // 같은 단계의 선택지만 다시 생성
        RefreshRerollUi();
        TryStartAutoSelect(); // 자동궤도면 새 후보 기준 자동선택 재시작
    }

    private void PlayRerollButtonClickTween()
    {
        if (rerollButton == null)
        {
            return;
        }

        RectTransform rt = rerollButton.transform as RectTransform;
        if (rt == null)
        {
            return;
        }

        rt.DOKill();
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(rt.DOScale(Vector3.one * 1.2f, selectionSelectUpSeconds).SetEase(Ease.OutBack));
        seq.Append(rt.DOScale(Vector3.one, selectionSelectDownSeconds));
    }

    private bool CanRerollCurrentChoices()
    {
        return remainingRerollCount > 0
            && rerollAllowedForCurrentChoices
            && !isProcessingSelection
            && IsLevelUpPanelOpen(); // 선택 처리 중/패널 닫힘 방지
    }

    private void RefreshRerollUi()
    {
        bool panelOpen = IsLevelUpPanelOpen(); // CanvasGroup 기준 표시 여부
        if (rerollUiRoot != null)
        {
            rerollUiRoot.SetActive(panelOpen); // 패널이 열릴 때만 표시
        }

        if (rerollCountText != null)
        {
            rerollCountText.text = $"남은 {remainingRerollCount}"; // 우측 남은 횟수
        }

        if (rerollButton != null)
        {
            bool canReroll = CanRerollCurrentChoices(); // 가능할 때만 클릭
            rerollButton.interactable = canReroll;
            ApplyRerollButtonVisual(canReroll);
        }
    }

    private void ApplyRerollButtonVisual(bool canReroll)
    {
        if (rerollButtonImage == null)
        {
            return;
        }

        Sprite sprite = canReroll ? rerollButtonActiveSprite : rerollButtonDisabledSprite;
        if (sprite == null)
        {
            return;
        }

        rerollButtonImage.sprite = sprite;
        rerollButtonImage.color = Color.white;
        rerollButtonImage.preserveAspect = true;
    }

    // 안건준 추가 - 0622 : 현재 자동궤도 모드 여부 확인
    private bool IsAutoOrbitActive()
    {
        TeamProject01.Gameplay.ConvoyController convoy =
            FindFirstObjectByType<TeamProject01.Gameplay.ConvoyController>();
        return convoy != null && convoy.IsAutoOrbitActive;
    }

    private void NotifySpawnedCardClicked(SpawnedCardEntry entry)
    {
        HandleCardClicked(entry); // 생성 카드 클릭
    }

    private void NotifySpawnedCardPointerEnter(SpawnedCardEntry entry)
    {
        if (entry == null || !entry.IsClickable)
        {
            return;
        }

        entry.RootTransform.DOKill();
        entry.RootTransform.DOScale(entry.OriginalScale * hoverScale, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        // 이팩트 컨테이너도 동일 배율로 확대
        cardEffect?.OnCardHoverEnter(entry.Root, hoverScale);
    }

    private void NotifySpawnedCardPointerExit(SpawnedCardEntry entry)
    {
        if (entry == null || !entry.IsClickable)
        {
            return;
        }

        entry.RootTransform.DOKill();
        entry.RootTransform.DOScale(entry.OriginalScale, 0.15f)
            .SetEase(Ease.InQuad)
            .SetUpdate(true);

        // 이팩트 컨테이너도 원래 크기로 복원
        cardEffect?.OnCardHoverExit(entry.Root);
    }

    private void ResolveManagerReferences()
    {
        if (levelUpUi == null)
        {
            levelUpUi = FindFirstObjectByType<LevelUpUi>();
        }

        if (levelUpPanelCanvasGroup == null && levelUpUi != null)
        {
            levelUpPanelCanvasGroup = levelUpUi.GetComponent<CanvasGroup>();
        }
    }

    private bool IsLevelUpPanelOpen()
    {
        return levelUpPanelCanvasGroup != null
            && levelUpPanelCanvasGroup.blocksRaycasts
            && levelUpPanelCanvasGroup.interactable;
    }

    private void SpawnLevelUpCards()
    {
        ClearSpawnedCards();
        rerollAllowedForCurrentChoices = false; // 기본 비활성

        if (cardSlots == null || cardSlots.Length == 0)
        {
            Debug.LogWarning("[CardUI] 카드 생성 슬롯이 비어 있습니다.", this);
            RefreshRerollUi();
            return;
        }

        currentSpawnPhase = ResolveLevelUpCardPhase(); // 스탯 → 무기강화 → 세그먼트 3종 순환
        rerollAllowedForCurrentChoices = true; // 1차 랜덤 선택지만 리롤 가능
        SpawnCardsForCurrentPhase();
        RefreshRerollUi();
    }

    private void SpawnCardsForCurrentPhase()
    {
        switch (currentSpawnPhase)
        {
            case LevelUpCardPhase.WeaponEnhance:
                if (useSegmentSelectWeaponEnhanceFlow)
                {
                    SpawnWeaponEnhanceCandidateCards(); // A: 세그먼트 선택 1단계
                }
                else
                {
                    SpawnRandomWeaponEnhancementCards(); // B: 강화 카드 랜덤 (세그먼트 선택 없음)
                }

                return;
            case LevelUpCardPhase.SegmentAction:
                SpawnSegmentCandidateCards(); // 1단계: 세그먼트 후보 → 추가/레벨업
                return;
            default:
                SpawnStatUpgradeCards(); // 스탯 강화 3장
                return;
        }
    }

    private void SpawnStatUpgradeCards()
    {
        GameObject[] sourcePrefabs = statUpgradeCards; // 스탯 카드 풀
        string poolName = "Stat Upgrade"; // 로그용 풀 이름

        if (sourcePrefabs == null || sourcePrefabs.Length == 0)
        {
            Debug.LogWarning($"[CardUI] {poolName} 카드 프리팹이 비어 있습니다.", this);
            return;
        }

        List<GameObject> pool = BuildPrefabPool(sourcePrefabs);
        if (pool.Count == 0)
        {
            return;
        }

        int spawnCount = Mathf.Min(cardsToSpawn, cardSlots.Length, pool.Count);
        List<GameObject> picked = PickWeightedStatPrefabs(pool, spawnCount); // 가중치 랜덤으로 3장 선택

        for (int i = 0; i < picked.Count; i++)
        {
            SpawnedCardEntry entry = CreateStatUpgradeCard(picked[i], cardSlots[i]); // 등급·프리팹 resolve 후 생성
            if (entry != null)
            {
                spawnedCards.Add(entry); // 생성 목록 등록
            }
        }

        PlaySpawnOpenTween(spawnedCards); // 등장 연출
    }

    private SpawnedCardEntry CreateStatUpgradeCard(GameObject sourcePrefab, RectTransform slot)
    {
        if (sourcePrefab == null || slot == null)
        {
            return null; // 프리팹/슬롯 없음
        }

        StatUpgrade templateStat = GetStatUpgradePresentation(sourcePrefab); // statUpgradeCards 풀 프리팹
        StatUpgrade.StatCardTier tier = StatUpgrade.RollTier(rareCardChancePercent, uniqueCardChancePercent); // 등급 선정
        StatUpgrade.CardSpawnResolve resolve = templateStat != null
            ? templateStat.ResolveCardSpawn(tier, sourcePrefab)
            : new StatUpgrade.CardSpawnResolve(sourcePrefab); // StatUpgrade 없으면 기본
        GameObject spawnPrefab = resolve.Prefab != null ? resolve.Prefab : sourcePrefab; // fallback
        SpawnedCardEntry entry = CreateSpawnedCard(spawnPrefab, slot, sourcePrefab, skipStatUpgradeRoll: true); // 생성
        if (entry == null)
        {
            return null; // 생성 실패
        }

        if (entry.StatUpgrade != null)
        {
            if (templateStat != null && spawnPrefab != sourcePrefab)
            {
                entry.StatUpgrade.CopyStatValuesFrom(templateStat); // 등급 프리팹 → 풀 프리팹 수치 복사
            }

            entry.StatUpgrade.ApplySpawnTier(tier); // 등급·배율 반영
            ApplyTierCardFrame(entry.Root, tier); // 등급별 카드 프레임 교체
            ApplyTierPrefixToCardDescription(entry.Root, tier, isReduction: false); // 등급 기호 제거 및 설명 정리
        }

        return entry;
    }

    private static StatUpgrade GetStatUpgradePresentation(GameObject prefab) // statUpgradeCards 프리팹의 StatUpgrade (Instantiate 전)
    {
        if (prefab == null)
        {
            return null; // 프리팹 없음
        }

        StatUpgrade presentation = prefab.GetComponent<StatUpgrade>(); // 루트
        if (presentation != null)
        {
            return presentation;
        }

        return prefab.GetComponentInChildren<StatUpgrade>(true); // 자식 fallback
    }

    private static LevelUpCardPhase ResolveLevelUpCardPhase() // CoreStatProvider 순환 인덱스 → 이번 카드 종류
    {
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        int cycleIndex = core != null ? core.LevelUpCardCycleIndex : 0; // 레벨업 선택 완료 횟수
        return (LevelUpCardPhase)(cycleIndex % 3); // 0=스탯, 1=무기강화, 2=세그먼트
    }

    // 세그먼트 ADD 풀: 카탈로그 후보 3장 생성, 부족하면 없음 카드 표시
    private void SpawnSegmentCandidateCards()
    {
        GameObject template = GetSegmentChoiceCardTemplate(); // 세그먼트 선택 전용 프리팹
        if (template == null)
        {
            Debug.LogWarning("[CardUI] Add Segment 카드 프리팹이 비어 있습니다.", this); // 템플릿 누락
            return;
        }

        List<SegmentCatalogEntry> candidates = new List<SegmentCatalogEntry>(); // 카탈로그 후보
        if (CoreStatProvider.Active != null)
        {
            CoreStatProvider.Active.TryGetSegmentChoiceCandidates(candidates); // 추가/레벨업 가능한 후보 수집
        }

        int spawnCount = Mathf.Min(cardsToSpawn, cardSlots.Length); // 표시할 카드 수
        List<SegmentCatalogEntry> picked = PickRandomSegmentEntries(candidates, spawnCount); // 후보 랜덤 선택
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = GetSegmentChoiceCardTemplate(); // 후보 선택 1단계 전용 템플릿
            SpawnedCardEntry entry = CreateSpawnedCard(prefab, cardSlots[i]); // 카드 생성
            if (entry == null)
            {
                continue; // 생성 실패
            }

            if (i < picked.Count)
            {
                ConfigureSegmentCandidateEntry(entry, picked[i]); // 실제 후보 카드
            }
            else
            {
                ConfigureEmptySegmentEntry(entry); // 후보 부족 → 없음 카드
            }

            spawnedCards.Add(entry); // 생성 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 기존 DOTween 등장 연출 재사용
    }

    // 무기 강화 1단계 - 강화 가능한 세그먼트 후보 3장
    private void SpawnWeaponEnhanceCandidateCards()
    {
        GameObject template = GetSegmentChoiceCardTemplate(); // 세그먼트 선택 전용 프리팹
        if (template == null)
        {
            Debug.LogWarning("[CardUI] Add Segment 카드 프리팹이 비어 있습니다.", this);
            return;
        }

        List<SegmentCatalogEntry> candidates = new List<SegmentCatalogEntry>(); // 강화 후보
        if (CoreStatProvider.Active != null)
        {
            CoreStatProvider.Active.TryGetWeaponEnhanceChoiceCandidates(candidates); // Segment Catalog 풀
        }

        int spawnCount = Mathf.Min(cardsToSpawn, cardSlots.Length); // 표시할 카드 수
        Dictionary<string, int> ownedCountsBySegmentId = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); // ID별 보유 개수
        ConvoyController convoy = CoreStatProvider.Active != null ? CoreStatProvider.Active.Convoy : null;
        if (convoy != null)
        {
            convoy.CollectAttachedSegmentCounts(ownedCountsBySegmentId); // 캐논 5개 등 집계
        }

        List<SegmentCatalogEntry> picked = PickWeightedWeaponEnhanceSegmentEntries(candidates, spawnCount, ownedCountsBySegmentId); // 보유 개수 가중치
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = GetSegmentChoiceCardTemplate(); // 후보 선택 1단계 전용 템플릿
            SpawnedCardEntry entry = CreateSpawnedCard(prefab, cardSlots[i]); // 카드 생성
            if (entry == null)
            {
                continue; // 생성 실패
            }

            if (i < picked.Count)
            {
                ConfigureWeaponEnhanceCandidateEntry(entry, picked[i]); // 강화 대상 세그먼트
            }
            else
            {
                ConfigureEmptySegmentEntry(entry); // 후보 부족
            }

            spawnedCards.Add(entry); // 생성 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 등장 연출
    }

    // B 기준 — 보유 세그먼트 강화만 랜덤 3장 (세그먼트 선택 단계 없음)
    private void SpawnRandomWeaponEnhancementCards()
    {
        List<WeaponDefinition> pool = new List<WeaponDefinition>(); // 카탈로그 강화 풀
        BuildWeaponEnhancementPool(pool, ownedSegmentsOnly: true); // B: Convoy에 붙은 세그먼트 ID만
        int resolvedLevelDelta = 1; // 레벨업 1회 소비
        int spawnCount = Mathf.Min(cardsToSpawn, cardSlots.Length); // 표시할 카드 수
        List<WeaponDefinition> picked = PickRandomWeaponDefinitions(pool, spawnCount); // 중복 없이 우선 뽑기

        for (int i = 0; i < spawnCount; i++)
        {
            RectTransform slot = cardSlots[i]; // 슬롯별 배치
            if (slot == null)
            {
                continue; // 슬롯 없음
            }

            GameObject prefab = GetSegmentCardTemplate(i); // 세그먼트 카드 프리팹 재사용
            SpawnedCardEntry entry;
            if (i < picked.Count)
            {
                entry = CreateWeaponEnhancementCard(picked[i], prefab, i, slot, resolvedLevelDelta); // 등급·프리팹 resolve 후 생성
            }
            else
            {
                entry = CreateSpawnedCard(prefab, slot, prefab); // 풀 부족 — 빈 껍데기
                if (entry != null)
                {
                    ConfigureEmptySegmentEntry(entry); // 없음 카드
                }
            }

            if (entry == null)
            {
                continue; // 생성 실패
            }

            spawnedCards.Add(entry); // 생성 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 등장 연출
    }

    private void BuildWeaponEnhancementPool(List<WeaponDefinition> results, bool ownedSegmentsOnly = false) // WeaponCatalog → 유효 강화 목록
    {
        results.Clear(); // 이전 결과 제거
        WeaponCatalogAsset catalog = ResolveWeaponCatalog(); // 카탈로그
        if (catalog == null)
        {
            return; // 카탈로그 없음
        }

        catalog.AppendAllEnhancements(results); // Cannon/Missile/AdditionalSegments 전체
        if (!ownedSegmentsOnly)
        {
            return; // A·기타 — 필터 없음
        }

        FilterWeaponEnhancementPoolByOwnedSegments(results); // B — 보유 세그먼트 TargetSegmentId 만
    }

    private static void FilterWeaponEnhancementPoolByOwnedSegments(List<WeaponDefinition> pool) // Convoy 보유 ID 외 강화 제거
    {
        if (pool == null || pool.Count == 0)
        {
            return; // 풀 없음
        }

        HashSet<string> ownedSegmentIds = CollectOwnedSegmentIds(); // SG01_Cannon 등
        if (ownedSegmentIds.Count == 0)
        {
            pool.Clear(); // 붙은 세그먼트 없음
            return;
        }

        for (int i = pool.Count - 1; i >= 0; i--)
        {
            WeaponDefinition definition = pool[i]; // 후보 강화
            if (definition == null || !ownedSegmentIds.Contains(definition.NormalizedTargetSegmentId))
            {
                pool.RemoveAt(i); // 미보유 세그먼트 강화 제외
            }
        }
    }

    private static HashSet<string> CollectOwnedSegmentIds() // ConvoySegments 에 붙은 세그먼트 ID 집합
    {
        HashSet<string> ownedSegmentIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase); // 대소문자 무시
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        ConvoyController convoy = core != null ? core.Convoy : null; // 플레이어 컨보이
        if (convoy == null)
        {
            return ownedSegmentIds; // 빈 집합
        }

        Dictionary<string, int> countsBySegmentId = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); // ID별 개수
        convoy.CollectAttachedSegmentCounts(countsBySegmentId); // 1개 이상이면 보유
        foreach (string segmentId in countsBySegmentId.Keys)
        {
            if (!string.IsNullOrWhiteSpace(segmentId))
            {
                ownedSegmentIds.Add(segmentId.Trim()); // 보유 목록 등록
            }
        }

        return ownedSegmentIds;
    }

    private static void AppendWeaponDefinitions(List<WeaponDefinition> results, WeaponDefinition[] entries) // 배열 → 풀 등록
    {
        if (entries == null || entries.Length == 0)
        {
            return; // 항목 없음
        }

        for (int i = 0; i < entries.Length; i++)
        {
            WeaponDefinition definition = entries[i]; // 후보
            if (definition == null || !definition.HasAnyStatBonus || !definition.HasTarget)
            {
                continue; // 적용 불가 항목 제외
            }

            results.Add(definition); // 풀에 추가
        }
    }

    private static List<WeaponDefinition> PickRandomWeaponDefinitions(List<WeaponDefinition> pool, int count) // 풀에서 랜덤 N장
    {
        List<WeaponDefinition> picked = new List<WeaponDefinition>(count); // 결과
        if (pool == null || pool.Count == 0 || count <= 0)
        {
            return picked; // 빈 결과
        }

        List<WeaponDefinition> working = new List<WeaponDefinition>(pool); // 중복 방지용 임시 풀
        int pickCount = Mathf.Min(count, working.Count); // 풀보다 많이 요청하면 풀 크기만큼
        for (int i = 0; i < pickCount; i++)
        {
            int index = UnityEngine.Random.Range(0, working.Count); // 랜덤 인덱스
            picked.Add(working[index]); // 선택
            working.RemoveAt(index); // 중복 제외
        }

        while (picked.Count < count && pool.Count > 0)
        {
            picked.Add(pool[UnityEngine.Random.Range(0, pool.Count)]); // 풀보다 많으면 중복 허용
        }

        return picked;
    }

    private void ConfigureWeaponEnhanceCandidateEntry(SpawnedCardEntry entry, SegmentCatalogEntry catalogEntry)
    {
        entry.SegmentRole = SegmentCardRole.Candidate; // 1단계 후보 (무기 강화 흐름)
        entry.SegmentCatalogEntry = catalogEntry; // 선택 후 2단계에 전달
        entry.SegmentId = catalogEntry.NormalizedId; // 대상 세그먼트 ID
        entry.LevelDelta = entry.SegmentAddCard != null ? entry.SegmentAddCard.LevelDelta : 1; // 소비 레벨
        entry.CanSelect = catalogEntry.HasId; // 카탈로그 풀 — 세그먼트 추가와 동일하게 선택
        entry.SegmentAddCard?.ConfigureCandidate(catalogEntry); // 세그먼트 추가와 동일 UI
        // 안건준 추가 - 0623 : 커스텀 프리팹 Card_Text / DescText + 현재 레벨 아이콘 주입
        if (entry.Root != null)
        {
            string segId = catalogEntry.NormalizedId;
            int currentLevel = CoreStatProvider.Active?.Convoy?.GetCurrentSegmentLevel(segId) ?? 1;
            Sprite icon = GetSegmentIconSprite(segId, currentLevel);
            string title = string.IsNullOrWhiteSpace(catalogEntry.DisplayName) ? segId : catalogEntry.DisplayName;
            string desc = string.IsNullOrWhiteSpace(catalogEntry.Description) ? $"{catalogEntry.NormalizedId} 선택" : catalogEntry.Description;
            ApplyCardTextsDirectly(entry.Root, title, desc, icon, GetSegmentIconSizeOffset(segId));
        }
    }

    // 세그먼트 추가/레벨업 2차 선택 카드 2장 생성
    private void SpawnSegmentActionCards(SegmentCatalogEntry entry, int levelDelta, bool canAdd, bool canLevelUp)
    {
        rerollAllowedForCurrentChoices = false; // 추가/레벨업 결정 화면은 리롤 제외
        RefreshRerollUi();
        ClearSpawnedCards(); // 후보 카드 제거
        RectTransform parentSlot = GetCenteredActionParentSlot(); // 2장 중앙 배치 기준
        if (parentSlot == null)
        {
            return;
        }

        bool[] selectable = { canAdd, canLevelUp }; // 각 액션 선택 가능 여부
        SegmentCardRole[] roles = { SegmentCardRole.AddAction, SegmentCardRole.LevelUpAction }; // 액션 종류
        int spawnCount = 2; // 추가/레벨업 2장
        for (int i = 0; i < spawnCount; i++)
        {
            SegmentCardRole role = roles[i]; // 추가 / 레벨업
            GameObject defaultTemplate = GetSegmentCardTemplate(i); // 기본 껍데기
            GameObject spawnPrefab = ResolveSegmentActionCardPrefab(role, defaultTemplate); // CardUI 교체 프리팹 (있을 때)
            SpawnedCardEntry spawnedEntry = CreateSpawnedCard(spawnPrefab, parentSlot, defaultTemplate); // 중앙 슬롯에 생성
            if (spawnedEntry == null)
            {
                continue; // 생성 실패
            }

            ConfigureSegmentActionEntry(spawnedEntry, entry, levelDelta, role, selectable[i]); // 액션 데이터 주입
            ApplyCenteredActionCardPosition(spawnedEntry, i, spawnCount); // 좌우 중앙 배치
            spawnedCards.Add(spawnedEntry); // 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 등장 연출
    }

    // 2단계 - 선택 세그먼트에 맞는 무기 강화 카드 생성
    private void SpawnSegmentEnhancementCards(string targetSegmentId, int levelDelta)
    {
        rerollAllowedForCurrentChoices = false; // 선택 세그먼트의 강화 카드 화면은 리롤 제외
        RefreshRerollUi();
        ClearSpawnedCards(); // 2단계 카드 제거
        int resolvedLevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        WeaponCatalogAsset catalog = ResolveWeaponCatalog(); // 카탈로그
        WeaponDefinition[] enhancements = System.Array.Empty<WeaponDefinition>(); // 기본값
        bool hasEnhancements = catalog != null
            && !string.IsNullOrWhiteSpace(targetSegmentId)
            && catalog.TryGetEnhancementsForSegment(targetSegmentId, out enhancements)
            && enhancements != null
            && enhancements.Length > 0; // 카탈로그 조회

        int spawnCount = Mathf.Min(cardsToSpawn, cardSlots.Length); // 3장
        for (int i = 0; i < spawnCount; i++)
        {
            RectTransform slot = cardSlots[i]; // 슬롯별 배치
            if (slot == null)
            {
                continue; // 슬롯 없음
            }

            GameObject defaultTemplate = GetSegmentCardTemplate(i); // 세그먼트 카드 프리팹 재사용
            WeaponDefinition definition = hasEnhancements && i < enhancements.Length ? enhancements[i] : null;
            SpawnedCardEntry entry = CreateWeaponEnhancementCard(definition, defaultTemplate, i, slot, resolvedLevelDelta, targetSegmentId); // 등급·프리팹 resolve
            if (entry == null)
            {
                continue; // 생성 실패
            }

            if (definition == null)
            {
                ConfigureEmptySegmentEntry(entry); // 강화 없음/부족
            }

            spawnedCards.Add(entry); // 생성 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 등장 연출
    }

    private WeaponCatalogAsset ResolveWeaponCatalog() // CardUI 또는 CoreStatProvider 카탈로그
    {
        if (weaponCatalogAsset != null)
        {
            return weaponCatalogAsset; // Inspector 연결 우선
        }

        CoreStatProvider core = CoreStatProvider.Active; // 코어 fallback
        return core != null ? core.WeaponCatalogAsset : null;
    }

    private bool CanShowWeaponEnhancements(string segmentId) // 무기 강화 카탈로그 존재 여부
    {
        WeaponCatalogAsset catalog = ResolveWeaponCatalog(); // 카탈로그
        return catalog != null
            && !string.IsNullOrWhiteSpace(segmentId)
            && catalog.TryGetEnhancementsForSegment(segmentId, out WeaponDefinition[] enhancements)
            && enhancements != null
            && enhancements.Length > 0; // 강화 목록 존재
    }

    private void ConfigureWeaponEnhancementEntry(
        SpawnedCardEntry entry,
        WeaponDefinition definition,
        string targetSegmentId,
        int levelDelta,
        StatUpgrade.StatCardTier tier)
    {
        entry.SegmentRole = SegmentCardRole.EnhanceChoice; // 2단계 강화 카드
        entry.WeaponDefinition = definition; // 선택 강화
        entry.SegmentId = targetSegmentId; // 대상 세그먼트
        entry.LevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        entry.CanSelect = definition != null && definition.HasAnyStatBonus; // 선택 가능
        string tieredDescription = BuildTierPrefixedWeaponDescription(definition, tier); // (N) 값 치환이 반영된 설명
        entry.SegmentAddCard?.ConfigureWeaponEnhancement(definition, entry.LevelDelta, tieredDescription); // 카드 문구·아이콘
        entry.SegmentAddCard?.ApplyWeaponEnhancementTier(tier); // 등급 저장
        // 건준수정 - 0621 ======
        entry.WeaponEnhancementTier = tier; // 적용 시 등급별 수치
        // 건준수정 - 0621 ======
        ApplyTierCardFrame(entry.Root, tier); // 등급별 카드 프레임 교체

        // 안건준 추가 - 0623 : SegmentAddCard 텍스트 주입 후 실제 텍스트가 바뀌었는지 확인 — Card_Text / DescText / Image 직접 fallback
        if (definition != null && entry.Root != null)
        {
            // 현재 세그먼트 레벨 조회 → 레벨에 맞는 아이콘 선택
            int segLevel = CoreStatProvider.Active?.Convoy?.GetCurrentSegmentLevel(definition.TargetSegmentId) ?? 1;
            Sprite iconSprite = definition.GetIconSpriteForLevel(segLevel);

            if (iconSprite == null)
            {
                Debug.LogWarning($"[CardUI] '{definition.name}' 레벨 {segLevel} 아이콘 없음. " +
                    $"CardIconSpritesPerLevel 또는 CardIconSprite 를 Inspector 에서 할당하세요. (TargetSegmentId={definition.TargetSegmentId})", definition);
            }

            ApplyCardTextsDirectly(entry.Root, definition.DisplayName, tieredDescription, iconSprite, definition.CardIconSizeOffset);
        }

        if (definition != null && !definition.HasAnyStatBonus)
        {
            Debug.LogWarning($"[CardUI] 강화 카드 '{definition.name}' 수치가 0 입니다. Inspector 에서 BaseDamage/ProjectileSpeed/PierceCount/ExplosionRadius 를 확인하세요.", definition);
        }
    }

    private SpawnedCardEntry CreateWeaponEnhancementCard(
        WeaponDefinition definition,
        GameObject defaultTemplate,
        int slotIndex,
        RectTransform slot,
        int levelDelta,
        string targetSegmentId = null)
    {
        if (defaultTemplate == null || slot == null)
        {
            return null; // 템플릿/슬롯 없음
        }

        if (definition == null)
        {
            return CreateSpawnedCard(defaultTemplate, slot, defaultTemplate); // 빈 슬롯용 기본 껍데기
        }

        string resolvedTargetSegmentId = string.IsNullOrWhiteSpace(targetSegmentId)
            ? definition.NormalizedTargetSegmentId
            : targetSegmentId.Trim(); // B 모드는 definition 대상, A 모드는 선택 세그먼트
        StatUpgrade.StatCardTier tier = StatUpgrade.RollTier(rareCardChancePercent, uniqueCardChancePercent); // 등급 선정
        SegmentAddCard templatePresentation = GetSegmentCardPresentation(slotIndex); // addSegmentCards 템플릿
        WeaponDefinition.CardSpawnResolve resolve = definition.ResolveCardSpawn(tier, defaultTemplate, templatePresentation); // 프리팹 결정
        GameObject spawnPrefab = resolve.Prefab != null ? resolve.Prefab : defaultTemplate; // fallback
        SpawnedCardEntry entry = CreateSpawnedCard(spawnPrefab, slot, defaultTemplate, skipStatUpgradeRoll: true); // 무기 강화 등급은 WeaponEnhancementTier만 사용
        if (entry == null)
        {
            return null; // 생성 실패
        }

        ConfigureWeaponEnhancementEntry(entry, definition, resolvedTargetSegmentId, levelDelta, tier); // 문구·등급
        return entry;
    }

    private SegmentAddCard GetSegmentCardPresentation(int index) // addSegmentCards 프리팹의 SegmentAddCard (Instantiate 전)
    {
        GameObject template = GetSegmentCardTemplate(index); // 슬롯 템플릿
        if (template == null)
        {
            return null; // 템플릿 없음
        }

        SegmentAddCard presentation = template.GetComponent<SegmentAddCard>(); // 루트
        if (presentation != null)
        {
            return presentation;
        }

        return template.GetComponentInChildren<SegmentAddCard>(true); // 자식 fallback
    }

    // 2차 액션 카드 부모로 가장 중앙에 가까운 슬롯 사용
    private RectTransform GetCenteredActionParentSlot()
    {
        if (cardSlots == null || cardSlots.Length == 0)
        {
            return null; // 슬롯 없음
        }

        RectTransform result = cardSlots[0]; // 기본값
        float bestDistance = result != null ? Mathf.Abs(result.anchoredPosition.x) : float.MaxValue; // 중앙 거리
        for (int i = 1; i < cardSlots.Length; i++)
        {
            RectTransform slot = cardSlots[i]; // 후보 슬롯
            if (slot == null)
            {
                continue; // 빈 슬롯 제외
            }

            float distance = Mathf.Abs(slot.anchoredPosition.x); // 중앙에서 떨어진 거리
            if (distance < bestDistance)
            {
                result = slot; // 더 중앙에 가까운 슬롯
                bestDistance = distance; // 거리 갱신
            }
        }

        return result; // 중앙 슬롯 반환
    }

    // 2차 액션 카드 2장을 중앙 기준 좌우로 배치
    private void ApplyCenteredActionCardPosition(SpawnedCardEntry entry, int index, int count)
    {
        if (entry == null || entry.RootTransform == null)
        {
            return; // 대상 없음
        }

        Vector2 targetPosition = GetCenteredActionCardPosition(index, count); // 목표 위치 계산
        entry.RootTransform.anchoredPosition = targetPosition; // 현재 위치 보정
        entry.OriginalPosition = targetPosition; // DOTween 등장/복귀 기준도 보정
    }

    // 기존 3슬롯 폭을 기준으로 2장 배치 간격 계산
    private Vector2 GetCenteredActionCardPosition(int index, int count)
    {
        if (count <= 1)
        {
            return Vector2.zero; // 1장이면 중앙
        }

        float halfSpacing = CalculateCenteredActionHalfSpacing(); // 중앙에서 좌우 거리
        float x = index == 0 ? -halfSpacing : halfSpacing; // 첫 장 왼쪽, 둘째 장 오른쪽
        return new Vector2(x, 0f); // y는 중앙 슬롯 기준 유지
    }

    // 후보 카드 3장 슬롯의 전체 폭에서 2장용 절반 간격 산출
    private float CalculateCenteredActionHalfSpacing()
    {
        if (cardSlots == null || cardSlots.Length < 2)
        {
            return 175f; // 기본 간격
        }

        float minX = float.MaxValue; // 가장 왼쪽
        float maxX = float.MinValue; // 가장 오른쪽
        for (int i = 0; i < cardSlots.Length; i++)
        {
            RectTransform slot = cardSlots[i]; // 후보 슬롯
            if (slot == null)
            {
                continue; // 빈 슬롯 제외
            }

            float x = slot.anchoredPosition.x; // 슬롯 x 위치
            minX = Mathf.Min(minX, x); // 왼쪽 갱신
            maxX = Mathf.Max(maxX, x); // 오른쪽 갱신
        }

        if (minX == float.MaxValue || maxX == float.MinValue || Mathf.Approximately(minX, maxX))
        {
            return 175f; // 계산 불가 fallback
        }

        return Mathf.Max(120f, (maxX - minX) * 0.25f); // 3슬롯 폭의 1/4 지점에 2장 배치
    }

    // 세그먼트 카드 템플릿 선택 (안건준 수정 - 0623 : segmentCardBasePrefab 사용)
    private GameObject GetSegmentCardTemplate(int index)
    {
        return segmentCardBasePrefab; // SegmentUpgradeCard 공통 프리팹
    }

    private GameObject GetSegmentChoiceCardTemplate() // 후보 선택 1단계 전용 템플릿
    {
        if (segmentChoiceCardPrefab != null)
        {
            return segmentChoiceCardPrefab; // Inspector 지정 우선
        }

        CardUiPrefabReferences references = GetPrefabReferences();
        if (references != null && references.SegmentChoiceCardPrefab != null)
        {
            return references.SegmentChoiceCardPrefab; // 이동된 프리팹 참조
        }

        return segmentCardBasePrefab; // 누락 시 기존 카드 유지
    }

    private CardUiPrefabReferences GetPrefabReferences()
    {
        if (prefabReferences != null)
        {
            return prefabReferences; // Inspector 지정 우선
        }

        if (cachedPrefabReferences == null)
        {
            cachedPrefabReferences = Resources.Load<CardUiPrefabReferences>(CardUiPrefabReferencesResourcePath); // 씬 수정 없는 fallback
        }

        return cachedPrefabReferences;
    }

    private void ApplyTierCardFrame(GameObject cardRoot, StatUpgrade.StatCardTier tier) // 스탯/무기 강화 카드 등급 프레임
    {
        if (cardRoot == null)
        {
            return; // 카드 없음
        }

        Sprite frameSprite = GetTierFrameSprite(tier); // 등급별 프레임
        if (frameSprite == null)
        {
            return; // 에셋 누락 시 기존 프레임 유지
        }

        Image rootImage = cardRoot.GetComponent<Image>(); // 카드 루트 배경 이미지
        if (rootImage == null)
        {
            return; // 루트 이미지가 없는 특수 카드
        }

        rootImage.sprite = frameSprite;
        rootImage.type = Image.Type.Simple;
        rootImage.preserveAspect = false;
        rootImage.color = Color.white;
    }

    private Sprite GetTierFrameSprite(StatUpgrade.StatCardTier tier) // Resources 캐시 로드
    {
        switch (tier)
        {
            case StatUpgrade.StatCardTier.Unique:
                if (cachedTierFrameUniqueSprite == null)
                {
                    cachedTierFrameUniqueSprite = Resources.Load<Sprite>(TierFrameUniqueResourcePath);
                }
                return cachedTierFrameUniqueSprite;
            case StatUpgrade.StatCardTier.Rare:
                if (cachedTierFrameRareSprite == null)
                {
                    cachedTierFrameRareSprite = Resources.Load<Sprite>(TierFrameRareResourcePath);
                }
                return cachedTierFrameRareSprite;
            default:
                if (cachedTierFrameNormalSprite == null)
                {
                    cachedTierFrameNormalSprite = Resources.Load<Sprite>(TierFrameNormalResourcePath);
                }
                return cachedTierFrameNormalSprite;
        }
    }

    private GameObject ResolveSegmentActionCardPrefab(SegmentCardRole role, GameObject defaultTemplate) // 2차 액션 카드 — CardUI 교체 프리팹
    {
        // 안건준 수정 - 0623 : segmentCardBasePrefab → defaultTemplate 순으로 fallback
        return segmentCardBasePrefab != null ? segmentCardBasePrefab : defaultTemplate;
    }

    // A 모드 1단계 — 보유 세그먼트 개수에 비례한 가중치로 후보 선택 (중복 없음)
    private List<SegmentCatalogEntry> PickWeightedWeaponEnhanceSegmentEntries(
        List<SegmentCatalogEntry> candidates,
        int count,
        Dictionary<string, int> ownedCountsBySegmentId)
    {
        List<WeightedSegmentCatalogEntry> remaining = new List<WeightedSegmentCatalogEntry>(); // 남은 후보+가중치
        if (candidates != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                SegmentCatalogEntry entry = candidates[i]; // 카탈로그 후보
                if (!entry.HasId)
                {
                    continue; // ID 없는 후보 제외
                }

                int ownedCount = 0; // 보유 개수
                if (ownedCountsBySegmentId != null
                    && ownedCountsBySegmentId.TryGetValue(entry.NormalizedId, out int countForId))
                {
                    ownedCount = Mathf.Max(0, countForId);
                }

                float weight = weaponEnhanceSegmentBaseWeight + ownedCount * weaponEnhanceSegmentWeightPerOwned; // 기본 + (개수 × 보너스)
                remaining.Add(new WeightedSegmentCatalogEntry
                {
                    Entry = entry,
                    Weight = weight
                });
            }
        }

        List<SegmentCatalogEntry> picked = new List<SegmentCatalogEntry>(count); // 선택 결과
        int pickCount = Mathf.Min(count, remaining.Count); // 뽑을 수량
        for (int pickIndex = 0; pickIndex < pickCount; pickIndex++)
        {
            if (!TryPickWeightedSegmentEntry(remaining, out WeightedSegmentCatalogEntry selected))
            {
                break; // 더 이상 선택 불가
            }

            picked.Add(selected.Entry); // 선택된 세그먼트
            remaining.Remove(selected); // 중복 방지
        }

        return picked;
    }

    private static bool TryPickWeightedSegmentEntry(List<WeightedSegmentCatalogEntry> pool, out WeightedSegmentCatalogEntry selected)
    {
        selected = default;
        if (pool == null || pool.Count == 0)
        {
            return false;
        }

        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += pool[i].Weight;
        }

        if (totalWeight <= 0f)
        {
            selected = pool[pool.Count - 1];
            return true;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        selected = pool[pool.Count - 1];
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += pool[i].Weight;
            if (roll < cumulative)
            {
                selected = pool[i];
                return true;
            }
        }

        return true;
    }

    // 카탈로그 후보 랜덤 선택
    private static List<SegmentCatalogEntry> PickRandomSegmentEntries(List<SegmentCatalogEntry> candidates, int count)
    {
        List<SegmentCatalogEntry> shuffled = new List<SegmentCatalogEntry>(); // 유효 후보 복사
        if (candidates != null)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].HasId)
                {
                    shuffled.Add(candidates[i]); // ID 있는 후보만 사용
                }
            }
        }

        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1); // 랜덤 교환 위치
            SegmentCatalogEntry temp = shuffled[i]; // 임시 저장
            shuffled[i] = shuffled[swapIndex]; // 교환
            shuffled[swapIndex] = temp; // 교환 완료
        }

        int pickCount = Mathf.Min(Mathf.Max(0, count), shuffled.Count); // 선택 수 보정
        return shuffled.GetRange(0, pickCount); // 선택 결과
    }

    // 세그먼트 후보 카드 데이터 주입
    private void ConfigureSegmentCandidateEntry(SpawnedCardEntry entry, SegmentCatalogEntry catalogEntry)
    {
        entry.SegmentRole = SegmentCardRole.Candidate; // 후보 카드
        entry.SegmentCatalogEntry = catalogEntry; // 선택 후보 저장
        entry.SegmentId = catalogEntry.NormalizedId; // 대상 ID 저장
        entry.LevelDelta = entry.SegmentAddCard != null ? entry.SegmentAddCard.LevelDelta : 1; // 소비 레벨
        entry.CanSelect = true; // 후보 선택 가능
        entry.SegmentAddCard?.ConfigureCandidate(catalogEntry); // 카드 문구 세팅

        // 안건준 추가 - 0623 : 현재 세그먼트 레벨에 맞는 아이콘 적용
        if (entry.Root != null)
        {
            string segId = catalogEntry.NormalizedId;
            int currentLevel = CoreStatProvider.Active?.Convoy?.GetCurrentSegmentLevel(segId) ?? 1;
            Sprite icon = GetSegmentIconSprite(segId, currentLevel);
            string title = string.IsNullOrWhiteSpace(catalogEntry.DisplayName) ? segId : catalogEntry.DisplayName;
            string desc = string.IsNullOrWhiteSpace(catalogEntry.Description) ? $"{catalogEntry.NormalizedId} 선택" : catalogEntry.Description;
            ApplyCardTextsDirectly(entry.Root, title, desc, icon, GetSegmentIconSizeOffset(segId));
        }
    }

    // 후보 부족 시 없음 카드 데이터 주입
    private void ConfigureEmptySegmentEntry(SpawnedCardEntry entry)
    {
        entry.SegmentRole = SegmentCardRole.Empty; // 없음 카드
        entry.SegmentId = string.Empty; // 대상 없음
        entry.LevelDelta = 1; // 기본값
        entry.CanSelect = false; // 클릭 불가
        entry.SegmentAddCard?.ConfigureEmpty(); // 카드 문구 세팅
    }

    // 추가/레벨업 액션 카드 데이터 주입
    private void ConfigureSegmentActionEntry(SpawnedCardEntry entry, SegmentCatalogEntry catalogEntry, int levelDelta, SegmentCardRole role, bool selectable)
    {
        string segId = catalogEntry.NormalizedId; // 대상 ID
        string displayName = string.IsNullOrWhiteSpace(catalogEntry.DisplayName) ? segId : catalogEntry.DisplayName; // 표시명
        // 안건준 수정 - 0623 : Card_Text = 세그먼트 이름만
        string title = displayName;
        string description = BuildSegmentActionDescription(segId, displayName, role, selectable); // 액션 설명
        entry.SegmentRole = role; // 액션 역할
        entry.SegmentCatalogEntry = catalogEntry; // 대상 후보 저장
        entry.SegmentId = segId; // 대상 ID
        entry.LevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        entry.CanSelect = selectable; // 선택 가능 여부
        entry.SegmentAddCard?.ConfigureAction(segId, title, description, selectable); // 카드 문구 세팅

        // 안건준 추가 - 0623 : 레벨에 맞는 아이콘 적용 (레벨업 카드는 다음 레벨 이미지)
        if (entry.Root != null)
        {
            int currentLevel = CoreStatProvider.Active?.Convoy?.GetCurrentSegmentLevel(segId) ?? 1;
            int iconLevel = (role == SegmentCardRole.LevelUpAction) ? currentLevel + 1 : currentLevel; // 레벨업=다음레벨
            Sprite icon = GetSegmentIconSprite(segId, iconLevel);
            ApplyCardTextsDirectly(entry.Root, title, description, icon, GetSegmentIconSizeOffset(segId));
        }
    }

    // 액션 카드 설명 생성 (안건준 수정 - 0623 : Card_Text에 이름이 있으므로 DescText는 상태 정보만)
    private static string BuildSegmentActionDescription(string segmentId, string displayName, SegmentCardRole role, bool selectable)
    {
        if (role == SegmentCardRole.AddAction)
        {
            return selectable ? "추가 +1" : "추가 불가"; // 이름은 Card_Text에, 여기선 상태만
        }

        if (role == SegmentCardRole.LevelUpAction)
        {
            if (CoreStatProvider.Active != null && CoreStatProvider.Active.TryGetSegmentModelLevelInfo(segmentId, out int currentLevel, out int maxLevel))
            {
                int nextLevel = Mathf.Min(currentLevel + 1, maxLevel); // 다음 레벨
                return selectable ? $"Lv.{currentLevel} → Lv.{nextLevel}" : "MAX"; // 이름은 Card_Text에
            }

            return selectable ? "레벨업 가능" : "레벨업 불가"; // fallback
        }

        return string.Empty;
    }

    // 무기 강화 디버그 - 시작 시 1회 (CoreStatProvider 현재값)
    private void LogWeaponEnhancementInitialOnce()
    {
        if (loggedWeaponEnhancementInitial)
        {
            return;
        }

        loggedWeaponEnhancementInitial = true; // 1회만 출력
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        if (core == null)
        {
            Debug.LogWarning("[CardUI] 무기 강화 초기: CoreStatProvider 없음");
            return;
        }

        WeaponCatalogAsset catalog = ResolveWeaponCatalog(); // 카탈로그
        if (catalog == null)
        {
            LogWeaponEnhancementState(core, string.Empty);
            return;
        }

        LogWeaponEnhancementState(core, "SG01_Cannon"); // 캐논
        LogWeaponEnhancementState(core, "SG02_Missile"); // 미사일
        catalog.ForEachAdditionalSegmentId(segmentId => LogWeaponEnhancementState(core, segmentId)); // 추가 무기
    }

    //전찬우 수정-0622
    private void LogWeaponEnhancementState(CoreStatProvider core, string segmentId)
    {
        if (!TryBuildSegmentWeaponStatDebugContext(core, segmentId, out SegmentWeaponStatDebugContext context))
        {
            return;
        }

        Debug.Log($"[CardUI] 무기 강화 초기 | 세그먼트: {context.SegmentId}\n  현재 →\n{FormatSegmentWeaponStatDebugText(context)}");
    }

    private static bool TryGetSegmentAttackProfile(CoreStatProvider core, string segmentId, out SegmentAttackProfile profile)
    {
        profile = null; // 기본값
        if (core == null || core.SegmentCatalogAsset == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return false; // 조회 불가
        }

        if (!core.SegmentCatalogAsset.TryFind(segmentId, out SegmentDefinition definition))
        {
            return false; // 정의 없음
        }

        int level = 1; // 기본 레벨
        if (core.TryGetSegmentModelLevelInfo(segmentId, out int currentLevel, out _))
        {
            level = currentLevel; // 장착 중이면 현재 모델 레벨
        }

        if (definition.TryGetLevel(level, out SegmentLevelDefinition levelDef) && levelDef.AttackProfile != null)
        {
            profile = levelDef.AttackProfile; // 레벨 정의에서 프로필
            return true;
        }

        if (!definition.TryGetSegmentPrefab(level, out GameObject prefab) || prefab == null)
        {
            return false; // 프리팹 없음
        }

        GenericSegmentWeapon weapon = prefab.GetComponentInChildren<GenericSegmentWeapon>(true); // 무기 컴포넌트
        if (weapon == null || weapon.AttackProfile == null)
        {
            return false; // 프로필 없음
        }

        profile = weapon.AttackProfile; // 프리팹에서 프로필
        return true;
    }

    // 무기 강화 디버그 - 카드 선택 후 누적 보너스
    private void LogWeaponEnhancementIncrease(string segmentId, WeaponDefinition definition, CoreStatProvider core)
    {
        if (definition == null || !TryBuildSegmentWeaponStatDebugContext(core, segmentId, out SegmentWeaponStatDebugContext context))
        {
            return;
        }

        string cardName = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.NormalizedId : definition.DisplayName; // 카드명
        Debug.Log($"[CardUI] 무기 강화 | 세그먼트: {context.SegmentId} | 카드: {cardName}\n  현재 →\n{FormatSegmentWeaponStatDebugText(context)}");
    }

    // 건춘추가 - 0621 ======
    private void RefreshSegmentWeaponStatUi() // 선택 세그먼트 스탯 TMP 갱신
    {
        if (!showSegmentWeaponStatUi || segmentWeaponStatText == null)
        {
            return; // UI 비활성 또는 TMP 없음
        }

        CoreStatProvider core = CoreStatProvider.Active;
        string segmentId = ResolveSegmentWeaponStatDebugTargetId();
        segmentWeaponStatText.text = TryBuildSegmentWeaponStatDebugContext(core, segmentId, out SegmentWeaponStatDebugContext context)
            ? FormatSegmentWeaponStatDebugText(context)
            : "Core 없음";
    }

    //전찬우 수정-0622
    public void SelectSegmentWeaponStatDebugContext(string segmentId) // 디버그 UI에서 직접 세그먼트 컨텍스트 선택
    {
        SetSegmentWeaponStatDebugTarget(segmentId);
    }

    public string GetSelectedSegmentWeaponStatDebugContextId() // 디버그 UI 표시용 현재 컨텍스트
    {
        return ResolveSegmentWeaponStatDebugTargetId();
    }

    private void SetSegmentWeaponStatDebugTarget(string segmentId) // 선택 흐름에서 현재 표시 대상 변경
    {
        if (string.IsNullOrWhiteSpace(segmentId))
        {
            return; // 대상 없음
        }

        selectedSegmentWeaponStatId = segmentId.Trim();
        RefreshSegmentWeaponStatUi();
    }

    private string ResolveSegmentWeaponStatDebugTargetId()
    {
        return string.IsNullOrWhiteSpace(selectedSegmentWeaponStatId)
            ? ResolveSegmentWeaponStatViewId(segmentWeaponStatViewTarget)
            : selectedSegmentWeaponStatId.Trim();
    }

    private static string ResolveSegmentWeaponStatViewId(SegmentWeaponStatViewTarget target) // 열거형 → SegmentId
    {
        switch (target)
        {
            case SegmentWeaponStatViewTarget.Missile:
                return "SG02_Missile";
            case SegmentWeaponStatViewTarget.Trebuchet:
                return "SG03_Trebuchet";
            case SegmentWeaponStatViewTarget.SawLauncher:
                return "SG04_SawLauncher";
            case SegmentWeaponStatViewTarget.Flamethrower:
                return "SG05_Flamethrower";
            case SegmentWeaponStatViewTarget.LightningObelisk:
                return "SG20_LightningObelisk";
            case SegmentWeaponStatViewTarget.FireballTower:
                return "SG21_FireballTower";
            default:
                return "SG01_Cannon";
        }
    }

    private bool TryBuildSegmentWeaponStatDebugContext(CoreStatProvider core, string segmentId, out SegmentWeaponStatDebugContext context)
    {
        context = default;
        if (core == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return false; // 조회 불가
        }

        string normalizedId = segmentId.Trim();
        string title = ResolveSegmentStatDisplayTitle(core, normalizedId);
        int level = 1;
        if (core.TryGetSegmentModelLevelInfo(normalizedId, out int currentLevel, out _))
        {
            level = currentLevel;
        }

        WeaponStatBonusData bonus = core.GetWeaponStatBonus(normalizedId);
        TryGetSegmentAttackProfile(core, normalizedId, out SegmentAttackProfile profile);
        SegmentWeaponStatDisplayFlags flags = ResolveSegmentWeaponStatDisplayFlags(normalizedId, profile, bonus);
        context = new SegmentWeaponStatDebugContext(normalizedId, title, level, profile, bonus, flags);
        return true;
    }

    private string FormatSegmentWeaponStatDebugText(SegmentWeaponStatDebugContext context)
    {
        StringBuilder sb = new StringBuilder(384);
        sb.Append('[').Append(context.Title).Append(" Lv").Append(context.Level).Append(']').AppendLine();

        if (!context.HasProfile)
        {
            sb.AppendLine("(프로필 없음)");
            if (!AppendCumulativeBonusLines(sb, context.Bonus))
            {
                sb.AppendLine("강화 없음");
            }

            return sb.ToString().TrimEnd();
        }

        AppendSegmentWeaponStatLines(sb, context.Profile, context.Bonus, context.DisplayFlags);
        return sb.ToString().TrimEnd();
    }

    private SegmentWeaponStatDisplayFlags ResolveSegmentWeaponStatDisplayFlags(string segmentId, SegmentAttackProfile profile, WeaponStatBonusData bonus)
    {
        SegmentWeaponStatDisplayFlags flags = SegmentWeaponStatDisplayFlags.BaseDamage
            | SegmentWeaponStatDisplayFlags.SearchRange
            | SegmentWeaponStatDisplayFlags.Cooldown; // 공통 핵심값

        AddWeaponEnhancementDisplayFlags(segmentId, ref flags);
        AddProfileImportantDisplayFlags(profile, ref flags);
        AddBonusDisplayFlags(bonus, ref flags);
        return flags;
    }

    private void AddWeaponEnhancementDisplayFlags(string segmentId, ref SegmentWeaponStatDisplayFlags flags)
    {
        WeaponCatalogAsset catalog = ResolveWeaponCatalog();
        if (catalog == null
            || string.IsNullOrWhiteSpace(segmentId)
            || !catalog.TryGetEnhancementsForSegment(segmentId, out WeaponDefinition[] enhancements)
            || enhancements == null)
        {
            return; // 카탈로그 없음
        }

        for (int i = 0; i < enhancements.Length; i++)
        {
            AddWeaponDefinitionDisplayFlags(enhancements[i], ref flags);
        }
    }

    private static void AddWeaponDefinitionDisplayFlags(WeaponDefinition definition, ref SegmentWeaponStatDisplayFlags flags)
    {
        if (definition == null)
        {
            return; // 정의 없음
        }

        if (HasAnyTierValue(definition.GetBaseDamage(StatUpgrade.StatCardTier.Normal), definition.GetBaseDamage(StatUpgrade.StatCardTier.Rare), definition.GetBaseDamage(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Normal), definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Rare), definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.BaseDamage;
        }

        if (HasAnyTierValue(definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Normal), definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Rare), definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Normal), definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Rare), definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.ProjectileSpeed;
        }

        if (HasAnyTierValue(definition.GetSearchRange(StatUpgrade.StatCardTier.Normal), definition.GetSearchRange(StatUpgrade.StatCardTier.Rare), definition.GetSearchRange(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Normal), definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Rare), definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.SearchRange;
        }

        if (HasAnyTierValue(definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Normal), definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Rare), definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.MaxChainDepth;
        }

        if (HasAnyTierValue(definition.GetChainRange(StatUpgrade.StatCardTier.Normal), definition.GetChainRange(StatUpgrade.StatCardTier.Rare), definition.GetChainRange(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetChainRangePercent(StatUpgrade.StatCardTier.Normal), definition.GetChainRangePercent(StatUpgrade.StatCardTier.Rare), definition.GetChainRangePercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.ChainRange;
        }

        if (HasAnyTierValue(definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Normal), definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Rare), definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.ChainDamageFalloff;
        }

        if (HasAnyTierValue(definition.GetProjectileCount(StatUpgrade.StatCardTier.Normal), definition.GetProjectileCount(StatUpgrade.StatCardTier.Rare), definition.GetProjectileCount(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.ProjectileCount;
        }

        if (HasAnyTierValue(definition.GetCooldownReduction(StatUpgrade.StatCardTier.Normal), definition.GetCooldownReduction(StatUpgrade.StatCardTier.Rare), definition.GetCooldownReduction(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.Cooldown;
        }

        if (HasAnyTierValue(definition.GetSideConeAngle(StatUpgrade.StatCardTier.Normal), definition.GetSideConeAngle(StatUpgrade.StatCardTier.Rare), definition.GetSideConeAngle(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.SideConeAngle;
        }

        if (HasAnyTierValue(definition.GetLaserDuration(StatUpgrade.StatCardTier.Normal), definition.GetLaserDuration(StatUpgrade.StatCardTier.Rare), definition.GetLaserDuration(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Normal), definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Rare), definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.LaserDuration;
        }

        if (HasAnyTierValue(definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Normal), definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Rare), definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.LaserTickInterval;
        }

        if (HasAnyTierValue(definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.LandingRollDistance;
        }

        if (HasAnyTierValue(definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.LandingRollDuration;
        }

        if (HasAnyTierValue(definition.GetPierceCount(StatUpgrade.StatCardTier.Normal), definition.GetPierceCount(StatUpgrade.StatCardTier.Rare), definition.GetPierceCount(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.PierceCount;
        }

        if (HasAnyTierValue(definition.GetExplosionRadius(StatUpgrade.StatCardTier.Normal), definition.GetExplosionRadius(StatUpgrade.StatCardTier.Rare), definition.GetExplosionRadius(StatUpgrade.StatCardTier.Unique))
            || HasAnyTierValue(definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Normal), definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Rare), definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.ExplosionRadius;
        }

        if (HasAnyTierValue(definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Normal), definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Rare), definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Unique)))
        {
            flags |= SegmentWeaponStatDisplayFlags.SawPierceDamageRatio;
        }
    }

    private static void AddProfileImportantDisplayFlags(SegmentAttackProfile profile, ref SegmentWeaponStatDisplayFlags flags)
    {
        if (profile == null)
        {
            return; // 프로필 없음
        }

        if (profile.MoveType != SegmentAttackMoveType.Laser && profile.MoveType != SegmentAttackMoveType.ChainLightning)
        {
            flags |= SegmentWeaponStatDisplayFlags.ProjectileSpeed;
        }

        if (profile.ProjectileCount > 1)
        {
            flags |= SegmentWeaponStatDisplayFlags.ProjectileCount;
        }

        if (profile.ImpactType == SegmentAttackImpactType.PierceDamage || profile.MoveType == SegmentAttackMoveType.PiercingProjectile)
        {
            flags |= SegmentWeaponStatDisplayFlags.PierceCount;
        }

        if (profile.ImpactType == SegmentAttackImpactType.ExplosionArea)
        {
            flags |= SegmentWeaponStatDisplayFlags.ExplosionRadius;
        }

        if (profile.AttackAreaMode == SegmentAttackAreaMode.SideCones)
        {
            flags |= SegmentWeaponStatDisplayFlags.SideConeAngle;
        }

        if (profile.MoveType == SegmentAttackMoveType.ChainLightning)
        {
            flags |= SegmentWeaponStatDisplayFlags.MaxChainDepth
                | SegmentWeaponStatDisplayFlags.ChainRange
                | SegmentWeaponStatDisplayFlags.ChainDamageFalloff;
        }

        if (profile.MoveType == SegmentAttackMoveType.SawBounceProjectile)
        {
            flags |= SegmentWeaponStatDisplayFlags.MaxChainDepth
                | SegmentWeaponStatDisplayFlags.ChainRange
                | SegmentWeaponStatDisplayFlags.SawPierceDamageRatio;
        }

        if (profile.MoveType == SegmentAttackMoveType.Laser || profile.MoveType == SegmentAttackMoveType.ExpandingFlameSphere)
        {
            flags |= SegmentWeaponStatDisplayFlags.LaserDuration
                | SegmentWeaponStatDisplayFlags.LaserTickInterval;
        }

        if (profile.RollAfterArcLanding || profile.LandingRollDistance > 0.0001f)
        {
            flags |= SegmentWeaponStatDisplayFlags.LandingRollDistance
                | SegmentWeaponStatDisplayFlags.LandingRollDuration;
        }
    }

    private static void AddBonusDisplayFlags(WeaponStatBonusData bonus, ref SegmentWeaponStatDisplayFlags flags)
    {
        if (bonus.BaseDamageBonus > 0.0001f || bonus.BaseDamagePercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.BaseDamage;
        if (bonus.ProjectileSpeedBonus > 0.0001f || bonus.ProjectileSpeedPercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.ProjectileSpeed;
        if (bonus.SearchRangeBonus > 0.0001f || bonus.SearchRangePercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.SearchRange;
        if (bonus.MaxChainDepthBonus != 0) flags |= SegmentWeaponStatDisplayFlags.MaxChainDepth;
        if (bonus.ChainRangeBonus > 0.0001f || bonus.ChainRangePercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.ChainRange;
        if (bonus.ChainDamageFalloffBonus > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.ChainDamageFalloff;
        if (bonus.ProjectileCountBonus != 0) flags |= SegmentWeaponStatDisplayFlags.ProjectileCount;
        if (WeaponStatBonusData.ToReductionDisplayRate(bonus.CooldownReductionMultiplier) > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.Cooldown;
        if (bonus.SideConeAngleBonus > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.SideConeAngle;
        if (bonus.LaserDurationBonus > 0.0001f || bonus.LaserDurationPercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.LaserDuration;
        if (WeaponStatBonusData.ToReductionDisplayRate(bonus.LaserTickIntervalReductionMultiplier) > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.LaserTickInterval;
        if (bonus.LandingRollDistanceBonus > 0.0001f || bonus.LandingRollDistancePercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.LandingRollDistance;
        if (bonus.LandingRollDurationBonus > 0.0001f || bonus.LandingRollDurationPercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.LandingRollDuration;
        if (bonus.PierceCountBonus != 0) flags |= SegmentWeaponStatDisplayFlags.PierceCount;
        if (bonus.ExplosionRadiusBonus > 0.0001f || bonus.ExplosionRadiusPercentMultiplier > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.ExplosionRadius;
        if (bonus.SawPierceDamageRatioBonus > 0.0001f) flags |= SegmentWeaponStatDisplayFlags.SawPierceDamageRatio;
    }

    private static void AppendSegmentWeaponStatLines(StringBuilder sb, SegmentAttackProfile profile, WeaponStatBonusData bonus, SegmentWeaponStatDisplayFlags flags)
    {
        if (Includes(flags, SegmentWeaponStatDisplayFlags.BaseDamage))
        {
            AppendStatLineFloat(sb, "공격력", profile.BaseDamage, bonus.ResolveBaseDamage(profile.BaseDamage), bonus.BaseDamageBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.BaseDamagePercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.Cooldown))
        {
            AppendCooldownStatLine(sb, profile.Cooldown, bonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.SearchRange))
        {
            AppendStatLineFloat(sb, "사거리", profile.SearchRange, bonus.ResolveSearchRange(profile.SearchRange), bonus.SearchRangeBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.SearchRangePercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.ProjectileSpeed))
        {
            AppendStatLineFloat(sb, "투사체속도", profile.ProjectileSpeed, bonus.ResolveProjectileSpeed(profile.ProjectileSpeed), bonus.ProjectileSpeedBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ProjectileSpeedPercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.ProjectileCount))
        {
            AppendStatLineInt(sb, "발사수", profile.ProjectileCount, bonus.ResolveProjectileCount(profile.ProjectileCount), bonus.ProjectileCountBonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.PierceCount))
        {
            AppendStatLineInt(sb, "관통", profile.PierceCount, bonus.ResolvePierceCount(profile.PierceCount), bonus.PierceCountBonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.ExplosionRadius))
        {
            AppendStatLineFloat(sb, "폭발반경", profile.ExplosionRadius, bonus.ResolveExplosionRadius(profile.ExplosionRadius), bonus.ExplosionRadiusBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ExplosionRadiusPercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.MaxChainDepth))
        {
            AppendStatLineInt(sb, "연쇄단계", profile.MaxChainDepth, bonus.ResolveMaxChainDepth(profile.MaxChainDepth), bonus.MaxChainDepthBonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.ChainRange))
        {
            AppendStatLineFloat(sb, "연쇄거리", profile.ChainRange, bonus.ResolveChainRange(profile.ChainRange), bonus.ChainRangeBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ChainRangePercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.ChainDamageFalloff))
        {
            AppendStatLineFloat(sb, "체인감쇠율", profile.ChainDamageFalloff, bonus.ResolveChainDamageFalloff(profile.ChainDamageFalloff), bonus.ChainDamageFalloffBonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.SideConeAngle))
        {
            AppendStatLineFloat(sb, "부채꼴각", profile.SideConeAngle, bonus.ResolveSideConeAngle(profile.SideConeAngle), bonus.SideConeAngleBonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.LaserDuration))
        {
            AppendStatLineFloat(sb, "레이저지속", profile.LaserDuration, bonus.ResolveLaserDuration(profile.LaserDuration), bonus.LaserDurationBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LaserDurationPercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.LaserTickInterval))
        {
            AppendLaserTickIntervalStatLine(sb, profile.LaserTickInterval, bonus);
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.LandingRollDistance))
        {
            AppendStatLineFloat(sb, "굴러거리", profile.LandingRollDistance, bonus.ResolveLandingRollDistance(profile.LandingRollDistance), bonus.LandingRollDistanceBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LandingRollDistancePercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.LandingRollDuration))
        {
            AppendStatLineFloat(sb, "굴러시간", profile.LandingRollDuration, bonus.ResolveLandingRollDuration(profile.LandingRollDuration), bonus.LandingRollDurationBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LandingRollDurationPercentMultiplier));
        }

        if (Includes(flags, SegmentWeaponStatDisplayFlags.SawPierceDamageRatio))
        {
            AppendStatLineFloat(sb, "관통피해비율", profile.SawPierceDamageRatio, bonus.ResolveSawPierceDamageRatio(profile.SawPierceDamageRatio), bonus.SawPierceDamageRatioBonus);
        }
    }

    private static void AppendCooldownStatLine(StringBuilder sb, float baseCooldown, WeaponStatBonusData bonus)
    {
        float cooldown = bonus.ResolveCooldown(baseCooldown);
        float cooldownReduction = WeaponStatBonusData.ToReductionDisplayRate(bonus.CooldownReductionMultiplier);
        sb.Append("기준쿨타임: ").Append(cooldown.ToString("0.##")).Append("초");
        if (cooldownReduction > 0.0001f)
        {
            sb.Append(" (쿨-").Append((cooldownReduction * 100f).ToString("0.#")).Append("%)");
        }

        sb.Append(" (실전 ±10%)");
        sb.AppendLine();
    }

    private static void AppendLaserTickIntervalStatLine(StringBuilder sb, float baseTickInterval, WeaponStatBonusData bonus)
    {
        float tickInterval = bonus.ResolveLaserTickInterval(baseTickInterval);
        float tickReduction = WeaponStatBonusData.ToReductionDisplayRate(bonus.LaserTickIntervalReductionMultiplier);
        sb.Append("레이저틱: ").Append(tickInterval.ToString("0.##")).Append('초');
        if (tickReduction > 0.0001f)
        {
            sb.Append(" (틱-").Append((tickReduction * 100f).ToString("0.#")).Append("%)");
        }

        sb.AppendLine();
    }

    private static bool AppendCumulativeBonusLines(StringBuilder sb, WeaponStatBonusData bonus)
    {
        bool appended = false;
        appended |= AppendBonusFloatLine(sb, "공격력", bonus.BaseDamageBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.BaseDamagePercentMultiplier));
        appended |= AppendBonusFloatLine(sb, "투사체속도", bonus.ProjectileSpeedBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ProjectileSpeedPercentMultiplier));
        appended |= AppendBonusFloatLine(sb, "사거리", bonus.SearchRangeBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.SearchRangePercentMultiplier));
        appended |= AppendBonusReductionLine(sb, "기준쿨타임", "쿨", WeaponStatBonusData.ToReductionDisplayRate(bonus.CooldownReductionMultiplier));
        appended |= AppendBonusIntLine(sb, "발사수", bonus.ProjectileCountBonus);
        appended |= AppendBonusIntLine(sb, "관통", bonus.PierceCountBonus);
        appended |= AppendBonusFloatLine(sb, "폭발반경", bonus.ExplosionRadiusBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ExplosionRadiusPercentMultiplier));
        appended |= AppendBonusIntLine(sb, "연쇄단계", bonus.MaxChainDepthBonus);
        appended |= AppendBonusFloatLine(sb, "연쇄거리", bonus.ChainRangeBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.ChainRangePercentMultiplier));
        appended |= AppendBonusFloatLine(sb, "체인감쇠율", bonus.ChainDamageFalloffBonus);
        appended |= AppendBonusFloatLine(sb, "부채꼴각", bonus.SideConeAngleBonus);
        appended |= AppendBonusFloatLine(sb, "레이저지속", bonus.LaserDurationBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LaserDurationPercentMultiplier));
        appended |= AppendBonusReductionLine(sb, "레이저틱", "틱", WeaponStatBonusData.ToReductionDisplayRate(bonus.LaserTickIntervalReductionMultiplier));
        appended |= AppendBonusFloatLine(sb, "굴러거리", bonus.LandingRollDistanceBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LandingRollDistancePercentMultiplier));
        appended |= AppendBonusFloatLine(sb, "굴러시간", bonus.LandingRollDurationBonus, WeaponStatBonusData.ToPercentDisplayRate(bonus.LandingRollDurationPercentMultiplier));
        appended |= AppendBonusFloatLine(sb, "관통피해비율", bonus.SawPierceDamageRatioBonus);
        return appended;
    }

    private static bool AppendBonusFloatLine(StringBuilder sb, string label, float flatBonus, float percentBonus = 0f)
    {
        bool hasFlat = Mathf.Abs(flatBonus) > 0.0001f;
        bool hasPercent = percentBonus > 0.0001f;
        if (!hasFlat && !hasPercent)
        {
            return false;
        }

        sb.Append(label).Append(": ");
        if (hasFlat)
        {
            sb.Append('+').Append(flatBonus.ToString("0.##"));
        }

        if (hasFlat && hasPercent)
        {
            sb.Append(", ");
        }

        if (hasPercent)
        {
            sb.Append('+').Append((percentBonus * 100f).ToString("0.#")).Append('%');
        }

        sb.AppendLine();
        return true;
    }

    private static bool AppendBonusIntLine(StringBuilder sb, string label, int bonus)
    {
        if (bonus == 0)
        {
            return false;
        }

        sb.Append(label).Append(": +").Append(bonus).AppendLine();
        return true;
    }

    private static bool AppendBonusReductionLine(StringBuilder sb, string label, string prefix, float reductionRate)
    {
        if (reductionRate <= 0.0001f)
        {
            return false;
        }

        sb.Append(label).Append(": (").Append(prefix).Append('-').Append((reductionRate * 100f).ToString("0.#")).Append("%)").AppendLine();
        return true;
    }

    private static string ResolveSegmentStatDisplayTitle(CoreStatProvider core, string segmentId)
    {
        if (core != null && core.TryFindSegmentEntry(segmentId, out SegmentCatalogEntry catalogEntry))
        {
            if (!string.IsNullOrWhiteSpace(catalogEntry.DisplayName))
            {
                return catalogEntry.DisplayName.Trim();
            }
        }

        return segmentId;
    }

    private static bool HasAnyTierValue(float normal, float rare, float unique)
    {
        return Mathf.Abs(normal) > 0.0001f || Mathf.Abs(rare) > 0.0001f || Mathf.Abs(unique) > 0.0001f;
    }

    private static bool HasAnyTierValue(int normal, int rare, int unique)
    {
        return normal != 0 || rare != 0 || unique != 0;
    }

    private static string BuildTierPrefixedWeaponDescription(WeaponDefinition definition, StatUpgrade.StatCardTier tier)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        string description = string.IsNullOrWhiteSpace(definition.Description)
            ? definition.NormalizedId
            : definition.Description;
        if (string.IsNullOrWhiteSpace(description) || !description.Contains(DescriptionValueToken))
        {
            description = BuildDefaultWeaponDescription(definition);
        }

        return BuildWeaponDescriptionWithValue(description, definition, tier);
    }

    private static void ApplyTierPrefixToCardDescription(GameObject root, StatUpgrade.StatCardTier tier, bool isReduction)
    {
        TMP_Text descText = FindCardDescriptionText(root);
        if (descText == null || string.IsNullOrWhiteSpace(descText.text))
        {
            return;
        }

        descText.richText = true;
        descText.text = BuildTierPrefixedDescription(descText.text, tier, isReduction);
    }

    private static string BuildTierPrefixedDescription(string description, StatUpgrade.StatCardTier tier, bool isReduction)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return string.Empty;
        }

        return StripTierSymbols(description);
    }

    private static string BuildWeaponDescriptionWithValue(string description, WeaponDefinition definition, StatUpgrade.StatCardTier tier)
    {
        string body = StripTierSymbols(description);
        if (string.IsNullOrWhiteSpace(body) || !body.Contains(DescriptionValueToken))
        {
            return body;
        }

        List<string> values = BuildWeaponDescriptionValues(definition, tier);
        if (values.Count == 0)
        {
            return body.Replace(DescriptionValueToken, string.Empty).Trim();
        }

        string[] lines = SplitDescriptionLines(body);
        int valueIndex = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains(DescriptionValueToken))
            {
                continue;
            }

            string valueText = valueIndex < values.Count ? values[valueIndex] : string.Empty;
            valueIndex++;
            string replacement = string.IsNullOrWhiteSpace(valueText)
                ? string.Empty
                : $"<color={DescriptionValueColor}><b>{valueText}</b></color>";
            lines[i] = lines[i].Replace(DescriptionValueToken, replacement);
        }

        return string.Join("\n", lines).Trim();
    }

    private static string BuildDefaultWeaponDescription(WeaponDefinition definition)
    {
        if (definition == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        AppendDefaultWeaponDescriptionLine(builder, "공격력 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetBaseDamage(StatUpgrade.StatCardTier.Normal), definition.GetBaseDamage(StatUpgrade.StatCardTier.Rare), definition.GetBaseDamage(StatUpgrade.StatCardTier.Unique), definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Normal), definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Rare), definition.GetBaseDamagePercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "관통피해율 (N) 증가", HasDescriptionTierValue(definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Normal), definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Rare), definition.GetSawPierceDamageRatio(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "투사체 속도 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Normal), definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Rare), definition.GetProjectileSpeed(StatUpgrade.StatCardTier.Unique), definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Normal), definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Rare), definition.GetProjectileSpeedPercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "사거리 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetSearchRange(StatUpgrade.StatCardTier.Normal), definition.GetSearchRange(StatUpgrade.StatCardTier.Rare), definition.GetSearchRange(StatUpgrade.StatCardTier.Unique), definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Normal), definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Rare), definition.GetSearchRangePercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "연쇄 단계 (N) 증가", HasDescriptionTierValue(definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Normal), definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Rare), definition.GetMaxChainDepth(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "연쇄 거리 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetChainRange(StatUpgrade.StatCardTier.Normal), definition.GetChainRange(StatUpgrade.StatCardTier.Rare), definition.GetChainRange(StatUpgrade.StatCardTier.Unique), definition.GetChainRangePercent(StatUpgrade.StatCardTier.Normal), definition.GetChainRangePercent(StatUpgrade.StatCardTier.Rare), definition.GetChainRangePercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "체인 피해 유지율 (N) 증가", HasDescriptionTierValue(definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Normal), definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Rare), definition.GetChainDamageFalloff(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "발사 수 (N) 증가", HasDescriptionTierValue(definition.GetProjectileCount(StatUpgrade.StatCardTier.Normal), definition.GetProjectileCount(StatUpgrade.StatCardTier.Rare), definition.GetProjectileCount(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "쿨타임 (N) 감소", HasDescriptionTierValue(definition.GetCooldownReduction(StatUpgrade.StatCardTier.Normal), definition.GetCooldownReduction(StatUpgrade.StatCardTier.Rare), definition.GetCooldownReduction(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "부채꼴 각도 (N) 증가", HasDescriptionTierValue(definition.GetSideConeAngle(StatUpgrade.StatCardTier.Normal), definition.GetSideConeAngle(StatUpgrade.StatCardTier.Rare), definition.GetSideConeAngle(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "지속시간 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetLaserDuration(StatUpgrade.StatCardTier.Normal), definition.GetLaserDuration(StatUpgrade.StatCardTier.Rare), definition.GetLaserDuration(StatUpgrade.StatCardTier.Unique), definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Normal), definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Rare), definition.GetLaserDurationPercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "틱 간격 (N) 감소", HasDescriptionTierValue(definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Normal), definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Rare), definition.GetLaserTickInterval(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "구르기 거리 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDistance(StatUpgrade.StatCardTier.Unique), definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDistancePercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "구르기 시간 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDuration(StatUpgrade.StatCardTier.Unique), definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Normal), definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Rare), definition.GetLandingRollDurationPercent(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "관통 수 (N) 증가", HasDescriptionTierValue(definition.GetPierceCount(StatUpgrade.StatCardTier.Normal), definition.GetPierceCount(StatUpgrade.StatCardTier.Rare), definition.GetPierceCount(StatUpgrade.StatCardTier.Unique)));
        AppendDefaultWeaponDescriptionLine(builder, "폭발 반경 (N) 증가", HasDescriptionFlatOrPercentValue(definition.GetExplosionRadius(StatUpgrade.StatCardTier.Normal), definition.GetExplosionRadius(StatUpgrade.StatCardTier.Rare), definition.GetExplosionRadius(StatUpgrade.StatCardTier.Unique), definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Normal), definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Rare), definition.GetExplosionRadiusPercent(StatUpgrade.StatCardTier.Unique)));
        string result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? definition.NormalizedId : result;
    }

    private static void AppendDefaultWeaponDescriptionLine(StringBuilder builder, string line, bool active)
    {
        if (!active)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
        }

        builder.Append(line);
    }

    private static List<string> BuildWeaponDescriptionValues(WeaponDefinition definition, StatUpgrade.StatCardTier tier)
    {
        List<string> values = new List<string>(4);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetBaseDamage(tier), definition.GetBaseDamagePercent(tier), out string baseDamageValue), baseDamageValue);
        AddWeaponDescriptionValue(values, TryFormatPercentRate(definition.GetSawPierceDamageRatio(tier), out string pierceRatioValue), pierceRatioValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetProjectileSpeed(tier), definition.GetProjectileSpeedPercent(tier), out string projectileSpeedValue), projectileSpeedValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetSearchRange(tier), definition.GetSearchRangePercent(tier), "M", out string searchRangeValue), searchRangeValue);
        AddWeaponDescriptionValue(values, TryFormatIntValue(definition.GetMaxChainDepth(tier), out string chainDepthValue), chainDepthValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetChainRange(tier), definition.GetChainRangePercent(tier), "M", out string chainRangeValue), chainRangeValue);
        AddWeaponDescriptionValue(values, TryFormatPercentRate(definition.GetChainDamageFalloff(tier), out string chainFalloffValue), chainFalloffValue);
        AddWeaponDescriptionValue(values, TryFormatIntValue(definition.GetProjectileCount(tier), out string projectileCountValue), projectileCountValue);
        AddWeaponDescriptionValue(values, TryFormatPercentRate(definition.GetCooldownReduction(tier), out string cooldownValue), cooldownValue);
        AddWeaponDescriptionValue(values, TryFormatFloatValue(definition.GetSideConeAngle(tier), out string sideConeValue), sideConeValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetLaserDuration(tier), definition.GetLaserDurationPercent(tier), out string laserDurationValue), laserDurationValue);
        AddWeaponDescriptionValue(values, TryFormatPercentRate(definition.GetLaserTickInterval(tier), out string laserTickValue), laserTickValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetLandingRollDistance(tier), definition.GetLandingRollDistancePercent(tier), "M", out string rollDistanceValue), rollDistanceValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetLandingRollDuration(tier), definition.GetLandingRollDurationPercent(tier), out string rollDurationValue), rollDurationValue);
        AddWeaponDescriptionValue(values, TryFormatIntValue(definition.GetPierceCount(tier), out string pierceCountValue), pierceCountValue);
        AddWeaponDescriptionValue(values, TryFormatFlatOrPercentValue(definition.GetExplosionRadius(tier), definition.GetExplosionRadiusPercent(tier), "M", out string explosionRadiusValue), explosionRadiusValue);
        return values;
    }

    private static void AddWeaponDescriptionValue(List<string> values, bool active, string valueText)
    {
        if (active)
        {
            values.Add(valueText);
        }
    }

    private static string[] SplitDescriptionLines(string text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? System.Array.Empty<string>()
            : text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
    }

    private static bool HasDescriptionFlatOrPercentValue(float normal, float rare, float unique, float normalPercent, float rarePercent, float uniquePercent)
    {
        return HasDescriptionTierValue(normal, rare, unique) || HasDescriptionTierValue(normalPercent, rarePercent, uniquePercent);
    }

    private static bool HasDescriptionTierValue(float normal, float rare, float unique)
    {
        return Mathf.Abs(normal) > 0.0001f || Mathf.Abs(rare) > 0.0001f || Mathf.Abs(unique) > 0.0001f;
    }

    private static bool HasDescriptionTierValue(int normal, int rare, int unique)
    {
        return normal != 0 || rare != 0 || unique != 0;
    }

    private static bool TryResolveWeaponDescriptionValue(WeaponDefinition definition, StatUpgrade.StatCardTier tier, out string valueText)
    {
        valueText = string.Empty;
        if (definition == null)
        {
            return false;
        }

        if (TryFormatFlatOrPercentValue(definition.GetBaseDamage(tier), definition.GetBaseDamagePercent(tier), out valueText)) return true;
        if (TryFormatPercentRate(definition.GetSawPierceDamageRatio(tier), out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetProjectileSpeed(tier), definition.GetProjectileSpeedPercent(tier), out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetSearchRange(tier), definition.GetSearchRangePercent(tier), "M", out valueText)) return true;
        if (TryFormatIntValue(definition.GetMaxChainDepth(tier), out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetChainRange(tier), definition.GetChainRangePercent(tier), "M", out valueText)) return true;
        if (TryFormatPercentRate(definition.GetChainDamageFalloff(tier), out valueText)) return true;
        if (TryFormatIntValue(definition.GetProjectileCount(tier), out valueText)) return true;
        if (TryFormatPercentRate(definition.GetCooldownReduction(tier), out valueText)) return true;
        if (TryFormatFloatValue(definition.GetSideConeAngle(tier), out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetLaserDuration(tier), definition.GetLaserDurationPercent(tier), out valueText)) return true;
        if (TryFormatPercentRate(definition.GetLaserTickInterval(tier), out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetLandingRollDistance(tier), definition.GetLandingRollDistancePercent(tier), "M", out valueText)) return true;
        if (TryFormatFlatOrPercentValue(definition.GetLandingRollDuration(tier), definition.GetLandingRollDurationPercent(tier), out valueText)) return true;
        if (TryFormatIntValue(definition.GetPierceCount(tier), out valueText)) return true;
        return TryFormatFlatOrPercentValue(definition.GetExplosionRadius(tier), definition.GetExplosionRadiusPercent(tier), "M", out valueText);
    }

    private static bool TryFormatFlatOrPercentValue(float flatValue, float percentValue, out string valueText)
    {
        return TryFormatFlatOrPercentValue(flatValue, percentValue, string.Empty, out valueText);
    }

    private static bool TryFormatFlatOrPercentValue(float flatValue, float percentValue, string flatSuffix, out string valueText)
    {
        if (TryFormatPercentRate(percentValue, out valueText))
        {
            return true;
        }

        if (!TryFormatFloatValue(flatValue, out valueText))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(flatSuffix))
        {
            valueText += flatSuffix;
        }

        return true;
    }

    private static bool TryFormatFloatValue(float value, out string valueText)
    {
        valueText = string.Empty;
        if (Mathf.Abs(value) <= 0.0001f)
        {
            return false;
        }

        valueText = value.ToString("0.###", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFormatIntValue(int value, out string valueText)
    {
        valueText = string.Empty;
        if (value == 0)
        {
            return false;
        }

        valueText = value.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryFormatPercentRate(float value, out string valueText)
    {
        valueText = string.Empty;
        if (value <= 0.0001f)
        {
            return false;
        }

        valueText = $"{(value * 100f).ToString("0.#", CultureInfo.InvariantCulture)}%";
        return true;
    }

    private static string StripTierSymbols(string text)
    {
        string trimmed = text.Trim();
        int index = 0;
        while (index < trimmed.Length && (trimmed[index] == '+' || trimmed[index] == '-'))
        {
            index++;
        }

        string body = trimmed;
        if (index > 0 && index < trimmed.Length && char.IsWhiteSpace(trimmed[index]))
        {
            body = trimmed.Substring(index).TrimStart();
        }

        int symbolStart = body.Length;
        while (symbolStart > 0 && (body[symbolStart - 1] == '+' || body[symbolStart - 1] == '-'))
        {
            symbolStart--;
        }

        if (symbolStart < body.Length && (symbolStart == 0 || char.IsWhiteSpace(body[symbolStart - 1])))
        {
            body = body.Substring(0, symbolStart).TrimEnd();
        }

        return body;
    }

    private static TMP_Text FindCardDescriptionText(GameObject root)
    {
        if (root == null)
        {
            return null;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "DescText")
            {
                return texts[i];
            }
        }

        return null;
    }

    private static bool Includes(SegmentWeaponStatDisplayFlags flags, SegmentWeaponStatDisplayFlags target)
    {
        return (flags & target) != 0;
    }

    private static void AppendStatLineFloat(StringBuilder sb, string label, float baseValue, float effectiveValue, float flatBonus, float percentBonus = 0f)
    {
        sb.Append(label).Append(": ").Append(effectiveValue.ToString("0.##"));
        bool hasFlat = Mathf.Abs(flatBonus) > 0.0001f;
        bool hasPercent = percentBonus > 0.0001f;
        if (hasFlat || hasPercent)
        {
            sb.Append(" (");
            if (hasFlat)
            {
                sb.Append('+').Append(flatBonus.ToString("0.##"));
            }

            if (hasFlat && hasPercent)
            {
                sb.Append(", ");
            }

            if (hasPercent)
            {
                sb.Append('+').Append((percentBonus * 100f).ToString("0.#")).Append('%');
            }

            sb.Append(')');
        }
        else if (Mathf.Abs(effectiveValue - baseValue) > 0.0001f)
        {
            sb.Append(" (기본 ").Append(baseValue.ToString("0.##")).Append(')');
        }

        sb.AppendLine();
    }

    private static void AppendStatLineInt(StringBuilder sb, string label, int baseValue, int effectiveValue, int bonusDelta)
    {
        sb.Append(label).Append(": ").Append(effectiveValue);
        if (bonusDelta != 0)
        {
            sb.Append(" (+").Append(bonusDelta).Append(')');
        }
        else if (effectiveValue != baseValue)
        {
            sb.Append(" (기본 ").Append(baseValue).Append(')');
        }

        sb.AppendLine();
    }
    // 건춘추가 - 0621 ======

    //전찬우 수정-0622
    private void TrySubscribeSegmentWeaponStatDebug() // CoreStatProvider 변경 구독
    {
        CoreStatProvider core = CoreStatProvider.Active;
        if (core == null || core == segmentWeaponStatSubscribedCore)
        {
            return; // 코어 없음 / 이미 구독
        }

        UnsubscribeSegmentWeaponStatDebug();
        segmentWeaponStatSubscribedCore = core;
        segmentWeaponStatSubscribedCore.StatsChanged += HandleCoreStatsChangedForWeaponStatDebug;
    }

    private void UnsubscribeSegmentWeaponStatDebug()
    {
        if (segmentWeaponStatSubscribedCore == null)
        {
            return; // 구독 없음
        }

        segmentWeaponStatSubscribedCore.StatsChanged -= HandleCoreStatsChangedForWeaponStatDebug;
        segmentWeaponStatSubscribedCore = null;
    }

    private void HandleCoreStatsChangedForWeaponStatDebug(CoreStatData stats)
    {
        RefreshSegmentWeaponStatUi(); // 리셋·디버그 버튼·외부 변경 반영
    }

    // =============== 세그먼트 구성 디버그 ===============
    private ConvoyController segmentDebugSubscribedConvoy; // 구독 중인 컨보이

    private void TrySubscribeSegmentCountDebug() // Convoy 세그먼트 수 변경 구독
    {
        ConvoyController convoy = CoreStatProvider.Active != null ? CoreStatProvider.Active.Convoy : null; // 현재 컨보이
        if (convoy == null || convoy == segmentDebugSubscribedConvoy)
        {
            return; // 컨보이 없음 / 이미 구독
        }

        UnsubscribeSegmentCountDebug(); // 이전 구독 해제
        segmentDebugSubscribedConvoy = convoy;
        segmentDebugSubscribedConvoy.SegmentCountChanged += HandleConvoySegmentCountChangedForDebug; // 추가/제거 알림
    }

    private void UnsubscribeSegmentCountDebug()
    {
        if (segmentDebugSubscribedConvoy == null)
        {
            return; // 구독 없음
        }

        segmentDebugSubscribedConvoy.SegmentCountChanged -= HandleConvoySegmentCountChangedForDebug;
        segmentDebugSubscribedConvoy = null;
    }

    private void HandleConvoySegmentCountChangedForDebug(int segmentCount) // 세그먼트 수 변경 시
    {
        LogPlayerSegmentCountsDebug($"전체 세그먼트 : {segmentCount} / 각 세그먼트 : "); // CoreTest·카드 UI 공통
        RefreshSegmentWeaponStatUi(); // 건춘추가 - 0621 ====== 레벨업·추가 후 스탯 UI 갱신
        RefreshSegmentListText(); // 안건준 추가 - 0622 — 세그먼트 변경 시 리스트 텍스트 갱신
    }

    private void LogPlayerSegmentCountsDebug(string reason) // ConvoySegments 현재 구성 출력
    {
        if (!logPlayerSegmentCounts)
        {
            return; // 비활성
        }

        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        ConvoyController convoy = core != null ? core.Convoy : null; // 플레이어 컨보이
        if (convoy == null)
        {
            Debug.LogWarning($"{reason} | Convoy 없음 — 세그먼트 집계 불가");
            return;
        }

        Dictionary<string, int> countsBySegmentId = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase); // ID별 개수
        convoy.CollectAttachedSegmentCounts(countsBySegmentId); // SG01_Cannon 등 집계
        int total = convoy.GetAttachedSegmentTotalCount(); // 전체 개수 (스타터 포함)

        List<string> sortedSegmentIds = new List<string>(countsBySegmentId.Keys); // 정렬용
        sortedSegmentIds.Sort(System.StringComparer.OrdinalIgnoreCase);

        StringBuilder builder = new StringBuilder(128);        
        builder.Append(reason);
        // builder.Append(" 전체 세그먼트 숫자 : ");
        // builder.Append(total);

        for (int i = 0; i < sortedSegmentIds.Count; i++)
        {
            string segmentId = sortedSegmentIds[i]; // SG01_Cannon / SG02_Missile 등
            builder.Append(" , ");
            builder.Append(segmentId);
            builder.Append(' ');
            builder.Append(countsBySegmentId[segmentId]);
        }

        Debug.Log(builder.ToString());
    }
    // =============== 끝 ===============

    private SpawnedCardEntry CreateSpawnedCard(GameObject prefab, RectTransform slot, GameObject sourcePrefab = null, bool skipStatUpgradeRoll = false) // sourcePrefab: 선택 가중치용 원본 프리팹
    {
        if (prefab == null || slot == null)
        {
            return null;
        }

        GameObject instance = Instantiate(prefab, slot); // 프리팹 생성
        RectTransform rectTransform = instance.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("[CardUI] 카드 프리팹에 RectTransform이 없습니다.", prefab);
            Destroy(instance);
            return null;
        }

        CanvasGroup canvasGroup = instance.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = instance.GetComponentInChildren<CanvasGroup>(true);
        }

        if (canvasGroup == null)
        {
            Debug.LogWarning("[CardUI] 카드 프리팹에 CanvasGroup이 없습니다.", prefab);
            Destroy(instance);
            return null;
        }

        StatUpgrade statUpgrade = instance.GetComponent<StatUpgrade>();
        if (statUpgrade == null)
        {
            statUpgrade = instance.GetComponentInChildren<StatUpgrade>(true);
        }

        SegmentAddCard segmentAddCard = instance.GetComponent<SegmentAddCard>();
        if (segmentAddCard == null)
        {
            segmentAddCard = instance.GetComponentInChildren<SegmentAddCard>(true);
        }

        // 안건준 추가 - 0623 : SegmentUpgradeCard 같이 SegmentAddCard가 없는 커스텀 프리팹도 텍스트 주입 가능하도록 자동 추가
        if (statUpgrade == null && segmentAddCard == null)
        {
            segmentAddCard = instance.AddComponent<SegmentAddCard>();
        }

        if (statUpgrade != null && !skipStatUpgradeRoll)
        {
            statUpgrade.RollSpawnVariant(rareCardChancePercent, uniqueCardChancePercent); // 등급(일반/레어/유니크) + 색상
        }

        ConfigureSpawnedRect(rectTransform, slot); // 프리팹 크기 유지 + 슬롯 중앙 배치

        SpawnedCardEntry entry = new SpawnedCardEntry
        {
            Root = instance,
            RootTransform = rectTransform,
            CanvasGroup = canvasGroup,
            StatUpgrade = statUpgrade,
            SegmentAddCard = segmentAddCard,
            SourcePrefab = sourcePrefab != null ? sourcePrefab : prefab, // null이면 prefab 자체를 추적
            OriginalPosition = rectTransform.anchoredPosition,
            OriginalScale = rectTransform.localScale,
            CanSelect = true // 기본 카드는 선택 가능
        };

        WireSpawnedCardInput(entry); // 클릭·호버 연결
        return entry;
    }

    private static void ConfigureSpawnedRect(RectTransform cardRect, RectTransform slot)
    {
        Vector2 sizeDelta = cardRect.sizeDelta; // 프리팹 원본 크기
        Vector3 localScale = cardRect.localScale; // 프리팹 원본 스케일
        Vector2 pivot = cardRect.pivot; // 프리팹 피벗

        cardRect.anchorMin = new Vector2(0.5f, 0.5f); // 슬롯 중앙 기준
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = pivot;
        cardRect.sizeDelta = sizeDelta; // stretch 금지 → 프리팹과 동일 크기
        cardRect.anchoredPosition = Vector2.zero; // 슬롯 중심 (프리팹 편집용 -350 등 제거)
        cardRect.localScale = localScale;

        if (slot != null && sizeDelta.sqrMagnitude > 0f)
        {
            slot.sizeDelta = sizeDelta; // 슬롯도 프리팹 크기에 맞춤
        }
    }

    private void WireSpawnedCardInput(SpawnedCardEntry entry)
    {
        Button button = entry.Root.GetComponent<Button>();
        if (button == null)
        {
            button = entry.Root.GetComponentInChildren<Button>(true);
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => NotifySpawnedCardClicked(entry));
        }

        CardInstanceBridge bridge = entry.Root.GetComponent<CardInstanceBridge>();
        if (bridge == null)
        {
            bridge = entry.Root.AddComponent<CardInstanceBridge>();
        }

        bridge.Initialize(this, entry);
    }

    private static List<GameObject> BuildPrefabPool(GameObject[] prefabs)
    {
        List<GameObject> pool = new List<GameObject>(prefabs.Length);
        for (int i = 0; i < prefabs.Length; i++)
        {
            if (prefabs[i] != null)
            {
                pool.Add(prefabs[i]);
            }
        }

        return pool;
    }

    private List<GameObject> PickWeightedStatPrefabs(List<GameObject> pool, int count) // 스탯 카드 가중치 랜덤 선택
    {
        List<WeightedPrefabEntry> remaining = new List<WeightedPrefabEntry>(pool.Count); // 남은 후보+가중치
        for (int i = 0; i < pool.Count; i++)
        {
            GameObject prefab = pool[i]; // 후보 프리팹
            if (prefab == null)
            {
                continue; // null 제외
            }

            float weight = baseCardSpawnWeight; // 기본 가중치
            if (lastSelectedStatCardPrefab != null && prefab == lastSelectedStatCardPrefab)
            {
                weight += selectedCardWeightBonus; // 직전 선택 카드 가중치 증가
            }

            remaining.Add(new WeightedPrefabEntry
            {
                Prefab = prefab, // 후보 프리팹
                Weight = weight // 최종 가중치
            });
        }

        List<GameObject> picked = new List<GameObject>(count); // 선택 결과
        int pickCount = Mathf.Min(count, remaining.Count); // 뽑을 수량
        for (int pickIndex = 0; pickIndex < pickCount; pickIndex++)
        {
            if (!TryPickWeightedPrefab(remaining, out WeightedPrefabEntry selected))
            {
                break; // 더 이상 선택 불가
            }

            picked.Add(selected.Prefab); // 선택된 프리팹 추가
            remaining.Remove(selected); // 중복 방지를 위해 후보에서 제거
        }

        return picked; // 최종 3장(또는 가능한 수)
    }

    private static bool TryPickWeightedPrefab(List<WeightedPrefabEntry> pool, out WeightedPrefabEntry selected) // 가중치 1장 뽑기
    {
        selected = default; // 기본값
        if (pool == null || pool.Count == 0)
        {
            return false; // 후보 없음
        }

        float totalWeight = 0f; // 전체 가중치 합
        for (int i = 0; i < pool.Count; i++)
        {
            totalWeight += pool[i].Weight; // 가중치 누적
        }

        if (totalWeight <= 0f)
        {
            selected = pool[pool.Count - 1]; // fallback: 마지막 후보
            return true;
        }

        float roll = Random.Range(0f, totalWeight); // 0~합계 난수
        float cumulative = 0f; // 누적 구간
        selected = pool[pool.Count - 1]; // fallback
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += pool[i].Weight; // 구간 확장
            if (roll < cumulative)
            {
                selected = pool[i]; // 해당 구간 당첨
                return true;
            }
        }

        return true; // 부동소수 오차 fallback
    }

    private void RememberSelectedStatCardPrefab(GameObject sourcePrefab) // 직전 선택 카드 저장
    {
        if (sourcePrefab == null)
        {
            return; // 저장할 프리팹 없음
        }

        lastSelectedStatCardPrefab = sourcePrefab; // 다음 SpawnLevelUpCards에서 가중치 적용
    }

    private struct WeightedPrefabEntry // 가중치 뽑기용 임시 구조체
    {
        public GameObject Prefab; // 카드 프리팹
        public float Weight; // 등장 가중치
    }

    private struct WeightedSegmentCatalogEntry // A 모드 세그먼트 선택 가중치용
    {
        public SegmentCatalogEntry Entry; // 카탈로그 후보
        public float Weight; // 등장 가중치
    }

    private static List<GameObject> PickRandomPrefabs(List<GameObject> pool, int count)
    {
        List<GameObject> shuffled = new List<GameObject>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            GameObject temp = shuffled[i];
            shuffled[i] = shuffled[swapIndex];
            shuffled[swapIndex] = temp;
        }

        int pickCount = Mathf.Min(count, shuffled.Count);
        return shuffled.GetRange(0, pickCount);
    }

    private void PlaySpawnOpenTween(IReadOnlyList<SpawnedCardEntry> cards)
    {
        if (cards == null || cards.Count == 0)
        {
            return;
        }

        // 카드 패널 등장 사운드 - 레벨업마다 패널이 열릴 때 1회 재생
        cardSound?.PlayCardAppear();

        for (int i = 0; i < cards.Count; i++)
        {
            HideInstant(cards[i]);
        }

        // 이팩트는 카드 오픈 트윈 완료 후 적용 (GetWorldCorners 정확도 보장)
        // 스탯 강화(None), 세그먼트 강화(EnhanceChoice) 카드만 VFX 적용
        if (cardEffect != null)
        {
            const float openDuration = 0.35f;
            const float openInterval = 0.12f;
            for (int i = 0; i < cards.Count; i++)
            {
                SpawnedCardEntry captured = cards[i];
                bool applyVfx = captured.SegmentRole == SegmentCardRole.None
                             || captured.SegmentRole == SegmentCardRole.EnhanceChoice;
                if (!applyVfx) continue;

                float delay = i * openInterval + openDuration;
                DOVirtual.DelayedCall(delay, () =>
                {
                    if (captured?.Root != null)
                        cardEffect.ApplyEffect(captured.Root, GetCardTier(captured));
                }, ignoreTimeScale: true);
            }
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < cards.Count; i++)
        {
            int index = i;
            sequence.AppendCallback(() => PlayOpenTween(cards[index]));
            sequence.AppendInterval(0.12f);
        }
    }

    private void HideInstant(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.IsClickable = false;
        entry.RootTransform.DOKill();
        entry.CanvasGroup.DOKill();
        entry.RootTransform.DOKill();
        entry.CanvasGroup.alpha = 0f;
        entry.CanvasGroup.blocksRaycasts = false;
        entry.CanvasGroup.interactable = false;
        entry.RootTransform.anchoredPosition = entry.OriginalPosition + new Vector2(0f, startYOffset);
        entry.RootTransform.localScale = Vector3.zero;
    }

    private void PlayOpenTween(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.IsClickable = entry.CanSelect; // 없음/불가 카드는 호버/클릭 비활성
        entry.CanvasGroup.blocksRaycasts = true;
        entry.CanvasGroup.interactable = entry.CanSelect; // 선택 불가 카드 입력 차단

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(entry.CanvasGroup.DOFade(1f, 0.25f));
        sequence.Join(entry.RootTransform.DOAnchorPos(entry.OriginalPosition, 0.35f).SetEase(Ease.OutCubic));
        sequence.Join(entry.RootTransform.DOScale(entry.OriginalScale, 0.35f).SetEase(Ease.OutBack));
    }

    private Tween PlaySelectTween(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        entry.IsClickable = false;
        entry.RootTransform.DOKill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(entry.RootTransform.DOScale(entry.OriginalScale * 1.2f, selectionSelectUpSeconds).SetEase(Ease.OutBack));
        sequence.Append(entry.RootTransform.DOScale(entry.OriginalScale, selectionSelectDownSeconds));
        return sequence;
    }

    private Tween PlayHideTween(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        entry.IsClickable = false;
        entry.RootTransform.DOKill();
        entry.CanvasGroup.DOKill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(entry.CanvasGroup.DOFade(0f, 0.2f));
        sequence.Join(entry.RootTransform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        return sequence;
    }

    private void HandleCardClicked(SpawnedCardEntry selectedEntry)
    {
        if (isProcessingSelection || selectedEntry == null || !selectedEntry.CanSelect)
        {
            return;
        }

        // 카드 선택 사운드
        cardSound?.PlayCardSelect();

        if (selectedEntry.SegmentRole == SegmentCardRole.Candidate)
        {
            HandleSegmentCandidateClicked(selectedEntry); // 세그먼트 후보 → 2단계 분기
            return;
        }

        if (selectedEntry.SegmentRole == SegmentCardRole.EnhanceChoice)
        {
            if (!TryApplySelectedCard(selectedEntry))
            {
                return;
            }

            isProcessingSelection = true; // 선택 처리 중
            PlaySelectionCloseSequence(selectedEntry); // 강화 적용 후 패널 닫기
            return;
        }

        if (!TryApplySelectedCard(selectedEntry))
        {
            return;
        }

        if (selectedEntry.StatUpgrade != null) // 스탯 카드 선택 성공 시
        {
            RememberSelectedStatCardPrefab(selectedEntry.SourcePrefab); // 다음 선택지에서 같은 카드 가중치 증가
        }

        isProcessingSelection = true;
        PlaySelectionCloseSequence(selectedEntry);
    }

    // 세그먼트 후보 클릭 - 무기강화/추가·레벨업 흐름 분기
    private void HandleSegmentCandidateClicked(SpawnedCardEntry selectedEntry)
    {
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        if (core == null || string.IsNullOrWhiteSpace(selectedEntry.SegmentId))
        {
            Debug.LogWarning("[CardUI] 세그먼트 후보 적용 실패: CoreStatProvider 또는 SegmentId 없음", selectedEntry.Root);
            return;
        }

        SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 후보 선택 즉시 해당 세그먼트 스탯 표시

        if (currentSpawnPhase == LevelUpCardPhase.WeaponEnhance && useSegmentSelectWeaponEnhanceFlow)
        {
            isProcessingSelection = true; // 2단계 전환 중
            PlaySegmentEnhancementChoiceSequence(selectedEntry); // A: 선택 세그먼트 강화 카드
            return;
        }

        bool canAdd = core.CanAddSegment(selectedEntry.SegmentId); // 추가 가능 여부
        bool canLevelUp = core.CanLevelUpSegmentModel(selectedEntry.SegmentId); // 레벨업 가능 여부
        isProcessingSelection = true; // 2단계 전환 중

        if (TryResolveSingleSegmentAction(canAdd, canLevelUp, out SegmentCardRole singleActionRole))
        {
            PlaySingleSegmentActionAutoApplySequence(selectedEntry, singleActionRole, canAdd, canLevelUp); // 선택지 1개면 2차 창 스킵
            return;
        }

        PlaySegmentActionChoiceSequence(selectedEntry, canAdd, canLevelUp); // 추가/레벨업 2장
    }

    // 무기 강화 1단계 → 2단계: 세그먼트 선택 후 강화 카드 3장 표시
    private void PlaySegmentEnhancementChoiceSequence(SpawnedCardEntry selectedEntry)
    {
        Sequence sequence = DOTween.Sequence().SetUpdate(true); // 선택 연출

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i]; // 현재 카드
            if (card == null)
            {
                continue; // null 방지
            }

            if (card == selectedEntry)
            {
                sequence.Join(PlaySelectTween(card)); // 선택 카드 강조
            }
            else
            {
                sequence.Join(PlayHideTween(card)); // 나머지 카드 숨김
            }
        }

        sequence.AppendInterval(0.25f); // 전환 여유
        sequence.OnComplete(() =>
        {
            SpawnSegmentEnhancementCards(selectedEntry.SegmentId, selectedEntry.LevelDelta); // 2단계: 무기 강화 카드
            isProcessingSelection = false; // 다시 클릭 허용
        });
    }

    // 후보 카드가 사라진 뒤 추가/레벨업 2차 카드 표시
    private void PlaySegmentActionChoiceSequence(SpawnedCardEntry selectedEntry, bool canAdd, bool canLevelUp)
    {
        SegmentCatalogEntry catalogEntry = selectedEntry.SegmentCatalogEntry; // 후보 데이터 보관
        int levelDelta = Mathf.Max(1, selectedEntry.LevelDelta); // 소비 레벨 보관
        Sequence sequence = DOTween.Sequence().SetUpdate(true); // 기존 선택 연출 재사용

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i]; // 현재 카드
            if (card == null)
            {
                continue; // null 방지
            }

            if (card == selectedEntry)
            {
                sequence.Join(PlaySelectTween(card)); // 선택 카드 강조
            }
            else
            {
                sequence.Join(PlayHideTween(card)); // 나머지 카드 숨김
            }
        }

        sequence.AppendInterval(0.25f); // 전환 여유
        sequence.OnComplete(() =>
        {
            SpawnSegmentActionCards(catalogEntry, levelDelta, canAdd, canLevelUp); // 2차 카드 생성
            isProcessingSelection = false; // 다시 클릭 허용
            TryStartAutoSelectSegmentAction(canAdd, canLevelUp); // 안건준 추가 - 0622 : 자동모드면 추가/레벨업 자동선택
        });
    }

    private static bool TryResolveSingleSegmentAction(bool canAdd, bool canLevelUp, out SegmentCardRole role) // 2차 선택지가 1개인지 판정
    {
        role = SegmentCardRole.None; // 기본값
        if (canAdd == canLevelUp)
        {
            return false; // 둘 다 가능하거나 둘 다 불가면 기존 2차 UI 유지
        }

        role = canAdd ? SegmentCardRole.AddAction : SegmentCardRole.LevelUpAction; // 유일한 액션
        return true;
    }

    private void PlaySingleSegmentActionAutoApplySequence(SpawnedCardEntry selectedEntry, SegmentCardRole actionRole, bool canAdd, bool canLevelUp)
    {
        SegmentCatalogEntry catalogEntry = selectedEntry.SegmentCatalogEntry; // 실패 fallback용 후보 데이터
        int levelDelta = Mathf.Max(1, selectedEntry.LevelDelta); // 소비 레벨 보관
        Sequence sequence = DOTween.Sequence().SetUpdate(true); // 후보 선택 연출 재사용

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i]; // 현재 후보 카드
            if (card == null)
            {
                continue; // null 방지
            }

            if (card == selectedEntry)
            {
                sequence.Join(PlaySelectTween(card)); // 선택 카드 강조
            }
            else
            {
                sequence.Join(PlayHideTween(card)); // 나머지 카드 숨김
            }
        }

        sequence.AppendInterval(0.25f); // 기존 2차 전환과 동일한 여유
        sequence.OnComplete(() =>
        {
            if (TryApplySingleSegmentAction(selectedEntry, actionRole))
            {
                cardEffect?.FadeAllEffects(0.2f); // 기존 닫기 경로와 동일하게 이펙트 정리
                CloseLevelUpPanelAfterSuccessfulSelection(); // 성공 시 바로 선택 완료
                return;
            }

            Debug.LogWarning("[CardUI] 단일 세그먼트 액션 자동 적용 실패: 2차 선택 UI로 fallback합니다.", selectedEntry.Root);
            SpawnSegmentActionCards(catalogEntry, levelDelta, canAdd, canLevelUp); // 실패 시 조작 가능한 화면 복구
            isProcessingSelection = false; // fallback 카드 클릭 허용
        });
    }

    private bool TryApplySingleSegmentAction(SpawnedCardEntry selectedEntry, SegmentCardRole actionRole) // 2차 카드 없이 코어에 직접 적용
    {
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        if (core == null || selectedEntry == null || string.IsNullOrWhiteSpace(selectedEntry.SegmentId))
        {
            Debug.LogWarning("[CardUI] 단일 세그먼트 액션 적용 실패: CoreStatProvider 또는 SegmentId 없음", selectedEntry?.Root);
            return false;
        }

        int levelDelta = Mathf.Max(1, selectedEntry.LevelDelta); // 소비 레벨
        if (actionRole == SegmentCardRole.AddAction)
        {
            int addCount = selectedEntry.SegmentAddCard != null ? selectedEntry.SegmentAddCard.SegmentAddCount : 1; // 카드 설정 우선
            bool applied = core.TryApplySegmentAddChoice(selectedEntry.SegmentId, levelDelta, addCount); // 추가 적용
            if (applied)
            {
                SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 추가 대상 표시
            }
            else
            {
                Debug.LogWarning("[CardUI] 세그먼트 추가 자동 적용 실패: 경험치/카탈로그/컨보이 조건 확인 필요", selectedEntry.Root);
            }

            return applied;
        }

        if (actionRole == SegmentCardRole.LevelUpAction)
        {
            bool applied = core.TryApplySegmentLevelUpChoice(selectedEntry.SegmentId, levelDelta); // 레벨업 적용
            if (applied)
            {
                SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 레벨업 대상 표시
            }
            else
            {
                Debug.LogWarning("[CardUI] 세그먼트 레벨업 자동 적용 실패: 만렙/경험치/장착 상태 확인 필요", selectedEntry.Root);
            }

            return applied;
        }

        return false; // 지원하지 않는 역할
    }

    private bool TryApplySelectedCard(SpawnedCardEntry selectedEntry)
    {
        if (selectedEntry.SegmentRole == SegmentCardRole.EnhanceChoice)
        {
            CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
            WeaponDefinition definition = selectedEntry.WeaponDefinition; // 선택 강화
            bool applied = core != null
                && definition != null
                && core.TryApplyWeaponEnhancementChoice(
                    selectedEntry.SegmentId,
                    selectedEntry.LevelDelta,
                    definition,
                    selectedEntry.WeaponEnhancementTier); // 강화 적용 (등급별 수치)
            if (applied)
            {
                SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 적용 대상 표시 유지
                LogWeaponEnhancementIncrease(selectedEntry.SegmentId, definition, core); // 누적 보너스 출력
            }
            else if (core == null)
            {
                Debug.LogWarning("[CardUI] 무기 강화 적용 실패: CoreStatProvider.Active 가 없습니다.", selectedEntry.Root);
            }

            return applied; // 적용 결과
        }

        if (selectedEntry.SegmentAddCard != null)
        {
            // 2차 선택 카드: 세그먼트 추가
            if (selectedEntry.SegmentRole == SegmentCardRole.AddAction)
            {
                CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
                bool applied = core != null && core.TryApplySegmentAddChoice(selectedEntry.SegmentId, selectedEntry.LevelDelta, selectedEntry.SegmentAddCard.SegmentAddCount); // 추가 적용
                if (!applied)
                {
                    Debug.LogWarning("[CardUI] 세그먼트 추가 적용 실패: 경험치/카탈로그/컨보이 조건 확인 필요", selectedEntry.Root); // 실패 로그
                }
                else
                {
                    SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 추가된 세그먼트 표시
                }

                return applied; // 적용 결과 — 성공 시 SegmentCountChanged 구독에서 디버그 1회 출력
            }

            // 2차 선택 카드: 해당 세그먼트 전체 모델 레벨업
            if (selectedEntry.SegmentRole == SegmentCardRole.LevelUpAction)
            {
                CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
                bool applied = core != null && core.TryApplySegmentLevelUpChoice(selectedEntry.SegmentId, selectedEntry.LevelDelta); // 레벨업 적용
                if (!applied)
                {
                    Debug.LogWarning("[CardUI] 세그먼트 레벨업 적용 실패: 만렙/경험치/장착 상태 확인 필요", selectedEntry.Root); // 실패 로그
                }
                else
                {
                    SetSegmentWeaponStatDebugTarget(selectedEntry.SegmentId); // 레벨업 대상 표시
                }

                return applied; // 적용 결과
            }

            return false; // 후보/없음 카드는 여기서 적용하지 않음
        }

        if (selectedEntry.StatUpgrade != null)
        {
            if (selectedEntry.StatUpgrade.TryApplyToCore())
            {
                return true;
            }

            Debug.LogWarning("[CardUI] 스탯 강화 적용 실패: CanLevelUp 미충족 또는 CoreStatProvider 없음", selectedEntry.Root);
            return false;
        }

        Debug.LogWarning("[CardUI] StatUpgrade/SegmentAddCard가 없어 코어에 반영하지 않습니다.", selectedEntry.Root);
        return false;
    }

    private void PlaySelectionCloseSequence(SpawnedCardEntry selectedEntry)
    {
        // 카드 페이드(0.2s)와 동시에 이팩트도 축소 후 제거
        cardEffect?.FadeAllEffects(0.2f);

        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i];
            if (card == null)
            {
                continue;
            }

            if (card == selectedEntry)
            {
                sequence.Join(PlaySelectTween(card));
            }
            else
            {
                sequence.Join(PlayHideTween(card));
            }
        }

        sequence.AppendInterval(Mathf.Max(0f, selectionCloseHoldSeconds));
        sequence.OnComplete(CloseLevelUpPanelAfterSuccessfulSelection);
    }

    private void CloseLevelUpPanelAfterSuccessfulSelection()
    {
        // 안건준 수정 - 0622 : 패널이 완전히 닫힌 후 CompleteLevelUpChoice 호출 — 연속 레벨업 대응
        CloseLevelUpPanelAfterSelection(() =>
        {
            // 패널이 같은 프레임에 재오픈될 경우 Update()가 닫힘을 감지 못하므로 여기서 직접 정리
            StopAutoSelect();           // 이전 자동선택 코루틴 정리
            ClearSpawnedCards();        // 이전 카드 오브젝트 파괴
            spawnedForCurrentOpen = false; // 다음 오픈 시 새 카드 생성 허용
            isProcessingSelection = false; // 입력 잠금 해제

            CoreStatProvider.Active?.CompleteLevelUpChoice(); // 순환 진행 + StatsChanged → 다음 레벨업 트리거
        });
    }

    private void CloseLevelUpPanelAfterSelection(System.Action onClosed = null) // 선택 완료 후 오버레이·일시정지 해제
    {
        LevelUpUi ui = ResolveLevelUpUi();
        if (ui != null)
        {
            ui.Close(selectionPanelCloseFadeSeconds, onClosed); // 페이드 완료 후 onClosed 실행
            return;
        }

        if (levelUpPanelCanvasGroup != null)
        {
            levelUpPanelCanvasGroup.DOKill();
            levelUpPanelCanvasGroup
                .DOFade(0f, Mathf.Max(0.01f, selectionPanelCloseFadeSeconds))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    levelUpPanelCanvasGroup.blocksRaycasts = false;
                    levelUpPanelCanvasGroup.interactable = false;
                    if (Time.timeScale <= 0f)
                    {
                        Time.timeScale = 1f; // LevelUpUi 없을 때 일시정지 해제
                    }

                    onClosed?.Invoke(); // 닫힌 후 콜백
                });
            return;
        }

        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f; // fallback
        }

        onClosed?.Invoke(); // 즉시 호출
    }

    private void ClearSpawnedCards()
    {
        // 안건준 추가 - 0623 : 카드 제거 전 이팩트 먼저 정리
        cardEffect?.ClearAll();

        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i]?.Root != null)
            {
                Destroy(spawnedCards[i].Root);
            }
        }

        spawnedCards.Clear();
    }

    private LevelUpUi ResolveLevelUpUi()
    {
        if (levelUpUi != null)
        {
            return levelUpUi;
        }

        return FindFirstObjectByType<LevelUpUi>();
    }

    // 안건준 추가 - 0622 ======
    private void SetupSegmentListHoverUi() // 호버 브릿지 부착 및 초기 비활성
    {
        HideSegmentListUi();

        if (segmentListPopup == null)
        {
            return;
        }

        EnsureUiRaycastTarget(segmentListPopup); // Image Raycast Target 보장

        SegmentListHoverBridge popupBridge = segmentListPopup.GetComponent<SegmentListHoverBridge>();
        if (popupBridge == null)
        {
            popupBridge = segmentListPopup.AddComponent<SegmentListHoverBridge>();
        }

        popupBridge.Initialize(this);

        // 안건준 수정 - 0622 — Segment List(+스크롤바 영역)에도 브릿지 추가: 리스트 위에서 스크롤 가능
        if (segmentList != null)
        {
            EnsureUiRaycastTarget(segmentList);
            SegmentListHoverBridge listBridge = segmentList.GetComponent<SegmentListHoverBridge>();
            if (listBridge == null)
            {
                listBridge = segmentList.AddComponent<SegmentListHoverBridge>();
            }

            listBridge.Initialize(this);
        }
    }

    private static void EnsureUiRaycastTarget(GameObject uiRoot) // 호버 감지용 Raycast
    {
        if (uiRoot == null || !uiRoot.TryGetComponent(out Graphic graphic))
        {
            return;
        }

        graphic.raycastTarget = true;
    }

    private void ShowSegmentListPopupOnPanelOpen() // 카드 패널 열릴 때 트리거 바 표시
    {
        if (segmentListPopup != null)
        {
            segmentListPopup.SetActive(true);
        }

        SetSegmentListVisible(false); // 리스트는 호버 전까지 숨김
    }

    private void HideSegmentListUi() // 패널 닫힐 때 전부 숨김
    {
        if (hideSegmentListCoroutine != null)
        {
            StopCoroutine(hideSegmentListCoroutine);
            hideSegmentListCoroutine = null;
        }

        SetSegmentListVisible(false);

        if (segmentListPopup != null)
        {
            segmentListPopup.SetActive(false);
        }
    }

    private void ShowSegmentListOnHover() // Popup/List 호버 시 목록 표시
    {
        if (hideSegmentListCoroutine != null)
        {
            StopCoroutine(hideSegmentListCoroutine);
            hideSegmentListCoroutine = null;
        }

        RefreshSegmentListText(); // 호버 시 최신 세그먼트 목록 갱신
        SetSegmentListVisible(true);
    }

    private void RequestHideSegmentListOnHoverExit() // Popup 또는 List에서 마우스가 나갔을 때
    {
        if (hideSegmentListCoroutine != null)
        {
            StopCoroutine(hideSegmentListCoroutine);
        }

        // 안건준 수정 - 0622 — 1프레임 대기 후 둘 다 벗어났는지 확인 (Popup→List 이동 시 깜빡임 방지)
        hideSegmentListCoroutine = StartCoroutine(HideSegmentListDelayed());
    }

    private IEnumerator HideSegmentListDelayed() // 1프레임 대기 후 호버 상태 재확인
    {
        yield return null; // 이동 중 false positive 방지

        // Popup 또는 List 위에 있으면 유지, 둘 다 아니면 숨김
        bool stillHovered = IsPointerOverSegmentUiArea();
        if (!stillHovered)
        {
            SetSegmentListVisible(false);
        }

        hideSegmentListCoroutine = null;
    }

    private bool IsPointerOverSegmentUiArea() // Popup 또는 Segment List 영역 위에 포인터 있는지
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        Vector2 screenPos;
        if (UnityEngine.InputSystem.Mouse.current != null)
        {
            screenPos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        }
        else
        {
            screenPos = Input.mousePosition;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = screenPos };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            Transform hit = results[i].gameObject.transform;

            if (segmentListPopup != null && segmentListPopup.activeInHierarchy
                && (hit == segmentListPopup.transform || hit.IsChildOf(segmentListPopup.transform)))
            {
                return true; // Popup 위
            }

            if (segmentList != null && segmentList.activeInHierarchy
                && (hit == segmentList.transform || hit.IsChildOf(segmentList.transform)))
            {
                return true; // List 또는 스크롤바 위
            }
        }

        return false;
    }

    private void SetSegmentListVisible(bool visible) // Segment List 활성/비활성
    {
        if (segmentList != null)
        {
            segmentList.SetActive(visible);
        }
    }

    private void RefreshSegmentListText() // 장착 세그먼트 이름:개수 텍스트 갱신
    {
        if (segmentListText == null)
        {
            return; // 텍스트 미연결
        }

        CoreStatProvider core = CoreStatProvider.Active;
        ConvoyController convoy = core != null ? core.Convoy : null;
        if (convoy == null)
        {
            segmentListText.text = string.Empty; // 컨보이 없으면 빈 텍스트
            return;
        }

        Dictionary<string, int> countsBySegmentId = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        convoy.CollectAttachedSegmentCounts(countsBySegmentId); // 현재 장착 세그먼트 ID별 개수

        Dictionary<string, string> idToDisplayName = BuildSegmentDisplayNameMap(); // ID → 표시명

        List<string> sortedIds = new List<string>(countsBySegmentId.Keys);
        sortedIds.Sort(System.StringComparer.OrdinalIgnoreCase); // 알파벳 순 정렬

        StringBuilder builder = new StringBuilder(256);
        for (int i = 0; i < sortedIds.Count; i++)
        {
            string segId = sortedIds[i];
            string displayName = idToDisplayName.TryGetValue(segId, out string found) ? found : segId; // 표시명 없으면 ID 그대로
            builder.Append(displayName);
            builder.Append(" : ");
            builder.Append(countsBySegmentId[segId]); // 개수
            if (i < sortedIds.Count - 1)
            {
                builder.AppendLine(); // 줄바꿈
            }
        }

        segmentListText.text = builder.ToString();
        ResizeSegmentListContent(); // 안건준 추가 - 0622 — 텍스트 높이에 맞게 Content 크기 조정
    }

    // 안건준 추가 - 0622
    private void ResizeSegmentListContent() // Content RT 높이를 TMP 필요 높이로 설정 (스크롤 활성화)
    {
        if (segmentListContent == null || segmentListText == null)
        {
            return; // 참조 미연결
        }

        segmentListText.ForceMeshUpdate(); // TMP 레이아웃 즉시 계산
        float needed = segmentListText.preferredHeight + 20f; // 텍스트 필요 높이 + 여유
        Vector2 sd = segmentListContent.sizeDelta;
        segmentListContent.sizeDelta = new Vector2(sd.x, Mathf.Max(needed, 50f)); // 최소 50px 보장
    }

    private Dictionary<string, string> BuildSegmentDisplayNameMap() // 카탈로그에서 ID→DisplayName 맵 빌드
    {
        Dictionary<string, string> map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        CoreStatProvider core = CoreStatProvider.Active;
        if (core == null)
        {
            return map;
        }

        List<SegmentCatalogEntry> entries = new List<SegmentCatalogEntry>();
        core.TryGetWeaponEnhanceChoiceCandidates(entries); // 카탈로그 전체 항목 (ID 있는 것)

        for (int i = 0; i < entries.Count; i++)
        {
            string id = entries[i].NormalizedId;
            if (string.IsNullOrWhiteSpace(id) || map.ContainsKey(id))
            {
                continue;
            }

            string name = !string.IsNullOrWhiteSpace(entries[i].DisplayName)
                ? entries[i].DisplayName
                : id; // DisplayName 없으면 ID 표시
            map[id] = name;
        }

        return map;
    }

    internal void NotifySegmentListHoverEnter() // 브릿지 → 호버 진입
    {
        if (!IsLevelUpPanelOpen())
        {
            return;
        }

        ShowSegmentListOnHover();
    }

    internal void NotifySegmentListHoverExit() // 브릿지 → 호버 이탈
    {
        if (!IsLevelUpPanelOpen())
        {
            return;
        }

        RequestHideSegmentListOnHoverExit();
    }
    // 안건준 추가 - 0622 ======

    private sealed class SpawnedCardEntry
    {
        public GameObject Root;
        public RectTransform RootTransform;
        public CanvasGroup CanvasGroup;
        public StatUpgrade StatUpgrade;
        public SegmentAddCard SegmentAddCard;
        public GameObject SourcePrefab; // 생성에 사용한 프리팹 (선택 가중치용)
        public Vector2 OriginalPosition;
        public Vector3 OriginalScale;
        public bool IsClickable;
        // 세그먼트 ADD 흐름용 역할
        public SegmentCardRole SegmentRole;
        // 세그먼트 ADD 흐름용 카탈로그 데이터
        public SegmentCatalogEntry SegmentCatalogEntry;
        // 세그먼트 ADD 흐름용 대상 ID
        public string SegmentId;
        // 세그먼트 ADD 흐름용 레벨 소비량
        public int LevelDelta = 1;
        // 2단계 무기 강화 선택 데이터
        public WeaponDefinition WeaponDefinition;
        // 건준수정 - 0621 ======
        public StatUpgrade.StatCardTier WeaponEnhancementTier = StatUpgrade.StatCardTier.Normal; // 레어/유니크 등급별 수치
        // 건준수정 - 0621 ======
        // 없음/불가 카드 클릭 차단
        public bool CanSelect = true;
    }

    private sealed class CardInstanceBridge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardUI manager;
        private SpawnedCardEntry entry;

        public void Initialize(CardUI owner, SpawnedCardEntry spawnedEntry)
        {
            manager = owner;
            entry = spawnedEntry;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            manager?.NotifySpawnedCardPointerEnter(entry);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            manager?.NotifySpawnedCardPointerExit(entry);
        }
    }

    // 안건준 추가 - 0622
    private sealed class SegmentListHoverBridge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private CardUI manager;

        public void Initialize(CardUI owner)
        {
            manager = owner;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            manager?.NotifySegmentListHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            manager?.NotifySegmentListHoverExit();
        }
    }

    // 안건준 추가 - 0622 ======
    // 자동모드 카드 자동선택 ─────────────────────────────────────────────────

    private void TryStartAutoSelect()
    {
        if (!autoSelectInAutoOrbit || !IsAutoOrbitActive())
        {
            return; // 자동모드가 아니거나 기능 꺼짐
        }

        StopAutoSelect();
        autoSelectRoutine = StartCoroutine(AutoSelectRoutine());
    }

    // 안건준 추가 - 0622 : 세그먼트 추가/레벨업 2차 카드 자동선택
    private void TryStartAutoSelectSegmentAction(bool canAdd, bool canLevelUp)
    {
        if (!autoSelectInAutoOrbit || !IsAutoOrbitActive())
        {
            return;
        }

        StopAutoSelect();
        autoSelectRoutine = StartCoroutine(AutoSelectSegmentActionRoutine(canAdd, canLevelUp));
    }

    private void StopAutoSelect()
    {
        if (autoSelectRoutine != null)
        {
            StopCoroutine(autoSelectRoutine);
            autoSelectRoutine = null;
        }
    }

    // 안건준 추가 - 0623 : 세그먼트 ID + 레벨로 SegmentDefinition 아이콘 스프라이트 조회
    private static Sprite GetSegmentIconSprite(string segmentId, int level)
    {
        SegmentCatalogAsset catalog = CoreStatProvider.Active?.SegmentCatalogAsset;
        if (catalog == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return null; // 카탈로그 없음
        }

        if (!catalog.TryFind(segmentId, out SegmentDefinition def))
        {
            return null; // 정의 없음
        }

        return def.GetIconSpriteForLevel(level); // 레벨별 스프라이트
    }

    // 안건준 추가 - 0623 : 세그먼트 카드 아이콘 크기 조절값 — CardUI 인스펙터값 우선, 없으면 SegmentDefinition 값 사용
    private float GetSegmentIconSizeOffset(string segmentId)
    {
        if (!Mathf.Approximately(segmentCardIconSizeOffset, 0f))
        {
            return segmentCardIconSizeOffset; // CardUI 인스펙터 값 우선
        }

        SegmentCatalogAsset catalog = CoreStatProvider.Active?.SegmentCatalogAsset;
        if (catalog == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return 0f; // 기본값
        }

        return catalog.TryFind(segmentId, out SegmentDefinition def) ? def.CardIconSizeOffset : 0f;
    }

    // 안건준 추가 - 0623 : SegmentUpgradeCard 같은 커스텀 프리팹에 Card_Text / DescText / Image 직접 주입
    private static void ApplyCardTextsDirectly(GameObject root, string title, string desc, Sprite iconSprite = null, float iconSizeOffset = 0f)
    {
        if (root == null)
        {
            return;
        }

        TMPro.TMP_Text[] texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
        TMPro.TMP_Text cardText = null;
        TMPro.TMP_Text descText = null;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "Card_Text")
            {
                cardText = texts[i];
            }
            else if (texts[i].gameObject.name == "DescText")
            {
                descText = texts[i];
            }
        }

        if (cardText != null && !string.IsNullOrWhiteSpace(title))
        {
            ApplyDirectSingleLineSizing(cardText, title);
            cardText.text = title; // 세그먼트 이름 (캐논, 미사일 등)
        }

        if (descText != null && !string.IsNullOrWhiteSpace(desc))
        {
            string displayDesc = SegmentCardTagPresenter.Apply(root, desc, descText);
            descText.richText = true;
            ApplyDirectDescriptionSizing(descText, displayDesc);
            descText.text = displayDesc; // WeaponDefinition Description
        }

        // 안건준 추가 - 0623 : "Image" 오브젝트에 세그먼트 Lv1 아이콘 적용
        if (iconSprite != null)
        {
            Transform imageTransform = root.transform.Find("Image");
            if (imageTransform != null && imageTransform.TryGetComponent(out UnityEngine.UI.Image img))
            {
                img.sprite = iconSprite;
                img.enabled = true;
                img.color = Color.white;
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = false;
                img.SetNativeSize(); // 원본 크기로 설정
                // 크기 조절 적용 (0=원본, -50=절반, 100=두배)
                if (!Mathf.Approximately(iconSizeOffset, 0f))
                {
                    float scale = Mathf.Max(0.01f, 1f + Mathf.Clamp(iconSizeOffset, -100f, 100f) / 100f);
                    img.rectTransform.sizeDelta *= scale;
                }
            }
            else
            {
                Debug.LogWarning($"[CardUI] 'Image' 자식 오브젝트를 찾지 못했습니다. root={root.name}, 자식 수={root.transform.childCount}");
            }
        }
    }

    private static void ApplyDirectDescriptionSizing(TMP_Text descText, string description)
    {
        if (descText == null)
        {
            return;
        }

        float baseSize = descText.fontSizeMax > 0f ? Mathf.Max(descText.fontSizeMax, descText.fontSize) : descText.fontSize;
        float maxSize = CountDescriptionLines(description) >= 3 ? baseSize * 0.86f : baseSize;
        ConfigureDirectAutoSize(descText, maxSize, true);
    }

    private static void ApplyDirectSingleLineSizing(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        float baseSize = text.fontSizeMax > 0f ? Mathf.Max(text.fontSizeMax, text.fontSize) : text.fontSize;
        ConfigureDirectAutoSize(text, baseSize, false);
    }

    private static void ConfigureDirectAutoSize(TMP_Text text, float maxSize, bool allowWrapping)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = Mathf.Max(8f, maxSize * 0.62f);
        text.fontSize = maxSize;
        text.textWrappingMode = allowWrapping ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
    }

    private static int CountDescriptionLines(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return 0;
        }

        string normalized = description.Replace("\r\n", "\n").Replace('\r', '\n');
        return Mathf.Max(1, normalized.Split('\n').Length);
    }

    private IEnumerator AutoSelectRoutine()
    {
        // 안건준 추가 - 0622 : WaitForSecondsRealtime — timeScale = 0 상태에서도 작동
        float waitTime = 0.4f + autoSelectDelay;
        yield return new WaitForSecondsRealtime(waitTime);

        if (isProcessingSelection || spawnedCards == null || spawnedCards.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        List<SpawnedCardEntry> selectable = new List<SpawnedCardEntry>();
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i];
            if (card != null && card.CanSelect && card.IsClickable)
            {
                selectable.Add(card);
            }
        }

        if (selectable.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 안건준 수정 - 0622 : 랜덤 → 최고 등급 우선 선택
        SpawnedCardEntry picked = PickHighestTierCard(selectable);

        NotifySpawnedCardPointerEnter(picked);
        yield return new WaitForSecondsRealtime(0.2f);

        NotifySpawnedCardClicked(picked);
        autoSelectRoutine = null;
    }

    // 안건준 추가 - 0622 : 추가/레벨업 2차 카드 자동선택 — 선택 불가 카드 제외 후 랜덤
    private IEnumerator AutoSelectSegmentActionRoutine(bool canAdd, bool canLevelUp)
    {
        // 카드 등장 연출 대기
        yield return new WaitForSecondsRealtime(0.4f + autoSelectDelay);

        if (isProcessingSelection || spawnedCards == null || spawnedCards.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 선택 가능한 카드만 수집 (CanSelect 기준 — 레벨업 불가면 LevelUpAction이 CanSelect=false)
        List<SpawnedCardEntry> selectable = new List<SpawnedCardEntry>();
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i];
            if (card == null || !card.IsClickable)
            {
                continue;
            }

            // 추가만 가능한 경우 AddAction만 허용
            // 레벨업만 가능한 경우 LevelUpAction만 허용
            // 둘 다 가능한 경우 둘 다 허용
            bool isAdd = card.SegmentRole == SegmentCardRole.AddAction;
            bool isLevelUp = card.SegmentRole == SegmentCardRole.LevelUpAction;

            if (isAdd && canAdd)
            {
                selectable.Add(card);
            }
            else if (isLevelUp && canLevelUp)
            {
                selectable.Add(card);
            }
            else if (!isAdd && !isLevelUp && card.CanSelect)
            {
                selectable.Add(card); // 기타 선택 가능 카드 fallback
            }
        }

        if (selectable.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 안건준 수정 - 0622 : 랜덤 → 최고 등급 우선 선택
        SpawnedCardEntry picked = PickHighestTierCard(selectable);

        NotifySpawnedCardPointerEnter(picked);
        yield return new WaitForSecondsRealtime(0.2f);

        NotifySpawnedCardClicked(picked);
        autoSelectRoutine = null;
    }

    // 안건준 추가 - 0622 : 카드 등급(티어) 반환 — 스탯/무기강화는 실제 등급, 세그먼트 계열은 Normal
    private StatUpgrade.StatCardTier GetCardTier(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return StatUpgrade.StatCardTier.Normal;
        }

        if (entry.SegmentRole == SegmentCardRole.EnhanceChoice)
        {
            return entry.WeaponEnhancementTier; // 무기 강화 카드 등급
        }

        if (entry.StatUpgrade != null)
        {
            return entry.StatUpgrade.CurrentTier; // 스탯 카드 등급
        }

        return StatUpgrade.StatCardTier.Normal; // 세그먼트 추가/레벨업 등 등급 없는 카드
    }

    // 안건준 추가 - 0622 : 후보 목록에서 가장 높은 등급의 카드를 반환 — 동급이면 랜덤
    private SpawnedCardEntry PickHighestTierCard(List<SpawnedCardEntry> candidates)
    {
        StatUpgrade.StatCardTier best = StatUpgrade.StatCardTier.Normal;
        for (int i = 0; i < candidates.Count; i++)
        {
            StatUpgrade.StatCardTier t = GetCardTier(candidates[i]);
            if (t > best)
            {
                best = t;
            }
        }

        List<SpawnedCardEntry> topTier = new List<SpawnedCardEntry>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (GetCardTier(candidates[i]) == best)
            {
                topTier.Add(candidates[i]);
            }
        }

        return topTier[UnityEngine.Random.Range(0, topTier.Count)];
    }
    // 안건준 추가 - 0622 ======
}
