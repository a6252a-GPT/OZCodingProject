using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    [DefaultExecutionOrder(1000)]
    public sealed class InGameSettingsPanelOverlay : MonoBehaviour // 인게임 설정 패널 오버레이
    {
        private const string SettingsButtonPath = "MinimapZoomButtons/Settings";
        private const float PanelOpenScale = 0.5f;

        [Header("Button Binding")]
        [SerializeField] private MinimapController minimap;
        [SerializeField] private Button settingsButton;
        [SerializeField, Min(0.05f)] private float rebindInterval = 0.25f;

        [Header("Input")]
        [SerializeField] private bool enableEscapeKey = true;

        [Header("Panel")]
        [SerializeField] private SettingsPanel settingsPanelPrefab;
        [SerializeField] private SettingsPanel settingsPanel;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Vector2 panelOpenPosition = Vector2.zero;

        [Header("Overlay")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private Image inputBlockerImage;
        [SerializeField] private UiBackgroundBlurLayer backgroundBlurLayer;

        [Header("Card Open Tween")]
        [SerializeField] private float startYOffset = -80f;
        [SerializeField, Min(0.01f)] private float openFadeSeconds = 0.25f;
        [SerializeField, Min(0.01f)] private float openMoveSeconds = 0.35f;
        [SerializeField, Min(0.01f)] private float closeFadeSeconds = 0.12f;

        private Button boundButton;
        private Coroutine openRoutine;
        private Sequence panelSequence;
        private bool isOpen;
        private bool isClosing;
        private bool ownsPause;
        private float previousTimeScale = 1f;
        private float nextRebindTime;
        private float EffectiveOpenSeconds => Mathf.Max(0.01f, Mathf.Max(openFadeSeconds, openMoveSeconds));
        private float EffectiveCloseSeconds => Mathf.Max(0.01f, closeFadeSeconds);

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        private IEnumerator Start()
        {
            yield return null; // MinimapController.EnsureUi 이후 연결
            ResolveReferences();
            BindSettingsButton();
        }

        private void OnEnable()
        {
            ResolveReferences();
            BindSettingsButton();
        }

        private void Update()
        {
            if (!Application.isPlaying || !enableEscapeKey || !WasEscapePressedThisFrame())
            {
                return;
            }

            if (isOpen || openRoutine != null)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || Time.unscaledTime < nextRebindTime)
            {
                return;
            }

            nextRebindTime = Time.unscaledTime + Mathf.Max(0.05f, rebindInterval);
            BindSettingsButton();
        }

        private void OnDisable()
        {
            if (isOpen)
            {
                RestoreTimeScale();
            }
        }

        private void OnDestroy()
        {
            UnbindSettingsButton();
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            panelSequence?.Kill(false);
        }

        public void Open()
        {
            if (openRoutine != null)
            {
                return;
            }

            openRoutine = StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            ResolveReferences();
            if (settingsPanel == null)
            {
                Debug.LogWarning("[InGameSettingsPanelOverlay] SettingsPanel reference is missing.");
                openRoutine = null;
                yield break;
            }

            if (isOpen && !isClosing)
            {
                openRoutine = null;
                yield break;
            }

            isOpen = true;
            isClosing = false;
            CaptureAndPauseTimeScale();
            PrepareOverlayForOpen();

            settingsPanel.SetCloseRequestHandler(Close);
            ResolvePanelReferences();
            PreparePanelForOpen();

            if (backgroundBlurLayer != null)
            {
                yield return backgroundBlurLayer.ShowRoutine(EffectiveOpenSeconds);
            }

            settingsPanel.Open();
            ResolvePanelReferences();
            PreparePanelForOpen();
            PlayPanelOpenTween();
            openRoutine = null;
        }

        public void Close()
        {
            if (!isOpen || isClosing)
            {
                return;
            }

            isClosing = true;
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            panelSequence?.Kill(false);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.blocksRaycasts = true;
                rootCanvasGroup.interactable = false;
            }

            if (backgroundBlurLayer != null)
            {
                backgroundBlurLayer.Hide(closeFadeSeconds);
            }

            float duration = EffectiveCloseSeconds;
            panelSequence = DOTween.Sequence().SetUpdate(true);
            if (panelCanvasGroup != null)
            {
                panelSequence.Join(panelCanvasGroup.DOFade(0f, duration));
            }

            if (panelRoot != null)
            {
                panelSequence.Join(panelRoot.DOAnchorPos(panelOpenPosition + new Vector2(0f, startYOffset), duration).SetEase(Ease.InCubic));
                panelSequence.Join(panelRoot.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            }

            if (panelCanvasGroup == null && panelRoot == null)
            {
                panelSequence.AppendInterval(duration);
            }

            panelSequence.OnComplete(() =>
            {
                settingsPanel?.Close();
                HideImmediate();
                RestoreTimeScale();
            });
        }

        private void ResolveReferences()
        {
            if (rootCanvasGroup == null)
            {
                rootCanvasGroup = GetComponent<CanvasGroup>();
            }

            if (backgroundBlurLayer == null)
            {
                backgroundBlurLayer = GetComponentInChildren<UiBackgroundBlurLayer>(true);
            }

            if (inputBlockerImage == null)
            {
                Transform blockerTransform = transform.Find("ModalInputBlocker");
                inputBlockerImage = blockerTransform != null ? blockerTransform.GetComponent<Image>() : null;
            }

            if (settingsPanel == null)
            {
                settingsPanel = GetComponentInChildren<SettingsPanel>(true);
            }

            if (settingsPanel == null && settingsPanelPrefab != null && Application.isPlaying)
            {
                settingsPanel = Instantiate(settingsPanelPrefab, transform);
                settingsPanel.name = settingsPanelPrefab.name;
            }

            ResolvePanelReferences();

            if (minimap == null)
            {
                minimap = FindFirstObjectByType<MinimapController>();
            }
        }

        private void ResolvePanelReferences()
        {
            if (settingsPanel == null)
            {
                return;
            }

            if (panelRoot == null)
            {
                panelRoot = settingsPanel.transform as RectTransform;
            }

            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = settingsPanel.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void BindSettingsButton()
        {
            Button current = ResolveSettingsButton();
            if (current == null)
            {
                return;
            }

            if (boundButton != null && boundButton != current)
            {
                boundButton.onClick.RemoveListener(Open);
            }

            boundButton = current;
            boundButton.onClick.RemoveListener(Open);
            boundButton.onClick.AddListener(Open);
        }

        private void UnbindSettingsButton()
        {
            if (boundButton == null)
            {
                return;
            }

            boundButton.onClick.RemoveListener(Open);
            boundButton = null;
        }

        private Button ResolveSettingsButton()
        {
            if (settingsButton != null)
            {
                return settingsButton;
            }

            if (minimap == null)
            {
                minimap = FindFirstObjectByType<MinimapController>();
            }

            Transform buttonTransform = minimap != null ? minimap.transform.Find(SettingsButtonPath) : null;
            settingsButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            return settingsButton;
        }

        private static bool WasEscapePressedThisFrame()
        {
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        }

        private void CaptureAndPauseTimeScale()
        {
            ownsPause = Time.timeScale > 0f;
            previousTimeScale = ownsPause ? Time.timeScale : 0f;
            Time.timeScale = 0f;
        }

        private void RestoreTimeScale()
        {
            if (ownsPause)
            {
                Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
            }

            ownsPause = false;
            previousTimeScale = 1f;
        }

        private void PrepareOverlayForOpen()
        {
            gameObject.SetActive(true);
            if (rootCanvasGroup == null)
            {
                return;
            }

            rootCanvasGroup.alpha = 1f;
            rootCanvasGroup.blocksRaycasts = true;
            rootCanvasGroup.interactable = true;
            SetInputBlocker(true);
        }

        private void PreparePanelForOpen()
        {
            if (panelRoot != null)
            {
                panelRoot.anchorMin = new Vector2(0.5f, 0.5f);
                panelRoot.anchorMax = new Vector2(0.5f, 0.5f);
                panelRoot.pivot = new Vector2(0.5f, 0.5f);
                panelRoot.anchoredPosition = panelOpenPosition + new Vector2(0f, startYOffset);
                panelRoot.localScale = Vector3.zero;
                panelRoot.SetAsLastSibling();
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }
        }

        private void PlayPanelOpenTween()
        {
            panelSequence?.Kill(false);
            panelSequence = DOTween.Sequence().SetUpdate(true);
            float duration = EffectiveOpenSeconds;

            if (panelCanvasGroup != null)
            {
                panelSequence.Join(panelCanvasGroup.DOFade(1f, duration));
            }

            if (panelRoot != null)
            {
                panelSequence.Join(panelRoot.DOAnchorPos(panelOpenPosition, duration).SetEase(Ease.OutCubic));
                panelSequence.Join(panelRoot.DOScale(Vector3.one * PanelOpenScale, duration).SetEase(Ease.OutBack));
            }

            if (panelCanvasGroup == null && panelRoot == null)
            {
                panelSequence.AppendInterval(duration);
            }

            panelSequence.OnComplete(() =>
            {
                if (panelRoot != null)
                {
                    panelRoot.localScale = Vector3.one * PanelOpenScale;
                }

                if (panelCanvasGroup != null)
                {
                    panelCanvasGroup.alpha = 1f;
                    panelCanvasGroup.blocksRaycasts = true;
                    panelCanvasGroup.interactable = true;
                }
            });
        }

        private void SetInputBlocker(bool enabled)
        {
            if (inputBlockerImage == null)
            {
                return;
            }

            inputBlockerImage.enabled = enabled;
            inputBlockerImage.raycastTarget = enabled;
            Color color = inputBlockerImage.color;
            color.a = 0f;
            inputBlockerImage.color = color;
        }

        private void HideImmediate()
        {
            isOpen = false;
            isClosing = false;
            if (openRoutine != null)
            {
                StopCoroutine(openRoutine);
                openRoutine = null;
            }

            panelSequence?.Kill(false);
            SetInputBlocker(false);

            if (rootCanvasGroup != null)
            {
                rootCanvasGroup.alpha = 1f;
                rootCanvasGroup.blocksRaycasts = false;
                rootCanvasGroup.interactable = false;
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0f;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }

            if (panelRoot != null)
            {
                panelRoot.anchoredPosition = panelOpenPosition;
                panelRoot.localScale = Vector3.zero;
            }

            settingsPanel?.Close();
            backgroundBlurLayer?.HideImmediate(true);
        }
    }
}
