using UnityEngine;

namespace TeamProject01.Gameplay
{
    public abstract class SegmentWeaponBehaviour : MonoBehaviour // 세그먼트 무기 공통
    {
        public ConvoySegmentRuntime Segment { get; private set; } // 소유 세그먼트
        public bool IsWeaponActive { get; private set; } // 작동 여부

        public virtual void Configure(ConvoySegmentRuntime segment) // 세그먼트 연결
        {
            Segment = segment; // 소유 저장
        }

        public virtual void SetWeaponActive(bool active) // 작동 상태
        {
            IsWeaponActive = active; // 상태 저장
        }

        public abstract void TickWeapon(float deltaTime); // 무기 갱신
    }
}
