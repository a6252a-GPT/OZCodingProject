using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySpawner : MonoBehaviour // 몬스터 스폰 전체 관리자
    {
        private enum SpawnDirection // 스폰 방향 구분
        {
            Front, // 앞쪽 게이트
            Back, // 뒤쪽 게이트
            Left, // 왼쪽 게이트
            Right // 오른쪽 게이트
        }

        [System.Serializable]
        private sealed class MonsterSpawnEntry // 한 번 소환할 몬스터 종류와 개수
        {
            [SerializeField] private EnemyController prefab; // 생성할 몬스터 Prefab

            [Min(0)]
            [SerializeField] private int count = 1; // 한 번 소환할 때 이 몬스터를 몇 마리 만들지

            public EnemyController Prefab
            {
                get
                {
                    return prefab;
                }
            }

            public int Count
            {
                get
                {
                    return count;
                }
            }
        }

        [System.Serializable]
        private sealed class StageSpawnRule // 누적형 단계별 스폰 규칙
        {
            [SerializeField] private string stageName = "Stage 1"; // Inspector에서 구분하기 위한 이름

            [Min(0.0f)]
            [SerializeField] private float startTime = 0.0f; // 게임 시작 후 몇 초부터 이 규칙이 켜질지

            [Min(0.1f)]
            [SerializeField] private float spawnInterval = 8.0f; // 이 규칙이 켜진 뒤 몇 초마다 군단을 반복 소환할지

            [Min(1)]
            [SerializeField] private int spawnGroupCount = 1; // 한 번 소환할 때 몇 개의 게이트에서 군단을 만들지

            [Min(1)]
            [SerializeField] private int frontRowCount = 3; // 이 단계에서 한 줄에 최대 몇 마리까지 배치할지

            [SerializeField] private MonsterSpawnEntry[] monsterEntries; // 이 규칙으로 한 번 소환할 몬스터 조합

            public string StageName
            {
                get
                {
                    return stageName;
                }
            }

            public float StartTime
            {
                get
                {
                    return startTime;
                }
            }

            public float SpawnInterval
            {
                get
                {
                    return spawnInterval;
                }
            }

            public int SpawnGroupCount
            {
                get
                {
                    return spawnGroupCount;
                }
            }

            public int FrontRowCount
            {
                get
                {
                    return frontRowCount;
                }
            }

            public MonsterSpawnEntry[] MonsterEntries
            {
                get
                {
                    return monsterEntries;
                }
            }
        }

        private Transform nexus; // Nexus Transform, Inspector에는 노출하지 않고 자동 탐색한다.

        private Transform monsterRoot; // 생성된 몬스터를 정리할 부모 Transform

        [Header("Spawn Gates")]
        [SerializeField] private Transform[] frontGates; // 앞쪽 스폰 게이트 목록
        [SerializeField] private Transform[] backGates; // 뒤쪽 스폰 게이트 목록
        [SerializeField] private Transform[] leftGates; // 왼쪽 스폰 게이트 목록
        [SerializeField] private Transform[] rightGates; // 오른쪽 스폰 게이트 목록

        [Header("Group Formation Setting")]
        [Min(0.0f)]
        [SerializeField] private float groupForwardOffset = 3.0f; // 게이트 앞쪽으로 군단 중심을 얼마나 밀지

        [Min(0.1f)]
        [SerializeField] private float columnSpacing = 1.5f; // 몬스터 좌우 간격

        [Min(0.1f)]
        [SerializeField] private float rowSpacing = 1.5f; // 몬스터 앞뒤 간격

        [Min(0.0f)]
        [SerializeField] private float spawnGroundHeight = 0.72f; // 스폰 위치를 바닥 위로 올릴 높이

        [Range(1, 300)]
        [SerializeField] private int maxActiveMonsters = 120; // 씬에 유지할 최대 몬스터 수

        [Min(0.0f)]
        [SerializeField] private float firstSpawnDelay = 1.0f; // 각 규칙이 켜진 뒤 첫 스폰까지 대기 시간

        [Header("Stage Rules")]
        [SerializeField] private StageSpawnRule[] stageRules; // 누적형 단계별 스폰 규칙 목록

        private float elapsedGameTime; // 스폰 시스템이 켜진 뒤 지난 시간
        private float[] stageSpawnTimers; // Stage Rule별 다음 스폰까지 남은 시간
        private int spawnSerial; // 생성된 몬스터 이름 번호

        private void Awake()
        {
            if(nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core");
                nexus = nexusObject != null ? nexusObject.transform : null;
            }

            monsterRoot = MonsterRuntimeRoot.GetRootOrFallback(transform);
        }

        private void OnEnable()
        {
            elapsedGameTime = 0.0f; // 게임 진행 시간을 초기화한다.
            ResetStageSpawnTimers(); // Stage별 스폰 타이머를 초기화한다.
        }

        private void Update()
        {
            if (nexus == null) // Nexus가 없다면
            {
                return; // 스폰하지 않는다.
            }

            if (stageRules == null || stageRules.Length == 0) // 단계 규칙이 없다면
            {
                return; // 스폰하지 않는다.
            }

            EnsureStageSpawnTimers(); // Stage Rule 개수와 타이머 배열 개수를 맞춘다.

            elapsedGameTime += Time.deltaTime; // 전체 진행 시간을 증가시킨다.

            UpdateActiveStageRules(); // 시작 시간이 지난 Stage Rule들을 누적 실행한다.
        }

        private void ResetStageSpawnTimers() // Stage별 첫 스폰 타이머 초기화
        {
            if (stageRules == null) // Stage Rules가 없다면
            {
                stageSpawnTimers = null; // 타이머도 비운다.
                return;
            }

            stageSpawnTimers = new float[stageRules.Length]; // Stage Rule 개수만큼 타이머를 만든다.

            for (int i = 0; i < stageSpawnTimers.Length; i++) // 모든 타이머를 순회한다.
            {
                stageSpawnTimers[i] = firstSpawnDelay; // 각 Stage Rule이 켜진 뒤 첫 스폰까지의 시간을 저장한다.
            }
        }

        private void EnsureStageSpawnTimers() // Stage Rules 개수와 타이머 배열 개수 맞추기
        {
            if (stageRules == null) // Stage Rules가 없다면
            {
                stageSpawnTimers = null; // 타이머도 비운다.
                return;
            }

            if (stageSpawnTimers != null && stageSpawnTimers.Length == stageRules.Length) // 개수가 이미 맞다면
            {
                return; // 다시 만들지 않는다.
            }

            ResetStageSpawnTimers(); // 개수가 다르면 다시 만든다.
        }

        private void UpdateActiveStageRules() // 시작 시간이 지난 모든 Stage Rule을 처리한다.
        {
            for (int i = 0; i < stageRules.Length; i++) // 모든 Stage Rule을 순회한다.
            {
                StageSpawnRule rule = stageRules[i]; // 현재 Stage Rule

                if (rule == null) // 비어 있다면
                {
                    continue; // 건너뛴다.
                }

                if (elapsedGameTime < rule.StartTime) // 아직 이 규칙이 켜질 시간이 아니라면
                {
                    continue; // 실행하지 않는다.
                }

                stageSpawnTimers[i] -= Time.deltaTime; // 이 Stage Rule의 스폰 대기 시간을 줄인다.

                if (stageSpawnTimers[i] > 0.0f) // 아직 스폰 시간이 남았다면
                {
                    continue; // 이번 프레임에는 이 규칙으로 스폰하지 않는다.
                }

                SpawnStageGroups(rule); // 이 Stage Rule에 맞는 군단을 생성한다.
                stageSpawnTimers[i] = Mathf.Max(0.1f, rule.SpawnInterval); // 다음 반복 스폰 시간을 설정한다.
            }
        }

        private void SpawnStageGroups(StageSpawnRule rule) // 현재 Stage Rule의 군단 스폰
        {
            int capacity = Mathf.Max(0, maxActiveMonsters - EnemyController.ActiveCount); // 남은 생성 가능 몬스터 수

            if (capacity <= 0) // 생성 가능 수가 없다면
            {
                return; // 스폰하지 않는다.
            }

            int groupCount = Mathf.Max(1, rule.SpawnGroupCount); // 한 번에 만들 군단 수

            for (int i = 0; i < groupCount; i++) // 군단 수만큼 반복한다.
            {
                if (capacity <= 0) // 더 이상 생성할 수 없다면
                {
                    return; // 종료한다.
                }

                Transform gate = PickRandomGateFromAllDirections(); // Front, Back, Left, Right 중 랜덤 게이트를 고른다.

                if (gate == null) // 사용할 게이트가 없다면
                {
                    return; // 스폰할 위치가 없으므로 종료한다.
                }

                SpawnGroupAtGate(rule, gate, ref capacity); // 선택한 게이트에서 군단을 생성한다.
            }
        }

        private void SpawnGroupAtGate(StageSpawnRule rule, Transform gate, ref int capacity) // 특정 게이트에서 군단 생성
        {
            MonsterSpawnEntry[] entries = rule.MonsterEntries; // 현재 규칙의 몬스터 조합

            if (entries == null || entries.Length == 0) // 몬스터 조합이 없다면
            {
                return; // 생성하지 않는다.
            }

            int totalMonsterCount = GetTotalMonsterCount(entries); // 이 군단에서 생성할 전체 몬스터 수를 계산한다.

            if (totalMonsterCount <= 0) // 생성할 몬스터가 없다면
            {
                return; // 종료한다.
            }

            int frontRowCount = Mathf.Max(1, rule.FrontRowCount); // 한 줄에 세울 최대 몬스터 수

            Vector3 groupCenter = gate.position + gate.forward * groupForwardOffset; // 게이트 앞쪽으로 민 군단 앞줄 중심 위치
            groupCenter = GroundService.ProjectToGround(groupCenter, spawnGroundHeight); // 바닥 높이에 맞춘다.

            int formationIndex = 0; // 오와열 배치 순서

            for (int entryIndex = 0; entryIndex < entries.Length; entryIndex++) // 몬스터 조합을 순회한다.
            {
                MonsterSpawnEntry entry = entries[entryIndex]; // 현재 조합 항목

                if (entry == null || entry.Prefab == null || entry.Count <= 0) // 유효하지 않다면
                {
                    continue; // 건너뛴다.
                }

                for (int countIndex = 0; countIndex < entry.Count; countIndex++) // 설정된 개수만큼 생성한다.
                {
                    if (capacity <= 0) // 최대 몬스터 수에 도달했다면
                    {
                        return; // 생성 중지
                    }

                    Vector3 formationOffset = GetFormationOffset(formationIndex, totalMonsterCount, frontRowCount, gate); // 오와열 위치 오프셋을 계산한다.
                    Vector3 spawnPosition = groupCenter + formationOffset; // 최종 생성 위치를 계산한다.
                    spawnPosition = GroundService.ProjectToGround(spawnPosition, spawnGroundHeight); // 바닥 높이에 맞춘다.

                    SpawnMonster(entry.Prefab, spawnPosition, gate.rotation); // 몬스터 하나 생성

                    formationIndex++; // 다음 오와열 위치로 이동한다.
                    capacity--; // 남은 생성 가능 수 감소
                }
            }
        }

        private int GetTotalMonsterCount(MonsterSpawnEntry[] entries) // 한 군단에 생성될 총 몬스터 수 계산
        {
            int totalCount = 0; // 총 몬스터 수

            if (entries == null) // 조합이 없다면
            {
                return 0; // 0 반환
            }

            for (int i = 0; i < entries.Length; i++) // 조합을 순회한다.
            {
                MonsterSpawnEntry entry = entries[i]; // 현재 항목

                if (entry == null || entry.Prefab == null || entry.Count <= 0) // 유효하지 않다면
                {
                    continue; // 제외한다.
                }

                totalCount += entry.Count; // 생성 개수를 더한다.
            }

            return totalCount; // 총 몬스터 수를 반환한다.
        }

        private Vector3 GetFormationOffset(int unitIndex, int totalMonsterCount, int frontRowCount, Transform gate) // 오와열 배치 오프셋 계산
        {
            int rowIndex = unitIndex / frontRowCount; // 몇 번째 줄인지 계산한다.
            int columnIndex = unitIndex % frontRowCount; // 해당 줄에서 몇 번째 칸인지 계산한다.

            int rowStartIndex = rowIndex * frontRowCount; // 현재 줄의 시작 인덱스
            int remainingCount = totalMonsterCount - rowStartIndex; // 현재 줄부터 남은 몬스터 수
            int rowCount = Mathf.Min(frontRowCount, Mathf.Max(0, remainingCount)); // 현재 줄에 실제로 배치될 몬스터 수

            if (rowCount <= 0) // 안전장치
            {
                rowCount = frontRowCount; // 기본 줄 개수 사용
            }

            float centeredColumn = columnIndex - (rowCount - 1) * 0.5f; // 현재 줄 가운데를 기준으로 좌우 위치를 계산한다.
            float sideOffset = centeredColumn * columnSpacing; // 좌우 간격 적용
            float backOffset = rowIndex * rowSpacing; // 줄 번호에 따른 뒤쪽 간격 적용

            Vector3 right = gate.right; // 게이트 기준 오른쪽 방향
            right.y = 0.0f; // 높이 제거

            if (right.sqrMagnitude <= 0.0001f) // 오른쪽 방향 계산이 불가능하다면
            {
                right = Vector3.right; // 월드 오른쪽 방향 사용
            }

            right.Normalize(); // 길이 1로 만든다.

            Vector3 forward = gate.forward; // 게이트 기준 앞 방향
            forward.y = 0.0f; // 높이 제거

            if (forward.sqrMagnitude <= 0.0001f) // 앞 방향 계산이 불가능하다면
            {
                forward = Vector3.forward; // 월드 앞 방향 사용
            }

            forward.Normalize(); // 길이 1로 만든다.

            return right * sideOffset - forward * backOffset; // 앞줄 기준으로 뒤쪽 줄을 추가한 오와열 위치를 반환한다.
        }

        private void SpawnMonster(EnemyController prefab, Vector3 spawnPosition, Quaternion gateRotation) // 몬스터 하나 생성
        {
            Transform root = monsterRoot != null ? monsterRoot : transform; // 몬스터 부모 선택
            EnemyController monster = Instantiate(prefab, spawnPosition, gateRotation, root); // 몬스터 생성

            monster.name = $"{prefab.name}_{++spawnSerial:000}"; // 생성된 몬스터 이름에 번호를 붙인다.
        }

        private Transform PickRandomGateFromAllDirections() // 모든 방향 중 랜덤 게이트 선택
        {
            for (int i = 0; i < 20; i++) // 여러 번 시도한다.
            {
                SpawnDirection direction = (SpawnDirection)Random.Range(0, 4); // 4방향 중 하나 선택
                Transform gate = PickRandomGate(direction); // 해당 방향 게이트 선택

                if (gate != null) // 게이트가 있다면
                {
                    return gate; // 반환
                }
            }

            return null; // 사용할 수 있는 게이트가 없다.
        }

        private Transform PickRandomGate(SpawnDirection direction) // 방향별 게이트 배열에서 하나 선택
        {
            if (direction == SpawnDirection.Front)
            {
                return PickValidGate(frontGates);
            }

            if (direction == SpawnDirection.Back)
            {
                return PickValidGate(backGates);
            }

            if (direction == SpawnDirection.Left)
            {
                return PickValidGate(leftGates);
            }

            if (direction == SpawnDirection.Right)
            {
                return PickValidGate(rightGates);
            }

            return null;
        }

        private Transform PickValidGate(Transform[] gates) // 비어 있지 않은 게이트 중 하나 선택
        {
            if (gates == null || gates.Length == 0) // 배열이 없다면
            {
                return null; // 선택 불가
            }

            int validCount = 0; // 실제 연결된 게이트 개수

            for (int i = 0; i < gates.Length; i++) // 배열을 순회한다.
            {
                if (gates[i] != null) // 연결된 게이트라면
                {
                    validCount++; // 유효 개수 증가
                }
            }

            if (validCount <= 0) // 유효한 게이트가 없다면
            {
                return null; // 선택 불가
            }

            int randomIndex = Random.Range(0, validCount); // 유효 게이트 중 랜덤 순번 선택

            for (int i = 0; i < gates.Length; i++) // 다시 배열을 순회한다.
            {
                if (gates[i] == null) // 비어 있다면
                {
                    continue; // 건너뛴다.
                }

                if (randomIndex == 0) // 선택된 순번이라면
                {
                    return gates[i]; // 이 게이트 반환
                }

                randomIndex--; // 다음 유효 게이트로 이동
            }

            return null; // 안전용 fallback
        }
    }
}