using System.Collections.Generic;
using DG.Tweening;
using TeamProject01.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    [Header("Stat Upgrade")]
    [SerializeField] private GameObject[] statUpgradeCards = System.Array.Empty<GameObject>(); // 스탯 강화 카드 프리팹

    [Header("Add Segment")]
    [SerializeField] private GameObject[] addSegmentCards = System.Array.Empty<GameObject>(); // 세그먼트 추가 카드

    [Header("스탯 카드 레어 확률")]    
    [Range(0f, 100f)][SerializeField] private float doubleUpgradeChancePercent = 30f; // 2배 강화 확률 (30 = 30%)

    [Header("카드 생성 슬롯")]
    [SerializeField] private RectTransform[] cardSlots = System.Array.Empty<RectTransform>(); // 카드 생성 위치
    [Min(1)][SerializeField] private int cardsToSpawn = 3; // 한 번에 생성할 카드 수

    [Header("카드 연출")]
    [SerializeField] private float startYOffset = -80.0f; // 등장 시작 Y 오프셋
    [SerializeField] private float hoverScale = 1.09f; // 마우스 오버 배율

    [Header("레벨업 패널 감지")]
    [SerializeField] private CanvasGroup levelUpPanelCanvasGroup; // LevelUpPanel Canvas Group

    [Header("레벨업UI")]
    [SerializeField] private LevelUpUi levelUpUi; // 비워두면 자동 검색

    private readonly List<SpawnedCardEntry> spawnedCards = new List<SpawnedCardEntry>(); // 생성된 카드 목록
    private bool spawnedForCurrentOpen; // 이번 패널 오픈에서 생성 완료 여부
    private bool isProcessingSelection; // 선택 처리 중
    ////// 전찬우추가 - 세그먼트 ADD 풀에서 후보/액션 카드 구분
    private enum SegmentCardRole
    {
        None = 0, // 일반 카드
        Candidate = 1, // 세그먼트 후보 카드
        AddAction = 2, // 세그먼트 추가 카드
        LevelUpAction = 3, // 세그먼트 레벨업 카드
        Empty = 4 // 후보 없음 카드
    }

    private void Awake()
    {
        ResolveManagerReferences(); // 참조 보강
    }

    private void Update()
    {
        bool panelOpen = IsLevelUpPanelOpen(); // 패널 열림 여부
        if (panelOpen && !spawnedForCurrentOpen)
        {
            SpawnLevelUpCards(); // 레벨에 맞는 카드 풀에서 랜덤 생성
            spawnedForCurrentOpen = true;
            return;
        }

        if (!panelOpen && spawnedForCurrentOpen)
        {
            ClearSpawnedCards(); // 패널 닫힘 → 카드 정리
            spawnedForCurrentOpen = false;
            isProcessingSelection = false;
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

        bool useSegmentPool = ShouldUseSegmentCardPool(); // 3,6,9... 레벨용 세그먼트 카드
        ////// 전찬우수정 - 세그먼트 ADD 풀은 카탈로그 기반 전용 생성으로 분기
        // GameObject[] sourcePrefabs = useSegmentPool ? addSegmentCards : statUpgradeCards;
        // string poolName = useSegmentPool ? "Add Segment" : "Stat Upgrade";
        if (useSegmentPool)
        {
            SpawnSegmentCandidateCards(); // 세그먼트 후보 카드 생성
            return; // 세그먼트 분기 완료
        }

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
        List<GameObject> picked = PickRandomPrefabs(pool, spawnCount);

        for (int i = 0; i < picked.Count; i++)
        {
            SpawnedCardEntry entry = CreateSpawnedCard(picked[i], cardSlots[i]);
            if (entry != null)
            {
                spawnedCards.Add(entry);
            }
        }

        PlaySpawnOpenTween(spawnedCards);
    }

    private static bool ShouldUseSegmentCardPool()
    {
        CoreStatData stats = CoreStatProvider.GetCurrentOrDefault(); // 현재 코어 레벨
        int nextLevel = Mathf.Max(1, stats.Level + 1); // 카드 선택 후 올라갈 레벨 (levelDelta=1 가정)
        return nextLevel % 3 == 0; // 3,6,9,12... → 세그먼트 카드
    }

    ////// 전찬우추가 - 세그먼트 ADD 풀: 카탈로그 후보 3장 생성, 부족하면 없음 카드 표시
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

    ////// 전찬우추가 - 추가/레벨업 2차 선택 카드 2장 생성
    private void SpawnSegmentActionCards(SegmentCatalogEntry entry, int levelDelta, bool canAdd, bool canLevelUp)
    {
        ClearSpawnedCards(); // 후보 카드 제거
        int spawnCount = Mathf.Min(2, cardSlots.Length); // 액션은 2장까지만 사용
        RectTransform actionParentSlot = GetCenteredActionParentSlot(); // 전찬우추가 - 2차 액션 카드는 중앙 슬롯 기준으로 배치
        for (int i = 0; i < spawnCount; i++)
        {
            GameObject prefab = GetSegmentCardTemplate(i); // 기존 카드 프리팹 재사용
            ////// 전찬우수정 - 기존 0/1번 슬롯 배치는 왼쪽으로 치우쳐 보여 중앙 기준 배치로 변경
            // SpawnedCardEntry spawnedEntry = CreateSpawnedCard(prefab, cardSlots[i]); // 기존 액션 카드 생성
            SpawnedCardEntry spawnedEntry = CreateSpawnedCard(prefab, actionParentSlot); // 중앙 부모 슬롯에 액션 카드 생성
            if (spawnedEntry == null)
            {
                continue; // 생성 실패
            }

            ApplyCenteredActionCardPosition(spawnedEntry, i, spawnCount); // 전찬우추가 - 2장일 때 중앙 기준 좌/우 배치

            if (i == 0)
            {
                ConfigureSegmentActionEntry(spawnedEntry, entry, levelDelta, SegmentCardRole.AddAction, canAdd); // 세그먼트 추가 카드
            }
            else
            {
                ConfigureSegmentActionEntry(spawnedEntry, entry, levelDelta, SegmentCardRole.LevelUpAction, canLevelUp); // 세그먼트 레벨업 카드
            }

            spawnedCards.Add(spawnedEntry); // 생성 목록 등록
        }

        PlaySpawnOpenTween(spawnedCards); // 기존 DOTween 등장 연출 재사용
    }

    ////// 전찬우추가 - 2차 액션 카드 부모로 가장 중앙에 가까운 슬롯 사용
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

    ////// 전찬우추가 - 2차 액션 카드 2장을 중앙 기준 좌우로 배치
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

    ////// 전찬우추가 - 기존 3슬롯 폭을 기준으로 2장 배치 간격 계산
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

    ////// 전찬우추가 - 후보 카드 3장 슬롯의 전체 폭에서 2장용 절반 간격 산출
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

    ////// 전찬우추가 - 세그먼트 카드 템플릿 선택
    private GameObject GetSegmentCardTemplate(int index)
    {
        if (addSegmentCards == null || addSegmentCards.Length == 0)
        {
            return null; // 템플릿 없음
        }

        int safeIndex = Mathf.Clamp(index, 0, addSegmentCards.Length - 1); // 배열 범위 보정
        return addSegmentCards[safeIndex] != null ? addSegmentCards[safeIndex] : addSegmentCards[0]; // null이면 첫 카드 fallback
    }

    ////// 전찬우추가 - 카탈로그 후보 랜덤 선택
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

    ////// 전찬우추가 - 세그먼트 후보 카드 데이터 주입
    private void ConfigureSegmentCandidateEntry(SpawnedCardEntry entry, SegmentCatalogEntry catalogEntry)
    {
        entry.SegmentRole = SegmentCardRole.Candidate; // 후보 카드
        entry.SegmentCatalogEntry = catalogEntry; // 선택 후보 저장
        entry.SegmentId = catalogEntry.NormalizedId; // 대상 ID 저장
        entry.LevelDelta = entry.SegmentAddCard != null ? entry.SegmentAddCard.LevelDelta : 1; // 소비 레벨
        entry.CanSelect = true; // 후보 선택 가능
        entry.SegmentAddCard?.ConfigureCandidate(catalogEntry); // 카드 문구 세팅
    }

    ////// 전찬우추가 - 후보 부족 시 없음 카드 데이터 주입
    private void ConfigureEmptySegmentEntry(SpawnedCardEntry entry)
    {
        entry.SegmentRole = SegmentCardRole.Empty; // 없음 카드
        entry.SegmentId = string.Empty; // 대상 없음
        entry.LevelDelta = 1; // 기본값
        entry.CanSelect = false; // 클릭 불가
        entry.SegmentAddCard?.ConfigureEmpty(); // 카드 문구 세팅
    }

    ////// 전찬우추가 - 추가/레벨업 액션 카드 데이터 주입
    private void ConfigureSegmentActionEntry(SpawnedCardEntry entry, SegmentCatalogEntry catalogEntry, int levelDelta, SegmentCardRole role, bool selectable)
    {
        string displayName = string.IsNullOrWhiteSpace(catalogEntry.DisplayName) ? catalogEntry.NormalizedId : catalogEntry.DisplayName; // 표시명
        string title = role == SegmentCardRole.AddAction ? "세그먼트 추가" : "세그먼트 레벨업"; // 액션 제목
        string description = BuildSegmentActionDescription(catalogEntry.NormalizedId, displayName, role, selectable); // 액션 설명
        entry.SegmentRole = role; // 액션 역할
        entry.SegmentCatalogEntry = catalogEntry; // 대상 후보 저장
        entry.SegmentId = catalogEntry.NormalizedId; // 대상 ID
        entry.LevelDelta = Mathf.Max(1, levelDelta); // 소비 레벨
        entry.CanSelect = selectable; // 선택 가능 여부
        entry.SegmentAddCard?.ConfigureAction(catalogEntry.NormalizedId, title, description, selectable); // 카드 문구 세팅
    }

    ////// 전찬우추가 - 액션 카드 설명 생성
    private static string BuildSegmentActionDescription(string segmentId, string displayName, SegmentCardRole role, bool selectable)
    {
        if (role == SegmentCardRole.AddAction)
        {
            return selectable ? $"{displayName} +1" : "추가 불가"; // 추가 설명
        }

        if (CoreStatProvider.Active != null && CoreStatProvider.Active.TryGetSegmentModelLevelInfo(segmentId, out int currentLevel, out int maxLevel))
        {
            int nextLevel = Mathf.Min(currentLevel + 1, maxLevel); // 다음 레벨
            return selectable ? $"{displayName} Lv.{currentLevel} → Lv.{nextLevel}" : $"{displayName} MAX"; // 레벨 설명
        }

        return selectable ? $"{displayName} 전체 레벨업" : "레벨업 불가"; // fallback
    }

    private SpawnedCardEntry CreateSpawnedCard(GameObject prefab, RectTransform slot)
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

        if (statUpgrade != null)
        {
            statUpgrade.RollSpawnVariant(doubleUpgradeChancePercent * 0.01f); // 2배 강화 확률 + 색상
        }

        ConfigureSpawnedRect(rectTransform, slot); // 프리팹 크기 유지 + 슬롯 중앙 배치

        SpawnedCardEntry entry = new SpawnedCardEntry
        {
            Root = instance,
            RootTransform = rectTransform,
            CanvasGroup = canvasGroup,
            StatUpgrade = statUpgrade,
            SegmentAddCard = segmentAddCard,
            OriginalPosition = rectTransform.anchoredPosition,
            OriginalScale = rectTransform.localScale,
            CanSelect = true // 전찬우추가 - 기본 카드는 선택 가능
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

        entry.IsClickable = entry.CanSelect; // 전찬우수정 - 없음/불가 카드는 호버/클릭 비활성
        entry.CanvasGroup.blocksRaycasts = true;
        entry.CanvasGroup.interactable = entry.CanSelect; // 전찬우수정 - 선택 불가 카드 입력 차단

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
        sequence.Append(entry.RootTransform.DOScale(entry.OriginalScale * 1.2f, 0.2f).SetEase(Ease.OutBack));
        sequence.Append(entry.RootTransform.DOScale(entry.OriginalScale, 0.15f));
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
            HandleSegmentCandidateClicked(selectedEntry); // 전찬우추가 - 세그먼트 후보는 2차 분기 처리
            return;
        }

        if (!TryApplySelectedCard(selectedEntry))
        {
            return;
        }

        isProcessingSelection = true;
        PlaySelectionCloseSequence(selectedEntry);
    }

    ////// 전찬우추가 - 세그먼트 후보 클릭 시 추가/레벨업 분기 처리
    private void HandleSegmentCandidateClicked(SpawnedCardEntry selectedEntry)
    {
        CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
        if (core == null || string.IsNullOrWhiteSpace(selectedEntry.SegmentId))
        {
            Debug.LogWarning("[CardUI] 세그먼트 후보 적용 실패: CoreStatProvider 또는 SegmentId 없음", selectedEntry.Root); // 코어 누락
            return;
        }

        bool canAdd = core.CanAddSegment(selectedEntry.SegmentId); // 추가 가능 여부
        bool canLevelUp = core.CanLevelUpSegmentModel(selectedEntry.SegmentId); // 모델 레벨업 가능 여부
        if (canLevelUp)
        {
            isProcessingSelection = true; // 2차 카드 전환 중
            PlaySegmentActionChoiceSequence(selectedEntry, canAdd, canLevelUp); // 추가/레벨업 카드 표시
            return;
        }

        if (!canAdd)
        {
            Debug.LogWarning("[CardUI] 세그먼트 후보 적용 실패: 추가/레벨업 모두 불가", selectedEntry.Root); // 적용 불가
            return;
        }

        if (core.TryApplySegmentAddChoice(selectedEntry.SegmentId, selectedEntry.LevelDelta, selectedEntry.SegmentAddCard != null ? selectedEntry.SegmentAddCard.SegmentAddCount : 1))
        {
            isProcessingSelection = true; // 선택 처리 시작
            PlaySelectionCloseSequence(selectedEntry); // 만렙/레벨업 불가면 분기 생략 후 바로 추가
            return;
        }

        Debug.LogWarning("[CardUI] 세그먼트 추가 적용 실패: 경험치/카탈로그/컨보이 조건 확인 필요", selectedEntry.Root); // 적용 실패
    }

    ////// 전찬우추가 - 후보 카드가 사라진 뒤 추가/레벨업 2차 카드 표시
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

    private static bool TryApplySelectedCard(SpawnedCardEntry selectedEntry)
    {
        if (selectedEntry.SegmentAddCard != null)
        {
            ////// 전찬우추가 - 2차 선택 카드: 세그먼트 추가
            if (selectedEntry.SegmentRole == SegmentCardRole.AddAction)
            {
                CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
                bool applied = core != null && core.TryApplySegmentAddChoice(selectedEntry.SegmentId, selectedEntry.LevelDelta, selectedEntry.SegmentAddCard.SegmentAddCount); // 추가 적용
                if (!applied)
                {
                    Debug.LogWarning("[CardUI] 세그먼트 추가 적용 실패: 경험치/카탈로그/컨보이 조건 확인 필요", selectedEntry.Root); // 실패 로그
                }

                return applied; // 적용 결과
            }

            ////// 전찬우추가 - 2차 선택 카드: 해당 세그먼트 전체 모델 레벨업
            if (selectedEntry.SegmentRole == SegmentCardRole.LevelUpAction)
            {
                CoreStatProvider core = CoreStatProvider.Active; // 현재 코어
                bool applied = core != null && core.TryApplySegmentLevelUpChoice(selectedEntry.SegmentId, selectedEntry.LevelDelta); // 레벨업 적용
                if (!applied)
                {
                    Debug.LogWarning("[CardUI] 세그먼트 레벨업 적용 실패: 만렙/경험치/장착 상태 확인 필요", selectedEntry.Root); // 실패 로그
                }

                return applied; // 적용 결과
            }

            ////// 전찬우삭제 - 세그먼트 카드 선택 시 바로 추가하던 이전 흐름은 후보/액션 2단계로 대체
            // if (selectedEntry.SegmentAddCard.TryApplyToCore())
            // {
            //     return true;
            // }
            //
            // Debug.LogWarning("[CardUI] 세그먼트 추가 적용 실패: CanLevelUp 미충족, 카탈로그/컨보이 조건 불충족", selectedEntry.Root);
            // return false;

            ////// 전찬우삭제 - 임시 레벨만 반영하던 흐름은 실제 추가/레벨업 적용으로 대체
            // if (selectedEntry.SegmentAddCard.TryApplyLevelOnlyToCore())
            // {
            //     return true;
            // }
            //
            // Debug.LogWarning("[CardUI] [임시] 세그먼트 카드 레벨 반영 실패: CanLevelUp 미충족 또는 CoreStatProvider 없음", selectedEntry.Root);
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

        sequence.AppendInterval(0.5f);
        sequence.OnComplete(() =>
        {
            ResolveLevelUpUi()?.Close();
            isProcessingSelection = false;
        });
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
        public Vector2 OriginalPosition;
        public Vector3 OriginalScale;
        public bool IsClickable;
        ////// 전찬우추가 - 세그먼트 ADD 흐름용 역할
        public SegmentCardRole SegmentRole;
        ////// 전찬우추가 - 세그먼트 ADD 흐름용 카탈로그 데이터
        public SegmentCatalogEntry SegmentCatalogEntry;
        ////// 전찬우추가 - 세그먼트 ADD 흐름용 대상 ID
        public string SegmentId;
        ////// 전찬우추가 - 세그먼트 ADD 흐름용 레벨 소비량
        public int LevelDelta = 1;
        ////// 전찬우추가 - 없음/불가 카드 클릭 차단
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
