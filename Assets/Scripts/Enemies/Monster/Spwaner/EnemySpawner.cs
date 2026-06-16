using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySpawner : MonoBehaviour // 몬스터 스포너
    {
        [SerializeField] private Transform nexus; // 이동 목표
        [SerializeField] private Transform monsterRoot; // 생성 부모
        [SerializeField] private Material monsterMaterial; // 몬스터 재질

        [SerializeField] private EnemyController meleeMonsterPrefab; // 근거리 몬스터 Prefab
        [SerializeField] private EnemyController rangedMonsterPrefab; // 원거리 몬스터 Prefab

        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 5f; // 스폰 간격

        [Range(1, 50)]
        [SerializeField] private int spawnCount = 15; // 묶음 수

        [Min(0f)]
        [SerializeField] private float minSpawnRadius = 24f; // 최소 반경

        [Min(0.1f)]
        [SerializeField] private float maxSpawnRadius = 32f; // 최대 반경

        [Min(0.1f)]
        [SerializeField] private float monsterMoveSpeed = 1.25f; // 이동 속도

        [Min(0.1f)]
        [SerializeField] private float nexusStopRadius = 1.65f; // 도달 거리

        [Range(1, 300)]
        [SerializeField] private int maxActiveMonsters = 120; // 활성 상한

        [Min(0f)]
        [SerializeField] private float monsterHeight = 0.72f; // 바닥 오프셋

        [SerializeField] private EnemyGrade spawnGrade = EnemyGrade.Monster; // 생성 등급

        [Range(0f, 1f)][SerializeField] private float rangedSpawnChance = 0.35f; // 원거리 몬스터 생성 확률

        private float spawnTimer; // 다음 스폰
        private int spawnSerial; // 이름 번호

        private void Awake() // 참조 보강
        {
            if (nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                nexus = nexusObject != null ? nexusObject.transform : null; // 목표 연결
            }

            if (monsterRoot == null)
            {
                monsterRoot = transform; // fallback 부모
            }
        }

        private void OnEnable() // 시작 예약
        {
            spawnTimer = 1f; // 첫 묶음 빠르게
        }

        private void Update() // 스폰 루프
        {
            if (nexus == null)
            {
                return; // 목표 없음
            }

            spawnTimer -= Time.deltaTime; // 시간 감소

            if (spawnTimer > 0f)
            {
                return; // 대기 중
            }

            SpawnWave(); // 묶음 생성
            spawnTimer = spawnInterval; // 다음 예약
        }

        private void SpawnWave() // 묶음 스폰
        {
            int capacity = Mathf.Max(0, maxActiveMonsters - EnemyController.ActiveCount); // 남은 슬롯
            int count = Mathf.Min(spawnCount, capacity); // 실제 생성 수

            for (int i = 0; i < count; i++)
            {
                SpawnMonster(); // 단일 생성
            }
        }

        private void SpawnMonster() // 몬스터 생성
        {
            EnemyController prefab = PickMonsterPrefab(); // 생성할 몬스터 Prefab 선택

            if (prefab == null) // 생성할 Prefab이 없다면
            {
                return; // 생성하지 않고 종료한다.
            }

            Transform root = monsterRoot != null ? monsterRoot : transform; // 부모 선택
            Vector3 spawnPosition = PickSpawnPosition(); // 위치 배치

            EnemyController monster = Instantiate(prefab, spawnPosition, Quaternion.identity, root); // 몬스터 Prefab 생성

            monster.name = $"{spawnGrade}_{prefab.name}_{++spawnSerial:000}"; // 생성된 몬스터 이름 설정

            EnemyTags.TryApplyTag(monster.gameObject, spawnGrade); // 태그 적용

            monster.Configure(nexus, monsterMaterial, monsterMoveSpeed, nexusStopRadius, monsterHeight, spawnGrade); // 값 연결
        }

        private EnemyController PickMonsterPrefab() // 생성할 몬스터 Prefab 선택
        {
            if (meleeMonsterPrefab == null && rangedMonsterPrefab == null) // 근거리와 원거리 Prefab이 모두 없다면
            {
                return null; // 생성할 Prefab이 없다고 반환한다.
            }

            if (meleeMonsterPrefab == null) // 근거리 Prefab이 없다면
            {
                return rangedMonsterPrefab; // 원거리 Prefab만 사용한다.
            }

            if (rangedMonsterPrefab == null) // 원거리 Prefab이 없다면
            {
                return meleeMonsterPrefab; // 근거리 Prefab만 사용한다.
            }

            if (Random.value < rangedSpawnChance) // 원거리 생성 확률에 걸렸다면
            {
                return rangedMonsterPrefab; // 원거리 몬스터 Prefab을 반환한다.
            }

            return meleeMonsterPrefab; // 기본적으로 근거리 몬스터 Prefab을 반환한다.
        }

        private Vector3 PickSpawnPosition() // 위치 선택
        {
            Vector2 direction = Random.insideUnitCircle; // 원형 랜덤

            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right; // 0벡터 보정
            }

            direction.Normalize(); // 방향화

            float min = Mathf.Min(minSpawnRadius, maxSpawnRadius); // 하한 보정
            float max = Mathf.Max(minSpawnRadius, maxSpawnRadius); // 상한 보정
            float radius = Random.Range(min, max); // 반경 선택

            Vector3 center = nexus != null ? nexus.position : Vector3.zero; // 넥서스 기준
            Vector3 position = center + new Vector3(direction.x * radius, 0f, direction.y * radius); // 평면 후보

            return GroundService.ProjectToGround(position, monsterHeight); // 바닥 위치
        }
    }
}