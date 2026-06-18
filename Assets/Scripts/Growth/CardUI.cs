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
        GameObject[] sourcePrefabs = useSegmentPool ? addSegmentCards : statUpgradeCards;
        string poolName = useSegmentPool ? "Add Segment" : "Stat Upgrade";

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
            OriginalScale = rectTransform.localScale
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

        entry.IsClickable = true;
        entry.CanvasGroup.blocksRaycasts = true;
        entry.CanvasGroup.interactable = true;

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
        if (isProcessingSelection || selectedEntry == null)
        {
            return;
        }

        if (!TryApplySelectedCard(selectedEntry))
        {
            return;
        }

        isProcessingSelection = true;
        PlaySelectionCloseSequence(selectedEntry);
    }

    private static bool TryApplySelectedCard(SpawnedCardEntry selectedEntry)
    {
        if (selectedEntry.SegmentAddCard != null)
        {
            if (selectedEntry.SegmentAddCard.TryApplyToCore())
            {
                return true;
            }

            Debug.LogWarning("[CardUI] 세그먼트 추가 적용 실패: CanLevelUp 미충족, 카탈로그/컨보이 조건 불충족", selectedEntry.Root);
            return false;
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
