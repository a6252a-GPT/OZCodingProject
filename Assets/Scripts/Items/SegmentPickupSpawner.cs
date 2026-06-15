using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class SegmentPickupSpawner : MonoBehaviour // 세그먼트 픽업 스포너
    {
        public ConvoyController Controller; // 획득 대상
        public Transform Nexus; // 스폰 기준
        public Transform PickupRoot; // 생성 부모
        public Material PickupMaterial; // 픽업 재질
        public GameObject[] SegmentPrefabs; // 랜덤 세그먼트 후보
        [Min(0.1f)] public float MinDropInterval = 2f; // 최소 드롭 간격
        [Min(0.1f)] public float MaxDropInterval = 5f; // 최대 드롭 간격
        [Min(0f)] public float MinSpawnRadius = 5f; // 최소 반경
        [Min(0.1f)] public float SpawnRadius = 24f; // 최대 반경
        [Min(0f)] public float MinPlayerDistance = 5f; // 플레이어 주변 제외
        [Min(0.1f)] public float PickupRadius = 1.2f; // 획득 거리
        [Range(1, 20)] public int MaxActivePickups = 6; // 필드 최대 수
        public Vector3 PickupScale = new Vector3(0.86f, 0.42f, 0.86f); // 픽업 크기
        [Min(0f)] public float PickupHeight = 0.36f; // 바닥 오프셋

        private readonly List<SegmentPickup> activePickups = new List<SegmentPickup>(8); // 활성 픽업
        private float nextDropTimer; // 다음 드롭
        private int spawnSerial; // 이름 번호

        private void Awake() // 참조 보강
        {
            if (Nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                Nexus = nexusObject != null ? nexusObject.transform : null; // 기준 연결
            }
        }

        private void OnEnable() // 활성화
        {
            ScheduleNextDrop(); // 첫 드롭 예약
        }

        private void Update() // 드롭 루프
        {
            CleanupPickedItems(); // null 정리

            if (Controller == null)
            {
                return; // 대상 없음
            }

            nextDropTimer -= Time.deltaTime; // 시간 감소
            if (nextDropTimer > 0f)
            {
                return; // 대기 중
            }

            if (activePickups.Count < MaxActivePickups)
            {
                SpawnPickup(); // 필드 드롭
                ScheduleNextDrop(); // 다음 예약
            }
            else
            {
                nextDropTimer = 1f; // 포화 재검사
            }
        }

        private void SpawnPickup() // 픽업 생성
        {
            GameObject segmentPrefab = PickSegmentPrefab(); // 지급 세그먼트 선택
            if (segmentPrefab == null)
            {
                return; // 후보 없음
            }

            Transform root = PickupRoot != null ? PickupRoot : transform; // 부모 선택
            GameObject pickupObject = GameObject.CreatePrimitive(PrimitiveType.Cube); // 픽업 큐브
            pickupObject.name = $"SegmentPickup_{segmentPrefab.name}_{++spawnSerial:00}";
            pickupObject.transform.SetParent(root, true); // 필드 루트
            pickupObject.transform.position = PickSpawnPosition(); // 위치 배치
            pickupObject.transform.localScale = PickupScale; // 크기 적용

            Collider collider = pickupObject.GetComponent<Collider>(); // 기본 collider
            if (collider != null)
            {
                Destroy(collider); // 거리 판정 사용
            }

            SegmentPickup pickup = pickupObject.AddComponent<SegmentPickup>(); // 획득 로직
            pickup.Configure(Controller, PickupMaterial, PickupRadius, segmentPrefab); // 연결 설정
            activePickups.Add(pickup); // 목록 등록
        }

        private GameObject PickSegmentPrefab() // 세그먼트 후보 선택
        {
            if (SegmentPrefabs != null && SegmentPrefabs.Length > 0)
            {
                int startIndex = Random.Range(0, SegmentPrefabs.Length); // 시작 후보
                for (int i = 0; i < SegmentPrefabs.Length; i++)
                {
                    int index = (startIndex + i) % SegmentPrefabs.Length; // 순환 검사
                    GameObject prefab = SegmentPrefabs[index]; // 후보 프리팹
                    if (prefab != null)
                    {
                        return prefab; // 유효 후보
                    }
                }
            }

            return Controller != null ? Controller.SegmentPrefab : null; // 기본 프리팹 fallback
        }

        private Vector3 PickSpawnPosition() // 위치 선택
        {
            Vector3 center = Nexus != null ? Nexus.position : Vector3.zero; // 넥서스 기준
            Vector3 position = center; // 후보 위치
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 direction = Random.insideUnitCircle; // 원형 랜덤
                if (direction.sqrMagnitude < 0.001f)
                {
                    direction = Vector2.right; // 0벡터 보정
                }

                direction.Normalize(); // 방향화
                float radius = Random.Range(Mathf.Min(MinSpawnRadius, SpawnRadius), SpawnRadius); // 반경 선택
                position = center + new Vector3(direction.x * radius, 0f, direction.y * radius); // 평면 후보
                position = GroundService.ProjectToGround(position, PickupHeight); // 바닥 위치

                if (Controller == null)
                {
                    break; // 대상 없음
                }

                Vector3 offset = position - Controller.transform.position; // 플레이어 거리
                offset.y = 0f; // 평면 거리
                if (offset.sqrMagnitude >= MinPlayerDistance * MinPlayerDistance)
                {
                    break; // 충분히 멂
                }
            }

            return position; // 최종 후보
        }

        private void ScheduleNextDrop() // 다음 시간
        {
            float min = Mathf.Min(MinDropInterval, MaxDropInterval); // 하한 보정
            float max = Mathf.Max(MinDropInterval, MaxDropInterval); // 상한 보정
            nextDropTimer = Random.Range(min, max); // 2~5초
        }

        private void CleanupPickedItems() // 목록 정리
        {
            activePickups.RemoveAll(pickup => pickup == null); // 소비된 픽업
        }
    }
}

