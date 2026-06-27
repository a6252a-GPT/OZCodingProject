using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class WaveRuntimeDebugOverlay : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private WaveController waveController; // 표시할 실제 WaveController입니다.

        [Header("팝업")]
        [SerializeField, Min(0.1f)] private float popupDuration = 2.2f; // Stage 시작 팝업 유지 시간입니다.

        [Header("표시 문구")]
        [SerializeField] private string normalStageHeaderFormat = "STAGE {0:00}"; // 일반 Stage 제목 형식입니다.
        [SerializeField] private string bossStageHeaderFormat = "BOSS STAGE {0:00}"; // 보스 Stage 제목 형식입니다.
        [SerializeField] private string normalPopupFormat = "STAGE {0:00} START"; // 일반 Stage 시작 팝업 형식입니다.
        [SerializeField] private string bossPopupFormat = "BOSS STAGE {0:00}"; // 보스 Stage 시작 팝업 형식입니다.
        [SerializeField] private string stateLabel = "상태"; // 상태 줄 제목입니다.
        [SerializeField] private string nextStageTimeLabel = "다음 Stage까지"; // 일반 Stage 타이머 줄 제목입니다.
        [SerializeField] private string bossWaitText = "보스 처치 대기"; // 보스 Stage에서 타이머 대신 표시할 문구입니다.
        [SerializeField] private string activeMonsterLabel = "현재 몬스터"; // 현재 몬스터 수 줄 제목입니다.
        [SerializeField] private string normalStateText = "일반"; // 일반 상태 표시 문구입니다.
        [SerializeField] private string bossStateText = "보스"; // 보스 상태 표시 문구입니다.
        [SerializeField] private string specialStateText = "특수"; // 특수 상태 표시 문구입니다.
        [SerializeField] private string missingControllerText = "WaveController 없음"; // 컨트롤러가 없을 때 표시할 문구입니다.

        private GameObject canvasObject; // 런타임에 만든 Canvas 오브젝트입니다.
        private Text headerText; // 상단 제목 텍스트입니다.
        private Text bodyText; // 상태 상세 텍스트입니다.
        private CanvasGroup popupGroup; // 팝업 표시/숨김 제어입니다.
        private Text popupText; // Stage 시작 팝업 텍스트입니다.

        private float popupTimer; // 팝업 남은 시간입니다.
        private int lastStage; // Stage 변경 감지를 위한 이전 Stage입니다.

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInEditorPlayMode()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (FindFirstObjectByType<WaveController>() == null)
            {
                return;
            }

            if (FindFirstObjectByType<WaveRuntimeDebugOverlay>() != null)
            {
                return;
            }

            GameObject overlayObject = new GameObject("WaveRuntimeDebugOverlay_Runtime");
            overlayObject.AddComponent<WaveRuntimeDebugOverlay>();
        }
#endif

        private void OnEnable()
        {
            ResolveWaveController();
            lastStage = waveController != null ? waveController.CurrentStage : 0;
            popupTimer = popupDuration;
            EnsureUi();
            RefreshTexts(true);
        }

        private void OnDisable()
        {
            if (canvasObject != null)
            {
                Destroy(canvasObject);
                canvasObject = null;
            }
        }

        private void Update()
        {
            ResolveWaveController();

            if (waveController == null)
            {
                RefreshMissingController();
                return;
            }

            if (waveController.CurrentStage != lastStage)
            {
                lastStage = waveController.CurrentStage;
                popupTimer = popupDuration;
            }

            if (popupTimer > 0.0f)
            {
                popupTimer -= Time.deltaTime;
            }

            RefreshTexts(false);
        }

        private void RefreshTexts(bool forcePopup)
        {
            EnsureUi();

            if (waveController == null)
            {
                RefreshMissingController();
                return;
            }

            int activeMonsters = EnemyController.ActiveCount;
            bool isBossStage = waveController.CurrentState == WaveController.WaveRunState.Boss;

            SetText(headerText, string.Format(isBossStage ? bossStageHeaderFormat : normalStageHeaderFormat, waveController.CurrentStage));
            SetText(bodyText,
                $"{stateLabel}: {GetStateText(waveController.CurrentState)}\n" +
                $"{GetProgressLine()}\n" +
                $"{activeMonsterLabel}: {activeMonsters}");

            bool showPopup = forcePopup || popupTimer > 0.0f;

            if (popupGroup != null)
            {
                popupGroup.alpha = showPopup ? Mathf.Clamp01(popupTimer / Mathf.Max(0.1f, popupDuration)) : 0.0f;
                popupGroup.blocksRaycasts = false;
                popupGroup.interactable = false;
            }

            SetText(popupText, string.Format(isBossStage ? bossPopupFormat : normalPopupFormat, waveController.CurrentStage));
        }

        private string GetProgressLine()
        {
            if (waveController == null)
            {
                return missingControllerText;
            }

            if (!waveController.UsesStageTimer)
            {
                return bossWaitText;
            }

            return $"{nextStageTimeLabel}: {FormatTime(waveController.RemainingStageSeconds)}";
        }

        private string GetStateText(WaveController.WaveRunState state)
        {
            switch (state)
            {
                case WaveController.WaveRunState.Boss:
                    return bossStateText;
                case WaveController.WaveRunState.Special:
                    return specialStateText;
                default:
                    return normalStateText;
            }
        }

        private void RefreshMissingController()
        {
            EnsureUi();
            SetText(headerText, "WAVE");
            SetText(bodyText, missingControllerText);

            if (popupGroup != null)
            {
                popupGroup.alpha = 0.0f;
                popupGroup.blocksRaycasts = false;
                popupGroup.interactable = false;
            }
        }

        private void EnsureUi()
        {
            if (canvasObject != null)
            {
                return;
            }

            canvasObject = new GameObject("WaveRuntimeDebugOverlayCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 31000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);

            CreateStatusPanel(canvasObject.transform);
            CreateStartPopup(canvasObject.transform);
        }

        private void CreateStatusPanel(Transform parent)
        {
            RectTransform panel = CreateRect(parent, "WaveStatusPanel", new Vector2(360.0f, 96.0f));
            panel.anchorMin = new Vector2(0.5f, 1.0f);
            panel.anchorMax = new Vector2(0.5f, 1.0f);
            panel.pivot = new Vector2(0.5f, 1.0f);
            panel.anchoredPosition = new Vector2(0.0f, -82.0f);

            Image background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.025f, 0.03f, 0.78f);

            headerText = CreateText(panel, "Header", new Vector2(332.0f, 26.0f), 21, TextAnchor.MiddleCenter);
            headerText.rectTransform.anchoredPosition = new Vector2(0.0f, -16.0f);
            headerText.fontStyle = FontStyle.Bold;
            headerText.color = new Color(1.0f, 0.92f, 0.55f, 1.0f);

            bodyText = CreateText(panel, "Body", new Vector2(332.0f, 50.0f), 14, TextAnchor.UpperLeft);
            bodyText.rectTransform.anchoredPosition = new Vector2(0.0f, -58.0f);
            bodyText.color = new Color(0.92f, 0.97f, 1.0f, 1.0f);
            bodyText.lineSpacing = 0.9f;
        }

        private void CreateStartPopup(Transform parent)
        {
            RectTransform popup = CreateRect(parent, "WaveStartPopup", new Vector2(360.0f, 54.0f));
            popup.anchorMin = new Vector2(0.5f, 0.5f);
            popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.pivot = new Vector2(0.5f, 0.5f);
            popup.anchoredPosition = new Vector2(0.0f, 130.0f);

            Image background = popup.gameObject.AddComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.86f);

            popupGroup = popup.gameObject.AddComponent<CanvasGroup>();
            popupGroup.blocksRaycasts = false;
            popupGroup.interactable = false;

            popupText = CreateText(popup, "PopupText", new Vector2(330.0f, 38.0f), 22, TextAnchor.MiddleCenter);
            popupText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            popupText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            popupText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            popupText.rectTransform.anchoredPosition = Vector2.zero;
            popupText.fontStyle = FontStyle.Bold;
            popupText.color = new Color(1.0f, 0.92f, 0.45f, 1.0f);
        }

        private void ResolveWaveController()
        {
            if (waveController == null)
            {
                waveController = FindFirstObjectByType<WaveController>();
            }
        }

        private static RectTransform CreateRect(Transform parent, string objectName, Vector2 size)
        {
            GameObject child = new GameObject(objectName);
            child.transform.SetParent(parent, false);

            RectTransform rect = child.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            return rect;
        }

        private static Text CreateText(RectTransform parent, string objectName, Vector2 size, int fontSize, TextAnchor alignment)
        {
            RectTransform rect = CreateRect(parent, objectName, size);
            rect.anchorMin = new Vector2(0.5f, 1.0f);
            rect.anchorMax = new Vector2(0.5f, 1.0f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Text text = rect.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0.0f, seconds));
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainSeconds:00}";
        }

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
