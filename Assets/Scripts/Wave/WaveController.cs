using System; //?덇굔以 異붽? - 0629 (CurrentStageChanged ?대깽?몄슜 Action<T>)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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

        [Header("?ㅽ뀒?댁? 吏꾪뻾")]
        [SerializeField] private bool autoStart = true; // Play ?쒖옉 ???먮룞?쇰줈 ?⑥씠釉뚮? ?쒖옉?좎??낅땲??

        [Min(1)]
        [SerializeField] private int startStage = 1; // 泥섏쓬 ?쒖옉??Stage 踰덊샇?낅땲??

        [Min(1.0f)]
        [SerializeField] private float stageDurationSeconds = 40.0f; // ?쇰컲 Stage ?섎굹??湲몄씠?낅땲??

        [Header("?ㅽ뀒?댁? ?대━??議곌굔")]
        [SerializeField] private bool advanceWhenAllMonstersCleared = true; // 紐ъ뒪?곌? ?꾨? ?뺣━?섎㈃ ?쒓컙怨??곴??놁씠 ?ㅼ쓬 Stage濡??섍만吏?낅땲??

        [Min(0.0f)]
        [SerializeField] private float clearCheckDelaySeconds = 1.0f; // Stage ?쒖옉 吏곹썑 諛붾줈 ?섏뼱媛???쇱쓣 留됯린 ?꾪븳 ?湲??쒓컙?낅땲??

        [Header("?ㅼ씠???대━??蹂댁긽")]
        [SerializeField] private bool spawnDiamondRewardOnWaveStepClear = true; // ?쇱젙 ?⑥씠釉??대━??蹂댁긽
        [Min(1)]
        [SerializeField] private int diamondRewardWaveStep = 5; // 5?⑥씠釉??⑥쐞
        [SerializeField] private int[] diamondRewardByWaveStep = { 30, 45, 60, 75, 90, 105, 115, 125 }; // 5~40?⑥씠釉?蹂댁긽??

        [Header("?대떦 而댄룷?뚰듃 ?곌껐")]
        [SerializeField] private EnemySpawner enemySpawner; // 湲곗〈 EnemySpawner???몃? ?ㅽ룿 API留??ъ슜?⑸땲??
        [SerializeField] private bool disableSpawnerStageRulesUpdate = true; // 湲곗〈 Stage Rules ?먮룞 ?ㅽ룿怨?以묐났?섏? ?딄쾶 留됱뒿?덈떎.
        [SerializeField] private NormalWaveSpawner normalWaveSpawner; // ?쇰컲 紐ъ뒪???섎웾/議고빀 ?대떦?낅땲??
        [SerializeField] private EliteMixController eliteMixController; // ?섎━??鍮꾩쑉/議고빀 ?대떦?낅땲??
        [SerializeField] private EliteWaveSpawner eliteWaveSpawner; // ?섎━??紐ъ뒪??吏???ㅽ룿 ?대떦?낅땲??
        [SerializeField] private bool enableBossWave = true; // 蹂댁뒪 ?⑥씠釉??ъ슜 ?щ???吏?섏옄??WaveController媛 愿由ы빀?덈떎.
        [SerializeField] private BossWaveController bossWaveController; // 蹂댁뒪 ?깆옣 ?대떦?낅땲??
        [SerializeField] private BonusChestWaveSpawner bonusChestWaveSpawner; // 蹂댁뒪/蹂댁긽 ?곸옄 ?대떦 而댄룷?뚰듃 ?곌껐?⑹엯?덈떎.

        [Header("?뺤옣 ?먮━")]
        [SerializeField] private bool enableSpecialWaveExtension; // 異뷀썑 蹂댁긽/留덈젰 援ъ뒳 ?뱀닔 Stage瑜?遺숈씠湲??꾪븳 ?ㅼ쐞移섏엯?덈떎.
        [SerializeField] private MonoBehaviour specialWaveController; // ?꾩쭅 吏곸젒 ?몄텧?섏? ?딅뒗 ?뺤옣 ?먮━?낅땲??
        [FormerlySerializedAs("goldCollectSpecialWave")]
        [SerializeField] private ManaOrbCollectSpecialWave manaOrbCollectSpecialWave; // 留덈젰 援ъ뒳 ?섏쭛 ?뱀닔 Stage瑜??대떦?섎뒗 而댄룷?뚰듃?낅땲??

        private float elapsedStageSeconds; // ?꾩옱 Stage ?덉뿉???먮Ⅸ ?쒓컙?낅땲??
        private int currentStage; // ?꾩옱 Stage 踰덊샇?낅땲??
        private bool isRunning; // ?⑥씠釉?吏꾪뻾 ?щ??낅땲??
        private bool specialWaveActive; // ?몃? ?뱀닔 ?⑥씠釉뚭? ?쇰컲 ?ㅽ룿???좉? ???ъ슜?섎뒗 媛믪엯?덈떎.
        private bool waitingForBossClearStage; // 蹂댁뒪 泥섏튂濡?醫낅즺?섎뒗 Stage?몄? 湲곕줉?⑸땲??
        private bool waitingForSpecialWaveStage; // ?뱀닔 Stage 蹂댁긽 醫낅즺瑜?湲곕떎由щ뒗吏 湲곕줉?⑸땲??
        private bool skipSpecialWaveCheckOnce; // 蹂대꼫??Stage媛 ?앸궃 ??媛숈? Stage瑜??쇰컲 ?⑥씠釉뚮줈 ?쒖옉?섍린 ?꾪븳 ?뚮옒洹몄엯?덈떎.

        private readonly List<EnemyController> currentStageEnemies = new List<EnemyController>(256); // ?대쾲 Stage?먯꽌 WaveSystem??吏곸젒 ?앹꽦??紐ъ뒪??紐⑸줉?낅땲??
        private int currentStageTargetEnemyCount; // ?대쾲 Stage???섏삱 ?덉젙?댁뿀??紐ъ뒪???섏엯?덈떎.
        private int currentStageDefeatedEnemyCount; // ?대쾲 Stage 紐ъ뒪??以??대? 泥섏튂???섏엯?덈떎.
        private int currentStageTrackingStage; // ?꾩옱 異붿쟻 以묒씤 Stage 踰덊샇?낅땲??

        private bool hasReservedManaOrbCollectSpecialWave;
        private int reservedManaOrbCollectSpecialWaveStage;
        private bool hasResolvedNoManaOrbCollectSpecialWave;
        private int resolvedNoManaOrbCollectSpecialWaveStage;

        public int CurrentStage => currentStage;
        public event Action<int> CurrentStageChanged; //?덇굔以 異붽? - 0629 (?⑥씠釉?蹂寃???SaveData??湲곕줉???⑥씠釉?踰덊샇 ????뚮┝)
        //?덇굔以 異붽? - 0630: ?쇰컲/蹂댁뒪/?뱀닔 ?꾪솚 ??援щ룆?먯뿉寃??뚮┝ (AudioManager BGM ?꾪솚)
        public event Action<WaveRunState> RunStateChanged;
        public float StageDurationSeconds => stageDurationSeconds;
        public float RemainingStageSeconds => Mathf.Max(0.0f, stageDurationSeconds - elapsedStageSeconds);
        public bool IsSpecialWaveActive => enableSpecialWaveExtension && specialWaveActive;
        public bool IsWaitingForBossClearStage => waitingForBossClearStage;
        public bool UsesStageTimer => !waitingForBossClearStage && !waitingForSpecialWaveStage;
        public ManaOrbCollectSpecialWave CurrentManaOrbCollectSpecialWave => IsSpecialWaveActive ? manaOrbCollectSpecialWave : null;
        public int CurrentStageTargetEnemyCount => currentStageTargetEnemyCount;
        public int CurrentStageRemainingEnemyCount
        {
            get
            {
                RefreshCurrentStageEnemyProgress();
                return Mathf.Max(0, currentStageTargetEnemyCount - currentStageDefeatedEnemyCount);
            }
        }

        //?덇굔以 異붽? - 0630: ?꾩옱 ?⑥씠釉?醫낅쪟 議고쉶 ???뱀닔 > 蹂댁뒪 > ?쇰컲 ?곗꽑?쒖쐞
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

        public void BeginCurrentStageEnemyTracking(int stage, int targetEnemyCount)
        {
            RefreshCurrentStageEnemyProgress();
            currentStageTrackingStage = stage;
            currentStageTargetEnemyCount += Mathf.Max(0, targetEnemyCount);
        }

        public void RegisterCurrentStageEnemies(int stage, List<EnemyController> spawnedEnemies)
        {
            if (stage != currentStageTrackingStage || spawnedEnemies == null)
            {
                return;
            }

            for (int i = 0; i < spawnedEnemies.Count; i++)
            {
                EnemyController enemy = spawnedEnemies[i];

                if (enemy == null || currentStageEnemies.Contains(enemy))
                {
                    continue;
                }

                currentStageEnemies.Add(enemy);
            }
        }

        public void CompleteCurrentStageEnemySpawning(int stage)
        {
            if (stage != currentStageTrackingStage)
            {
                return;
            }

            RefreshCurrentStageEnemyProgress();
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

        private void Start() //?덇굔以 ?섏젙 - 0629 (SaveData ?ㅼ씠?꽷룰컯??蹂듭썝 ???⑥씠釉?1遺???쒖옉)
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

            if (waitingForSpecialWaveStage)
            {
                return;
            }

            elapsedStageSeconds += Time.deltaTime;

            if (ShouldAdvanceByClear())
            {
                AdvanceStage();
                return;
            }

            if (elapsedStageSeconds >= stageDurationSeconds)
            {
                if (ShouldDelayNextPriorityStageUntilCurrentMonstersCleared())
                {
                    return;
                }

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
            waitingForSpecialWaveStage = false;
            skipSpecialWaveCheckOnce = false;
            ClearReservedManaOrbCollectSpecialWave();
            ClearResolvedNoManaOrbCollectSpecialWave();
            isRunning = true;
            ResetEnemyTracking();

            SegmentDpsDebugMeter.ResetRun(); // DPS 誘명꽣 ?꾩껜 ?꾩쟻 珥덇린??
            CurrentStageChanged?.Invoke(currentStage); //?덇굔以 異붽? - 0629 (?⑥씠釉??쒖옉 ??援щ룆?먯뿉寃??꾩옱 Stage ?뚮┝)
            StartCurrentStage();
        }

        private void StartCurrentStage()
        {
            ResolveReferences();
            SegmentDpsDebugMeter.BeginWave(currentStage); // ?대쾲 ?⑥씠釉?湲곕줉 珥덇린??

            eliteWaveSpawner?.StopCurrentStage(); // ?댁쟾 Stage??吏???섎━??猷⑦떞???⑥븘 ?덉쑝硫??뺣━
            BeginCurrentStageEnemyTracking(currentStage, 0);
            specialWaveActive = false;
            waitingForSpecialWaveStage = false;

            if (!skipSpecialWaveCheckOnce && TryStartManaOrbCollectSpecialWave())
            {
                NotifyRunStateChanged(); //?덇굔以 異붽? - 0630: ?뱀닔 ?⑥씠釉??쒖옉 ??EventStage BGM
                return;
            }

            skipSpecialWaveCheckOnce = false;

            bool bossSpawned = false;

            if (enableBossWave && bossWaveController != null)
            {
                bossSpawned = bossWaveController.BeginStage(currentStage);
                waitingForBossClearStage = bossSpawned && bossWaveController.ShouldEndStageOnBossClear;
            }

            if (bossSpawned && bossWaveController != null && bossWaveController.ShouldPauseNormalSpawn)
            {
                NotifyRunStateChanged(); //?덇굔以 異붽? - 0630: 蹂댁뒪 ?⑥씠釉??쒖옉 ??Boss BGM
                return;
            }

            if (normalWaveSpawner == null)
            {
                NotifyRunStateChanged(); //?덇굔以 異붽? - 0630: ?쇰컲 ?ㅽ룿 ?놁쓬 ???꾩옱 ?곹깭留??뚮┝
                return;
            }

            int totalSpawnCount = normalWaveSpawner.CalculateTotalSpawnCount(currentStage);
            int requestedEliteSpawnCount = eliteMixController != null
                ? eliteMixController.CalculateEliteCount(currentStage, totalSpawnCount)
                : 0;

            EliteMixController.EliteStagePlan elitePlan = eliteMixController != null
                ? eliteMixController.BuildStagePlan(currentStage, requestedEliteSpawnCount)
                : default;

            int normalSpawnCount = Mathf.Max(0, totalSpawnCount);
            normalWaveSpawner.BeginStage(currentStage, stageDurationSeconds, normalSpawnCount, this);
            eliteWaveSpawner?.BeginStage(currentStage, elitePlan, normalWaveSpawner.ResolveDifficultyForStage(currentStage), normalWaveSpawner, this);
            NotifyRunStateChanged(); //?덇굔以 異붽? - 0630: ?쇰컲 ?⑥씠釉??쒖옉 ??Stage BGM
        }

        //?덇굔以 異붽? - 0630: RunStateChanged 援щ룆?먯뿉寃?CurrentState ?꾨떖
        private void NotifyRunStateChanged()
        {
            RunStateChanged?.Invoke(CurrentState);
        }

        private bool TryStartManaOrbCollectSpecialWave()
        {
            if (!enableSpecialWaveExtension || manaOrbCollectSpecialWave == null)
            {
                return false;
            }

            bool isBossStage = IsBossStage(currentStage);

            if (TryConsumeResolvedNoManaOrbCollectSpecialWave(currentStage))
            {
                return false;
            }

            if (!TryConsumeReservedManaOrbCollectSpecialWave(currentStage)
                && !TryReserveManaOrbCollectSpecialWave(currentStage, isBossStage))
            {
                return false;
            }

            ClearReservedManaOrbCollectSpecialWave();
            manaOrbCollectSpecialWave.BeginReservedStage(HandleManaOrbCollectSpecialWaveFinished);
            specialWaveActive = true;
            waitingForSpecialWaveStage = true;
            return true;
        }

        private bool ShouldDelayNextPriorityStageUntilCurrentMonstersCleared()
        {
            if (!HasAnyBlockingMonsterForPriorityStage())
            {
                return false;
            }

            int nextStage = currentStage + 1;
            bool nextStageIsBoss = IsBossStage(nextStage);

            if (!skipSpecialWaveCheckOnce && CanCheckManaOrbCollectSpecialWave(nextStage, nextStageIsBoss))
            {
                if (TryReserveManaOrbCollectSpecialWave(nextStage, nextStageIsBoss))
                {
                    return true;
                }

                MarkResolvedNoManaOrbCollectSpecialWave(nextStage);
            }

            return nextStageIsBoss;
        }

        private bool HasAnyBlockingMonsterForPriorityStage()
        {
            return CurrentStageRemainingEnemyCount > 0 || EnemyController.ActiveCount > 0;
        }

        private bool IsBossStage(int stage)
        {
            return enableBossWave && bossWaveController != null && bossWaveController.IsBossStage(stage);
        }

        private bool CanCheckManaOrbCollectSpecialWave(int stage, bool isBossStage)
        {
            return enableSpecialWaveExtension
                && manaOrbCollectSpecialWave != null
                && manaOrbCollectSpecialWave.CanCheckStageForReservation(stage, isBossStage);
        }

        private bool TryReserveManaOrbCollectSpecialWave(int stage, bool isBossStage)
        {
            if (!enableSpecialWaveExtension || manaOrbCollectSpecialWave == null)
            {
                return false;
            }

            if (hasReservedManaOrbCollectSpecialWave)
            {
                if (reservedManaOrbCollectSpecialWaveStage == stage)
                {
                    return true;
                }

                if (stage > reservedManaOrbCollectSpecialWaveStage)
                {
                    ClearReservedManaOrbCollectSpecialWave();
                }
                else
                {
                    return false;
                }
            }

            if (!manaOrbCollectSpecialWave.TryReserveStage(stage, isBossStage))
            {
                return false;
            }

            hasReservedManaOrbCollectSpecialWave = true;
            reservedManaOrbCollectSpecialWaveStage = stage;
            return true;
        }

        private void MarkResolvedNoManaOrbCollectSpecialWave(int stage)
        {
            hasResolvedNoManaOrbCollectSpecialWave = true;
            resolvedNoManaOrbCollectSpecialWaveStage = stage;
        }

        private bool TryConsumeReservedManaOrbCollectSpecialWave(int stage)
        {
            if (!hasReservedManaOrbCollectSpecialWave || reservedManaOrbCollectSpecialWaveStage != stage)
            {
                return false;
            }

            ClearReservedManaOrbCollectSpecialWave();
            return true;
        }

        private bool TryConsumeResolvedNoManaOrbCollectSpecialWave(int stage)
        {
            if (!hasResolvedNoManaOrbCollectSpecialWave)
            {
                return false;
            }

            if (resolvedNoManaOrbCollectSpecialWaveStage == stage)
            {
                ClearResolvedNoManaOrbCollectSpecialWave();
                return true;
            }

            if (stage > resolvedNoManaOrbCollectSpecialWaveStage)
            {
                ClearResolvedNoManaOrbCollectSpecialWave();
            }

            return false;
        }

        private void ClearReservedManaOrbCollectSpecialWave()
        {
            hasReservedManaOrbCollectSpecialWave = false;
            reservedManaOrbCollectSpecialWaveStage = 0;
        }

        private void ClearResolvedNoManaOrbCollectSpecialWave()
        {
            hasResolvedNoManaOrbCollectSpecialWave = false;
            resolvedNoManaOrbCollectSpecialWaveStage = 0;
        }

        private void HandleManaOrbCollectSpecialWaveFinished()
        {
            if (!isRunning || !waitingForSpecialWaveStage)
            {
                return;
            }

            specialWaveActive = false;
            waitingForSpecialWaveStage = false;
            elapsedStageSeconds = 0.0f;
            skipSpecialWaveCheckOnce = true;
            StartCurrentStage();
        }

        private void RefreshCurrentStageEnemyProgress()
        {
            for (int i = currentStageEnemies.Count - 1; i >= 0; i--)
            {
                EnemyController enemy = currentStageEnemies[i];

                if (enemy != null && !enemy.IsDead)
                {
                    continue;
                }

                currentStageEnemies.RemoveAt(i);
                currentStageDefeatedEnemyCount = Mathf.Min(currentStageTargetEnemyCount, currentStageDefeatedEnemyCount + 1);
            }
        }

        private void ResetEnemyTracking()
        {
            currentStageTrackingStage = 0;
            currentStageTargetEnemyCount = 0;
            currentStageDefeatedEnemyCount = 0;
            currentStageEnemies.Clear();
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

            return CurrentStageRemainingEnemyCount <= 0;
        }

        private void AdvanceStage()
        {
            int completedStage = currentStage; // 蹂댁긽 湲곗? ?⑥씠釉?
            TrySpawnWaveClearDiamondReward(completedStage); // ?대━???ㅼ씠???쎌뾽
            elapsedStageSeconds = 0.0f;
            skipSpecialWaveCheckOnce = false;
            currentStage++;
            CurrentStageChanged?.Invoke(currentStage); //?덇굔以 異붽? - 0629 (?ㅼ쓬 ?⑥씠釉?吏꾩엯 ??援щ룆?먯뿉寃?Stage ?뚮┝)
            StartCurrentStage();
        }

        private void TrySpawnWaveClearDiamondReward(int completedStage) // ?⑥씠釉??대━???ㅼ씠??
        {
            if (!spawnDiamondRewardOnWaveStepClear || completedStage <= 0)
            {
                return; // 蹂댁긽 鍮꾪솢??
            }

            if (completedStage % Mathf.Max(1, diamondRewardWaveStep) != 0)
            {
                return; // 蹂댁긽 ?⑥씠釉??꾨떂
            }

            int reward = ResolveWaveClearDiamondReward(completedStage);
            if (reward <= 0)
            {
                return; // 吏湲??놁쓬
            }

            RewardDropService.SpawnDiamond(reward, ResolveWaveRewardDropPosition()); // ?붾뱶 ?쎌뾽 ?앹꽦
        }

        private int ResolveWaveClearDiamondReward(int completedStage) // 蹂댁긽??議고쉶
        {
            if (diamondRewardByWaveStep == null || diamondRewardByWaveStep.Length == 0)
            {
                return 0; // ?뚯씠釉??놁쓬
            }

            int stepIndex = Mathf.Max(0, completedStage / Mathf.Max(1, diamondRewardWaveStep) - 1);
            int clampedIndex = Mathf.Min(stepIndex, diamondRewardByWaveStep.Length - 1); // 40 ?댄썑??留덉?留?媛?諛섎났
            return Mathf.Max(0, diamondRewardByWaveStep[clampedIndex]); // ?덉쟾 蹂댁젙
        }

        private Vector3 ResolveWaveRewardDropPosition() // ?⑥씠釉?蹂댁긽 ?꾩튂
        {
            NexusController nexus = NexusController.Active;
            return nexus != null ? nexus.transform.position : transform.position; // ?μ꽌??洹쇱쿂 ?곗꽑
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

            if (eliteWaveSpawner == null)
            {
                eliteWaveSpawner = ResolveWaveSiblingOrSceneComponent<EliteWaveSpawner>();
            }

            if (eliteWaveSpawner == null)
            {
                eliteWaveSpawner = gameObject.AddComponent<EliteWaveSpawner>(); // 湲곗〈 ???섏젙 ?놁씠 ?고??꾩뿉??遺꾨━ ?ㅽ룷?덈? 蹂닿컯
            }

            if (bossWaveController == null)
            {
                bossWaveController = ResolveWaveSiblingOrSceneComponent<BossWaveController>();
            }

            if (bonusChestWaveSpawner == null)
            {
                bonusChestWaveSpawner = ResolveWaveSiblingOrSceneComponent<BonusChestWaveSpawner>();
            }

            if (manaOrbCollectSpecialWave == null)
            {
                manaOrbCollectSpecialWave = ResolveWaveSiblingOrSceneComponent<ManaOrbCollectSpecialWave>();
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
