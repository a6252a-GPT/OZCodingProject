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
        private const string StageScenePath = "Assets/Scenes/StageScene.unity"; // 기본 스테이지 씬 경로 //안건준 추가 - 0628
        private const string StageSceneName = "StageScene"; // 빌드 로드용 씬 이름 //안건준 수정 - 0628
        private const string LegacyCoreTestScenePath = "Assets/Scenes/Dev/CoreTest_StageScene.unity"; // 이전 코어 테스트 씬 (경로 보정용)
        private const string LegacyCoreTestScenePathOld = "Assets/Scenes/Dev/StageScene_CoreTest.unity"; // 더 이전 코어 테스트 씬 (경로 보정용)

        private Button runtimeMagicWormButton; // 런타임 마법형 버튼

        public MetaProgressionManager Meta; // 메타 데이터
        public string TargetStageScenePath = StageScenePath; // 게임 시작 시 로드할 스테이지 씬 //안건준 수정 - 0628
        [Min(0)] public int HighestReachedWave; // 최고 도달 웨이브
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

            ResolvePreviewReferences(); // 프리뷰 참조
            ResolveTitleLogoReference(); // 로고 참조
        }

        private void OnEnable() // 표시 시작
        {
            if (Meta != null)
            {
                Meta.DiamondChanged += OnDiamondChanged; // 다이아 갱신
                Meta.SelectedWormChanged += OnSelectedWormChanged; // 지렁이 갱신
                Meta.SelectedMapChanged += OnSelectedMapChanged; // 맵 갱신
            }

            EnsureWormSelectionButtons(); // 지렁이 버튼 보강
            ResolvePreviewReferences(); // 프리뷰 참조
            ResolveTitleLogoReference(); // 로고 참조
            ShowMainMenu(); // 기본 화면
            RefreshAll(); // 즉시 갱신
        }

        private void OnDisable() // 이벤트 해제
        {
            if (Meta != null)
            {
                Meta.DiamondChanged -= OnDiamondChanged; // 다이아 해제
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

            RunResultData result = RunResultData.Create(DebugReachedWave, 0f, 0, DebugRunClear, DebugEarnedDiamond, 0, Meta.SelectedWormId); // 결과
            int reward = Meta.ApplyRunResult(result); // 보상 적용
            SetStatus($"임시 웨이브 보상 +{reward} 다이아"); // 상태
            RefreshAll(); // 갱신
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

            string upgradeName = MetaProgressionManager.GetUpgradeDisplayName(upgradeId); // 표시명
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

        private void LoadStageScene() // 스테이지 로드
        {
            NormalizeTargetStageScenePath(); // 직렬화된 이전 값 보정
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(TargetStageScenePath, new LoadSceneParameters(LoadSceneMode.Single)); // 에디터에서 StageScene 경로로 로드 //안건준 수정 - 0628
#else
            SceneManager.LoadScene(StageSceneName); // 빌드에서 StageScene 이름으로 로드 //안건준 수정 - 0628
#endif
        }

        private void NormalizeTargetStageScenePath() // 스테이지 씬 경로 보정
        {
            if (string.IsNullOrWhiteSpace(TargetStageScenePath)
                || TargetStageScenePath == LegacyCoreTestScenePath
                || TargetStageScenePath == LegacyCoreTestScenePathOld)
            {
                TargetStageScenePath = StageScenePath; // 이전 테스트 씬 경로를 StageScene으로 통일 //안건준 수정 - 0628
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
            SetActive(TitleLogoObject, target != WormSelectPanel); // 지렁이 선택에서는 숨김
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
            SetText(DiamondText, Meta.Diamond.ToString()); // 다이아
            SetText(HighestWaveText, HighestReachedWave.ToString()); // 기록
            SetText(SelectedWormNameText, GetWormDisplayName(displayWormId)); // 이름
            SetText(SelectedWormBonusText, GetWormBonusText(displayWormId)); // 효과
            SetText(UpgradeSummaryText, BuildUpgradeSummary()); // 강화 요약
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
            string name = MetaProgressionManager.GetUpgradeDisplayName(upgradeId); // 이름
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

            if (SelectedMapPreview != null)
            {
                SelectedMapPreview.color = GetMapPreviewColor(mapId); // 색상
            }
        }

        private void OnDiamondChanged(int diamond) // 다이아 이벤트
        {
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
            return string.IsNullOrWhiteSpace(mapId) ? MetaMapIds.Map1 : mapId; // 기본 맵
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
                    return "맵 2";
                case MetaMapIds.Map3:
                    return "맵 3";
                default:
                    return "맵 1";
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
                    return "새로운 지형과 웨이브가 들어갈 예정입니다.";
                case MetaMapIds.Map3:
                    return "후반 난이도용 맵으로 업데이트 예정입니다.";
                default:
                    return "현재 테스트 가능한 기본 맵입니다. 선택 시 StageScene으로 이동합니다."; //안건준 수정 - 0628
            }
        }

        private static Color GetMapPreviewColor(string mapId) // 맵 프리뷰 색
        {
            switch (NormalizeMapId(mapId))
            {
                case MetaMapIds.Map2:
                    return new Color(0.25f, 0.30f, 0.36f, 1f); // 예정
                case MetaMapIds.Map3:
                    return new Color(0.30f, 0.26f, 0.36f, 1f); // 예정
                default:
                    return new Color(0.22f, 0.42f, 0.30f, 1f); // 맵1
            }
        }
    }
}
