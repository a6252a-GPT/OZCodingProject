using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySpawner : MonoBehaviour // 몬스터 스포너
    {
        public Transform Nexus; // 이동 목표
        public Transform MonsterRoot; // 생성 부모
        public Material MonsterMaterial; // 몬스터 재질
        [Min(0.1f)] public float SpawnInterval = 5f; // 스폰 간격
        [Range(1, 50)] public int SpawnCount = 15; // 묶음 수
        [Min(0f)] public float MinSpawnRadius = 24f; // 최소 반경
        [Min(0.1f)] public float MaxSpawnRadius = 32f; // 최대 반경
        [Min(0.1f)] public float MonsterMoveSpeed = 1.25f; // 이동 속도
        [Min(0.1f)] public float NexusStopRadius = 1.65f; // 도달 거리
        [Range(1, 300)] public int MaxActiveMonsters = 120; // 활성 상한
        public Vector3 MonsterScale = new Vector3(0.78f, 1.18f, 0.78f); // 캡슐 크기
        [Min(0f)] public float MonsterHeight = 0.72f; // 바닥 오프셋
        public EnemyGrade SpawnGrade = EnemyGrade.Monster; // 생성 등급

        private float spawnTimer; // 다음 스폰
        private int spawnSerial; // 이름 번호

        private void Awake() // 참조 보강
        {
            if (Nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                Nexus = nexusObject != null ? nexusObject.transform : null; // 목표 연결
            }

            if (MonsterRoot == null)
            {
                MonsterRoot = transform; // fallback 부모
            }
        }

        private void OnEnable() // 시작 예약
        {
            spawnTimer = 1f; // 첫 묶음 빠르게
        }

        private void Update() // 스폰 루프
        {
            if (Nexus == null)
            {
                return; // 목표 없음
            }

            spawnTimer -= Time.deltaTime; // 시간 감소
            if (spawnTimer > 0f)
            {
                return; // 대기 중
            }

            SpawnWave(); // 묶음 생성
            spawnTimer = SpawnInterval; // 다음 예약
        }

        private void SpawnWave() // 묶음 스폰
        {
            int capacity = Mathf.Max(0, MaxActiveMonsters - EnemyController.ActiveCount); // 남은 슬롯
            int count = Mathf.Min(SpawnCount, capacity); // 실제 생성 수
            for (int i = 0; i < count; i++)
            {
                SpawnMonster(); // 단일 생성
            }
        }

        private void SpawnMonster() // 몬스터 생성
        {
            Transform root = MonsterRoot != null ? MonsterRoot : transform; // 부모 선택
            GameObject monsterObject = GameObject.CreatePrimitive(PrimitiveType.Capsule); // 캡슐 몬스터
            monsterObject.name = $"{SpawnGrade}_Capsule_{++spawnSerial:000}";
            EnemyTags.TryApplyTag(monsterObject, SpawnGrade); // 태그 적용
            monsterObject.transform.SetParent(root, true); // 월드 루트
            monsterObject.transform.position = PickSpawnPosition(); // 위치 배치
            monsterObject.transform.localScale = MonsterScale; // 크기 적용

            Rigidbody rigidbody = monsterObject.AddComponent<Rigidbody>(); // 트리거용 바디
            rigidbody.isKinematic = true; // 스크립트 이동
            rigidbody.useGravity = false; // 중력 제외
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 관통 완화

            EnemyController monster = monsterObject.AddComponent<EnemyController>(); // 이동 로직
            monster.Configure(Nexus, MonsterMaterial, MonsterMoveSpeed, NexusStopRadius, MonsterHeight, SpawnGrade); // 값 연결
        }

        private Vector3 PickSpawnPosition() // 위치 선택
        {
            Vector2 direction = Random.insideUnitCircle; // 원형 랜덤
            if (direction.sqrMagnitude < 0.001f)
            {
                direction = Vector2.right; // 0벡터 보정
            }

            direction.Normalize(); // 방향화
            float min = Mathf.Min(MinSpawnRadius, MaxSpawnRadius); // 하한 보정
            float max = Mathf.Max(MinSpawnRadius, MaxSpawnRadius); // 상한 보정
            float radius = Random.Range(min, max); // 반경 선택
            Vector3 center = Nexus != null ? Nexus.position : Vector3.zero; // 넥서스 기준
            Vector3 position = center + new Vector3(direction.x * radius, 0f, direction.y * radius); // 평면 후보
            return GroundService.ProjectToGround(position, MonsterHeight); // 바닥 위치
        }
    }
}

