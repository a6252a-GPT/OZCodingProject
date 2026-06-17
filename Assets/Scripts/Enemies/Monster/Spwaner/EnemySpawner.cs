using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySpawner : MonoBehaviour // 몬스터 스포너
    {
        [System.Serializable]
        private sealed class EnemySpawnEntry // 스폰 가능한 몬스터 Prefab과 확률 가중치를 묶은 데이터
        {
            [SerializeField] private EnemyController prefab; // 생성할 몬스터 Prefab

            [Min(0)]
            [SerializeField] private int weight = 1; // 이 Prefab이 뽑힐 확률 가중치

            public EnemyController Prefab // 외부에서 Prefab을 읽기 위한 property
            {
                get
                {
                    return prefab; // 생성할 몬스터 Prefab을 반환한다.
                }
            }

            public int Weight // 외부에서 Weight를 읽기 위한 property
            {
                get
                {
                    return weight; // 확률 가중치를 반환한다.
                }
            }
        }

        [Header("Reference")]
        [SerializeField] private Transform nexus; // 이동 목표
        [SerializeField] private Transform monsterRoot; // 생성 부모
        [SerializeField] private Transform spawnArea; // 스폰 범위로 사용할 Ground Transform

        [Header("Spawn List")]
        [SerializeField] private EnemySpawnEntry[] spawnEntries; // 스폰 가능한 몬스터 Prefab 목록

        [Header("Spawn Timing")]
        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 5f; // 스폰 간격

        [Range(1, 50)]
        [SerializeField] private int spawnCount = 15; // 한 번에 생성할 몬스터 수

        [Range(1, 300)]
        [SerializeField] private int maxActiveMonsters = 120; // 씬에 유지할 최대 몬스터 수

        [Header("Spawn Area")]
        [Min(0f)]
        [SerializeField] private float spawnEdgePadding = 2f; // Ground 진짜 끝에서 안쪽으로 띄울 여백

        [Min(0.1f)]
        [SerializeField] private float spawnEdgeBandWidth = 5f; // 가장자리 안쪽 스폰 가능 띠 두께

        [Min(0f)]
        [SerializeField] private float spawnGroundHeight = 0.72f; // 스폰 위치를 바닥 위로 올릴 높이 오프셋

        private float spawnTimer; // 다음 스폰까지 남은 시간
        private int spawnSerial; // 생성된 몬스터 이름 번호

        private void Awake()
        {
            if (nexus == null) // 스폰 기준 위치가 연결되지 않았다면
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                nexus = nexusObject != null ? nexusObject.transform : null; // 목표 연결
            }

            if (monsterRoot == null) // 생성 부모가 연결되지 않았다면
            {
                monsterRoot = transform; // 자기 자신을 fallback 부모로 사용한다.
            }
        }

        private void OnEnable() // 시작 예약
        {
            spawnTimer = 1f; // 첫 묶음 빠르게
        }

        private void Update()  // 스폰 루프
        {
            if (nexus == null) // 스폰 기준 위치가 없다면
            {
                return; // 스폰하지 않고 종료한다.
            }

            spawnTimer -= Time.deltaTime; // 지난 시간만큼 스폰 대기 시간을 줄인다.

            if (spawnTimer > 0f) // 아직 다음 스폰 시간이 남았다면
            {
                return; // 이번 프레임에는 스폰하지 않는다.
            }

            SpawnWave(); // 묶음 스폰
            spawnTimer = spawnInterval; // 다음 스폰 시간을 다시 설정한다.
        }

        private void SpawnWave() // 묶음 스폰
        {
            int capacity = Mathf.Max(0, maxActiveMonsters - EnemyController.ActiveCount);  // 남은 슬롯
            int count = Mathf.Min(spawnCount, capacity);  // 실제 생성 수

            for (int i = 0; i < count; i++) // 생성할 수만큼 반복한다.
            {
                SpawnMonster(); // 몬스터 하나를 생성한다.
            }
        }

        private void SpawnMonster()// 몬스터 생성
        {
            EnemyController prefab = PickMonsterPrefab(); // 스폰 목록에서 생성할 몬스터 Prefab을 하나 고른다.

            if (prefab == null) // 생성할 Prefab이 없다면
            {
                return; // 생성하지 않고 종료한다.
            }

            Transform root = monsterRoot != null ? monsterRoot : transform; // 몬스터를 넣을 부모 Transform을 선택한다.
            Vector3 spawnPosition = PickSpawnPosition(); // 몬스터가 생성될 위치를 계산한다.

            EnemyController monster = Instantiate(prefab, spawnPosition, Quaternion.identity, root); // 선택된 Prefab을 생성한다.

            monster.name = $"{prefab.name}_{++spawnSerial:000}"; // 생성된 몬스터 이름에 번호를 붙인다.
        }

        private EnemyController PickMonsterPrefab() // 스폰 목록에서 확률 가중치에 따라 Prefab을 선택하는 함수
        {
            if (spawnEntries == null || spawnEntries.Length == 0) // 스폰 목록이 없거나 비어 있다면
            {
                return null; // 생성할 Prefab이 없다고 반환한다.
            }

            int totalWeight = 0; // 전체 가중치 합계

            for (int i = 0; i < spawnEntries.Length; i++) // 스폰 목록을 순회한다.
            {
                EnemySpawnEntry entry = spawnEntries[i]; // 현재 스폰 항목을 가져온다.

                if (entry == null || entry.Prefab == null || entry.Weight <= 0) // 항목이 비어 있거나 Prefab이 없거나 가중치가 0 이하라면
                {
                    continue; // 이 항목은 확률 계산에서 제외한다.
                }

                totalWeight += entry.Weight; // 유효한 항목의 가중치를 합산한다.
            }

            if (totalWeight <= 0) // 유효한 가중치가 없다면
            {
                return null; // 생성할 Prefab이 없다고 반환한다.
            }

            int randomValue = Random.Range(0, totalWeight); // 0부터 전체 가중치 직전까지 랜덤 값을 뽑는다.

            for (int i = 0; i < spawnEntries.Length; i++) // 다시 스폰 목록을 순회한다.
            {
                EnemySpawnEntry entry = spawnEntries[i]; // 현재 스폰 항목을 가져온다.

                if (entry == null || entry.Prefab == null || entry.Weight <= 0) // 유효하지 않은 항목이라면
                {
                    continue; // 선택 대상에서 제외한다.
                }

                if (randomValue < entry.Weight) // 랜덤 값이 현재 항목의 가중치 범위 안에 들어왔다면
                {
                    return entry.Prefab; // 이 Prefab을 선택한다.
                }

                randomValue -= entry.Weight; // 현재 항목의 가중치만큼 랜덤 값을 줄이고 다음 항목을 확인한다.
            }

            return null; // 안전용 fallback, 정상 상황에서는 거의 도달하지 않는다.
        }

        private Vector3 PickSpawnPosition() // Ground 가장자리 안쪽 띠 영역에서 스폰 위치를 선택하는 함수
        {
            Bounds bounds = GetSpawnAreaBounds(); // Ground 크기를 기준으로 스폰 범위를 가져온다.

            float paddingX = Mathf.Min(spawnEdgePadding, Mathf.Max(0f, bounds.extents.x - 0.1f)); // X축 여백이 Ground 크기를 넘지 않게 보정한다.
            float paddingZ = Mathf.Min(spawnEdgePadding, Mathf.Max(0f, bounds.extents.z - 0.1f)); // Z축 여백이 Ground 크기를 넘지 않게 보정한다.

            float minX = bounds.min.x + paddingX; // Ground 왼쪽 끝에서 여백만큼 안쪽 위치
            float maxX = bounds.max.x - paddingX; // Ground 오른쪽 끝에서 여백만큼 안쪽 위치
            float minZ = bounds.min.z + paddingZ; // Ground 아래쪽 끝에서 여백만큼 안쪽 위치
            float maxZ = bounds.max.z - paddingZ; // Ground 위쪽 끝에서 여백만큼 안쪽 위치

            float bandWidth = Mathf.Max(0.1f, spawnEdgeBandWidth); // 가장자리 스폰 띠 두께가 너무 작아지지 않게 보정한다.

            int side = Random.Range(0, 4); // 0 왼쪽, 1 오른쪽, 2 아래쪽, 3 위쪽 중 하나를 고른다.

            float x; // 최종 X 위치
            float z; // 최종 Z 위치

            if (side == 0) // 왼쪽 가장자리 띠
            {
                float bandMaxX = Mathf.Min(minX + bandWidth, maxX); // 왼쪽 안쪽 띠의 최대 X 위치
                x = RandomRangeSafe(minX, bandMaxX); // 왼쪽 안쪽 띠에서 X를 뽑는다.
                z = RandomRangeSafe(minZ, maxZ); // Z는 Ground 높이 범위 안에서 뽑는다.
            }
            else if (side == 1) // 오른쪽 가장자리 띠
            {
                float bandMinX = Mathf.Max(maxX - bandWidth, minX); // 오른쪽 안쪽 띠의 최소 X 위치
                x = RandomRangeSafe(bandMinX, maxX); // 오른쪽 안쪽 띠에서 X를 뽑는다.
                z = RandomRangeSafe(minZ, maxZ); // Z는 Ground 높이 범위 안에서 뽑는다.
            }
            else if (side == 2) // 아래쪽 가장자리 띠
            {
                x = RandomRangeSafe(minX, maxX); // X는 Ground 너비 범위 안에서 뽑는다.
                float bandMaxZ = Mathf.Min(minZ + bandWidth, maxZ); // 아래쪽 안쪽 띠의 최대 Z 위치
                z = RandomRangeSafe(minZ, bandMaxZ); // 아래쪽 안쪽 띠에서 Z를 뽑는다.
            }
            else // 위쪽 가장자리 띠
            {
                x = RandomRangeSafe(minX, maxX); // X는 Ground 너비 범위 안에서 뽑는다.
                float bandMinZ = Mathf.Max(maxZ - bandWidth, minZ); // 위쪽 안쪽 띠의 최소 Z 위치
                z = RandomRangeSafe(bandMinZ, maxZ); // 위쪽 안쪽 띠에서 Z를 뽑는다.
            }

            Vector3 position = new Vector3(x, 0f, z); // Ground 가장자리 안쪽에서 뽑은 스폰 위치를 만든다.

            return GroundService.ProjectToGround(position, spawnGroundHeight); // 바닥 기준 높이에 맞춰 보정한 위치를 반환한다.
        }

        private Bounds GetSpawnAreaBounds() // 스폰 범위 Bounds를 가져오는 함수
        {
            if (spawnArea != null) // 스폰 범위 Ground가 연결되어 있다면
            {
                Collider areaCollider = spawnArea.GetComponent<Collider>(); // Ground에 Collider가 있는지 확인한다.

                if (areaCollider != null) // Collider가 있다면
                {
                    return areaCollider.bounds; // Collider의 실제 월드 크기를 스폰 범위로 사용한다.
                }

                Renderer areaRenderer = spawnArea.GetComponent<Renderer>(); // Collider가 없다면 Renderer가 있는지 확인한다.

                if (areaRenderer != null) // Renderer가 있다면
                {
                    return areaRenderer.bounds; // Renderer의 실제 월드 크기를 스폰 범위로 사용한다.
                }
            }

            Vector3 center = nexus != null ? nexus.position : Vector3.zero; // Ground 연결이 없을 때 사용할 기준 위치
            return new Bounds(center, new Vector3(50f, 1f, 50f)); // 임시 사각형 범위를 반환한다.
        }

        private float RandomRangeSafe(float min, float max) // 최소값과 최대값이 뒤집혀도 안전하게 랜덤 값을 반환하는 함수
        {
            if (min > max) // 최소값이 최대값보다 크다면
            {
                float temp = min; // 임시 변수에 min을 저장한다.
                min = max; // max를 min에 넣는다.
                max = temp; // 기존 min을 max에 넣는다.
            }

            if (Mathf.Approximately(min, max)) // 두 값이 거의 같다면
            {
                return min; // 랜덤 범위가 없으므로 그 값을 그대로 반환한다.
            }

            return Random.Range(min, max); // 정상 범위에서 랜덤 값을 반환한다.
        }
    }
}