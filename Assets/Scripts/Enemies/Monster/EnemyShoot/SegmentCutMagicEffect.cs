using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SegmentCutMagicEffect : MonoBehaviour // 선택된 무기 세그먼트에 표시하는 절단 마법 경고 효과
    {
        [Header("Effect Reference")]
        [SerializeField] private GameObject targetMarker; // 시전 중 선택된 세그먼트를 표시할 오브젝트

        private void Awake()
        {
            if (targetMarker != null)
            {
                targetMarker.SetActive(false); // 효과가 생성된 직후에는 경고 표시를 숨긴 상태로 시작한다.
            }
        }

        public void ShowWarning() // 선택된 무기 세그먼트에 절단 마법 경고 표시를 시작하는 함수
        {
            if (targetMarker != null)
            {
                targetMarker.SetActive(true); // 선택된 무기 세그먼트에 경고 표시를 활성화한다.
            }
        }

        public void Cancel() // 투사체 발사 또는 시전 취소 시 경고 효과를 제거하는 함수
        {
            Destroy(gameObject); // 생성된 경고 효과 인스턴스를 제거한다.
        }
    }
}