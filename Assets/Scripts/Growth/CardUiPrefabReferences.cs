using UnityEngine;

[CreateAssetMenu(fileName = "CardUiPrefabReferences", menuName = "OZ/Card UI Prefab References")]
public sealed class CardUiPrefabReferences : ScriptableObject
{
    [SerializeField] private GameObject segmentChoiceCardPrefab; // 세그먼트 후보 선택 전용 카드

    public GameObject SegmentChoiceCardPrefab => segmentChoiceCardPrefab;
}
