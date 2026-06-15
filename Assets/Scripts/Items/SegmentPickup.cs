using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SegmentPickup : MonoBehaviour // 필드 세그먼트 픽업
    {
        public ConvoyController Controller; // 획득 대상
        public GameObject SegmentPrefab; // 지급할 세그먼트
        [Min(0.1f)] public float PickupRadius = 1.2f; // 획득 거리
        [Min(0f)] public float RotateSpeed = 55f; // 회전 속도
        [Min(0f)] public float BobAmplitude = 0.08f; // 흔들림 높이
        [Min(0f)] public float BobSpeed = 2.6f; // 흔들림 속도

        private Vector3 basePosition; // 기준 위치
        private bool collected; // 중복 획득 방지

        private void Awake() // 초기화
        {
            basePosition = transform.position; // 떠오름 기준
        }

        private void Update() // 획득 검사
        {
            Animate(); // 표시 연출
            TryCollect(); // 플레이어 접촉
        }

        public void Configure(ConvoyController controller, Material material, float pickupRadius, GameObject segmentPrefab) // 스폰 설정
        {
            Controller = controller; // 대상 연결
            SegmentPrefab = segmentPrefab; // 지급 프리팹
            PickupRadius = pickupRadius; // 거리 적용
            basePosition = transform.position; // 기준 갱신

            Renderer renderer = GetComponent<Renderer>(); // 표시 renderer
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material; // 픽업 재질
            }
        }

        private void Animate() // 간단 연출
        {
            if (RotateSpeed > 0f)
            {
                transform.Rotate(0f, RotateSpeed * Time.deltaTime, 0f, Space.World); // 회전
            }

            if (BobAmplitude > 0f && BobSpeed > 0f)
            {
                Vector3 position = basePosition; // 기준 복사
                position.y += Mathf.Sin(Time.time * BobSpeed) * BobAmplitude; // 상하 흔들림
                transform.position = position; // 위치 반영
            }
        }

        private void TryCollect() // 획득 판정
        {
            if (collected || Controller == null)
            {
                return; // 처리 불가
            }

            Vector3 offset = Controller.transform.position - transform.position; // 거리 벡터
            offset.y = 0f; // 평면 판정
            if (offset.sqrMagnitude > PickupRadius * PickupRadius)
            {
                return; // 범위 밖
            }

            if (Controller.TryAddSegment(SegmentPrefab))
            {
                collected = true; // 중복 방지
                Destroy(gameObject); // 픽업 소비
            }
        }
    }
}

