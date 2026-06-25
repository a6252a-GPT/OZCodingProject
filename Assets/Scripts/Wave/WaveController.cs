using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class WaveController : MonoBehaviour // 무한 웨이브의 진행과 몬스터 조합풀 선택을 관리한다.
    {
        public enum SpecialWaveType // 현재 Stage가 어떤 특수 웨이브인지 구분한다.
        {
            Normal, // 일반 웨이브
            Elite, // 엘리트 웨이브
            Boss // 보스 웨이브
        }

        [Serializable]
        public sealed class MonsterPoolEntry // 조합풀 안에 들어가는 몬스터 1종류
        {
            [Tooltip("이 조합풀에서 생성할 몬스터 Prefab입니다.")]
            public EnemyController prefab; // 생성할 몬스터 Prefab

            [Tooltip("이 몬스터를 한 번에 몇 마리 생성할지 정합니다.")]
            [Range(1, 300)]
            public int count = 1; // 이 조합풀에서 생성할 수량
        }

        [Serializable]
        public sealed class MonsterPool // 웨이브 컨트롤러가 선택할 몬스터 조합 단위
        {
            [Tooltip("이 조합풀이 일반/엘리트/보스 중 어떤 웨이브에서 쓰일지 정합니다.")]
            public SpecialWaveType waveType = SpecialWaveType.Normal; // 이 Pool이 쓰일 웨이브 타입

            [Tooltip("문서나 회의에서 구분하기 위한 조합풀 ID입니다. 예: P01")]
            public string poolId = "P00"; // 문서와 대화할 때 쓰는 짧은 ID

            [Tooltip("Inspector에서 알아보기 쉬운 조합풀 이름입니다.")]
            public string displayName = "새 조합풀"; // Inspector에서 알아보기 쉬운 이름

            [Tooltip("현재 스테이지 위협도가 이 값 이상일 때부터 이 조합풀이 등장할 수 있습니다.")]
            [Range(1, 100)]
            public int minThreatLevel = 1; // 몇 단계 위협도부터 이 조합풀이 등장할 수 있는지

            [Tooltip("같은 조건의 조합풀 중 얼마나 자주 뽑힐지 정합니다. 0이면 뽑히지 않습니다.")]
            [Range(0, 300)]
            public int weight = 100; // 같은 조건의 조합풀 중 뽑힐 확률 가중치

            [Tooltip("이 조합풀이 선택됐을 때 몇 개 게이트에서 동시에 군단을 만들지 정합니다.")]
            [Range(1, 8)]
            public int spawnGroupCount = 1; // 이 조합풀을 선택했을 때 몇 개 게이트에서 군단을 만들지

            [Tooltip("몬스터를 한 줄에 최대 몇 마리까지 세울지 정합니다.")]
            [Range(1, 20)]
            public int frontRowCount = 5; // 한 줄에 몇 마리까지 세울지

            [Tooltip("켜두면 매 스폰마다 기본 물량 후보로 우선 고려합니다.")]
            public bool alwaysIncludeAsBasePool; // true면 매 스폰마다 기본 물량 후보로 우선 고려한다.

            [Tooltip("이 조합풀에 들어갈 몬스터 종류와 수량 목록입니다.")]
            public MonsterPoolEntry[] entries = Array.Empty<MonsterPoolEntry>(); // 실제 몬스터 조합

            public bool IsAvailable(int threatLevel) // 현재 위협도에서 이 풀이 사용 가능한지 확인한다.
            {
                return weight > 0 && threatLevel >= minThreatLevel && HasValidEntry();
            }

            public bool IsRegularWavePool(int threatLevel) // 일반 반복 스폰에서 사용할 수 있는 Pool인지 확인한다.
            {
                return waveType == SpecialWaveType.Normal && IsAvailable(threatLevel);
            }

            public bool IsSpecialWavePool(SpecialWaveType targetWaveType, int threatLevel) // 특수 웨이브 시작 시 사용할 수 있는 Pool인지 확인한다.
            {
                return targetWaveType != SpecialWaveType.Normal && waveType == targetWaveType && IsAvailable(threatLevel);
            }

            public bool HasValidEntry() // Prefab과 수량이 정상인 항목이 하나라도 있는지 확인한다.
            {
                if (entries == null)
                {
                    return false;
                }

                for (int i = 0; i < entries.Length; i++)
                {
                    MonsterPoolEntry entry = entries[i];

                    if (entry != null && entry.prefab != null && entry.count > 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [Serializable]
        public sealed class SpecialWaveSpawnRule // Elite/Boss 같은 특수 웨이브에서 Stage 시작 시 1회 추가 스폰할 몬스터 규칙
        {
            [Tooltip("이 규칙이 어떤 특수 웨이브에서 실행될지 정합니다.")]
            public SpecialWaveType waveType = SpecialWaveType.Elite; // Elite 또는 Boss 타입

            [Tooltip("특수 웨이브에서 추가로 생성할 몬스터 Prefab입니다.")]
            public EnemyController prefab; // 특수 웨이브에서 생성할 몬스터 Prefab

            [Tooltip("특수 웨이브 시작 시 이 몬스터를 몇 마리 생성할지 정합니다.")]
            [Range(1, 300)]
            public int count = 1; // 생성 수량

            [Tooltip("특수 몬스터를 한 줄에 최대 몇 마리까지 세울지 정합니다.")]
            [Range(1, 20)]
            public int frontRowCount = 1; // 한 줄 배치 수

            [Tooltip("특수 몬스터를 몇 개 스폰 게이트에서 나눠 생성할지 정합니다.")]
            [Range(1, 8)]
            public int gateDirectionCount = 1; // 사용할 게이트 방향 수

            public bool IsValidFor(SpecialWaveType targetWaveType) // 현재 특수 웨이브에서 실행 가능한 규칙인지 확인한다.
            {
                return waveType == targetWaveType && prefab != null && count > 0;
            }
        }

        [Header("참조 연결")]
        [Tooltip("실제 몬스터 생성은 이 EnemySpawner에게 요청합니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private EnemySpawner enemySpawner; // 실제 몬스터 생성은 EnemySpawner에게 맡긴다.

        [Tooltip("켜두면 WaveController가 시작될 때 EnemySpawner의 기존 Stage Rules Update를 잠시 꺼서 중복 스폰을 막습니다.")]
        [SerializeField] private bool disableSpawnerStageRulesUpdate = true; // 기존 Stage Rules와 새 WaveController가 동시에 스폰하지 않게 막는다.

        [Header("스테이지 진행 설정")]
        [Tooltip("한 스테이지를 몇 초로 계산할지 정합니다. 현재 기획 기준은 60초입니다.")]
        [Range(10.0f, 180.0f)]
        [SerializeField] private float stageDurationSeconds = 60.0f; // 한 스테이지를 몇 초로 볼지

        [Tooltip("게임 시작 후 첫 몬스터 스폰까지 기다리는 시간입니다.")]
        [Range(0.0f, 30.0f)]
        [SerializeField] private float firstSpawnDelay = 1.0f; // 시작 후 첫 스폰까지 기다릴 시간

        [Tooltip("초반 기본 스폰 간격입니다. 값이 작을수록 몬스터가 자주 나옵니다.")]
        [Range(0.5f, 30.0f)]
        [SerializeField] private float baseSpawnInterval = 8.0f; // 초반 기본 스폰 간격

        [Tooltip("후반에도 이 시간보다 더 빠르게는 스폰되지 않도록 막는 최소 간격입니다.")]
        [Range(0.5f, 30.0f)]
        [SerializeField] private float minSpawnInterval = 4.0f; // 후반에도 이 값보다 빠르게는 줄이지 않는다.

        [Tooltip("스테이지가 1 올라갈 때마다 스폰 간격을 얼마나 줄일지 정합니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float intervalReductionPerStage = 0.08f; // 스테이지가 오를 때마다 스폰 간격을 조금 줄이는 값

        [Header("조합풀 선택 설정")]
        [Tooltip("이 위협도부터 한 번 스폰할 때 조합풀 2개를 섞습니다.")]
        [Range(1, 100)]
        [SerializeField] private int secondPoolThreatLevel = 4; // 이 위협도부터 한 번에 2개 조합풀을 섞는다.

        [Tooltip("이 위협도부터 한 번 스폰할 때 조합풀 3개를 섞습니다.")]
        [Range(1, 100)]
        [SerializeField] private int thirdPoolThreatLevel = 10; // 이 위협도부터 한 번에 3개 조합풀을 섞는다.

        [Tooltip("한 번 스폰할 때 최대 몇 개 조합풀까지 섞을지 정합니다.")]
        [Range(1, 3)]
        [SerializeField] private int maxPoolsPerSpawn = 3; // 한 번 스폰에서 최대 몇 개 조합풀까지 섞을지

        [Tooltip("웨이브 컨트롤러가 자동으로 골라 사용할 몬스터 조합풀 목록입니다.")]
        [SerializeField] private MonsterPool[] monsterPools = Array.Empty<MonsterPool>(); // Inspector에서 관리하는 조합풀 목록

        [Header("게이트 방향 설정")]
        [Tooltip("초반에 한 번 스폰할 때 사용할 방향 수입니다. 8방향 게이트가 모두 있어도 초반에는 이 개수만큼만 랜덤 사용합니다.")]
        [Range(1, 8)]
        [SerializeField] private int baseGateDirectionCount = 2; // 초반에 사용할 기본 방향 수

        [Tooltip("이 위협도부터 중반 방향 수를 사용합니다.")]
        [Range(1, 100)]
        [SerializeField] private int midGateDirectionThreatLevel = 15; // 중반 방향 수로 넘어갈 스테이지 기준

        [Tooltip("중반부터 한 번 스폰할 때 사용할 방향 수입니다.")]
        [Range(1, 8)]
        [SerializeField] private int midGateDirectionCount = 3; // 중반에 사용할 방향 수

        [Tooltip("이 위협도부터 후반 방향 수를 사용합니다.")]
        [Range(1, 100)]
        [SerializeField] private int lateGateDirectionThreatLevel = 25; // 후반 방향 수로 넘어갈 스테이지 기준

        [Tooltip("후반부터 한 번 스폰할 때 사용할 방향 수입니다.")]
        [Range(1, 8)]
        [SerializeField] private int lateGateDirectionCount = 5; // 후반에 사용할 방향 수

        [Tooltip("이 위협도부터 최종 방향 수를 사용합니다.")]
        [Range(1, 100)]
        [SerializeField] private int fullGateDirectionThreatLevel = 35; // 최종 방향 수로 넘어갈 스테이지 기준

        [Tooltip("최종 단계에서 한 번 스폰할 때 사용할 방향 수입니다.")]
        [Range(1, 8)]
        [SerializeField] private int fullGateDirectionCount = 8; // 최종 단계에서 사용할 방향 수

        [Header("특수 웨이브 자동 설정")]
        [Tooltip("켜두면 정해진 규칙에 따라 Elite Wave를 자동 판정합니다.")]
        [SerializeField] private bool enableEliteWave = true; // Elite Wave 자동 판정 사용 여부

        [Tooltip("Elite Wave가 처음 등장할 Stage입니다.")]
        [Range(1, 100)]
        [SerializeField] private int eliteWaveStartStage = 10; // Elite Wave 시작 Stage

        [Tooltip("Elite Wave가 한 번 나온 뒤 몇 Stage 동안 다시 나오지 못하게 할지 정합니다.")]
        [Range(1, 100)]
        [SerializeField] private int eliteWaveInterval = 3; // Elite Wave 재등장 대기 Stage

        [Tooltip("Elite Wave가 등장 가능할 때 처음 판정에 사용하는 기본 확률입니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float eliteBaseChance = 0.2f; // Elite Wave 기본 등장 확률

        [Tooltip("Elite Wave가 안 나올 때마다 다음 판정 확률에 더해지는 값입니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float eliteChanceIncreasePerMiss = 0.08f; // Elite Wave 실패 누적 확률 증가량

        [Tooltip("Elite Wave 등장 확률이 이 값보다 높아지지 않게 막습니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float eliteMaxChance = 0.6f; // Elite Wave 최대 등장 확률

        [Tooltip("켜두면 정해진 규칙에 따라 Boss Wave를 자동 판정합니다.")]
        [SerializeField] private bool enableBossWave = true; // Boss Wave 자동 판정 사용 여부

        [Tooltip("Boss Wave가 처음 등장할 Stage입니다.")]
        [Range(1, 100)]
        [SerializeField] private int bossWaveStartStage = 20; // Boss Wave 시작 Stage

        [Tooltip("Boss Wave가 한 번 나온 뒤 몇 Stage 동안 다시 나오지 못하게 할지 정합니다.")]
        [Range(1, 100)]
        [SerializeField] private int bossWaveInterval = 8; // Boss Wave 재등장 대기 Stage

        [Tooltip("Boss Wave가 등장 가능할 때 처음 판정에 사용하는 기본 확률입니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float bossBaseChance = 0.1f; // Boss Wave 기본 등장 확률

        [Tooltip("Boss Wave가 안 나올 때마다 다음 판정 확률에 더해지는 값입니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float bossChanceIncreasePerMiss = 0.05f; // Boss Wave 실패 누적 확률 증가량

        [Tooltip("Boss Wave 등장 확률이 이 값보다 높아지지 않게 막습니다.")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float bossMaxChance = 0.4f; // Boss Wave 최대 등장 확률

        [Tooltip("Elite/Boss Stage 시작 시 1회 추가 스폰할 몬스터 규칙입니다. 비워두면 타입 판정만 하고 추가 스폰은 하지 않습니다.")]
        [SerializeField] private SpecialWaveSpawnRule[] specialWaveSpawnRules = Array.Empty<SpecialWaveSpawnRule>(); // 특수 웨이브 추가 스폰 규칙

        [Header("보너스 상자 웨이브 설정")]
        [Tooltip("켜두면 특수웨이브 몬스터가 모두 정리된 뒤 보너스 상자 웨이브를 생성합니다.")]
        [SerializeField] private bool enableBonusChestAfterSpecialWave = true; // 특수웨이브 클리어 후 상자 웨이브 사용 여부

        [Tooltip("특수웨이브 클리어 후 호출할 보너스 상자 스포너입니다. 비워두면 씬에서 자동으로 찾습니다.")]
        [SerializeField] private BonusChestWaveSpawner bonusChestWaveSpawner; // 상자 웨이브 생성 담당

        [Header("특수 웨이브 성장 설정")]
        [Tooltip("켜두면 특수 웨이브 몬스터 수가 Stage 진행에 따라 자동으로 증가합니다.")]
        [SerializeField] private bool enableSpecialWaveCountScaling = true; // 특수 웨이브 수량 자동 증가 사용 여부

        [Tooltip("몇 Stage마다 특수 웨이브 수량 배율을 올릴지 정합니다. 10이면 10Stage 단위로 증가합니다.")]
        [Range(1, 50)]
        [SerializeField] private int specialWaveCountStageStep = 10; // 특수 웨이브 수량 배율 기준 Stage 간격

        [Tooltip("Stage Step마다 수량 배율이 얼마나 증가할지 정합니다. 0.5면 10Stage=1배, 20Stage=1.5배, 30Stage=2배입니다.")]
        [Range(0.0f, 2.0f)]
        [SerializeField] private float specialWaveCountIncreasePerStep = 0.5f; // 특수 웨이브 단계별 수량 증가 배율

        [Tooltip("특수 웨이브 수량 배율의 최대값입니다. 너무 무한정 늘어나는 것을 막습니다.")]
        [Range(1.0f, 20.0f)]
        [SerializeField] private float specialWaveMaxCountMultiplier = 5.0f; // 특수 웨이브 수량 최대 배율

        private readonly List<MonsterPool> availablePools = new List<MonsterPool>(16); // 현재 위협도에서 쓸 수 있는 후보 목록
        private readonly List<MonsterPool> selectedPools = new List<MonsterPool>(4); // 이번 스폰에 실제 선택된 조합풀
        private readonly List<EnemySpawner.ExternalSpawnEntry> spawnEntries = new List<EnemySpawner.ExternalSpawnEntry>(32); // EnemySpawner에게 넘길 최종 조합
        private readonly List<EnemyController> trackedSpecialWaveMonsters = new List<EnemyController>(64); // 보상 상자 타이밍을 확인할 특수웨이브 몬스터 목록

        private float elapsedSeconds; // WaveController가 켜진 뒤 지난 시간
        private float spawnTimer; // 다음 스폰까지 남은 시간
        private int currentStage = 1; // 현재 스테이지 번호
        private int lastTriggeredSpecialWaveStage; // 특수 웨이브를 이미 실행한 마지막 Stage
        private int lastEliteWaveStage = -1000; // Elite Wave가 마지막으로 나온 Stage
        private int lastBossWaveStage = -1000; // Boss Wave가 마지막으로 나온 Stage
        private int eliteMissCount; // Elite Wave가 등장 가능했지만 나오지 않은 누적 횟수
        private int bossMissCount; // Boss Wave가 등장 가능했지만 나오지 않은 누적 횟수
        private SpecialWaveType currentSpecialWaveType = SpecialWaveType.Normal; // 현재 Stage의 특수 웨이브 타입
        private bool waitingForSpecialWaveClear; // 특수웨이브 몬스터가 모두 사라지길 기다리는 중인지

        public int CurrentStage // UI나 테스트 코드에서 현재 스테이지를 읽을 수 있게 열어둔다.
        {
            get
            {
                return currentStage;
            }
        }

        public SpecialWaveType CurrentSpecialWave // UI나 테스트 코드에서 현재 특수 웨이브 타입을 읽을 수 있게 열어둔다.
        {
            get
            {
                return currentSpecialWaveType;
            }
        }

        private void Reset() // 컴포넌트를 처음 붙였을 때 기본 조합풀 껍데기를 만들어준다.
        {
            stageDurationSeconds = 60.0f;
            firstSpawnDelay = 1.0f;
            baseSpawnInterval = 8.0f;
            minSpawnInterval = 4.0f;
            intervalReductionPerStage = 0.08f;
            secondPoolThreatLevel = 4;
            thirdPoolThreatLevel = 10;
            maxPoolsPerSpawn = 3;
            baseGateDirectionCount = 2;
            midGateDirectionThreatLevel = 15;
            midGateDirectionCount = 3;
            lateGateDirectionThreatLevel = 25;
            lateGateDirectionCount = 5;
            fullGateDirectionThreatLevel = 35;
            fullGateDirectionCount = 8;
            enableEliteWave = true;
            eliteWaveStartStage = 6;
            eliteWaveInterval = 3;
            eliteBaseChance = 0.2f;
            eliteChanceIncreasePerMiss = 0.08f;
            eliteMaxChance = 0.6f;
            enableBossWave = true;
            bossWaveStartStage = 15;
            bossWaveInterval = 8;
            bossBaseChance = 0.1f;
            bossChanceIncreasePerMiss = 0.05f;
            bossMaxChance = 0.4f;
            enableSpecialWaveCountScaling = true;
            specialWaveCountStageStep = 10;
            specialWaveCountIncreasePerStep = 0.5f;
            specialWaveMaxCountMultiplier = 5.0f;
            enableBonusChestAfterSpecialWave = true;
            disableSpawnerStageRulesUpdate = true;
            monsterPools = CreateDefaultPoolShells();
        }

        private void OnEnable()
        {
            elapsedSeconds = 0.0f;
            currentStage = 1;
            currentSpecialWaveType = GetSpecialWaveType(currentStage);
            lastTriggeredSpecialWaveStage = 0;
            lastEliteWaveStage = -1000;
            lastBossWaveStage = -1000;
            eliteMissCount = 0;
            bossMissCount = 0;
            waitingForSpecialWaveClear = false;
            trackedSpecialWaveMonsters.Clear();
            spawnTimer = firstSpawnDelay;
        }

        private void Start()
        {
            ResolveEnemySpawner(); // Inspector 연결이 없으면 씬에서 EnemySpawner를 찾는다.

            if (enemySpawner != null && disableSpawnerStageRulesUpdate) // 중복 스폰 방지 옵션이 켜져 있다면
            {
                enemySpawner.enabled = false; // EnemySpawner의 Update만 끄고, public 스폰 메서드는 WaveController가 계속 호출한다.
            }
        }

        private void Update()
        {
            ResolveEnemySpawner(); // Play 중에 늦게 생기거나 연결된 경우를 대비한다.

            if (enemySpawner == null) // 스포너가 없으면 아무것도 할 수 없다.
            {
                return;
            }

            ResolveBonusChestWaveSpawner(); // 보너스 상자 스포너 연결이 없다면 씬에서 찾는다.

            elapsedSeconds += Time.deltaTime; // 전체 진행 시간을 증가시킨다.
            int nextStage = CalculateCurrentStage(elapsedSeconds); // 60초 단위로 현재 스테이지를 계산한다.

            if (nextStage != currentStage) // Stage가 바뀌었다면
            {
                currentStage = nextStage; // 현재 Stage 갱신
                currentSpecialWaveType = GetSpecialWaveType(currentStage); // 새 Stage의 특수 웨이브 타입 계산
                TryTriggerSpecialWave(currentSpecialWaveType); // 특수 웨이브면 Stage 시작 시 1회 추가 스폰한다.
            }

            UpdateSpecialWaveBonusChest(); // 특수웨이브 몬스터가 모두 정리됐는지 확인하고 상자를 생성한다.

            spawnTimer -= Time.deltaTime; // 다음 스폰까지 남은 시간을 줄인다.

            if (spawnTimer > 0.0f) // 아직 스폰 시간이 아니라면
            {
                return;
            }

            TrySpawnSelectedPools(); // 현재 스테이지에 맞는 조합풀을 골라 스폰한다.
            spawnTimer = GetCurrentSpawnInterval(currentStage); // 다음 반복 스폰 시간을 설정한다.
        }

        private void ResolveEnemySpawner() // EnemySpawner 참조를 찾는다.
        {
            if (enemySpawner != null)
            {
                return;
            }

            enemySpawner = FindFirstObjectByType<EnemySpawner>(); // 씬에 있는 EnemySpawner를 자동으로 찾는다.
        }

        private void ResolveBonusChestWaveSpawner() // 보너스 상자 스포너 참조를 찾는다.
        {
            if (bonusChestWaveSpawner != null)
            {
                return;
            }

            bonusChestWaveSpawner = FindFirstObjectByType<BonusChestWaveSpawner>(); // 씬에 있으면 자동 연결한다.
        }

        private int CalculateCurrentStage(float seconds) // 누적 시간을 스테이지 번호로 바꾼다.
        {
            int stageIndex = Mathf.FloorToInt(seconds / Mathf.Max(1.0f, stageDurationSeconds)); // 0부터 시작하는 단계 번호
            return Mathf.Max(1, stageIndex + 1); // 사람이 읽기 쉬운 1부터 시작하는 스테이지 번호
        }

        private float GetCurrentSpawnInterval(int stage) // 현재 스테이지의 스폰 간격을 계산한다.
        {
            float reducedInterval = baseSpawnInterval - (stage - 1) * intervalReductionPerStage; // 스테이지가 오를수록 조금씩 빠르게 만든다.
            return Mathf.Max(minSpawnInterval, reducedInterval); // 너무 빠르게 줄어들지 않게 하한선을 둔다.
        }

        private int GetThreatLevel(int stage) // 지금은 스테이지 번호를 위협도로 그대로 사용한다.
        {
            return Mathf.Max(1, stage);
        }

        private int GetPoolCountForThreat(int threatLevel) // 위협도에 따라 한 번에 몇 개 조합풀을 섞을지 정한다.
        {
            int poolCount = 1; // 초반은 하나의 조합풀로 시작한다.

            if (threatLevel >= secondPoolThreatLevel)
            {
                poolCount++;
            }

            if (threatLevel >= thirdPoolThreatLevel)
            {
                poolCount++;
            }

            return Mathf.Clamp(poolCount, 1, maxPoolsPerSpawn);
        }

        private int GetGateDirectionCountForThreat(int threatLevel) // 위협도에 따라 한 번 스폰에 사용할 방향 수를 정한다.
        {
            int directionCount = baseGateDirectionCount; // 초반 기본 방향 수

            if (threatLevel >= midGateDirectionThreatLevel) // 중반 기준을 넘었다면
            {
                directionCount = midGateDirectionCount; // 중반 방향 수 사용
            }

            if (threatLevel >= lateGateDirectionThreatLevel) // 후반 기준을 넘었다면
            {
                directionCount = lateGateDirectionCount; // 후반 방향 수 사용
            }

            if (threatLevel >= fullGateDirectionThreatLevel) // 최종 기준을 넘었다면
            {
                directionCount = fullGateDirectionCount; // 최종 방향 수 사용
            }

            return Mathf.Clamp(directionCount, 1, 8); // 8방향 범위를 넘지 않게 보호한다.
        }

        private SpecialWaveType GetSpecialWaveType(int stage) // 현재 Stage가 Normal/Elite/Boss 중 무엇인지 자동 판정한다.
        {
            if (TryRollSpecialWave(stage, enableBossWave, bossWaveStartStage, bossWaveInterval, bossBaseChance, bossChanceIncreasePerMiss, bossMaxChance, lastBossWaveStage, ref bossMissCount)) // Boss가 가장 높은 우선순위다.
            {
                lastBossWaveStage = stage;
                return SpecialWaveType.Boss;
            }

            if (TryRollSpecialWave(stage, enableEliteWave, eliteWaveStartStage, eliteWaveInterval, eliteBaseChance, eliteChanceIncreasePerMiss, eliteMaxChance, lastEliteWaveStage, ref eliteMissCount)) // Boss가 아니라면 Elite를 확인한다.
            {
                lastEliteWaveStage = stage;
                return SpecialWaveType.Elite;
            }

            return SpecialWaveType.Normal;
        }

        private bool TryRollSpecialWave(
            int stage,
            bool enabled,
            int startStage,
            int cooldownStage,
            float baseChance,
            float chanceIncreasePerMiss,
            float maxChance,
            int lastWaveStage,
            ref int missCount) // 시작 Stage, 확률, 실패 누적, 재등장 대기로 특수 웨이브 여부를 판정한다.
        {
            if (!enabled) // 이 특수 웨이브가 꺼져 있다면
            {
                return false;
            }

            if (stage < startStage) // 시작 Stage 전이라면
            {
                return false;
            }

            int safeCooldown = Mathf.Max(1, cooldownStage); // 재등장 대기 Stage 보호

            if (lastWaveStage > 0 && stage - lastWaveStage < safeCooldown) // 아직 재등장 대기 중이라면
            {
                return false;
            }

            float safeMaxChance = Mathf.Clamp01(maxChance);
            float currentChance = Mathf.Clamp(baseChance + (missCount * chanceIncreasePerMiss), 0.0f, safeMaxChance); // 실패할수록 확률 증가

            if (UnityEngine.Random.value <= currentChance) // 이번 Stage에서 확률에 성공했다면
            {
                missCount = 0;
                return true;
            }

            missCount++; // 등장 가능했지만 실패했으므로 다음 판정 확률을 올린다.
            return false;
        }

        private void TryTriggerSpecialWave(SpecialWaveType waveType) // 특수 웨이브 Stage에 진입했을 때 1회 추가 스폰한다.
        {
            if (waveType == SpecialWaveType.Normal) // 일반 웨이브라면
            {
                return; // 특수 스폰 없음
            }

            if (lastTriggeredSpecialWaveStage == currentStage) // 이미 이 Stage에서 특수 스폰을 실행했다면
            {
                return; // 중복 실행 방지
            }

            lastTriggeredSpecialWaveStage = currentStage; // 이 Stage의 특수 스폰 실행 기록
            trackedSpecialWaveMonsters.Clear(); // 이번 특수웨이브 몬스터만 추적한다.
            bool spawnedFromPool = SpawnSpecialWavePool(waveType); // 먼저 조합풀 기반 특수 스폰을 시도한다.
            bool spawnedSpecialWave = spawnedFromPool;

            if (!spawnedFromPool) // 특수 조합풀이 비어 있다면
            {
                spawnedSpecialWave = SpawnSpecialWaveRules(waveType); // 기존 직접 Prefab 규칙을 fallback으로 사용한다.
            }

            waitingForSpecialWaveClear = spawnedSpecialWave && trackedSpecialWaveMonsters.Count > 0; // 생성된 몬스터가 있을 때만 클리어 보상을 기다린다.
        }

        private bool SpawnSpecialWavePool(SpecialWaveType waveType) // 특수 웨이브 타입에 맞는 조합풀을 1회 추가 스폰한다.
        {
            int threatLevel = GetThreatLevel(currentStage); // 현재 Stage를 기준으로 후보를 고른다.
            MonsterPool specialPool = PickWeightedSpecialPool(waveType, threatLevel); // 현재 타입에 맞는 Pool 하나 선택

            if (specialPool == null) // 사용할 Pool이 없다면
            {
                return false;
            }

            spawnEntries.Clear(); // 기존 임시 목록 비우기
            AddPoolEntriesToSpawnEntries(specialPool, GetSpecialWaveCountMultiplier(currentStage)); // 선택한 특수 Pool의 몬스터 조합에 Stage 배율을 적용해 요청 목록으로 변환

            if (spawnEntries.Count == 0) // Prefab이 연결되지 않았다면
            {
                return false;
            }

            int spawnGroupCount = Mathf.Max(1, specialPool.spawnGroupCount); // 특수 Pool의 군단 수
            int frontRowCount = Mathf.Max(1, specialPool.frontRowCount); // 특수 Pool의 한 줄 배치 수
            int gateDirectionCount = Mathf.Clamp(spawnGroupCount, 1, 8); // 특수 Pool은 군단 수만큼 게이트 방향을 사용한다.

            return enemySpawner.TrySpawnExternalEntriesDistributed(spawnEntries.ToArray(), gateDirectionCount, frontRowCount, trackedSpecialWaveMonsters); // 생성된 몬스터를 기록한다.
        }

        private MonsterPool PickWeightedSpecialPool(SpecialWaveType waveType, int threatLevel) // 특수 웨이브 타입에 맞는 Pool 중 하나를 가중치로 선택한다.
        {
            if (monsterPools == null || monsterPools.Length == 0)
            {
                return null;
            }

            int totalWeight = 0;

            for (int i = 0; i < monsterPools.Length; i++)
            {
                MonsterPool pool = monsterPools[i];

                if (pool != null && pool.IsSpecialWavePool(waveType, threatLevel))
                {
                    totalWeight += Mathf.Max(0, pool.weight);
                }
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < monsterPools.Length; i++)
            {
                MonsterPool pool = monsterPools[i];

                if (pool == null || !pool.IsSpecialWavePool(waveType, threatLevel))
                {
                    continue;
                }

                randomWeight -= Mathf.Max(0, pool.weight);

                if (randomWeight < 0)
                {
                    return pool;
                }
            }

            return null;
        }

        private bool SpawnSpecialWaveRules(SpecialWaveType waveType) // 현재 특수 웨이브 타입에 맞는 몬스터를 추가 스폰한다.
        {
            if (specialWaveSpawnRules == null || specialWaveSpawnRules.Length == 0) // 연결된 규칙이 없다면
            {
                return false; // 타입 판정만 하고 실제 추가 스폰은 하지 않는다.
            }

            bool spawnedAny = false;

            for (int i = 0; i < specialWaveSpawnRules.Length; i++) // 모든 특수 스폰 규칙을 확인한다.
            {
                SpecialWaveSpawnRule rule = specialWaveSpawnRules[i]; // 현재 규칙

                if (rule == null || !rule.IsValidFor(waveType)) // 현재 타입에 맞지 않거나 Prefab이 없다면
                {
                    continue; // 건너뛴다.
                }

                EnemySpawner.ExternalSpawnEntry[] entries =
                {
                    new EnemySpawner.ExternalSpawnEntry(rule.prefab, GetScaledSpecialWaveCount(rule.count, GetSpecialWaveCountMultiplier(currentStage)))
                };

                int gateDirectionCount = Mathf.Clamp(rule.gateDirectionCount, 1, 8); // 특수 스폰에서 사용할 게이트 수
                int frontRowCount = Mathf.Max(1, rule.frontRowCount); // 특수 스폰 줄 배치 수

                if (enemySpawner.TrySpawnExternalEntriesDistributed(entries, gateDirectionCount, frontRowCount, trackedSpecialWaveMonsters)) // 생성된 몬스터를 기록한다.
                {
                    spawnedAny = true;
                }
            }

            return spawnedAny;
        }

        private void UpdateSpecialWaveBonusChest() // 특수웨이브가 정리되면 보너스 상자 웨이브를 생성한다.
        {
            if (!waitingForSpecialWaveClear)
            {
                return;
            }

            for (int i = trackedSpecialWaveMonsters.Count - 1; i >= 0; i--)
            {
                EnemyController monster = trackedSpecialWaveMonsters[i];

                if (monster == null || !monster.gameObject.activeInHierarchy)
                {
                    trackedSpecialWaveMonsters.RemoveAt(i); // 죽었거나 제거된 몬스터는 추적 목록에서 뺀다.
                }
            }

            if (trackedSpecialWaveMonsters.Count > 0)
            {
                return; // 아직 특수웨이브 몬스터가 남아 있다.
            }

            waitingForSpecialWaveClear = false;

            if (!enableBonusChestAfterSpecialWave)
            {
                return;
            }

            ResolveBonusChestWaveSpawner();

            if (bonusChestWaveSpawner != null)
            {
                bonusChestWaveSpawner.SpawnBonusChestWave(); // 특수웨이브 클리어 보상 상자를 생성한다.
            }
        }

        private void TrySpawnSelectedPools() // 조합풀을 고르고 EnemySpawner에게 스폰을 요청한다.
        {
            int threatLevel = GetThreatLevel(currentStage); // 현재 스테이지를 기준으로 위협도를 계산한다.
            int targetPoolCount = GetPoolCountForThreat(threatLevel); // 이번 스폰에서 섞을 조합풀 개수

            BuildAvailablePools(threatLevel); // 현재 사용할 수 있는 후보 목록을 만든다.

            if (availablePools.Count == 0) // 사용할 수 있는 조합풀이 없다면
            {
                return;
            }

            PickPools(targetPoolCount); // 실제 조합풀을 고른다.
            BuildSpawnEntries(); // 선택된 조합풀들을 EnemySpawner 요청 데이터로 바꾼다.

            if (spawnEntries.Count == 0) // 최종 생성 조합이 비어 있다면
            {
                return;
            }

            int gateDirectionCount = GetGateDirectionCountForThreat(threatLevel); // 이번 스폰에서 사용할 방향 수
            int spawnGroupCount = Mathf.Max(GetSelectedSpawnGroupCount(), gateDirectionCount); // 방향 수만큼 군단을 만들 수 있게 보장한다.
            int frontRowCount = GetSelectedFrontRowCount(); // 선택된 풀 중 가장 큰 줄 수를 사용한다.

            enemySpawner.TrySpawnExternalEntries(spawnEntries.ToArray(), spawnGroupCount, frontRowCount, gateDirectionCount); // 실제 생성은 EnemySpawner에게 맡긴다.
        }

        private void BuildAvailablePools(int threatLevel) // 현재 위협도에서 사용할 수 있는 풀 목록을 만든다.
        {
            availablePools.Clear();

            if (monsterPools == null)
            {
                return;
            }

            for (int i = 0; i < monsterPools.Length; i++)
            {
                MonsterPool pool = monsterPools[i];

                if (pool != null && pool.IsRegularWavePool(threatLevel))
                {
                    availablePools.Add(pool);
                }
            }
        }

        private void PickPools(int targetPoolCount) // 기본 물량풀을 우선 잡고, 나머지는 가중치로 뽑는다.
        {
            selectedPools.Clear();

            MonsterPool basePool = PickBasePool();

            if (basePool != null)
            {
                selectedPools.Add(basePool);
            }

            while (selectedPools.Count < targetPoolCount && selectedPools.Count < availablePools.Count)
            {
                MonsterPool pickedPool = PickWeightedPoolExceptSelected();

                if (pickedPool == null)
                {
                    break;
                }

                selectedPools.Add(pickedPool);
            }
        }

        private MonsterPool PickBasePool() // alwaysIncludeAsBasePool이 켜진 후보 중 하나를 우선 선택한다.
        {
            int totalWeight = 0;

            for (int i = 0; i < availablePools.Count; i++)
            {
                MonsterPool pool = availablePools[i];

                if (pool.alwaysIncludeAsBasePool)
                {
                    totalWeight += Mathf.Max(0, pool.weight);
                }
            }

            if (totalWeight <= 0)
            {
                return PickWeightedPoolExceptSelected();
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < availablePools.Count; i++)
            {
                MonsterPool pool = availablePools[i];

                if (!pool.alwaysIncludeAsBasePool)
                {
                    continue;
                }

                randomWeight -= Mathf.Max(0, pool.weight);

                if (randomWeight < 0)
                {
                    return pool;
                }
            }

            return null;
        }

        private MonsterPool PickWeightedPoolExceptSelected() // 이미 뽑은 풀을 제외하고 가중치 랜덤으로 하나 고른다.
        {
            int totalWeight = 0;

            for (int i = 0; i < availablePools.Count; i++)
            {
                MonsterPool pool = availablePools[i];

                if (selectedPools.Contains(pool))
                {
                    continue;
                }

                totalWeight += Mathf.Max(0, pool.weight);
            }

            if (totalWeight <= 0)
            {
                return null;
            }

            int randomWeight = UnityEngine.Random.Range(0, totalWeight);

            for (int i = 0; i < availablePools.Count; i++)
            {
                MonsterPool pool = availablePools[i];

                if (selectedPools.Contains(pool))
                {
                    continue;
                }

                randomWeight -= Mathf.Max(0, pool.weight);

                if (randomWeight < 0)
                {
                    return pool;
                }
            }

            return null;
        }

        private void BuildSpawnEntries() // 선택된 조합풀들을 EnemySpawner.ExternalSpawnEntry 배열 재료로 바꾼다.
        {
            spawnEntries.Clear();

            for (int poolIndex = 0; poolIndex < selectedPools.Count; poolIndex++)
            {
                MonsterPool pool = selectedPools[poolIndex];

                AddPoolEntriesToSpawnEntries(pool);
            }
        }

        private void AddPoolEntriesToSpawnEntries(MonsterPool pool) // Pool 하나의 Entries를 EnemySpawner 요청 목록에 추가한다.
        {
            AddPoolEntriesToSpawnEntries(pool, 1);
        }

        private void AddPoolEntriesToSpawnEntries(MonsterPool pool, float countMultiplier) // Pool 하나의 Entries를 배율 적용 후 EnemySpawner 요청 목록에 추가한다.
        {
            if (pool == null || pool.entries == null)
            {
                return;
            }

            float safeMultiplier = Mathf.Max(1.0f, countMultiplier);

            for (int entryIndex = 0; entryIndex < pool.entries.Length; entryIndex++)
            {
                MonsterPoolEntry entry = pool.entries[entryIndex];

                if (entry == null || entry.prefab == null || entry.count <= 0)
                {
                    continue;
                }

                spawnEntries.Add(new EnemySpawner.ExternalSpawnEntry(entry.prefab, GetScaledSpecialWaveCount(entry.count, safeMultiplier)));
            }
        }

        private float GetSpecialWaveCountMultiplier(int stage) // 10Stage=1배, 20Stage=1.5배, 30Stage=2배 방식으로 특수 웨이브 수량 배율을 계산한다.
        {
            if (!enableSpecialWaveCountScaling)
            {
                return 1.0f;
            }

            int safeStep = Mathf.Max(1, specialWaveCountStageStep);
            int completedStep = Mathf.Max(1, stage / safeStep);
            float multiplier = 1.0f + ((completedStep - 1) * Mathf.Max(0.0f, specialWaveCountIncreasePerStep));

            return Mathf.Clamp(multiplier, 1.0f, Mathf.Max(1.0f, specialWaveMaxCountMultiplier));
        }

        private static int GetScaledSpecialWaveCount(int baseCount, float multiplier) // 소수 배율을 적용한 뒤 실제 스폰 수량으로 반올림한다.
        {
            return Mathf.Max(1, Mathf.RoundToInt(baseCount * Mathf.Max(1.0f, multiplier)));
        }

        private int GetSelectedSpawnGroupCount() // 선택된 조합풀 중 가장 큰 군단 수를 사용한다.
        {
            int result = 1;

            for (int i = 0; i < selectedPools.Count; i++)
            {
                MonsterPool pool = selectedPools[i];

                if (pool != null)
                {
                    result = Mathf.Max(result, pool.spawnGroupCount);
                }
            }

            return result;
        }

        private int GetSelectedFrontRowCount() // 선택된 조합풀 중 가장 큰 한 줄 배치 수를 사용한다.
        {
            int result = 1;

            for (int i = 0; i < selectedPools.Count; i++)
            {
                MonsterPool pool = selectedPools[i];

                if (pool != null)
                {
                    result = Mathf.Max(result, pool.frontRowCount);
                }
            }

            return result;
        }

        private static MonsterPoolEntry CreateEmptyEntry(int count) // Inspector에서 Prefab만 넣으면 되도록 수량이 적힌 빈 칸을 만든다.
        {
            return new MonsterPoolEntry
            {
                count = count
            };
        }

        private MonsterPool[] CreateDefaultPoolShells() // Prefab은 비워두고, 우리가 합의한 8개 조합풀 틀과 추천 수량을 만든다.
        {
            return new[]
            {
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P01",
                    displayName = "기본 물량",
                    minThreatLevel = 1,
                    weight = 120,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    alwaysIncludeAsBasePool = true,
                    entries = new[]
                    {
                        CreateEmptyEntry(12)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P02",
                    displayName = "기본 혼합",
                    minThreatLevel = 2,
                    weight = 100,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    alwaysIncludeAsBasePool = true,
                    entries = new[]
                    {
                        CreateEmptyEntry(10),
                        CreateEmptyEntry(4)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P03",
                    displayName = "원거리 압박",
                    minThreatLevel = 4,
                    weight = 85,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(8),
                        CreateEmptyEntry(8)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P04",
                    displayName = "이동 방해",
                    minThreatLevel = 5,
                    weight = 70,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(10),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P05",
                    displayName = "경로 방해",
                    minThreatLevel = 6,
                    weight = 65,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(10),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P06",
                    displayName = "순간 위협",
                    minThreatLevel = 7,
                    weight = 60,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(8),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P07",
                    displayName = "성장/버프 압박",
                    minThreatLevel = 9,
                    weight = 50,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(10),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Normal,
                    poolId = "P08",
                    displayName = "후반 기믹",
                    minThreatLevel = 12,
                    weight = 35,
                    spawnGroupCount = 1,
                    frontRowCount = 5,
                    entries = new[]
                    {
                        CreateEmptyEntry(10),
                        CreateEmptyEntry(4),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Elite,
                    poolId = "P09",
                    displayName = "엘리트 웨이브",
                    minThreatLevel = 10,
                    weight = 100,
                    spawnGroupCount = 1,
                    frontRowCount = 3,
                    entries = new[]
                    {
                        CreateEmptyEntry(1),
                        CreateEmptyEntry(1)
                    }
                },
                new MonsterPool
                {
                    waveType = SpecialWaveType.Boss,
                    poolId = "P10",
                    displayName = "보스 웨이브",
                    minThreatLevel = 20,
                    weight = 100,
                    spawnGroupCount = 1,
                    frontRowCount = 1,
                    entries = new[]
                    {
                        CreateEmptyEntry(1)
                    }
                }
            };
        }
    }
}
