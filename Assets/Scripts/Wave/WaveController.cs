using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class WaveController : MonoBehaviour
    {
        public enum WaveRunState
        {
            Normal,
            Boss,
            Special
        }

        [Header("스테이지 진행")]
        [SerializeField] private bool autoStart = true; // Play 시작 시 자동으로 웨이브를 시작할지입니다.

        [Min(1)]
        [SerializeField] private int startStage = 1; // 처음 시작할 Stage 번호입니다.

        [Min(1.0f)]
        [SerializeField] private float stageDurationSeconds = 40.0f; // 일반 Stage 하나의 길이입니다.

        [Header("스테이지 클리어 조건")]
        [SerializeField] private bool advanceWhenAllMonstersCleared = true; // 몬스터가 전부 정리되면 시간과 상관없이 다음 Stage로 넘길지입니다.

        [Min(0.0f)]
        [SerializeField] private float clearCheckDelaySeconds = 1.0f; // Stage 시작 직후 바로 넘어가는 일을 막기 위한 대기 시간입니다.

        [Header("담당 컴포넌트 연결")]
        [SerializeField] private EnemySpawner enemySpawner; // 기존 EnemySpawner의 외부 스폰 API만 사용합니다.
        [SerializeField] private bool disableSpawnerStageRulesUpdate = true; // 기존 Stage Rules 자동 스폰과 중복되지 않게 막습니다.
        [SerializeField] private NormalWaveSpawner normalWaveSpawner; // 일반 몬스터 수량/조합 담당입니다.
        [SerializeField] private EliteMixController eliteMixController; // 엘리트 비율/조합 담당입니다.
        [SerializeField] private BossWaveController bossWaveController; // 보스 등장 담당입니다.
        [SerializeField] private BonusChestWaveSpawner bonusChestWaveSpawner; // 보스/보상 상자 담당 컴포넌트 연결용입니다.

        [Header("확장 자리")]
        [SerializeField] private bool enableSpecialWaveExtension; // 추후 보상/골드 특수 Stage를 붙이기 위한 스위치입니다.
        [SerializeField] private MonoBehaviour specialWaveController; // 아직 직접 호출하지 않는 확장 자리입니다.

        private float elapsedStageSeconds; // 현재 Stage 안에서 흐른 시간입니다.
        private int currentStage; // 현재 Stage 번호입니다.
        private bool isRunning; // 웨이브 진행 여부입니다.
        private bool specialWaveActive; // 외부 특수 웨이브가 일반 스폰을 잠글 때 사용하는 값입니다.
        private bool waitingForBossClearStage; // 보스 처치로 종료되는 Stage인지 기록합니다.

        public int CurrentStage => currentStage;
        public float StageDurationSeconds => stageDurationSeconds;
        public float RemainingStageSeconds => Mathf.Max(0.0f, stageDurationSeconds - elapsedStageSeconds);
        public bool IsSpecialWaveActive => enableSpecialWaveExtension && specialWaveActive;
        public bool IsWaitingForBossClearStage => waitingForBossClearStage;
        public bool UsesStageTimer => !waitingForBossClearStage;

        public WaveRunState CurrentState
        {
            get
            {
                if (IsSpecialWaveActive)
                {
                    return WaveRunState.Special;
                }

                if (waitingForBossClearStage || bossWaveController != null && bossWaveController.HasActiveBoss)
                {
                    return WaveRunState.Boss;
                }

                return WaveRunState.Normal;
            }
        }

        public void SetSpecialWaveActive(bool active)
        {
            specialWaveActive = enableSpecialWaveExtension && active;
        }

        private void Reset()
        {
            autoStart = true;
            startStage = 1;
            stageDurationSeconds = 40.0f;
            disableSpawnerStageRulesUpdate = true;
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            if (autoStart)
            {
                StartWave();
            }
        }

        private void Update()
        {
            if (!isRunning)
            {
                return;
            }

            if (waitingForBossClearStage)
            {
                if (bossWaveController == null || !bossWaveController.HasActiveBoss)
                {
                    waitingForBossClearStage = false;
                    AdvanceStage();
                }

                return;
            }

            elapsedStageSeconds += Time.deltaTime;

            if (ShouldAdvanceByClear() || elapsedStageSeconds >= stageDurationSeconds)
            {
                AdvanceStage();
            }
        }

        public void StartWave()
        {
            ResolveReferences();
            DisableLegacySpawnerUpdateIfNeeded();

            currentStage = Mathf.Max(1, startStage);
            elapsedStageSeconds = 0.0f;
            specialWaveActive = false;
            waitingForBossClearStage = false;
            isRunning = true;

            StartCurrentStage();
        }

        private void StartCurrentStage()
        {
            ResolveReferences();

            if (IsSpecialWaveActive)
            {
                return;
            }

            bool bossSpawned = false;

            if (bossWaveController != null)
            {
                bossSpawned = bossWaveController.BeginStage(currentStage);
                waitingForBossClearStage = bossSpawned && bossWaveController.ShouldEndStageOnBossClear;
            }

            if (bossSpawned && bossWaveController != null && bossWaveController.ShouldPauseNormalSpawn)
            {
                return;
            }

            if (normalWaveSpawner == null)
            {
                return;
            }

            int totalSpawnCount = normalWaveSpawner.CalculateTotalSpawnCount(currentStage);
            int requestedEliteSpawnCount = eliteMixController != null
                ? eliteMixController.CalculateEliteCount(currentStage, totalSpawnCount)
                : 0;

            EliteMixController.EliteStagePlan elitePlan = eliteMixController != null
                ? eliteMixController.BuildStagePlan(currentStage, requestedEliteSpawnCount)
                : default;

            int normalSpawnCount = Mathf.Max(0, totalSpawnCount - elitePlan.TotalCount);
            normalWaveSpawner.BeginStage(currentStage, stageDurationSeconds, normalSpawnCount, elitePlan);
        }

        private bool ShouldAdvanceByClear()
        {
            if (!advanceWhenAllMonstersCleared || IsSpecialWaveActive)
            {
                return false;
            }

            if (elapsedStageSeconds < clearCheckDelaySeconds)
            {
                return false;
            }

            return EnemyController.ActiveCount <= 0;
        }

        private void AdvanceStage()
        {
            elapsedStageSeconds = 0.0f;
            currentStage++;
            StartCurrentStage();
        }

        private void ResolveReferences()
        {
            if (enemySpawner == null)
            {
                enemySpawner = FindFirstObjectByType<EnemySpawner>();
            }

            if (normalWaveSpawner == null)
            {
                normalWaveSpawner = ResolveWaveSiblingOrSceneComponent<NormalWaveSpawner>();
            }

            if (eliteMixController == null)
            {
                eliteMixController = ResolveWaveSiblingOrSceneComponent<EliteMixController>();
            }

            if (bossWaveController == null)
            {
                bossWaveController = ResolveWaveSiblingOrSceneComponent<BossWaveController>();
            }

            if (bonusChestWaveSpawner == null)
            {
                bonusChestWaveSpawner = ResolveWaveSiblingOrSceneComponent<BonusChestWaveSpawner>();
            }
        }

        private T ResolveWaveSiblingOrSceneComponent<T>() where T : Component
        {
            T component = GetComponent<T>();

            if (component != null)
            {
                return component;
            }

            if (transform.parent != null)
            {
                component = transform.parent.GetComponentInChildren<T>(true);

                if (component != null)
                {
                    return component;
                }
            }

            return FindFirstObjectByType<T>();
        }

        private void DisableLegacySpawnerUpdateIfNeeded()
        {
            if (!disableSpawnerStageRulesUpdate || enemySpawner == null)
            {
                return;
            }

            enemySpawner.enabled = false;
        }
    }
}
