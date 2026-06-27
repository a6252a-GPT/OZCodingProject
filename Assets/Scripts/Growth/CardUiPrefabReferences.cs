using UnityEngine;

[CreateAssetMenu(fileName = "CardUiPrefabReferences", menuName = "OZ/Card UI Prefab References")]
public sealed class CardUiPrefabReferences : ScriptableObject
{
    [SerializeField] private GameObject segmentChoiceCardPrefab; // 세그먼트 후보 선택 전용 카드
    [SerializeField] private StatUpgradeCatalogAsset statUpgradeCatalog; // 공통 강화 카드 데이터 카탈로그
    [SerializeField] private GameObject[] extraStatUpgradeCards = System.Array.Empty<GameObject>(); // 씬 수정 없이 추가하는 공통 카드

    public GameObject SegmentChoiceCardPrefab => segmentChoiceCardPrefab;
    public StatUpgradeCatalogAsset StatUpgradeCatalog => statUpgradeCatalog;
    public GameObject[] ExtraStatUpgradeCards => extraStatUpgradeCards;
}
