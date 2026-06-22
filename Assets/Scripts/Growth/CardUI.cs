using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TeamProject01.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("Stat Upgrade")]
    [SerializeField] private GameObject[] statUpgradeCards = System.Array.Empty<GameObject>(); // 스탯 강화 카드 프리팹

    [Header("Add Segment")]
    [SerializeField] private GameObject[] addSegmentCards = System.Array.Empty<GameObject>(); // 세그먼트 추가 카드

    [Header("세그먼트 추가 / 레벨업 액션 카드 (추후 프리팹 교체)")]
    [Tooltip("비우면 addSegmentCards 기본 프리팹 — 2차 선택 '세그먼트 추가' 카드만 교체")]
    [SerializeField] private GameObject segmentAddActionCardPrefab; // 세그먼트 추가 2차 카드 UI
    [Tooltip("비우면 addSegmentCards 기본 프리팹 — 2차 선택 '세그먼트 레벨업' 카드만 교체")]
    [SerializeField] private GameObject segmentLevelUpActionCardPrefab; // 세그먼트 레벨업 2차 카드 UI

    [Header("Weapon Enhancement")]
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
    private bool spawnedForCurrentOpen; // 이번 패널 오픈에서 생성 완료 여부
    private bool isProcessingSelection; // 선택 처리 중
    private static bool loggedWeaponEnhancementInitial; // 무기 강화 초기 디버그 1회
    private LevelUpCardPhase currentSpawnPhase = LevelUpCardPhase.StatUpgrade; // 이번 레벨업 카드 종류
    private string selectedSegmentWeaponStatId; // 카드 선택으로 갱신되는 디버그 표시 대상
    private CoreStatProvider segmentWeaponStatSubscribedCore; // 스탯 변경 구독 대상

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
        UnsubscribeSegmentCountDebug(); // [임시] 구독 해제
        UnsubscribeSegmentWeaponStatDebug(); // 스탯 변경 구독 해제
    }

    // 건춘추가 - 0621 ======
    private void OnValidate() // Inspector에서 세그먼트 변경 시 플레이 중 미리보기
    {
        if (Application.isPlaying)
        {
            RefreshSegmentWeaponStatUi();
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
            SpawnLevelUpCards(); // 순환 순서에 맞는 카드 생성
            spawnedForCurrentOpen = true;
            return;
        }

        if (!panelOpen && spawnedForCurrentOpen)
        {
            ClearSpawnedCards(); // 패널 닫힘 → 카드 정리
            spawnedForCurrentOpen = false;
            isProcessingSelection = false;
            currentSpawnPhase = LevelUpCardPhase.StatUpgrade; // 다음 오픈 시 재계산
            if (CoreStatProvider.Active != null && CoreStatProvider.Active.IsLevelUpChoicePending)
            {
                CoreStatProvider.Active.CancelLevelUpChoice(); // 선택 없이 닫힘 → 경험치 유지
            }
        }
    }

    public void PlayLevelUpTween()
    {
        ResolveLevelUpUi()?.Open(); // 레벨업 패널 열기
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

        if (cardSlots == null || cardSlots.Length == 0)
        {
            Debug.LogWarning("[CardUI] 카드 생성 슬롯이 비어 있습니다.", this);
            return;
        }

        currentSpawnPhase = ResolveLevelUpCardPhase(); // 스탯 → 무기강화 → 세그먼트 3종 순환
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
            : new StatUpgrade.CardSpawnResolve(sourcePrefab, true); // StatUpgrade 없으면 기본
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

            entry.StatUpgrade.ApplySpawnTier(tier, resolve.ApplyFallbackVisual); // 등급·배율·색상(기본 껍데기일 때)
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
        GameObject template = GetSegmentCardTemplate(0); // 세그먼트 카드 기본 프리팹
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
            GameObject prefab = GetSegmentCardTemplate(i); // 슬롯별 카드 템플릿
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
        GameObject template = GetSegmentCardTemplate(0); // 세그먼트 카드 기본 프리팹
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
            GameObject prefab = GetSegmentCardTemplate(i); // 슬롯별 카드 템플릿
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
    }

    // 세그먼트 추가/레벨업 2차 선택 카드 2장 생성
    private void SpawnSegmentActionCards(SegmentCatalogEntry entry, int levelDelta, bool canAdd, bool canLevelUp)
    {
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
        StatUpgrade.StatCardTier tier,
        bool applyFallbackVisual)
    {
        entry.SegmentRole = SegmentCardRole.EnhanceChoice; // 2단계 강화 카드
        entry.WeaponDefinition = definition; // 선택 강화
        entry.SegmentId = targetSegmentId; // 대상 세그먼트
        entry.LevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        entry.CanSelect = definition != null && definition.HasAnyStatBonus; // 선택 가능
        entry.SegmentAddCard?.ConfigureWeaponEnhancement(definition, entry.LevelDelta); // 카드 문구·아이콘
        entry.SegmentAddCard?.ApplyWeaponEnhancementTier(tier, applyFallbackVisual); // 등급·색상(기본 껍데기일 때)
        // 건준수정 - 0621 ======
        entry.WeaponEnhancementTier = tier; // 적용 시 등급별 수치
        // 건준수정 - 0621 ======

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
        SpawnedCardEntry entry = CreateSpawnedCard(spawnPrefab, slot, defaultTemplate); // 생성
        if (entry == null)
        {
            return null; // 생성 실패
        }

        ConfigureWeaponEnhancementEntry(entry, definition, resolvedTargetSegmentId, levelDelta, tier, resolve.ApplyFallbackVisual); // 문구·등급
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

    // 세그먼트 카드 템플릿 선택
    private GameObject GetSegmentCardTemplate(int index)
    {
        if (addSegmentCards == null || addSegmentCards.Length == 0)
        {
            return null; // 템플릿 없음
        }

        int safeIndex = Mathf.Clamp(index, 0, addSegmentCards.Length - 1); // 배열 범위 보정
        return addSegmentCards[safeIndex] != null ? addSegmentCards[safeIndex] : addSegmentCards[0]; // null이면 첫 카드 fallback
    }

    private GameObject ResolveSegmentActionCardPrefab(SegmentCardRole role, GameObject defaultTemplate) // 2차 액션 카드 — CardUI 교체 프리팹
    {
        if (defaultTemplate == null)
        {
            return null; // 기본 템플릿 없음
        }

        switch (role)
        {
            case SegmentCardRole.AddAction:
                if (segmentAddActionCardPrefab != null)
                {
                    return segmentAddActionCardPrefab; // 세그먼트 추가 전용 UI
                }

                break;
            case SegmentCardRole.LevelUpAction:
                if (segmentLevelUpActionCardPrefab != null)
                {
                    return segmentLevelUpActionCardPrefab; // 세그먼트 레벨업 전용 UI
                }

                break;
        }

        return defaultTemplate; // 비워두면 addSegmentCards 기본
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
        string displayName = string.IsNullOrWhiteSpace(catalogEntry.DisplayName) ? catalogEntry.NormalizedId : catalogEntry.DisplayName; // 표시명
        string title = role switch // 액션 제목
        {
            SegmentCardRole.AddAction => "세그먼트 추가",
            SegmentCardRole.LevelUpAction => "세그먼트 레벨업",
            _ => string.Empty
        };
        string description = BuildSegmentActionDescription(catalogEntry.NormalizedId, displayName, role, selectable); // 액션 설명
        entry.SegmentRole = role; // 액션 역할
        entry.SegmentCatalogEntry = catalogEntry; // 대상 후보 저장
        entry.SegmentId = catalogEntry.NormalizedId; // 대상 ID
        entry.LevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        entry.CanSelect = selectable; // 선택 가능 여부
        entry.SegmentAddCard?.ConfigureAction(catalogEntry.NormalizedId, title, description, selectable); // 카드 문구 세팅
    }

    // 액션 카드 설명 생성
    private static string BuildSegmentActionDescription(string segmentId, string displayName, SegmentCardRole role, bool selectable)
    {
        if (role == SegmentCardRole.AddAction)
        {
            return selectable ? $"{displayName} +1" : "추가 불가"; // 추가 설명
        }

        if (role == SegmentCardRole.LevelUpAction)
        {
            if (CoreStatProvider.Active != null && CoreStatProvider.Active.TryGetSegmentModelLevelInfo(segmentId, out int currentLevel, out int maxLevel))
            {
                int nextLevel = Mathf.Min(currentLevel + 1, maxLevel); // 다음 레벨
                return selectable ? $"{displayName} Lv.{currentLevel} → Lv.{nextLevel}" : $"{displayName} MAX"; // 레벨 설명
            }

            return selectable ? $"{displayName} 전체 레벨업" : "레벨업 불가"; // fallback
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

        if (statUpgrade == null && segmentAddCard == null)
        {
            Debug.LogWarning("[CardUI] 카드 프리팹에 StatUpgrade 또는 SegmentAddCard가 없습니다.", prefab);
            Destroy(instance);
            return null;
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

        for (int i = 0; i < cards.Count; i++)
        {
            HideInstant(cards[i]);
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
        });
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
        sequence.OnComplete(() =>
        {
            CoreStatProvider.Active?.CompleteLevelUpChoice(); // 순환 진행 + 선택 UI 종료
            CloseLevelUpPanelAfterSelection(); // 패널 닫기 + 일시정지 해제
            isProcessingSelection = false; // 다음 입력 허용
        });
    }

    private void CloseLevelUpPanelAfterSelection() // 선택 완료 후 오버레이·일시정지 해제
    {
        LevelUpUi ui = ResolveLevelUpUi();
        if (ui != null)
        {
            ui.Close(selectionPanelCloseFadeSeconds); // 페이드 후 ResumeGame
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
                });
            return;
        }

        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f; // fallback
        }
    }

    private void ClearSpawnedCards()
    {
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
}
