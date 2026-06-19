using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class ConvoyHudController : MonoBehaviour // 컨보이 HUD
    {
        public ConvoyController Controller; // 표시 대상
        public Text SpeedText; // 속도 텍스트
        public Text TurnText; // 회전 텍스트
        public Text SegmentText; // 길이 텍스트
        public Text LevelText; // 레벨 텍스트
        public Text ExperienceText; // 경험치 텍스트
        public Text GoldText; // 골드 텍스트
        public Text ModeText; // 모드 텍스트
        public Text HelpText; // 도움말 텍스트
        public Button RelativeTurnButton; // 1번 버튼
        public Button WasdDirectionButton; // 2번 버튼
        public Button MousePointerButton; // 3번 버튼
        public Button WasdManualForwardButton; // 4번 버튼
        public Button AutoOrbitButton; // 자동궤도 버튼
        public Color ButtonNormalColor = new Color(0.1f, 0.12f, 0.13f, 0.88f); // 기본 배경
        public Color ButtonSelectedColor = new Color(0.72f, 0.94f, 1f, 0.95f); // 선택 배경
        public Color ButtonNormalTextColor = new Color(0.93f, 0.96f, 0.96f, 1f); // 기본 글자
        public Color ButtonSelectedTextColor = new Color(0.08f, 0.1f, 0.11f, 1f); // 선택 글자

        private bool buttonsWired; // 이벤트 연결 여부

        private void Awake() // 초기 연결
        {
            WireButtons(); // 버튼 연결
        }

        private void OnEnable() // 활성화 갱신
        {
            WireButtons(); // 버튼 보장
            RefreshAll(); // 즉시 표시
        }

        private void Update() // 표시 갱신
        {
            RefreshAll(); // HUD 값 갱신
        }

        private void WireButtons() // 버튼 이벤트
        {
            if (buttonsWired)
            {
                return; // 이미 연결
            }

            BindModeButton(RelativeTurnButton, ConvoyControlMode.RelativeTurn); // 1번
            BindModeButton(WasdDirectionButton, ConvoyControlMode.WasdDirection); // 2번
            BindModeButton(MousePointerButton, ConvoyControlMode.MousePointer); // 3번
            BindModeButton(WasdManualForwardButton, ConvoyControlMode.WasdManualForward); // 4번
            ResolveAutoOrbitButton(); // 씬 배치 버튼 연결
            BindAutoOrbitButton(AutoOrbitButton); // 자동궤도
            buttonsWired = true; // 연결 완료
        }

        private void BindModeButton(Button button, ConvoyControlMode mode) // 버튼 바인딩
        {
            if (button == null)
            {
                return; // 버튼 없음
            }

            button.onClick.RemoveAllListeners(); // 중복 제거
            button.onClick.AddListener(() =>
            {
                if (Controller != null)
                {
                    Controller.SetControlMode(mode); // 모드 변경
                    RefreshAll(); // 선택 표시
                }
            });
        }

        private void BindAutoOrbitButton(Button button) // 자동궤도 바인딩
        {
            if (button == null)
            {
                return; // 버튼 없음
            }

            button.onClick.RemoveAllListeners(); // 중복 제거
            button.onClick.AddListener(() =>
            {
                if (Controller != null)
                {
                    Controller.ToggleAutoOrbit(); // 자동궤도 토글
                    RefreshAll(); // 선택 표시
                }
            });
        }

        private void RefreshAll() // 전체 갱신
        {
            if (Controller == null)
            {
                return; // 대상 없음
            }

            SetText(SpeedText, $"속도 {Controller.CurrentSpeed:0.00}"); // 속도
            SetText(TurnText, $"회전 {Controller.CurrentTurnVelocity:0} 도/초"); // 회전
            SetText(SegmentText, $"세그먼트 {Controller.SegmentCount}"); // 길이
            CoreStatData stats = CoreStatProvider.GetCurrentOrDefault(); // 코어 표시값
            SetText(LevelText, $"레벨 {stats.Level}"); // 레벨
            SetText(ExperienceText, $"경험치 {stats.CurrentExperience}/{stats.ExperienceToNextLevel}"); // 경험치
            SetText(GoldText, $"골드 {stats.Gold}"); // 골드
            SetText(ModeText, Controller.CurrentControlModeLabel); // 모드명
            SetText(HelpText, "1~4 전환\nSpace 세그먼트 추가\nBackspace 세그먼트 제거\nR 리셋\nQ/E 카메라각도조절"); // 도움말

            bool autoOrbit = Controller.IsAutoOrbitActive; // 자동궤도 상태
            RefreshButton(RelativeTurnButton, !autoOrbit && Controller.CurrentControlMode == ConvoyControlMode.RelativeTurn); // 1번 상태
            RefreshButton(WasdDirectionButton, !autoOrbit && Controller.CurrentControlMode == ConvoyControlMode.WasdDirection); // 2번 상태
            RefreshButton(MousePointerButton, !autoOrbit && Controller.CurrentControlMode == ConvoyControlMode.MousePointer); // 3번 상태
            RefreshButton(WasdManualForwardButton, !autoOrbit && Controller.CurrentControlMode == ConvoyControlMode.WasdManualForward); // 4번 상태
            RefreshButton(AutoOrbitButton, autoOrbit); // 자동궤도 상태
        }

        private void ResolveAutoOrbitButton() // 자동궤도 버튼 연결
        {
            if (AutoOrbitButton != null)
            {
                SetButtonLabel(AutoOrbitButton, "자동궤도"); // 라벨 보장
                return; // 이미 있음
            }

            Transform searchRoot = GetButtonSearchRoot(); // 버튼 검색 루트
            AutoOrbitButton = FindButtonByName(searchRoot, "AutoOrbitButton"); // 씬 버튼 찾기

            if (AutoOrbitButton != null)
            {
                SetButtonLabel(AutoOrbitButton, "자동궤도"); // 라벨 보장
            }
        }

        private Transform GetButtonSearchRoot() // 버튼 검색 루트 반환
        {
            if (WasdManualForwardButton != null && WasdManualForwardButton.transform.parent != null)
            {
                return WasdManualForwardButton.transform.parent; // 버튼 패널
            }

            if (MousePointerButton != null && MousePointerButton.transform.parent != null)
            {
                return MousePointerButton.transform.parent; // 대체 패널
            }

            if (RelativeTurnButton != null && RelativeTurnButton.transform.parent != null)
            {
                return RelativeTurnButton.transform.parent; // 대체 패널
            }

            return transform; // HUD 루트 fallback
        }

        private static Button FindButtonByName(Transform root, string objectName) // 이름으로 버튼 찾기
        {
            if (root == null)
            {
                return null; // 검색 불가
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true); // 하위 버튼
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                {
                    return buttons[i]; // 대상 버튼
                }
            }

            return null; // 없음
        }

        private static void PositionAutoOrbitButton(Button template, Button button) // 버튼 위치 보정
        {
            if (template == null || button == null)
            {
                return; // 대상 없음
            }

            RectTransform templateRect = template.GetComponent<RectTransform>(); // 기준
            RectTransform rect = button.GetComponent<RectTransform>(); // 대상
            if (templateRect == null || rect == null)
            {
                return; // Rect 없음
            }

            float width = templateRect.rect.width > 1f ? templateRect.rect.width : 110f; // 폭
            rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(width + 8f, 0f); // 오른쪽 배치
        }

        private static void SetButtonLabel(Button button, string label) // 버튼 글자
        {
            if (button == null)
            {
                return; // 버튼 없음
            }

            Text text = button.GetComponentInChildren<Text>(true); // 라벨
            if (text != null)
            {
                text.text = label; // 표시
            }
        }

        private void RefreshButton(Button button, bool selected) // 버튼 표시
        {
            if (button == null)
            {
                return; // 버튼 없음
            }

            Image image = button.targetGraphic as Image; // 배경 이미지
            if (image != null)
            {
                image.color = selected ? ButtonSelectedColor : ButtonNormalColor; // 배경색
            }

            Text text = button.GetComponentInChildren<Text>(); // 라벨
            if (text != null)
            {
                text.color = selected ? ButtonSelectedTextColor : ButtonNormalTextColor; // 글자색
            }
        }

        private static void SetText(Text target, string value) // 텍스트 설정
        {
            if (target != null)
            {
                target.text = value; // 값 반영
            }
        }
    }
}

