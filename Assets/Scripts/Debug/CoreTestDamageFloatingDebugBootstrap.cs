using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public static class CoreTestDamageFloatingDebugBootstrap //전찬우추가 - CoreTest 데미지 버튼 런타임 추가
    {
        private const string PanelName = "SegmentDebugPanel"; //전찬우추가 - 오른쪽 디버그 패널
        private const string ButtonName = "CoreTest_DamageFloatingFontButton"; //전찬우추가 - 버튼 이름

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallOnSceneLoad() //전찬우추가 - 씬 로드 후 설치
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; //전찬우추가 - 중복 방지
            SceneManager.sceneLoaded += OnSceneLoaded; //전찬우추가 - 이후 씬 대응
            TryInstallButton(); //전찬우추가 - 현재 씬 즉시 처리
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) //전찬우추가 - 씬 전환 대응
        {
            TryInstallButton(); //전찬우추가 - 버튼 설치
        }

        private static void TryInstallButton() //전찬우추가 - 버튼 설치 시도
        {
            GameObject panel = GameObject.Find(PanelName); //전찬우추가 - 패널 검색
            if (panel == null)
            {
                return; //전찬우추가 - CoreTest UI 없음
            }

            if (FindChildButton(panel.transform, ButtonName) != null)
            {
                return; //전찬우추가 - 이미 설치됨
            }

            Button template = FindTemplateButton(panel.transform); //전찬우추가 - 복제 기준
            if (template == null)
            {
                return; //전찬우추가 - 기준 버튼 없음
            }

            GameObject instance = Object.Instantiate(template.gameObject, panel.transform); //전찬우추가 - 기존 버튼 복제
            instance.name = ButtonName; //전찬우추가 - 식별명
            instance.transform.SetAsLastSibling(); //전찬우추가 - 맨 아래 배치

            Button button = instance.GetComponent<Button>(); //전찬우추가 - 버튼 참조
            ConfigureButton(button); //전찬우추가 - 폰트 변경 버튼화
            ExpandPanelHeight(panel.transform as RectTransform); //전찬우추가 - 패널 높이 보정
        }

        private static void ConfigureButton(Button button) //전찬우추가 - 버튼 구성
        {
            if (button == null)
            {
                return; //전찬우추가 - 버튼 없음
            }

            CoreTestSegmentAddButton addButton = button.GetComponent<CoreTestSegmentAddButton>(); //전찬우추가 - 복제 잔여
            if (addButton != null)
            {
                Object.Destroy(addButton); //전찬우추가 - 기존 역할 제거
            }

            CoreTestCannonLevelUpButton levelButton = button.GetComponent<CoreTestCannonLevelUpButton>(); //전찬우추가 - 복제 잔여
            if (levelButton != null)
            {
                Object.Destroy(levelButton); //전찬우추가 - 기존 역할 제거
            }

            button.onClick.RemoveAllListeners(); //전찬우추가 - 기존 클릭 제거
            CoreTestDamageFloatingFontButton fontButton = button.GetComponent<CoreTestDamageFloatingFontButton>(); //전찬우추가 - 폰트 버튼
            if (fontButton == null)
            {
                fontButton = button.gameObject.AddComponent<CoreTestDamageFloatingFontButton>(); //전찬우추가 - 컴포넌트 추가
            }

            Text label = button.GetComponentInChildren<Text>(true); //전찬우추가 - 라벨
            if (label != null)
            {
                label.text = "데미지폰트"; //전찬우추가 - 표시명
                label.resizeTextForBestFit = true; //전찬우추가 - 넘침 방지
                label.resizeTextMinSize = 10; //전찬우추가 - 최소 크기
                label.resizeTextMaxSize = 14; //전찬우추가 - 최대 크기
            }

            fontButton.Button = button; //전찬우추가 - 버튼 연결
            fontButton.Label = label; //전찬우추가 - 라벨 연결
        }

        private static Button FindTemplateButton(Transform panel) //전찬우추가 - 복제 기준 찾기
        {
            Button preferred = FindChildButton(panel, "CoreTest_SawLauncherLevelUpButton"); //전찬우추가 - 톱날 버튼 우선
            if (preferred != null)
            {
                return preferred; //전찬우추가 - 기준 반환
            }

            Button[] buttons = panel.GetComponentsInChildren<Button>(true); //전찬우추가 - 하위 버튼
            return buttons.Length > 0 ? buttons[buttons.Length - 1] : null; //전찬우추가 - fallback
        }

        private static Button FindChildButton(Transform root, string objectName) //전찬우추가 - 이름으로 버튼 찾기
        {
            if (root == null)
            {
                return null; //전찬우추가 - 루트 없음
            }

            Button[] buttons = root.GetComponentsInChildren<Button>(true); //전찬우추가 - 하위 버튼
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == objectName)
                {
                    return buttons[i]; //전찬우추가 - 찾음
                }
            }

            return null; //전찬우추가 - 없음
        }

        private static void ExpandPanelHeight(RectTransform panelRect) //전찬우추가 - 패널 높이 보정
        {
            if (panelRect == null)
            {
                return; //전찬우추가 - Rect 없음
            }

            Vector2 size = panelRect.sizeDelta; //전찬우추가 - 현재 크기
            size.y = Mathf.Max(size.y, 476f); //전찬우추가 - 버튼 한 줄 여유
            panelRect.sizeDelta = size; //전찬우추가 - 반영
        }
    }
}
