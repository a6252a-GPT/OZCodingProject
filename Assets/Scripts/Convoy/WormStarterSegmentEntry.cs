using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [Serializable]
    public struct WormStarterSegmentEntry // 지렁이별 시작 무기 세그먼트 매핑
    {
        public string WormId; // MetaWormIds 값
        public GameObject Prefab; // 해당 지렁이의 시작 세그먼트
    }
}
