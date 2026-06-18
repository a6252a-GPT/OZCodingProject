using TeamProject01.Gameplay;
using UnityEngine;
using UnityEngine.UI;

public class StatUpgrade : MonoBehaviour
{
    public enum StatCardTier
    {
        Normal = 1, // 일반 (1배, 흰색)
        Rare = 2, // 레어 (2배, 노란색)
        Unique = 3 // 유니크 (3배, 초록색)
    }

    [Header("코어 성장 연동")]
    [Min(1)][SerializeField] private int levelDelta = 1; // 선택 시 소비할 레벨 증가량
    [Header("공격력 배율 보너스")]
    [SerializeField] private float damageMultiplierBonus; // 공격력 배율 보너스
    [Header("공격속도 배율 보너스")]
    [SerializeField] private float attackSpeedMultiplierBonus; // 공격속도 배율 보너스
    [Header("회전력 보너스")]
    [SerializeField] private float turnSpeedBonus; // 회전력 보너스
    [Header("충돌힘 보너스")]
    [SerializeField] private float collisionForceBonus; // 충돌힘 보너스
    [Header("재결합 범위 보너스")]
    [SerializeField] private float rejoinRangeBonus; // 재결합 범위 보너스

    [Header("카드 등급 표시")]
    [SerializeField] private Image cardHighlightImage; // 비우면 자식 Image 탐색
    [SerializeField] private Color normalCardColor = Color.white; // 일반 카드 색
    [SerializeField] private Color rareCardColor = Color.yellow; // 레어 카드 색 (2배)
    [SerializeField] private Color uniqueCardColor = Color.green; // 유니크 카드 색 (3배)

    private float upgradeMultiplier = 1f; // 생성 시 1, 2, 3
    private StatCardTier currentTier = StatCardTier.Normal; // 현재 등급

    public StatCardTier CurrentTier => currentTier; // 외부 등급 조회
    public bool IsRareUpgrade => currentTier == StatCardTier.Rare; // 레어 카드 여부
    public bool IsUniqueUpgrade => currentTier == StatCardTier.Unique; // 유니크 카드 여부

    public void RollSpawnVariant(float rareChancePercent, float uniqueChancePercent) // 생성 시 등급·배율·색상 결정
    {
        float uniqueChance = Mathf.Clamp(uniqueChancePercent, 0f, 100f) * 0.01f; // 유니크 확률(0~1)
        float rareChance = Mathf.Clamp(rareChancePercent, 0f, 100f) * 0.01f; // 레어 확률(0~1)
        float roll = Random.value; // 0~1 난수

        if (roll < uniqueChance) // 유니크 구간
        {
            currentTier = StatCardTier.Unique; // 유니크 등급 저장
            upgradeMultiplier = 3f; // Inspector 수치 3배
        }
        else if (roll < uniqueChance + rareChance) // 레어 구간
        {
            currentTier = StatCardTier.Rare; // 레어 등급 저장
            upgradeMultiplier = 2f; // Inspector 수치 2배
        }
        else // 일반 구간
        {
            currentTier = StatCardTier.Normal; // 일반 등급 저장
            upgradeMultiplier = 1f; // Inspector 수치 그대로
        }

        ApplyCardVisual(); // 등급에 맞는 색상 적용
    }

    public GrowthStatData CreateGrowthStatData() // 코어로 보낼 성장값 생성
    {
        return GrowthStatData.CreateConvoyUpgrade(
            levelDelta,
            damageMultiplierBonus * upgradeMultiplier,
            attackSpeedMultiplierBonus * upgradeMultiplier,
            turnSpeedBonus * upgradeMultiplier,
            collisionForceBonus * upgradeMultiplier,
            rejoinRangeBonus * upgradeMultiplier);
    }

    public bool TryApplyToCore() // 코어에 성장값 적용
    {
        GrowthStatData growth = CreateGrowthStatData(); // 적용할 데이터 준비
        if (!growth.HasAnyValue) // 레벨/보너스 없음
        {
            return false; // 적용 실패
        }

        return CoreStatProvider.TryApplyGrowth(growth); // 경험치 소비 + 스탯 반영
    }

    private void ApplyCardVisual() // 등급에 따라 카드 색 변경
    {
        Image image = ResolveCardHighlightImage(); // 강조 Image 찾기
        if (image == null)
        {
            return; // 표시 대상 없음
        }

        Color targetColor = normalCardColor; // 기본은 일반 색
        if (currentTier == StatCardTier.Unique)
        {
            targetColor = uniqueCardColor; // 유니크 → 초록
        }
        else if (currentTier == StatCardTier.Rare)
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
}
