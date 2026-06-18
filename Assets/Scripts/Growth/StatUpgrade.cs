using TeamProject01.Gameplay;
using UnityEngine;
using UnityEngine.UI;

public class StatUpgrade : MonoBehaviour
{
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

    [Header("2배 강화 표시")]
    [SerializeField] private Image cardHighlightImage; // 비우면 자식 Image 탐색
    [SerializeField] private Color normalCardColor = Color.white; // 일반 카드 색
    [SerializeField] private Color doubleUpgradeCardColor = Color.yellow; // 2배 강화 카드 색

    private float upgradeMultiplier = 1f; // 생성 시 1 또는 2

    public bool IsDoubleUpgrade => upgradeMultiplier > 1f; // 2배 강화 여부

    public void RollSpawnVariant(float doubleUpgradeChance) // 생성 시 강화 배율·색상 결정
    {
        upgradeMultiplier = doubleUpgradeChance > 0f && Random.value < doubleUpgradeChance ? 2f : 1f;
        ApplyCardVisual();
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

    private void ApplyCardVisual() // 2배 강화 여부에 따라 카드 색 변경
    {
        Image image = ResolveCardHighlightImage();
        if (image == null)
        {
            return;
        }

        Color targetColor = IsDoubleUpgrade ? doubleUpgradeCardColor : normalCardColor;
        image.color = new Color(targetColor.r, targetColor.g, targetColor.b, image.color.a); // 알파 유지
    }

    private Image ResolveCardHighlightImage() // 강조용 Image 참조
    {
        if (cardHighlightImage != null)
        {
            return cardHighlightImage;
        }

        Transform imageTransform = transform.Find("Image");
        if (imageTransform != null && imageTransform.TryGetComponent(out Image childImage))
        {
            cardHighlightImage = childImage;
            return cardHighlightImage;
        }

        if (TryGetComponent(out Image rootImage))
        {
            cardHighlightImage = rootImage;
        }

        return cardHighlightImage;
    }
}
