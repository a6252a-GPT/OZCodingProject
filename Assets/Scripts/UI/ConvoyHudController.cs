using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class ConvoyHudController : MonoBehaviour // 컨보이 HUD 차민규
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
            SetText(HelpText, "1~4 전환  Space 추가  Backspace 제거  R 리셋  Q/E 카메라"); // 도움말

            RefreshButton(RelativeTurnButton, Controller.CurrentControlMode == ConvoyControlMode.RelativeTurn); // 1번 상태
            RefreshButton(WasdDirectionButton, Controller.CurrentControlMode == ConvoyControlMode.WasdDirection); // 2번 상태
            RefreshButton(MousePointerButton, Controller.CurrentControlMode == ConvoyControlMode.MousePointer); // 3번 상태
            RefreshButton(WasdManualForwardButton, Controller.CurrentControlMode == ConvoyControlMode.WasdManualForward); // 4번 상태
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

