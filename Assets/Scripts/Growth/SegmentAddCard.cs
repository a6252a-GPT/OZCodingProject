using TeamProject01.Gameplay;
using UnityEngine;

public class SegmentAddCard : MonoBehaviour
{
    [Header("코어 세그먼트 추가")]
    [Min(1)][SerializeField] private int levelDelta = 1; // 선택 시 소비할 레벨 증가량
    [SerializeField] private string segmentId = "SG01_MachineGun"; // 추가할 세그먼트 ID
    [Min(1)][SerializeField] private int segmentAddCount = 1; // 추가할 세그먼트 수

    public GrowthStatData CreateGrowthStatData() // 코어로 보낼 세그먼트 추가 데이터
    {
        return GrowthStatData.CreateAddSegment(levelDelta, segmentId, segmentAddCount);
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
}
