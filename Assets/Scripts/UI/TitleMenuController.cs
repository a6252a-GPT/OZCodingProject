using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace TeamProject01.Gameplay
{
    public sealed class TitleMenuController : MonoBehaviour // 타이틀 메뉴
    {
        private const string CurrentCoreTestScenePath = "Assets/Scenes/Dev/CoreTest_StageScene.unity"; // 최신 코어 테스트 씬
        private const string LegacyCoreTestScenePath = "Assets/Scenes/Dev/StageScene_CoreTest.unity"; // 이전 코어 테스트 씬

        private Button runtimeMagicWormButton; // 런타임 마법형 버튼
        private bool mapCardButtonsWired; // 맵 카드 런타임 리스너 중복 방지
        private bool upgradeButtonsWired; // 강화 버튼 런타임 리스너 중복 방지

        [System.Serializable]
        public sealed class TitleMapCardView // 맵 카드 표시 묶음
        {
            public string MapId; // map_01 등
            public Button Button; // 카드 클릭
            public Image PreviewImage; // 썸네일 슬롯
            public Image FrameImage; // 카드 테두리/배경
            public Image EmblemImage; // 문장
            public Image SelectionGlowImage; // 선택 발광
            public GameObject LockedOverlay; // 잠금 딤
            public Text NameText; // 맵 이름
            public Text StateText; // 선택 가능/예정
        }

        [System.Serializable]
        public sealed class TitleUpgradeRowView // 영구 강화 행 묶음
        {
            public MetaUpgradeId UpgradeId; // 실제 강화 ID
            public bool Planned; // 추후 연결 예약칸
            public string PlannedKey; // 예약칸 식별값
            public string PlannedName; // 예약칸 이름
            public Button Button; // 행 클릭
            public Image BackgroundImage; // 행 배경
            public Image IconImage; // 아이콘 슬롯
            public Image SelectionGlowImage; // 선택 발광
            public GameObject PlannedOverlay; // 예정 딤
            public Text NameText; // 행 이름
            public Text StateText; // 상태
            public Image[] LevelPipImages = System.Array.Empty<Image>(); // 5단계 표시
        }

        public MetaProgressionManager Meta; // 메타 데이터
        public string TargetStageScenePath = CurrentCoreTestScenePath; // 현재 코어 테스트 대상
        [HideInInspector][Min(0)] public int HighestReachedWave; // 이전 타이틀 필드 기록
        [Min(0)] public int TemporaryUpgradeBaseCost = 50; // 임시 강화 기본 비용

        [Header("Panels")]
        public GameObject MainMenuPanel; // 메인 메뉴
        public GameObject MapSelectPanel; // 맵 선택
        public GameObject WormSelectPanel; // 지렁이 선택
        public GameObject UpgradePanel; // 업그레이드
        public GameObject SettingsPanel; // 설정

        [Header("Shared UI")]
        public GameObject TitleLogoObject; // 타이틀 로고

        [Header("Preview")]
        public Image SelectedWormPreview; // 지렁이 프리뷰
        public TitleWormPortraitPreview WormPortraitPreview; // 3D 초상화
        public Text SelectedWormNameText; // 지렁이 이름
        public Text SelectedWormBonusText; // 지렁이 보너스

        [Header("Status")]
        public Text DiamondText; // 다이아
        public Text HighestWaveText; // 최고 웨이브
        public Text UpgradeSummaryText; // 업그레이드 요약
        public Text StatusText; // 상태 메시지

        [Header("Map Select")]
        public string SelectedMapId = MetaMapIds.Map1; // 선택 맵
        public Text SelectedMapNameText; // 맵 이름
        public Text SelectedMapStateText; // 맵 상태
        public Text SelectedMapDescriptionText; // 맵 설명
        public Image SelectedMapPreview; // 맵 프리뷰
        public Image SelectedMapEmblemImage; // 선택 맵 문장
        public Text SelectedMapRecommendedLevelText; // 추천 레벨
        public Text SelectedMapPowerText; // 권장 전투력
        public Text SelectedMapEnemyTypeText; // 주요 적 유형
        public Text SelectedMapRuleText; // 특수 규칙
        public Text SelectedMapRewardText; // 보상 요약
        public Text SelectedMapRecordText; // 최고 웨이브 기록
        public Text MapDiamondText; // 맵 선택 상단 다이아
        public Text MapHighestWaveText; // 맵 선택 상단 최고 웨이브
        public Button StartSelectedMapButton; // 선택 버튼
        public Text StartSelectedMapButtonText; // 선택 버튼 텍스트
        public TitleMapCardView[] MapCards = System.Array.Empty<TitleMapCardView>(); // 하단 맵 카드들

        [Header("Upgrade Select")]
        public MetaUpgradeId SelectedUpgradeId = MetaUpgradeId.AttackSpeed; // 선택 강화
        public string SelectedPlannedUpgradeKey; // 선택 예약 강화
        public Text UpgradeDiamondText; // 강화 화면 보유 다이아
        public Text UpgradeHighestWaveText; // 강화 화면 최고 웨이브
        public TitleUpgradeRowView[] UpgradeRows = System.Array.Empty<TitleUpgradeRowView>(); // 강화 행들
        public Image UpgradeDetailIconImage; // 상세 아이콘 슬롯
        public Text UpgradeDetailNameText; // 상세 이름
        public Text UpgradeDetailCurrentLevelText; // 현재 레벨
        public Text UpgradeDetailCurrentEffectText; // 현재 효과
        public Text UpgradeDetailNextLevelText; // 다음 레벨
        public Text UpgradeDetailNextEffectText; // 다음 효과
        public Text UpgradeDetailCostText; // 필요 다이아
        public Text UpgradeDetailStatusText; // 상세 상태
        public Button UpgradeConfirmButton; // 강화 버튼
        public Text UpgradeConfirmButtonText; // 강화 버튼 텍스트

        [Header("Debug")]
        [Min(0)] public int DebugDiamondAmount = 1000; // 테스트 지급 다이아
        [Min(0)] public int DebugReachedWave = 20; // 테스트 웨이브
        [Min(0)] public int DebugEarnedDiamond; // 테스트 직접 지급값
        public bool DebugRunClear; // 테스트 클리어 여부
        private string previewWormId; // 현재 미리보기 지렁이

        private void Awake() // 초기 참조
        {
            NormalizeTargetStageScenePath(); // 이전 씬 경로 보정
            if (Meta == null)
            {
                Meta = FindFirstObjectByType<MetaProgressionManager>(); // 씬 메타 검색
            }

            MigrateLegacyHighestReachedWave(); // 이전 타이틀 필드 기록 보존
            ResolvePreviewReferences(); // 프리뷰 참조
            ResolveTitleLogoReference(); // 로고 참조
            WireMapCardButtons(); // 맵 카드 클릭 연결
            WireUpgradeButtons(); // 강화 행 클릭 연결
        }

        private void OnEnable() // 표시 시작
        {
            if (Meta != null)
            {
                Meta.DiamondChanged += OnDiamondChanged; // 다이아 갱신
                Meta.HighestReachedWaveChanged += OnHighestReachedWaveChanged; // 최고 웨이브 갱신
                Meta.SelectedWormChanged += OnSelectedWormChanged; // 지렁이 갱신
                Meta.SelectedMapChanged += OnSelectedMapChanged; // 맵 갱신
            }

            EnsureWormSelectionButtons(); // 지렁이 버튼 보강
            ResolvePreviewReferences(); // 프리뷰 참조
            ResolveTitleLogoReference(); // 로고 참조
            WireMapCardButtons(); // 씬 오브젝트 리스너 보강
            WireUpgradeButtons(); // 강화 리스너 보강
            TryConsumePendingRunResult(); // 스테이지 결과 보상 반영
            ShowMainMenu(); // 기본 화면
            RefreshAll(); // 즉시 갱신
        }

        private void OnDisable() // 이벤트 해제
        {
            if (Meta != null)
            {
                Meta.DiamondChanged -= OnDiamondChanged; // 다이아 해제
                Meta.HighestReachedWaveChanged -= OnHighestReachedWaveChanged; // 최고 웨이브 해제
                Meta.SelectedWormChanged -= OnSelectedWormChanged; // 지렁이 해제
                Meta.SelectedMapChanged -= OnSelectedMapChanged; // 맵 해제
            }
        }

        public void ShowMainMenu() // 메인 표시
        {
            ShowOnly(MainMenuPanel); // 메인만
            RefreshAll(); // 표시 갱신
        }

        public void ShowMapSelect() // 맵 선택 표시
        {
            ShowOnly(MapSelectPanel); // 맵 선택
            SelectMap(Meta != null ? Meta.SelectedMapId : SelectedMapId); // 현재 선택 맵 표시
            RefreshAll(); // 표시 갱신
        }

        public void ShowWormSelect() // 지렁이 선택 표시
        {
            ShowOnly(WormSelectPanel); // 지렁이 선택
            PreviewWorm(Meta != null ? Meta.SelectedWormId : MetaWormIds.Basic); // 현재 선택 프리뷰
            RefreshAll(); // 표시 갱신
            ApplyWormSelectTextColor(); // 지렁이 선택 글자색
        }

        public void ShowUpgrade() // 업그레이드 표시
        {
            ShowOnly(UpgradePanel); // 업그레이드
            RefreshAll(); // 표시 갱신
        }

        public void ShowSettings() // 설정 표시
        {
            ShowOnly(SettingsPanel); // 설정
            RefreshAll(); // 표시 갱신
        }

        public void SelectBasicWorm() // 기본형 선택
        {
            PreviewWorm(MetaWormIds.Basic); // 먼저 미리보기
            SelectOrPurchaseWorm(MetaWormIds.Basic); // 기본형
        }

        public void SelectAttackWorm() // 공격형 선택/구매
        {
            PreviewWorm(MetaWormIds.Attack); // 먼저 미리보기
            SelectOrPurchaseWorm(MetaWormIds.Attack); // 공격형
        }

        public void SelectMobilityWorm() // 이속형 선택/구매
        {
            PreviewWorm(MetaWormIds.Mobility); // 먼저 미리보기
            SelectOrPurchaseWorm(MetaWormIds.Mobility); // 이속형
        }

        public void SelectSupportWorm() // 지원형 선택/구매
        {
            PreviewWorm(MetaWormIds.Support); // 먼저 미리보기
            SelectOrPurchaseWorm(MetaWormIds.Support); // 지원형
        }

        public void SelectMagicWorm() // 마법형 선택/구매
        {
            PreviewWorm(MetaWormIds.Magic); // 먼저 미리보기
            SelectOrPurchaseWorm(MetaWormIds.Magic); // 마법형
        }

        public void SelectDefenseWorm() // 이전 버튼 호환
        {
            SelectSupportWorm(); // 지원형
        }

        public void SelectArmedWorm() // 이전 버튼 호환
        {
            SelectAttackWorm(); // 공격형
        }

        public void SelectChargeWorm() // 이전 버튼 호환
        {
            SelectMobilityWorm(); // 이속형
        }

        public void StartMap1() // 맵 1 시작
        {
            SelectAndStartMap(MetaMapIds.Map1); // 맵1 시작
        }

        public void StartMap2() // 맵 2
        {
            SelectMap(MetaMapIds.Map2); // 맵2 표시
            SetStatus("맵 2는 업데이트 예정입니다."); // 잠금
        }

        public void StartMap3() // 맵 3
        {
            SelectMap(MetaMapIds.Map3); // 맵3 표시
            SetStatus("맵 3은 업데이트 예정입니다."); // 잠금
        }

        public void StartMap4() // 맵 4
        {
            SelectMap(MetaMapIds.Map4); // 맵4 표시
            SetStatus("맵 4는 업데이트 예정입니다."); // 잠금
        }

        public void StartMap5() // 맵 5
        {
            SelectMap(MetaMapIds.Map5); // 맵5 표시
            SetStatus("맵 5는 업데이트 예정입니다."); // 잠금
        }

        public void SelectMap1() // 맵 1 선택
        {
            SelectMap(MetaMapIds.Map1); // 맵1
        }

        public void SelectMap2() // 맵 2 선택
        {
            SelectMap(MetaMapIds.Map2); // 맵2
        }

        public void SelectMap3() // 맵 3 선택
        {
            SelectMap(MetaMapIds.Map3); // 맵3
        }

        public void SelectMap4() // 맵 4 선택
        {
            SelectMap(MetaMapIds.Map4); // 맵4
        }

        public void SelectMap5() // 맵 5 선택
        {
            SelectMap(MetaMapIds.Map5); // 맵5
        }

        public void SelectMapById(string mapId) // 버튼/카드 공통 선택
        {
            SelectMap(mapId); // ID 기반 선택
        }

        public void StartSelectedMap() // 선택 맵 시작
        {
            SelectAndStartMap(SelectedMapId); // 현재 선택값
        }

        public void UpgradeGoldBonus() // 골드 강화
        {
            Upgrade(MetaUpgradeId.GoldBonus); // 골드
        }

        public void UpgradeDiamondBonus() // 다이아 강화
        {
            Upgrade(MetaUpgradeId.DiamondBonus); // 다이아
        }

        public void UpgradeTurnBonus() // 회전 강화
        {
            Upgrade(MetaUpgradeId.TurnBonus); // 회전
        }

        public void UpgradeCollisionForce() // 충돌 강화
        {
            Upgrade(MetaUpgradeId.CollisionForce); // 충돌
        }

        public void UpgradeBaseAttack() // 공격력 강화
        {
            Upgrade(MetaUpgradeId.BaseAttack); // 공격력
        }

        public void UpgradeAttackSpeed() // 공속 강화
        {
            Upgrade(MetaUpgradeId.AttackSpeed); // 공속
        }

        public void UpgradeNexusMaxHp() // 넥서스 체력 강화
        {
            Upgrade(MetaUpgradeId.NexusMaxHp); // 체력
        }

        public void UpgradeNexusRegen() // 넥서스 회복 강화
        {
            Upgrade(MetaUpgradeId.NexusRegen); // 회복
        }

        public void SelectGoldBonusUpgrade() // 골드 선택
        {
            SelectUpgrade(MetaUpgradeId.GoldBonus); // 골드
        }

        public void SelectDiamondBonusUpgrade() // 다이아 선택
        {
            SelectUpgrade(MetaUpgradeId.DiamondBonus); // 다이아
        }

        public void SelectTurnBonusUpgrade() // 회전 선택
        {
            SelectUpgrade(MetaUpgradeId.TurnBonus); // 회전
        }

        public void SelectCollisionForceUpgrade() // 충돌 선택
        {
            SelectUpgrade(MetaUpgradeId.CollisionForce); // 충돌
        }

        public void SelectBaseAttackUpgrade() // 공격력 선택
        {
            SelectUpgrade(MetaUpgradeId.BaseAttack); // 공격력
        }

        public void SelectAttackSpeedUpgrade() // 공속 선택
        {
            SelectUpgrade(MetaUpgradeId.AttackSpeed); // 공속
        }

        public void SelectNexusMaxHpUpgrade() // 체력 선택
        {
            SelectUpgrade(MetaUpgradeId.NexusMaxHp); // 체력
        }

        public void SelectNexusRegenUpgrade() // 회복 선택
        {
            SelectUpgrade(MetaUpgradeId.NexusRegen); // 회복
        }

        public void ConfirmSelectedUpgrade() // 선택 강화 실행
        {
            if (!string.IsNullOrWhiteSpace(SelectedPlannedUpgradeKey))
            {
                SetStatus($"{ResolvePlannedUpgradeName(SelectedPlannedUpgradeKey)}는 추후 적용 예정입니다."); // 예정
                RefreshUpgradePanel(); // 표시 유지
                return;
            }

            Upgrade(SelectedUpgradeId); // 실제 강화
        }

        public void QuitGame() // 종료
        {
            Application.Quit(); // 빌드 종료
            SetStatus("에디터에서는 종료 버튼이 상태만 표시됩니다."); // 에디터 안내
        }

        public void DebugAddDiamond() // 테스트 다이아 지급
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            Meta.AddDiamond(DebugDiamondAmount); // 지급
            SetStatus($"테스트 다이아 +{DebugDiamondAmount}"); // 상태
            RefreshAll(); // 갱신
        }

        public void DebugResetProgress() // 테스트 진행도 초기화
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            Meta.ResetProgress(); // 초기화
            SetStatus("메타 진행도 초기화 완료"); // 상태
            RefreshAll(); // 갱신
        }

        public void DebugSaveProgress() // 테스트 저장
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            Meta.SaveProgress(); // 저장
            SetStatus("메타 저장 완료"); // 상태
            RefreshAll(); // 갱신
        }

        public void DebugLoadProgress() // 테스트 로드
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            bool loaded = Meta.LoadProgress(); // 로드
            SetStatus(loaded ? "메타 로드 완료" : "저장된 메타 데이터가 없습니다."); // 상태
            RefreshAll(); // 갱신
        }

        public void DebugDeleteSavedProgress() // 테스트 저장 삭제
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            Meta.DeleteSavedProgress(); // 저장 삭제
            SetStatus("저장 데이터 삭제 완료. 현재 런타임 값은 유지됩니다."); // 상태
            RefreshAll(); // 갱신
        }

        public void DebugShowMetaSummary() // 테스트 상태 요약
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            SetStatus(Meta.BuildDebugSummary(TemporaryUpgradeBaseCost)); // 요약
            RefreshAll(); // 갱신
        }

        public void DebugApplyRunReward() // 테스트 웨이브 보상
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            RunResultData result = RunResultData.CreateWithExplicitDiamond(DebugReachedWave, 0f, 0, DebugRunClear, DebugEarnedDiamond, 0, Meta.SelectedWormId); // 직접 입력 보상
            int reward = Meta.ApplyRunResult(result); // 보상 적용
            SetStatus($"임시 웨이브 보상 +{reward} 다이아"); // 상태
            RefreshAll(); // 갱신
        }

        private void TryConsumePendingRunResult() // 스테이지 결과 보상 적용
        {
            if (Meta == null || !RunResultContext.TryConsumePendingResult(out RunResultData result))
            {
                return; // 메타 없음/결과 없음
            }

            int reward = Meta.ApplyRunResult(result); // 다이아 지급/저장
            HighestReachedWave = Meta.HighestReachedWave; // 메타 기록 동기화
            string resultLabel = result.IsClear ? "게임 클리어" : "게임 오버";
            string bonusText = result.ClearDiamondBonus > 0 ? $" / 보너스 +{result.ClearDiamondBonus}" : string.Empty; // 클리어 보너스
            SetStatus($"{resultLabel} / 도달 웨이브 {result.ReachedWave} / 수집 {result.CollectedDiamond}{bonusText} / 다이아 +{reward}"); // 결과 요약
        }

        private void MigrateLegacyHighestReachedWave() // 이전 컨트롤러 필드 기록 이전
        {
            if (Meta == null || HighestReachedWave <= Meta.HighestReachedWave)
            {
                return; // 이전할 기록 없음
            }

            Meta.RegisterReachedWave(HighestReachedWave); // 메타 저장 기록으로 이전
        }

        private void SelectOrPurchaseWorm(string wormId) // 지렁이 선택/구매
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            if (!Meta.IsWormUnlocked(wormId) && !Meta.TryPurchaseWorm(wormId))
            {
                SetStatus($"{GetWormDisplayName(wormId)} 구매에 필요한 다이아가 부족합니다."); // 구매 실패
                RefreshAll(); // 갱신
                return;
            }

            if (Meta.TrySelectWorm(wormId))
            {
                SetStatus($"{GetWormDisplayName(wormId)} 선택 완료"); // 성공
            }

            RefreshAll(); // 갱신
        }

        private void SelectMap(string mapId) // 맵 선택 표시
        {
            SelectedMapId = NormalizeMapId(mapId); // 선택 저장
            if (Meta != null)
            {
                Meta.SelectMap(SelectedMapId); // 메타 동기화/저장
            }

            SetStatus(IsMapPlayable(SelectedMapId) ? $"{GetMapDisplayName(SelectedMapId)} 선택됨" : $"{GetMapDisplayName(SelectedMapId)}는 업데이트 예정입니다."); // 상태
            RefreshAll(); // 갱신
        }

        private void SelectAndStartMap(string mapId) // 맵 선택 후 시작
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            SelectMap(mapId); // 맵 선택
            if (!IsMapPlayable(SelectedMapId))
            {
                SetStatus($"{GetMapDisplayName(SelectedMapId)}는 업데이트 예정입니다."); // 잠금
                return;
            }

            Meta.SelectMap(SelectedMapId); // 맵 확정/저장
            Meta.PushStartBonusToContext(); // 보너스 준비
            LoadStageScene(); // 스테이지 로드
        }

        private void Upgrade(MetaUpgradeId upgradeId) // 업그레이드 처리
        {
            if (Meta == null)
            {
                SetStatus("메타 시스템이 없습니다."); // 누락
                return;
            }

            SelectedUpgradeId = upgradeId; // 상세 선택 동기화
            SelectedPlannedUpgradeKey = string.Empty; // 예약 선택 해제
            string upgradeName = GetTitleUpgradeDisplayName(upgradeId); // 표시명
            int currentLevel = Meta.GetUpgradeLevel(upgradeId); // 현재 단계
            if (Meta.IsUpgradeMaxed(upgradeId))
            {
                SetStatus($"{upgradeName}은 이미 최대 단계입니다."); // 최대
                RefreshAll(); // 갱신
                return;
            }

            int cost = Meta.GetNextUpgradeCost(upgradeId, TemporaryUpgradeBaseCost); // 필요 비용
            if (Meta.Diamond < cost)
            {
                SetStatus($"{upgradeName} 강화 불가: 다이아 {cost} 필요"); // 부족
                RefreshAll(); // 갱신
                return;
            }

            string beforeEffect = MetaProgressionManager.GetUpgradeEffectText(upgradeId, currentLevel); // 현재 효과
            string afterEffect = MetaProgressionManager.GetUpgradeEffectText(upgradeId, currentLevel + 1); // 다음 효과
            if (Meta.TryUpgrade(upgradeId, TemporaryUpgradeBaseCost))
            {
                SetStatus($"{upgradeName} {currentLevel + 1}/{MetaProgressionManager.MaxUpgradeLevel} 강화 완료 ({beforeEffect} -> {afterEffect})"); // 성공
            }
            else
            {
                SetStatus($"{upgradeName} 강화 실패"); // 예외 실패
            }

            RefreshAll(); // 갱신
        }

        private void SelectUpgrade(MetaUpgradeId upgradeId) // 강화 선택
        {
            SelectedUpgradeId = upgradeId; // 선택 저장
            SelectedPlannedUpgradeKey = string.Empty; // 예약 해제
            SetStatus($"{GetTitleUpgradeDisplayName(upgradeId)} 선택됨"); // 상태
            RefreshAll(); // 갱신
        }

        private void SelectPlannedUpgrade(string plannedKey) // 예약 강화 선택
        {
            SelectedPlannedUpgradeKey = string.IsNullOrWhiteSpace(plannedKey) ? "planned_upgrade" : plannedKey; // 키 보정
            SetStatus($"{ResolvePlannedUpgradeName(SelectedPlannedUpgradeKey)}는 추후 적용 예정입니다."); // 상태
            RefreshAll(); // 갱신
        }

        private void LoadStageScene() // 스테이지 로드
        {
            NormalizeTargetStageScenePath(); // 직렬화된 이전 값 보정
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(TargetStageScenePath, new LoadSceneParameters(LoadSceneMode.Single)); // 에디터 테스트
#else
            SceneManager.LoadScene(TargetStageScenePath); // 빌드 로드
#endif
        }

        private void NormalizeTargetStageScenePath() // 최신 코어 테스트 씬 경로 보정
        {
            if (string.IsNullOrWhiteSpace(TargetStageScenePath) || TargetStageScenePath == LegacyCoreTestScenePath)
            {
                TargetStageScenePath = CurrentCoreTestScenePath; // 최신 씬 사용
            }
        }

        private void ShowOnly(GameObject target) // 패널 전환
        {
            ResolveTitleLogoReference(); // 로고 찾기
            SetActive(MainMenuPanel, target == MainMenuPanel); // 메인
            SetActive(MapSelectPanel, target == MapSelectPanel); // 맵
            SetActive(WormSelectPanel, target == WormSelectPanel); // 지렁이
            SetActive(UpgradePanel, target == UpgradePanel); // 업그레이드
            SetActive(SettingsPanel, target == SettingsPanel); // 설정
            SetActive(TitleLogoObject, target != WormSelectPanel && target != MapSelectPanel && target != UpgradePanel); // 전용 화면은 자체 로고 사용
        }

        private void RefreshAll() // 전체 표시 갱신
        {
            if (Meta == null)
            {
                return; // 대상 없음
            }

            EnsureWormSelectionButtons(); // 런타임 버튼 유지
            ResolvePreviewReferences(); // 프리뷰 참조
            string displayWormId = string.IsNullOrWhiteSpace(previewWormId) ? Meta.SelectedWormId : previewWormId; // 표시 대상
            RefreshProgressTexts(); // 보유 정보
            SetText(SelectedWormNameText, GetWormDisplayName(displayWormId)); // 이름
            SetText(SelectedWormBonusText, GetWormBonusText(displayWormId)); // 효과
            SetText(UpgradeSummaryText, BuildUpgradeSummary()); // 강화 요약
            RefreshUpgradePanel(); // 강화 화면
            RefreshMapPreview(); // 맵 표시
            if (WormPortraitPreview != null)
            {
                WormPortraitPreview.PreviewWorm(displayWormId); // 3D 초상화
            }

            if (SelectedWormPreview != null)
            {
                SelectedWormPreview.color = GetWormPreviewColor(displayWormId); // 프리뷰 색
            }

            if (WormSelectPanel != null && WormSelectPanel.activeInHierarchy)
            {
                ApplyWormSelectTextColor(); // 런타임 생성 버튼 보정
            }
        }

        private string BuildUpgradeSummary() // 업그레이드 요약
        {
            if (Meta == null)
            {
                return string.Empty; // 없음
            }

            return BuildUpgradeLine(MetaUpgradeId.GoldBonus) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.DiamondBonus) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.TurnBonus) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.CollisionForce) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.BaseAttack) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.AttackSpeed) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.NexusMaxHp) + "\n"
                + BuildUpgradeLine(MetaUpgradeId.NexusRegen); // 요약
        }

        private string BuildUpgradeLine(MetaUpgradeId upgradeId) // 강화 한 줄 요약
        {
            int level = Meta.GetUpgradeLevel(upgradeId); // 현재 단계
            string name = GetTitleUpgradeDisplayName(upgradeId); // 이름
            string current = CompactEffectText(MetaProgressionManager.GetUpgradeEffectText(upgradeId, level)); // 현재 효과
            if (Meta.IsUpgradeMaxed(upgradeId))
            {
                return $"{name} {level}/{MetaProgressionManager.MaxUpgradeLevel} {current} MAX"; // 최대
            }

            int cost = Meta.GetNextUpgradeCost(upgradeId, TemporaryUpgradeBaseCost); // 비용
            string next = CompactEffectText(MetaProgressionManager.GetUpgradeEffectText(upgradeId, level + 1)); // 다음 효과
            string costText = Meta.Diamond >= cost ? $"비용 {cost}" : $"부족 {cost}"; // 구매 상태
            return $"{name} {level}/{MetaProgressionManager.MaxUpgradeLevel} {current}->{next} {costText}"; // 표시
        }

        private void RefreshMapPreview() // 맵 표시 갱신
        {
            string mapId = Meta != null ? NormalizeMapId(Meta.SelectedMapId) : NormalizeMapId(SelectedMapId); // 보정
            SelectedMapId = mapId; // 로드값 동기화
            SetText(SelectedMapNameText, GetMapDisplayName(mapId)); // 이름
            SetText(SelectedMapStateText, GetMapStateText(mapId)); // 상태
            SetText(SelectedMapDescriptionText, GetMapDescription(mapId)); // 설명
            SetText(SelectedMapRecommendedLevelText, GetMapRecommendedLevelText(mapId)); // 추천 레벨
            SetText(SelectedMapPowerText, GetMapPowerText(mapId)); // 전투력
            SetText(SelectedMapEnemyTypeText, GetMapEnemyTypeText(mapId)); // 적 유형
            SetText(SelectedMapRuleText, GetMapRuleText(mapId)); // 특수 규칙
            SetText(SelectedMapRewardText, GetMapRewardText(mapId)); // 보상
            SetText(SelectedMapRecordText, GetMapRecordText(mapId)); // 기록
            SetText(StartSelectedMapButtonText, IsMapPlayable(mapId) ? "선택" : "예정"); // 시작 버튼

            if (SelectedMapPreview != null)
            {
                ApplyMapImageSlotColor(SelectedMapPreview, mapId); // 실제 사진은 원색 유지
            }

            if (SelectedMapEmblemImage != null)
            {
                SelectedMapEmblemImage.color = GetMapEmblemColor(mapId); // 문장색
            }

            if (StartSelectedMapButton != null)
            {
                StartSelectedMapButton.interactable = IsMapPlayable(mapId); // 잠금 맵 시작 금지
            }

            RefreshMapCardViews(mapId); // 하단 카드 상태 갱신
        }

        private void OnDiamondChanged(int diamond) // 다이아 이벤트
        {
            RefreshAll(); // 갱신
        }

        private void OnHighestReachedWaveChanged(int highestWave) // 최고 웨이브 이벤트
        {
            HighestReachedWave = Mathf.Max(0, highestWave); // 표시 필드 동기화
            RefreshAll(); // 갱신
        }

        private void OnSelectedWormChanged(string wormId) // 지렁이 이벤트
        {
            previewWormId = NormalizeWormId(wormId); // 선택값을 미리보기로
            RefreshAll(); // 갱신
        }

        private void OnSelectedMapChanged(string mapId) // 맵 이벤트
        {
            SelectedMapId = NormalizeMapId(mapId); // 선택 동기화
            RefreshAll(); // 갱신
        }

        private void SetStatus(string message) // 상태 메시지
        {
            SetText(StatusText, message); // 표시
        }

        private void WireMapCardButtons() // 하단 카드 클릭 리스너 연결
        {
            if (mapCardButtonsWired)
            {
                return; // 중복 연결 방지
            }

            mapCardButtonsWired = true; // 1회만
            if (StartSelectedMapButton != null && StartSelectedMapButton.onClick.GetPersistentEventCount() == 0)
            {
                StartSelectedMapButton.onClick.AddListener(StartSelectedMap); // 선택 버튼
            }

            for (int i = 0; MapCards != null && i < MapCards.Length; i++)
            {
                TitleMapCardView card = MapCards[i]; // 카드 묶음
                if (card == null || card.Button == null || string.IsNullOrWhiteSpace(card.MapId))
                {
                    continue; // 연결 불가
                }

                string capturedMapId = NormalizeMapId(card.MapId); // 클로저용 복사
                card.Button.onClick.AddListener(() => SelectMapById(capturedMapId)); // 카드 선택
            }
        }

        private void RefreshProgressTexts() // 보유 정보 표시
        {
            int diamond = Meta != null ? Mathf.Max(0, Meta.Diamond) : 0; // 보유 다이아
            int highestWave = ResolveHighestReachedWave(); // 최고 웨이브
            HighestReachedWave = highestWave; // 레거시 표시 필드 동기화
            string diamondText = diamond.ToString(); // 공통 문구
            string highestWaveText = highestWave.ToString(); // 공통 문구
            SetText(DiamondText, diamondText); // 메인 다이아
            SetText(HighestWaveText, highestWaveText); // 메인 기록
            SetText(MapDiamondText, diamondText); // 맵 선택 다이아
            SetText(MapHighestWaveText, highestWaveText); // 맵 선택 기록
            SetText(UpgradeDiamondText, diamondText); // 강화 화면 다이아
            SetText(UpgradeHighestWaveText, highestWaveText); // 강화 화면 기록
        }

        private void WireUpgradeButtons() // 강화 행/버튼 리스너 연결
        {
            if (upgradeButtonsWired)
            {
                return; // 중복 방지
            }

            upgradeButtonsWired = true; // 1회 연결
            if (UpgradeConfirmButton != null && UpgradeConfirmButton.onClick.GetPersistentEventCount() == 0)
            {
                UpgradeConfirmButton.onClick.AddListener(ConfirmSelectedUpgrade); // 강화 버튼
            }

            for (int i = 0; UpgradeRows != null && i < UpgradeRows.Length; i++)
            {
                TitleUpgradeRowView row = UpgradeRows[i]; // 행
                if (row == null || row.Button == null)
                {
                    continue; // 누락
                }

                MetaUpgradeId capturedId = row.UpgradeId; // enum 복사
                bool capturedPlanned = row.Planned; // 예약 여부
                string capturedKey = row.PlannedKey; // 예약 키
                row.Button.onClick.AddListener(() =>
                {
                    if (capturedPlanned)
                    {
                        SelectPlannedUpgrade(capturedKey); // 예약 선택
                    }
                    else
                    {
                        SelectUpgrade(capturedId); // 실제 강화 선택
                    }
                });
            }
        }

        private void RefreshUpgradePanel() // 강화 화면 표시
        {
            WireUpgradeButtons(); // 런타임 연결 보강
            RefreshUpgradeRows(); // 좌측 목록
            RefreshUpgradeDetail(); // 우측 상세
        }

        private void RefreshUpgradeRows() // 강화 목록 표시
        {
            for (int i = 0; UpgradeRows != null && i < UpgradeRows.Length; i++)
            {
                TitleUpgradeRowView row = UpgradeRows[i]; // 행
                if (row == null)
                {
                    continue; // 누락
                }

                string plannedKey = string.IsNullOrWhiteSpace(row.PlannedKey) ? row.PlannedName : row.PlannedKey; // 예약 키
                bool selected = row.Planned
                    ? !string.IsNullOrWhiteSpace(SelectedPlannedUpgradeKey) && SelectedPlannedUpgradeKey == plannedKey
                    : string.IsNullOrWhiteSpace(SelectedPlannedUpgradeKey) && row.UpgradeId == SelectedUpgradeId; // 선택
                int level = row.Planned || Meta == null ? 0 : Meta.GetUpgradeLevel(row.UpgradeId); // 현재 단계
                bool maxed = !row.Planned && Meta != null && Meta.IsUpgradeMaxed(row.UpgradeId); // 최대

                SetText(row.NameText, row.Planned ? ResolvePlannedUpgradeName(plannedKey, row.PlannedName) : GetTitleUpgradeDisplayName(row.UpgradeId)); // 이름
                SetText(row.StateText, row.Planned ? "예정" : maxed ? "MAX" : $"{level}/{MetaProgressionManager.MaxUpgradeLevel}"); // 상태
                SetActive(row.PlannedOverlay, row.Planned); // 예약 딤
                ApplyUpgradeRowVisual(row, selected, row.Planned, level); // 비주얼
            }
        }

        private void RefreshUpgradeDetail() // 강화 상세 표시
        {
            if (!string.IsNullOrWhiteSpace(SelectedPlannedUpgradeKey))
            {
                RefreshPlannedUpgradeDetail(); // 예약 상세
                return;
            }

            MetaUpgradeId upgradeId = SelectedUpgradeId; // 현재 선택
            string name = GetTitleUpgradeDisplayName(upgradeId); // 이름
            int level = Meta != null ? Meta.GetUpgradeLevel(upgradeId) : 0; // 현재 단계
            bool maxed = Meta != null && Meta.IsUpgradeMaxed(upgradeId); // 최대
            int nextLevel = maxed ? level : Mathf.Min(level + 1, MetaProgressionManager.MaxUpgradeLevel); // 다음 단계
            int cost = Meta != null ? Meta.GetNextUpgradeCost(upgradeId, TemporaryUpgradeBaseCost) : 0; // 비용
            bool affordable = Meta != null && !maxed && Meta.Diamond >= cost; // 구매 가능

            SetText(UpgradeDetailNameText, name); // 이름
            SetText(UpgradeDetailCurrentLevelText, $"{level} / {MetaProgressionManager.MaxUpgradeLevel}"); // 현재 레벨
            SetText(UpgradeDetailCurrentEffectText, MetaProgressionManager.GetUpgradeEffectText(upgradeId, level)); // 현재 효과
            SetText(UpgradeDetailNextLevelText, maxed ? "MAX" : $"{nextLevel} / {MetaProgressionManager.MaxUpgradeLevel}"); // 다음 레벨
            SetText(UpgradeDetailNextEffectText, maxed ? "최대 단계" : MetaProgressionManager.GetUpgradeEffectText(upgradeId, nextLevel)); // 다음 효과
            SetText(UpgradeDetailCostText, maxed ? "-" : cost.ToString()); // 비용
            SetText(UpgradeDetailStatusText, maxed ? "이미 최대 강화입니다." : affordable ? "강화 가능" : "다이아가 부족합니다."); // 상태
            SetText(UpgradeConfirmButtonText, maxed ? "최대" : affordable ? "강화" : "부족"); // 버튼
            if (UpgradeConfirmButton != null)
            {
                UpgradeConfirmButton.interactable = affordable; // 상호작용
                ApplyButtonColor(UpgradeConfirmButton.image, affordable, maxed); // 버튼 색
            }

            ApplyUpgradeIconVisual(UpgradeDetailIconImage, upgradeId, false); // 아이콘
        }

        private void RefreshPlannedUpgradeDetail() // 예약 상세
        {
            string name = ResolvePlannedUpgradeName(SelectedPlannedUpgradeKey); // 이름
            SetText(UpgradeDetailNameText, name); // 이름
            SetText(UpgradeDetailCurrentLevelText, "-"); // 현재
            SetText(UpgradeDetailCurrentEffectText, "추후 적용"); // 현재 효과
            SetText(UpgradeDetailNextLevelText, "예정"); // 다음
            SetText(UpgradeDetailNextEffectText, "강화값 적용 구조 협의 후 연결"); // 다음 효과
            SetText(UpgradeDetailCostText, "-"); // 비용
            SetText(UpgradeDetailStatusText, "기능 연결 예정"); // 상태
            SetText(UpgradeConfirmButtonText, "예정"); // 버튼
            if (UpgradeConfirmButton != null)
            {
                UpgradeConfirmButton.interactable = false; // 잠금
                ApplyButtonColor(UpgradeConfirmButton.image, false, false); // 비활성 색
            }

            ApplyUpgradeIconVisual(UpgradeDetailIconImage, MetaUpgradeId.GoldBonus, true); // 예약 아이콘
        }

        private void RefreshMapCardViews(string selectedMapId) // 하단 맵 카드 표시
        {
            for (int i = 0; MapCards != null && i < MapCards.Length; i++)
            {
                TitleMapCardView card = MapCards[i]; // 카드 묶음
                if (card == null)
                {
                    continue; // 누락 방지
                }

                string mapId = NormalizeMapId(card.MapId); // 카드 맵
                bool selected = mapId == selectedMapId; // 선택 여부
                bool playable = IsMapPlayable(mapId); // 플레이 가능

                SetText(card.NameText, GetMapDisplayName(mapId)); // 이름
                SetText(card.StateText, GetMapStateText(mapId)); // 상태
                SetActive(card.LockedOverlay, !playable); // 잠금 딤
                if (card.PreviewImage != null)
                {
                    ApplyMapImageSlotColor(card.PreviewImage, mapId); // 실제 사진은 원색 유지
                }

                if (card.FrameImage != null)
                {
                    card.FrameImage.color = selected
                        ? new Color(0.16f, 0.72f, 1f, 0.95f)
                        : new Color(0.86f, 0.58f, 0.24f, 0.95f); // 선택/일반 테두리
                }

                if (card.EmblemImage != null)
                {
                    card.EmblemImage.color = GetMapEmblemColor(mapId); // 문장색
                }

                if (card.SelectionGlowImage != null)
                {
                    card.SelectionGlowImage.enabled = selected; // 선택 발광
                }
            }
        }

        private void EnsureWormSelectionButtons() // 지렁이 버튼 보강
        {
            if (WormSelectPanel == null)
            {
                return; // 패널 없음
            }

            SetWormButtonLabel("BasicWormButton", "기본형 지렁이\n시작 무기: 대포"); // 기본형
            SetWormButtonLabel("DefenseWormButton", "지원형 지렁이\n시작 무기: 화염구 / 150 다이아"); // 기존 방어형
            SetWormButtonLabel("ArmedWormButton", "공격형 지렁이\n시작 무기: 미사일 / 200 다이아"); // 기존 무장형
            SetWormButtonLabel("ChargeWormButton", "이속형 지렁이\n시작 무기: 톱날 / 200 다이아"); // 기존 돌격형

            if (runtimeMagicWormButton != null)
            {
                SetWormButtonLabel("MagicWormButton", "마법형 지렁이\n시작 무기: 전기지직 / 250 다이아"); // 라벨 유지
                return; // 이미 있음
            }

            Transform existingMagic = FindWormButtonTransform("MagicWormButton"); // 기존 버튼
            if (existingMagic != null)
            {
                runtimeMagicWormButton = existingMagic.GetComponent<Button>(); // 참조 저장
                SetWormButtonLabel("MagicWormButton", "마법형 지렁이\n시작 무기: 전기지직 / 250 다이아"); // 라벨
                return; // 이미 있음
            }

            Transform source = FindWormButtonTransform("ChargeWormButton") ?? FindWormButtonTransform("ArmedWormButton"); // 복제 기준
            if (source == null)
            {
                return; // 기준 없음
            }

            runtimeMagicWormButton = CreateRuntimeWormButton(source, "MagicWormButton", "마법형 지렁이\n시작 무기: 전기지직 / 250 다이아"); // 마법형
        }

        private void PreviewWorm(string wormId) // 지렁이 미리보기
        {
            previewWormId = NormalizeWormId(wormId); // 표시 ID
            ResolvePreviewReferences(); // 프리뷰 참조
            if (WormPortraitPreview != null)
            {
                WormPortraitPreview.PreviewWorm(previewWormId); // 3D 모델 교체
            }

            SetText(SelectedWormNameText, GetWormDisplayName(previewWormId)); // 이름 즉시 표시
            SetText(SelectedWormBonusText, GetWormBonusText(previewWormId)); // 보너스 즉시 표시
            if (SelectedWormPreview != null)
            {
                SelectedWormPreview.color = GetWormPreviewColor(previewWormId); // 색 프리뷰
            }
        }

        private void ResolvePreviewReferences() // 프리뷰 참조 찾기
        {
            if (WormPortraitPreview == null)
            {
                WormPortraitPreview = FindFirstObjectByType<TitleWormPortraitPreview>(); // 씬 검색
            }
        }

        private void ResolveTitleLogoReference() // 로고 참조 찾기
        {
            if (TitleLogoObject == null)
            {
                TitleLogoObject = GameObject.Find("TitleLogo"); // 씬 검색
            }
        }

        private Button CreateRuntimeWormButton(Transform source, string objectName, string label) // 런타임 버튼 생성
        {
            RectTransform sourceRect = source as RectTransform; // 기준 Rect
            Transform parent = source.parent != null ? source.parent : WormSelectPanel.transform; // 부모
            GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button)); // 버튼 오브젝트
            RectTransform rect = buttonObject.GetComponent<RectTransform>(); // Rect
            rect.SetParent(parent, false); // 같은 그룹
            CopyRectTransform(sourceRect, rect); // 배치 복사
            rect.anchoredPosition += new Vector2(0f, -64f); // 레이아웃 없을 때 아래 배치
            rect.SetSiblingIndex(Mathf.Min(source.GetSiblingIndex() + 1, parent.childCount - 1)); // 순서

            Image sourceImage = source.GetComponent<Image>(); // 기준 이미지
            Image image = buttonObject.GetComponent<Image>(); // 버튼 이미지
            if (sourceImage != null)
            {
                image.sprite = sourceImage.sprite; // 스프라이트
                image.type = sourceImage.type; // 타입
                image.color = sourceImage.color; // 색
                image.material = sourceImage.material; // 재질
                image.raycastTarget = sourceImage.raycastTarget; // 입력
            }

            Button sourceButton = source.GetComponent<Button>(); // 기준 버튼
            Button button = buttonObject.GetComponent<Button>(); // 새 버튼
            button.targetGraphic = image; // 대상 그래픽
            if (sourceButton != null)
            {
                button.transition = sourceButton.transition; // 전환
                button.colors = sourceButton.colors; // 색상
                button.spriteState = sourceButton.spriteState; // 스프라이트 상태
                button.navigation = sourceButton.navigation; // 네비게이션
            }

            Text sourceText = source.GetComponentInChildren<Text>(true); // 기준 텍스트
            if (sourceText != null)
            {
                Text labelText = Instantiate(sourceText.gameObject, buttonObject.transform, false).GetComponent<Text>(); // 텍스트 복제
                labelText.text = label; // 라벨
            }

            button.onClick.AddListener(SelectMagicWorm); // 마법형 선택
            return button; // 결과
        }

        private void SetWormButtonLabel(string objectName, string label) // 버튼 라벨 변경
        {
            Transform button = FindWormButtonTransform(objectName); // 버튼 찾기
            if (button == null)
            {
                return; // 없음
            }

            Text text = button.GetComponentInChildren<Text>(true); // 라벨
            if (text != null)
            {
                text.text = label; // 표시
            }
        }

        private Transform FindWormButtonTransform(string objectName) // 버튼 찾기
        {
            if (WormSelectPanel == null || string.IsNullOrWhiteSpace(objectName))
            {
                return null; // 대상 없음
            }

            Transform[] children = WormSelectPanel.GetComponentsInChildren<Transform>(true); // 하위 검색
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i]; // 후보
                if (child != null && child.name == objectName)
                {
                    return child; // 찾음
                }
            }

            return null; // 없음
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target) // Rect 복사
        {
            if (source == null || target == null)
            {
                return; // 대상 없음
            }

            target.anchorMin = source.anchorMin; // 앵커
            target.anchorMax = source.anchorMax; // 앵커
            target.pivot = source.pivot; // 피벗
            target.sizeDelta = source.sizeDelta; // 크기
            target.anchoredPosition = source.anchoredPosition; // 위치
            target.localRotation = source.localRotation; // 회전
            target.localScale = source.localScale; // 크기
        }

        private static void SetActive(GameObject target, bool active) // 활성화
        {
            if (target != null)
            {
                target.SetActive(active); // 상태 반영
            }
        }

        private static void SetText(Text target, string value) // 텍스트 설정
        {
            if (target != null)
            {
                target.text = value; // 값 반영
            }
        }

        private void ApplyWormSelectTextColor() // 지렁이 선택 글자색 보정
        {
            if (WormSelectPanel == null)
            {
                return; // 대상 없음
            }

            Text[] texts = WormSelectPanel.GetComponentsInChildren<Text>(true); // 선택 화면 텍스트
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].color = Color.white; // 임시 흰색
                }
            }
        }

        private static string CompactEffectText(string effectText) // 요약용 축약
        {
            return effectText == "효과 없음" ? "없음" : effectText; // 0단계 축약
        }

        private static void ApplyUpgradeRowVisual(TitleUpgradeRowView row, bool selected, bool planned, int level) // 강화 행 비주얼
        {
            if (row == null)
            {
                return; // 대상 없음
            }

            if (row.BackgroundImage != null)
            {
                row.BackgroundImage.type = Image.Type.Simple; // 사각 슬롯
                row.BackgroundImage.color = selected
                    ? new Color(0.78f, 0.92f, 1f, 0.96f)
                    : planned ? new Color(0.54f, 0.48f, 0.40f, 0.84f) : new Color(0.84f, 0.70f, 0.50f, 0.94f); // 선택/예정/일반
            }

            if (row.SelectionGlowImage != null)
            {
                row.SelectionGlowImage.enabled = selected; // 선택 테두리
                row.SelectionGlowImage.color = new Color(0.18f, 0.78f, 1f, 0.86f); // 청색 강조
            }

            ApplyUpgradeIconVisual(row.IconImage, row.UpgradeId, planned); // 아이콘
            for (int i = 0; row.LevelPipImages != null && i < row.LevelPipImages.Length; i++)
            {
                Image pip = row.LevelPipImages[i]; // 단계 점
                if (pip == null)
                {
                    continue; // 누락
                }

                pip.type = Image.Type.Simple; // 다이아 슬롯
                pip.raycastTarget = false; // 입력 통과
                bool filled = !planned && i < Mathf.Clamp(level, 0, MetaProgressionManager.MaxUpgradeLevel); // 채움
                pip.color = planned
                    ? new Color(0.38f, 0.34f, 0.29f, 0.72f)
                    : filled ? new Color(1f, 0.68f, 0.16f, 1f) : new Color(0.49f, 0.43f, 0.35f, 0.98f); // 단계색
            }
        }

        private static void ApplyUpgradeIconVisual(Image image, MetaUpgradeId upgradeId, bool planned) // 강화 아이콘 표시
        {
            if (image == null)
            {
                return; // 대상 없음
            }

            image.enabled = true; // 표시
            image.type = Image.Type.Simple; // 아이콘 슬롯
            image.preserveAspect = true; // 실제 아이콘 비율 유지
            image.raycastTarget = false; // 행 버튼 입력 우선
            image.color = planned ? new Color(0.55f, 0.55f, 0.55f, 0.9f) : image.sprite != null ? Color.white : GetUpgradeIconColor(upgradeId); // 스프라이트 교체 대응
        }

        private static void ApplyButtonColor(Image image, bool affordable, bool maxed) // 강화 버튼 색
        {
            if (image == null)
            {
                return; // 대상 없음
            }

            image.type = Image.Type.Simple; // 버튼 슬롯
            image.color = maxed
                ? new Color(0.65f, 0.56f, 0.40f, 0.96f)
                : affordable ? new Color(0.34f, 0.62f, 0.18f, 1f) : new Color(0.38f, 0.33f, 0.27f, 0.88f); // 가능/불가
        }

        private static string GetTitleUpgradeDisplayName(MetaUpgradeId upgradeId) // 타이틀용 강화 이름
        {
            switch (upgradeId)
            {
                case MetaUpgradeId.GoldBonus:
                    return "골드 보너스";
                case MetaUpgradeId.DiamondBonus:
                    return "다이아 보너스";
                case MetaUpgradeId.TurnBonus:
                    return "회전력 증가";
                case MetaUpgradeId.CollisionForce:
                    return "충돌힘 증가";
                case MetaUpgradeId.BaseAttack:
                    return "기본 공격력 증가";
                case MetaUpgradeId.AttackSpeed:
                    return "기본 공격속도 증가";
                case MetaUpgradeId.NexusMaxHp:
                    return "알 최대체력 증가";
                case MetaUpgradeId.NexusRegen:
                    return "알 분당회복";
                default:
                    return MetaProgressionManager.GetUpgradeDisplayName(upgradeId); // 기본값
            }
        }

        private static string ResolvePlannedUpgradeName(string plannedKey) // 예정 강화 이름
        {
            return ResolvePlannedUpgradeName(plannedKey, string.Empty); // 기본
        }

        private static string ResolvePlannedUpgradeName(string plannedKey, string fallbackName) // 예정 강화 이름
        {
            string key = string.IsNullOrWhiteSpace(plannedKey) ? string.Empty : plannedKey.Trim(); // 키 보정
            switch (key)
            {
                case "planned_rejoin_range":
                    return "재결합 범위 증가";
                case "planned_pickup_range":
                    return "픽업 회수 범위 증가";
                default:
                    return string.IsNullOrWhiteSpace(fallbackName) ? "예정 강화" : fallbackName; // 대체명
            }
        }

        private static Color GetUpgradeIconColor(MetaUpgradeId upgradeId) // 임시 아이콘 색
        {
            switch (upgradeId)
            {
                case MetaUpgradeId.GoldBonus:
                    return new Color(1f, 0.74f, 0.12f, 1f);
                case MetaUpgradeId.DiamondBonus:
                    return new Color(0.18f, 0.76f, 1f, 1f);
                case MetaUpgradeId.TurnBonus:
                    return new Color(0.78f, 0.78f, 0.74f, 1f);
                case MetaUpgradeId.CollisionForce:
                    return new Color(0.54f, 0.48f, 0.42f, 1f);
                case MetaUpgradeId.BaseAttack:
                    return new Color(0.90f, 0.90f, 0.86f, 1f);
                case MetaUpgradeId.AttackSpeed:
                    return new Color(0.96f, 0.80f, 0.30f, 1f);
                case MetaUpgradeId.NexusMaxHp:
                    return new Color(0.86f, 0.94f, 1f, 1f);
                case MetaUpgradeId.NexusRegen:
                    return new Color(0.46f, 0.95f, 0.32f, 1f);
                default:
                    return Color.white;
            }
        }

        private static string GetWormDisplayName(string wormId) // 지렁이 이름
        {
            switch (NormalizeWormId(wormId))
            {
                case MetaWormIds.Attack:
                    return "공격형 지렁이";
                case MetaWormIds.Mobility:
                    return "이속형 지렁이";
                case MetaWormIds.Support:
                    return "지원형 지렁이";
                case MetaWormIds.Magic:
                    return "마법형 지렁이";
                default:
                    return "기본형 지렁이";
            }
        }

        private static string GetWormBonusText(string wormId) // 지렁이 효과
        {
            switch (NormalizeWormId(wormId))
            {
                case MetaWormIds.Attack:
                    return "시작 무기: 미사일\n기본 공격력 +1 / 공격속도 +5%";
                case MetaWormIds.Mobility:
                    return "시작 무기: 톱날발사기\n회전력 +10% / 충돌힘 +10%";
                case MetaWormIds.Support:
                    return "시작 무기: 화염구\n넥서스 체력 +15% / 회복 +5";
                case MetaWormIds.Magic:
                    return "시작 무기: 전기지직\n추가 보너스 없음";
                default:
                    return "시작 무기: 대포\n추가 보너스 없음";
            }
        }

        private static Color GetWormPreviewColor(string wormId) // 프리뷰 색
        {
            switch (NormalizeWormId(wormId))
            {
                case MetaWormIds.Attack:
                    return new Color(1f, 0.48f, 0.36f, 1f); // 공격형
                case MetaWormIds.Mobility:
                    return new Color(1f, 0.86f, 0.28f, 1f); // 이속형
                case MetaWormIds.Support:
                    return new Color(0.35f, 0.75f, 1f, 1f); // 지원형
                case MetaWormIds.Magic:
                    return new Color(0.62f, 0.48f, 1f, 1f); // 마법형
                default:
                    return new Color(0.48f, 0.9f, 0.56f, 1f); // 기본형
            }
        }

        private static string NormalizeWormId(string wormId) // 지렁이 ID 보정
        {
            return MetaWormIds.Normalize(wormId); // 공용 보정
        }

        private static string NormalizeMapId(string mapId) // 맵 ID 보정
        {
            return MetaMapIds.Normalize(mapId); // 공용 보정
        }

        private static void ApplyMapImageSlotColor(Image image, string mapId) // 맵 사진 슬롯 색상
        {
            if (image == null)
            {
                return; // 대상 없음
            }

            image.type = Image.Type.Simple; // 실제 맵 사진은 사각 이미지로 표시
            image.preserveAspect = false; // 정해진 슬롯 비율에 맞춰 채움
            image.color = image.sprite != null ? Color.white : GetMapPreviewColor(mapId); // 사진 교체 시 원색 유지
        }

        private static bool IsMapPlayable(string mapId) // 플레이 가능 여부
        {
            return NormalizeMapId(mapId) == MetaMapIds.Map1; // 현재 맵1만 가능
        }

        private static string GetMapDisplayName(string mapId) // 맵 이름
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "숲의 경계";
                case MetaMapIds.Map3:
                    return "바위 고원";
                case MetaMapIds.Map4:
                    return "황혼 늪지";
                case MetaMapIds.Map5:
                    return "빛의 신전";
                default:
                    return "초원 유적";
            }
        }

        private static string GetMapStateText(string mapId) // 맵 상태
        {
            return IsMapPlayable(mapId) ? "선택 가능" : "업데이트 예정"; // 상태
        }

        private static string GetMapDescription(string mapId) // 맵 설명
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "짙은 숲길과 좁은 진입로가 이어지는 경계 지역입니다.\n빠른 적과 매복형 웨이브가 들어갈 예정입니다.";
                case MetaMapIds.Map3:
                    return "무너진 바위 지형이 많은 고원입니다.\n방어선을 흔드는 돌파형 웨이브가 들어갈 예정입니다.";
                case MetaMapIds.Map4:
                    return "해질녘 안개와 늪지가 깔린 위험 지역입니다.\n감속과 원거리 압박 규칙이 들어갈 예정입니다.";
                case MetaMapIds.Map5:
                    return "폐허가 된 빛의 신전입니다.\n후반 고난도 보스 웨이브가 들어갈 예정입니다.";
                default:
                    return "고대의 유적이 남아 있는 드넓은 초원입니다.\n균형 잡힌 지형으로 초보자에게 추천됩니다.";
            }
        }

        private static string GetMapRecommendedLevelText(string mapId) // 추천 레벨
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "추천 레벨 : 6 ~ 12";
                case MetaMapIds.Map3:
                    return "추천 레벨 : 13 ~ 20";
                case MetaMapIds.Map4:
                    return "추천 레벨 : 21 ~ 30";
                case MetaMapIds.Map5:
                    return "추천 레벨 : 31+";
                default:
                    return "추천 레벨 : 1 ~ 5";
            }
        }

        private static string GetMapPowerText(string mapId) // 권장 전투력
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "1,800";
                case MetaMapIds.Map3:
                    return "3,200";
                case MetaMapIds.Map4:
                    return "5,000";
                case MetaMapIds.Map5:
                    return "7,500";
                default:
                    return "1,000";
            }
        }

        private static string GetMapEnemyTypeText(string mapId) // 주요 적 유형
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "기동형";
                case MetaMapIds.Map3:
                    return "돌파형";
                case MetaMapIds.Map4:
                    return "마법형";
                case MetaMapIds.Map5:
                    return "보스형";
                default:
                    return "균형형";
            }
        }

        private static string GetMapRuleText(string mapId) // 특수 규칙
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "숲길 매복";
                case MetaMapIds.Map3:
                    return "낙석 지역";
                case MetaMapIds.Map4:
                    return "늪지 감속";
                case MetaMapIds.Map5:
                    return "정예 강화";
                default:
                    return "없음";
            }
        }

        private string GetMapRecordText(string mapId) // 맵 기록
        {
            return IsMapPlayable(mapId) ? ResolveHighestReachedWave().ToString() : "-"; // 현재는 맵1 기록만 사용
        }

        private int ResolveHighestReachedWave() // 표시용 최고 웨이브
        {
            return Meta != null ? Meta.HighestReachedWave : Mathf.Max(0, HighestReachedWave); // 메타 우선
        }

        private static string GetMapRewardText(string mapId) // 보상 요약
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return "골드 / 다이아 / 숲 문장";
                case MetaMapIds.Map3:
                    return "골드 / 다이아 / 고원 문장";
                case MetaMapIds.Map4:
                    return "골드 / 다이아 / 늪지 문장";
                case MetaMapIds.Map5:
                    return "골드 / 다이아 / 신전 문장";
                default:
                    return "골드 / 다이아 / 초원 문장";
            }
        }

        private static Color GetMapPreviewColor(string mapId) // 맵 프리뷰 색
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return new Color(0.22f, 0.50f, 0.32f, 1f); // 숲
                case MetaMapIds.Map3:
                    return new Color(0.55f, 0.49f, 0.36f, 1f); // 바위
                case MetaMapIds.Map4:
                    return new Color(0.28f, 0.25f, 0.42f, 1f); // 늪지
                case MetaMapIds.Map5:
                    return new Color(0.58f, 0.72f, 0.78f, 1f); // 신전
                default:
                    return new Color(0.36f, 0.62f, 0.32f, 1f); // 초원
            }
        }

        private static Color GetMapEmblemColor(string mapId) // 맵 문장색
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return new Color(0.42f, 0.76f, 0.28f, 1f); // 녹색
                case MetaMapIds.Map3:
                    return new Color(0.82f, 0.34f, 0.22f, 1f); // 적갈색
                case MetaMapIds.Map4:
                    return new Color(0.55f, 0.30f, 0.82f, 1f); // 보라
                case MetaMapIds.Map5:
                    return new Color(1.0f, 0.72f, 0.18f, 1f); // 금색
                default:
                    return new Color(0.58f, 0.78f, 0.22f, 1f); // 초원
            }
        }
    }
}
