using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class WaveRuntimeDebugOverlay : MonoBehaviour // 임시 웨이브 상태 표시창
    {
        [Header("Stage Display")]
        [SerializeField, Min(1f)] private float stageDurationSeconds = 60f; // 1스테이지를 몇 초로 볼지
        [SerializeField, Min(1)] private int firstStageNumber = 1; // 표시를 시작할 스테이지 번호
        [SerializeField, Min(1)] private int loopStageCount = 10; // 무한 루프 한 바퀴에 들어가는 스테이지 수

        [Header("Popup")]
        [SerializeField, Min(0.1f)] private float popupDuration = 2.2f; // Stage 시작 팝업 유지 시간

        private GameObject canvasObject; // 런타임에 만든 Canvas 오브젝트
        private Text headerText; // 상단 제목 텍스트
        private Text bodyText; // 상태 상세 텍스트
        private CanvasGroup popupGroup; // 팝업 표시/숨김 제어
        private Text popupText; // Stage 시작 팝업 텍스트

        private float elapsedTime; // 오버레이가 켜진 뒤 흐른 시간
        private float popupTimer; // 팝업이 남은 시간
        private int currentStage; // 현재 표시 중인 Stage 번호

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInEditorPlayMode() // 씬 저장 없이 에디터 PlayMode에서만 자동 생성
        {
            if (!Application.isPlaying) // PlayMode가 아니라면
            {
                return; // 아무것도 만들지 않는다.
            }

            Scene activeScene = SceneManager.GetActiveScene(); // 현재 열린 씬 정보
            if (!activeScene.IsValid() || !string.Equals(activeScene.name, "SegmentTest_StageScene", StringComparison.OrdinalIgnoreCase))
            {
                return; // 민규 담당 테스트 씬에서만 자동 표시해 공용 씬 변경 위험을 줄인다.
            }

            if (FindFirstObjectByType<WaveRuntimeDebugOverlay>() != null) // 이미 씬에 있다면
            {
                return; // 중복 생성하지 않는다.
            }

            GameObject overlayObject = new GameObject("WaveRuntimeDebugOverlay_Runtime"); // 런타임 전용 오브젝트
            overlayObject.AddComponent<WaveRuntimeDebugOverlay>(); // 표시 스크립트 부착
        }
#endif

        private void OnEnable()
        {
            elapsedTime = 0f; // 테스트용 타이머 초기화
            currentStage = CalculateStageNumber(); // 첫 Stage 계산
            popupTimer = popupDuration; // 시작 팝업 표시
            EnsureUi(); // UI 자동 생성
            RefreshTexts(true); // 첫 화면 즉시 갱신
        }

        private void OnDisable()
        {
            if (canvasObject != null) // 런타임에 만든 UI가 남아 있다면
            {
                Destroy(canvasObject); // PlayMode 종료나 비활성화 때 정리한다.
                canvasObject = null; // 참조 비우기
            }
        }

        private void Update()
        {
            elapsedTime += Time.deltaTime; // 진행 시간 증가

            int nextStage = CalculateStageNumber(); // 현재 시간 기준 Stage 계산
            if (nextStage != currentStage) // Stage가 바뀌었다면
            {
                currentStage = nextStage; // 현재 Stage 갱신
                popupTimer = popupDuration; // Stage 시작 팝업 다시 표시
            }

            if (popupTimer > 0f) // 팝업 시간이 남아 있다면
            {
                popupTimer -= Time.deltaTime; // 남은 팝업 시간 감소
            }

            RefreshTexts(false); // 화면 표시 갱신
        }

        private int CalculateStageNumber()
        {
            int stageOffset = Mathf.FloorToInt(elapsedTime / Mathf.Max(1f, stageDurationSeconds)); // 0부터 시작하는 단계
            return firstStageNumber + stageOffset; // 실제 표시 Stage 번호
        }

        private void RefreshTexts(bool forcePopup)
        {
            EnsureUi(); // 혹시 UI가 없으면 다시 만든다.

            int loopIndex = Mathf.Max(0, (currentStage - firstStageNumber) / Mathf.Max(1, loopStageCount)); // 몇 번째 루프인지
            int activeMonsters = EnemyController.ActiveCount; // 현재 살아있는 몬스터 수
            float remainingSeconds = GetSecondsUntilNextStage(); // 다음 Stage까지 남은 시간

            SetText(headerText, $"STAGE {currentStage:00}"); // 큰 제목
            SetText(bodyText,
                $"루프 {loopIndex + 1}\n" +
                $"다음 Stage까지: {FormatTime(remainingSeconds)}\n" +
                $"현재 몬스터: {activeMonsters}");

            bool showPopup = forcePopup || popupTimer > 0f; // 새 Stage 시작 직후인지
            if (popupGroup != null)
            {
                popupGroup.alpha = showPopup ? Mathf.Clamp01(popupTimer / Mathf.Max(0.1f, popupDuration)) : 0f; // 서서히 사라짐
                popupGroup.blocksRaycasts = false; // 테스트 UI가 클릭을 막지 않게 한다.
                popupGroup.interactable = false; // 테스트 UI는 조작 대상이 아니다.
            }

            SetText(popupText, $"STAGE {currentStage:00} START"); // 팝업 문구
        }

        private float GetSecondsUntilNextStage()
        {
            float duration = Mathf.Max(1f, stageDurationSeconds); // 0 나누기 방지
            float progress = elapsedTime % duration; // 현재 Stage 안에서 지난 시간
            return Mathf.Max(0f, duration - progress); // 다음 Stage까지 남은 시간
        }

        private void EnsureUi()
        {
            if (canvasObject != null) // 이미 UI를 만들었다면
            {
                return; // 다시 만들지 않는다.
            }

            canvasObject = new GameObject("WaveRuntimeDebugOverlayCanvas"); // 런타임 전용 Canvas
            canvasObject.transform.SetParent(transform, false); // 이 컴포넌트 아래에 정리

            Canvas canvas = canvasObject.AddComponent<Canvas>(); // 화면 UI를 그리는 Canvas
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; // 화면 위에 직접 표시
            canvas.sortingOrder = 31000; // 기존 HUD보다 위에 보이도록 높은 순서 사용

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); // 해상도 대응
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; // 기준 해상도 기반 스케일
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 일반적인 테스트 기준

            CreateStatusPanel(canvasObject.transform); // 항상 보이는 상태창
            CreateStartPopup(canvasObject.transform); // Stage 시작 팝업
        }

        private void CreateStatusPanel(Transform parent)
        {
            RectTransform panel = CreateRect(parent, "WaveStatusPanel", new Vector2(360f, 96f)); // 패널 생성
            panel.anchorMin = new Vector2(0.5f, 1f); // 상단 중앙 고정
            panel.anchorMax = new Vector2(0.5f, 1f);
            panel.pivot = new Vector2(0.5f, 1f);
            panel.anchoredPosition = new Vector2(0f, -82f); // 기존 상단 더미와 너무 겹치지 않게 약간 아래

            Image background = panel.gameObject.AddComponent<Image>(); // 배경 이미지
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.78f); // 반투명 어두운 배경

            headerText = CreateText(panel, "Header", new Vector2(332f, 26f), 21, TextAnchor.MiddleCenter); // 제목
            headerText.rectTransform.anchoredPosition = new Vector2(0f, -16f);
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(1f, 0.92f, 0.55f, 1f);

            bodyText = CreateText(panel, "Body", new Vector2(332f, 50f), 14, TextAnchor.UpperLeft); // 상세 내용
            bodyText.rectTransform.anchoredPosition = new Vector2(0f, -58f);
            bodyText.color = new Color(0.92f, 0.97f, 1f, 1f);
            bodyText.lineSpacing = 0.9f;
        }

        private void CreateStartPopup(Transform parent)
        {
            RectTransform popup = CreateRect(parent, "WaveStartPopup", new Vector2(360f, 54f)); // 팝업 생성
            popup.anchorMin = new Vector2(0.5f, 0.5f); // 화면 중앙 근처
            popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.pivot = new Vector2(0.5f, 0.5f);
            popup.anchoredPosition = new Vector2(0f, 130f);

            Image background = popup.gameObject.AddComponent<Image>(); // 팝업 배경
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.86f);

            popupGroup = popup.gameObject.AddComponent<CanvasGroup>(); // 팝업 숨김/표시 제어
            popupGroup.blocksRaycasts = false;
            popupGroup.interactable = false;

            popupText = CreateText(popup, "PopupText", new Vector2(330f, 38f), 22, TextAnchor.MiddleCenter); // 팝업 글자
            popupText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f); // 팝업 박스 중앙 기준
            popupText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            popupText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            popupText.rectTransform.anchoredPosition = Vector2.zero; // 글자를 박스 한가운데에 둔다.
            popupText.fontStyle = FontStyle.Bold;
            popupText.color = new Color(1f, 0.92f, 0.45f, 1f);
        }

        private static RectTransform CreateRect(Transform parent, string objectName, Vector2 size)
        {
            GameObject child = new GameObject(objectName); // 새 UI 오브젝트
            child.transform.SetParent(parent, false); // 부모에 붙이기

            RectTransform rect = child.AddComponent<RectTransform>(); // UI 위치/크기 컴포넌트
            rect.sizeDelta = size; // 크기 설정
            rect.localScale = Vector3.one; // 스케일 기본값
            rect.localRotation = Quaternion.identity; // 회전 기본값
            return rect; // 만든 RectTransform 반환
        }

        private static Text CreateText(RectTransform parent, string objectName, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, objectName, size); // 텍스트 오브젝트 생성
            rect.anchorMin = new Vector2(0.5f, 1f); // 부모 상단 기준
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Text text = rect.gameObject.AddComponent<Text>(); // 기본 UI Text
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); // Unity 6000에서 사용할 수 있는 기본 런타임 폰트
            text.fontSize = fontSize; // 글자 크기
            text.alignment = alignment; // 정렬
            text.horizontalOverflow = HorizontalWrapMode.Wrap; // 긴 문장은 줄바꿈
            text.verticalOverflow = VerticalWrapMode.Overflow; // 세로는 임시 표시용으로 허용
            text.raycastTarget = false; // 클릭을 막지 않음
            return text; // 만든 Text 반환
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds)); // 올림 처리
            int minutes = totalSeconds / 60; // 분
            int remainSeconds = totalSeconds % 60; // 초
            return $"{minutes:00}:{remainSeconds:00}"; // 00:00 형식
        }

        private static void SetText(Text target, string value)
        {
            if (target != null) // 대상 텍스트가 있으면
            {
                target.text = value; // 문구 반영
            }
        }
    }
}
