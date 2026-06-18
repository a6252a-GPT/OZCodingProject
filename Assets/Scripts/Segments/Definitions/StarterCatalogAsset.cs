using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [CreateAssetMenu(menuName = "OZ/Segments/Starter Catalog", fileName = "StarterCatalog")]
    public sealed class StarterCatalogAsset : ScriptableObject // 지렁이별 시작 세그먼트 목록
    {
        public StarterSegmentEntry[] Starters = Array.Empty<StarterSegmentEntry>(); // 시작 무기 목록

        public bool TryGetStarterPrefab(string wormId, out GameObject prefab) // 지렁이 → 스타터 프리팹
        {
            prefab = null; // 기본값
            if (Starters == null)
            {
                return false; // 목록 없음
            }

            string normalizedWormId = string.IsNullOrWhiteSpace(wormId) ? MetaWormIds.Basic : wormId.Trim(); // 기본 지렁이
            for (int i = 0; i < Starters.Length; i++)
            {
                StarterSegmentEntry entry = Starters[i];
                if (!string.Equals(entry.NormalizedWormId, normalizedWormId, StringComparison.OrdinalIgnoreCase))
                {
                    continue; // 다른 지렁이
                }

                return entry.TryGetPrefab(out prefab); // 찾은 프리팹
            }

            return false; // 없음
        }
    }

    [System.Serializable]
    public struct StarterSegmentEntry // 지렁이별 시작 무기 등록
    {
        public string WormId; // 예: worm_basic
        public SegmentDefinition StarterDefinition; // 스타터 정의
        public GameObject StarterPrefab; // 직접 지정 fallback
        [TextArea(1, 3)] public string Memo; // 팀원 메모

        public string NormalizedWormId => string.IsNullOrWhiteSpace(WormId) ? MetaWormIds.Basic : WormId.Trim(); // 비교 ID

        public bool TryGetPrefab(out GameObject prefab) // 프리팹 결정
        {
            prefab = StarterPrefab; // 직접 지정 우선
            if (prefab != null)
            {
                return true;
            }

            return StarterDefinition != null && StarterDefinition.TryGetSegmentPrefab(1, out prefab); // 정의 fallback
        }
    }
}
