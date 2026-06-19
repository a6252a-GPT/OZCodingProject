using TeamProject01.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SegmentAddCard : MonoBehaviour
{
    [Header("코어 세그먼트 추가")]
    [Min(1)][SerializeField] private int levelDelta = 1; // 선택 시 소비할 레벨 증가량
    ////// 전찬우수정 - 카탈로그 후보/액션 카드에서 세그먼트 ID를 런타임 주입받도록 변경
    // [SerializeField] private string segmentId = "SG01_MachineGun"; // 기존 고정 세그먼트 ID
    [SerializeField] private string segmentId = string.Empty; // 현재 카드가 가리키는 세그먼트 ID
    [Min(1)][SerializeField] private int segmentAddCount = 1; // 추가할 세그먼트 수

    ////// 전찬우추가 - 기존 카드 프리팹 텍스트를 런타임에서 바꾸기 위한 참조
    [SerializeField] private TMP_Text titleText; // 카드 제목
    ////// 전찬우추가 - 기존 카드 프리팹 텍스트를 런타임에서 바꾸기 위한 참조
    [SerializeField] private TMP_Text descriptionText; // 카드 설명

    [Header("무기 강화 카드 등급 표시")]
    [SerializeField] private Image cardHighlightImage; // 비우면 자식 Image 탐색
    [SerializeField] private Color normalCardColor = Color.white; // 일반 카드 색
    [SerializeField] private Color rareCardColor = Color.yellow; // 레어 카드 색 (2배)
    [SerializeField] private Color uniqueCardColor = Color.green; // 유니크 카드 색 (3배)

    [Header("추후 — 등급별 프리팹 교체 (비우면 색상 틴트 방식)")]
    [SerializeField] private GameObject rareCardPrefab; // 레어 전용 프리팹
    [SerializeField] private GameObject uniqueCardPrefab; // 유니크 전용 프리팹

    [Header("추후 — 카드 아이콘 (비우면 자식 Icon/Image 탐색)")]
    [SerializeField] private Image cardIconImage; // 강화별 아이콘 표시 대상

    private float weaponEnhancementBonusMultiplier = 1f; // 레어 2 / 유니크 3

    public float WeaponEnhancementBonusMultiplier => weaponEnhancementBonusMultiplier; // 적용 배율

    ////// 전찬우추가 - 없음/불가 카드 클릭 방지용 상태
    public bool IsSelectableChoice { get; private set; } = true; // CardUI가 클릭 가능 여부 판단
    ////// 전찬우추가 - 외부에서 정리된 세그먼트 ID를 조회
    public string SegmentId => string.IsNullOrWhiteSpace(segmentId) ? string.Empty : segmentId.Trim(); // 공백 제거 ID
    ////// 전찬우추가 - 외부에서 레벨 소비량 조회
    public int LevelDelta => Mathf.Max(1, levelDelta); // 최소 1 보정
    ////// 전찬우추가 - 외부에서 추가 수 조회
    public int SegmentAddCount => Mathf.Max(1, segmentAddCount); // 최소 1 보정

    ////// 전찬우추가 - 세그먼트 카탈로그 후보 카드로 설정
    public void ConfigureCandidate(SegmentCatalogEntry entry)
    {
        segmentId = entry.NormalizedId; // 카탈로그 ID 저장
        IsSelectableChoice = entry.CanShowAsAddChoice; // 유효 후보만 선택 가능
        string title = string.IsNullOrWhiteSpace(entry.DisplayName) ? entry.NormalizedId : entry.DisplayName; // 표시명 fallback
        string description = string.IsNullOrWhiteSpace(entry.Description) ? $"{entry.NormalizedId} 선택" : entry.Description; // 설명 fallback
        ApplyTexts(title, description); // 화면 문구 갱신
        SetButtonInteractable(IsSelectableChoice); // 버튼 상태 갱신
    }

    ////// 전찬우추가 - 추가/레벨업 2차 선택 카드로 설정
    public void ConfigureAction(string targetSegmentId, string title, string description, bool selectable)
    {
        segmentId = string.IsNullOrWhiteSpace(targetSegmentId) ? string.Empty : targetSegmentId.Trim(); // 대상 ID 저장
        IsSelectableChoice = selectable && !string.IsNullOrWhiteSpace(segmentId); // 대상이 있어야 선택 가능
        ApplyTexts(title, description); // 액션 문구 갱신
        SetButtonInteractable(IsSelectableChoice); // 버튼 상태 갱신
    }

    ////// 전찬우추가 - 후보가 3개보다 적을 때 표시하는 빈 카드
    public void ConfigureEmpty()
    {
        segmentId = string.Empty; // 적용 대상 없음
        IsSelectableChoice = false; // 클릭 불가
        ApplyTexts("없음", "선택 가능한 세그먼트 없음"); // 빈 슬롯 표시
        SetButtonInteractable(false); // 버튼 비활성화
    }

    ////// 무기 강화 2단계 카드 설정 (세그먼트 선택 후 강화 카드 UI)
    public void ConfigureWeaponEnhancement(WeaponDefinition definition, int levelDeltaValue)
    {
        if (definition == null)
        {
            ConfigureEmpty(); // 데이터 없음
            return;
        }

        levelDelta = Mathf.Max(1, levelDeltaValue); // 소비 레벨
        segmentId = definition.NormalizedTargetSegmentId; // 대상 세그먼트
        IsSelectableChoice = definition.HasAnyStatBonus && !string.IsNullOrWhiteSpace(segmentId); // 유효 강화만 선택
        string title = string.IsNullOrWhiteSpace(definition.DisplayName) ? definition.NormalizedId : definition.DisplayName; // 제목
        string description = string.IsNullOrWhiteSpace(definition.Description) ? definition.NormalizedId : definition.Description; // 설명
        ApplyTexts(title, description); // 카드 문구
        ApplyCardIcon(definition.CardIconOverride); // 에셋 아이콘 (있을 때만)
        SetButtonInteractable(IsSelectableChoice); // 버튼 상태
    }

    public GameObject ResolveSpawnPrefabForTier(StatUpgrade.StatCardTier tier, GameObject defaultPrefab) // addSegmentCards 템플릿 등급별 프리팹 (추후)
    {
        switch (tier)
        {
            case StatUpgrade.StatCardTier.Unique:
                if (uniqueCardPrefab != null)
                {
                    return uniqueCardPrefab; // 유니크 전용
                }

                break;
            case StatUpgrade.StatCardTier.Rare:
                if (rareCardPrefab != null)
                {
                    return rareCardPrefab; // 레어 전용
                }

                break;
        }

        return defaultPrefab != null ? defaultPrefab : gameObject; // 일반 또는 대체 없음
    }

    public void ApplyWeaponEnhancementTier(StatUpgrade.StatCardTier tier, bool applyFallbackVisual) // 생성 후 등급·배율·색상
    {
        weaponEnhancementBonusMultiplier = IsSelectableChoice
            ? StatUpgrade.GetMultiplierForTier(tier)
            : 1f; // 선택 불가면 1배

        if (applyFallbackVisual)
        {
            ApplyWeaponEnhancementCardVisual(tier); // 기본 껍데기 → 색 틴트
        }
    }

    public void RollWeaponEnhancementVariant(float rareChancePercent, float uniqueChancePercent) // CardUI 확률 — 등급·색상·배율 (기존 호출 호환)
    {
        if (!IsSelectableChoice)
        {
            weaponEnhancementBonusMultiplier = 1f; // 선택 불가 카드
            return;
        }

        ApplyWeaponEnhancementTier(
            StatUpgrade.RollTier(rareChancePercent, uniqueChancePercent),
            applyFallbackVisual: true); // 기본 껍데기 + 색 틴트
    }

    public GrowthStatData CreateGrowthStatData() // 코어로 보낼 세그먼트 추가 데이터
    {
        ////// 전찬우수정 - 런타임 주입 ID/보정값을 사용
        // return GrowthStatData.CreateAddSegment(levelDelta, segmentId, segmentAddCount); // 기존 고정 ID 기반
        return GrowthStatData.CreateAddSegment(LevelDelta, SegmentId, SegmentAddCount); // 현재 카드 ID 기반
    }

    public bool TryApplyToCore() // 코어에 세그먼트 추가 적용
    {
        GrowthStatData growth = CreateGrowthStatData(); // 적용할 데이터 준비
        if (!growth.HasAnyValue) // 레벨/세그먼트 ID 없음
        {
            return false; // 적용 실패
        }

        return CoreStatProvider.TryApplyGrowth(growth); // 경험치 소비 + 세그먼트 추가
    }

    // =============== [임시] 시작 ===============
    // 세그먼트 추가 없이 레벨/경험치만 반영 (세그먼트 코어 연동 복구 전까지)
    public bool TryApplyLevelOnlyToCore()
    {
        GrowthStatData growth = GrowthStatData.CreateConvoyUpgrade(levelDelta, 0f, 0f, 0f, 0f, 0f);
        return CoreStatProvider.TryApplyGrowth(growth);
    }
    // =============== [임시] 끝 ===============

    private void ApplyWeaponEnhancementCardVisual(StatUpgrade.StatCardTier tier) // 등급별 Image 색
    {
        Image image = ResolveCardHighlightImage(); // 자식 Image
        if (image == null)
        {
            return; // 표시 대상 없음
        }

        Color targetColor = normalCardColor; // 기본은 일반 색
        if (tier == StatUpgrade.StatCardTier.Unique)
        {
            targetColor = uniqueCardColor; // 유니크 → 초록
        }
        else if (tier == StatUpgrade.StatCardTier.Rare)
        {
            targetColor = rareCardColor; // 레어 → 노랑
        }

        image.color = new Color(targetColor.r, targetColor.g, targetColor.b, image.color.a); // 알파 유지
    }

    private Image ResolveCardHighlightImage() // 강조용 Image 참조
    {
        if (cardHighlightImage != null)
        {
            return cardHighlightImage; // Inspector 지정값
        }

        Transform imageTransform = transform.Find("Image"); // 자식 Image 탐색
        if (imageTransform != null && imageTransform.TryGetComponent(out Image childImage))
        {
            cardHighlightImage = childImage; // 캐시
            return cardHighlightImage;
        }

        if (TryGetComponent(out Image rootImage))
        {
            cardHighlightImage = rootImage; // 루트 Image fallback
        }

        return cardHighlightImage;
    }

    private void ApplyCardIcon(Sprite icon) // WeaponDefinition.CardIconOverride → 카드 Image
    {
        if (icon == null)
        {
            return; // 아이콘 없음 — 프리팹 기본 유지
        }

        Image image = ResolveCardIconImage(); // Icon/Image 참조
        if (image == null)
        {
            return; // 표시 대상 없음
        }

        image.sprite = icon; // 강화별 아이콘
        image.enabled = true;
    }

    private Image ResolveCardIconImage() // 아이콘 Image 참조
    {
        if (cardIconImage != null)
        {
            return cardIconImage; // Inspector 지정
        }

        Transform iconTransform = transform.Find("Icon"); // 자식 Icon
        if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
        {
            cardIconImage = iconImage; // 캐시
            return cardIconImage;
        }

        return null; // Icon 없으면 스킵
    }

    ////// 전찬우추가 - 카드 프리팹 안의 TMP 텍스트에 문구 반영
    private void ApplyTexts(string title, string description)
    {
        CacheTextReferences(); // 텍스트 참조 보강
        if (titleText != null)
        {
            titleText.text = title; // 제목 갱신
        }

        if (descriptionText != null)
        {
            descriptionText.text = description; // 설명 갱신
        }
    }

    ////// 전찬우추가 - 인스펙터 연결 없이 기존 카드 텍스트 2개를 자동 사용
    private void CacheTextReferences()
    {
        if (titleText != null && descriptionText != null)
        {
            return; // 이미 준비됨
        }

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true); // 하위 텍스트 검색
        if (titleText == null && texts.Length > 0)
        {
            titleText = texts[0]; // 기존 카드 첫 텍스트
        }

        if (descriptionText == null && texts.Length > 1)
        {
            descriptionText = texts[1]; // 기존 카드 두 번째 텍스트
        }
    }

    ////// 전찬우추가 - 없음/불가 카드는 실제 버튼도 막음
    private void SetButtonInteractable(bool interactable)
    {
        Button button = GetComponent<Button>(); // 루트 버튼
        if (button == null)
        {
            button = GetComponentInChildren<Button>(true); // 자식 버튼 fallback
        }

        if (button != null)
        {
            button.interactable = interactable; // 클릭 가능 여부 반영
        }
    }
}
