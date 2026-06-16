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
        public MetaProgressionManager Meta; // 메타 데이터
        public string TargetStageScenePath = "Assets/Scenes/Dev/StageScene_CoreTest.unity"; // 현재 코어 테스트 대상
        [Min(0)] public int HighestReachedWave; // 최고 도달 웨이브
        [Min(0)] public int TemporaryUpgradeBaseCost = 50; // 임시 강화 기본 비용

        [Header("Panels")]
        public GameObject MainMenuPanel; // 메인 메뉴
        public GameObject MapSelectPanel; // 맵 선택
        public GameObject WormSelectPanel; // 지렁이 선택
        public GameObject UpgradePanel; // 업그레이드
        public GameObject SettingsPanel; // 설정

        [Header("Preview")]
        public Image SelectedWormPreview; // 지렁이 프리뷰
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
        [Min(0)] public int DebugDiamondAmount = 500; // 테스트 지급 다이아
        [Min(0)] public int DebugReachedWave = 20; // 테스트 웨이브
        [Min(0)] public int DebugEarnedDiamond; // 테스트 직접 지급값
        public bool DebugRunClear; // 테스트 클리어 여부

        private void Awake() // 초기 참조
        {
            if (Meta == null)
            {
                Meta = FindFirstObjectByType<MetaProgressionManager>(); // 씬 메타 검색
            }
        }

        private void OnEnable() // 표시 시작
        {
            if (Meta != null)
            {
                Meta.DiamondChanged += OnDiamondChanged; // 다이아 갱신
                Meta.SelectedWormChanged += OnSelectedWormChanged; // 지렁이 갱신
                Meta.SelectedMapChanged += OnSelectedMapChanged; // 맵 갱신
            }

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
            RefreshAll(); // 표시 갱신
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
            SelectOrPurchaseWorm(MetaWormIds.Basic); // 기본형
        }

        public void SelectDefenseWorm() // 방어형 선택/구매
        {
            SelectOrPurchaseWorm(MetaWormIds.Defense); // 방어형
        }

        public void SelectArmedWorm() // 무장형 선택/구매
        {
            SelectOrPurchaseWorm(MetaWormIds.Armed); // 무장형
        }

        public void SelectChargeWorm() // 돌격형 선택
        {
            SetStatus("돌격형 지렁이는 업데이트 예정입니다."); // 미구현
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
#if UNITY_EDITOR
            EditorSceneManager.LoadSceneInPlayMode(TargetStageScenePath, new LoadSceneParameters(LoadSceneMode.Single)); // 에디터 테스트
#else
            SceneManager.LoadScene(TargetStageScenePath); // 빌드 로드
#endif
        }

        private void ShowOnly(GameObject target) // 패널 전환
        {
            SetActive(MainMenuPanel, target == MainMenuPanel); // 메인
            SetActive(MapSelectPanel, target == MapSelectPanel); // 맵
            SetActive(WormSelectPanel, target == WormSelectPanel); // 지렁이
            SetActive(UpgradePanel, target == UpgradePanel); // 업그레이드
            SetActive(SettingsPanel, target == SettingsPanel); // 설정
        }

        private void RefreshAll() // 전체 표시 갱신
        {
            if (Meta == null)
            {
                return; // 대상 없음
            }

            SetText(DiamondText, $"다이아 {Meta.Diamond}"); // 다이아
            SetText(HighestWaveText, $"최고 웨이브 {HighestReachedWave}"); // 기록
            SetText(SelectedWormNameText, GetWormDisplayName(Meta.SelectedWormId)); // 이름
            SetText(SelectedWormBonusText, GetWormBonusText(Meta.SelectedWormId)); // 효과
            SetText(UpgradeSummaryText, BuildUpgradeSummary()); // 강화 요약
            RefreshMapPreview(); // 맵 표시

            if (SelectedWormPreview != null)
            {
                SelectedWormPreview.color = GetWormPreviewColor(Meta.SelectedWormId); // 프리뷰 색
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

        private static string CompactEffectText(string effectText) // 요약용 축약
        {
            return effectText == "효과 없음" ? "없음" : effectText; // 0단계 축약
        }

        private static string GetWormDisplayName(string wormId) // 지렁이 이름
        {
            switch (wormId)
            {
                case MetaWormIds.Defense:
                    return "방어형 지렁이";
                case MetaWormIds.Armed:
                    return "무장형 지렁이";
                case MetaWormIds.Charge:
                    return "돌격형 지렁이";
                default:
                    return "기본형 지렁이";
            }
        }

        private static string GetWormBonusText(string wormId) // 지렁이 효과
        {
            switch (wormId)
            {
                case MetaWormIds.Defense:
                    return "넥서스 최대 체력 +15%\n넥서스 분당 회복 +5";
                case MetaWormIds.Armed:
                    return "기본 공격력 +1\n기본 공격속도 +5%";
                case MetaWormIds.Charge:
                    return "회전력 +10%\n충돌힘 +10%";
                default:
                    return "추가 보너스 없음";
            }
        }

        private static Color GetWormPreviewColor(string wormId) // 프리뷰 색
        {
            switch (wormId)
            {
                case MetaWormIds.Defense:
                    return new Color(0.35f, 0.75f, 1f, 1f); // 방어형
                case MetaWormIds.Armed:
                    return new Color(1f, 0.48f, 0.36f, 1f); // 무장형
                case MetaWormIds.Charge:
                    return new Color(1f, 0.86f, 0.28f, 1f); // 돌격형
                default:
                    return new Color(0.48f, 0.9f, 0.56f, 1f); // 기본형
            }
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
                    return "현재 테스트 가능한 기본 맵입니다. 선택 시 CoreTest 스테이지로 이동합니다.";
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
