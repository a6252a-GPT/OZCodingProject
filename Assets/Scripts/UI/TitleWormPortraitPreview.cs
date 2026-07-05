using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class TitleWormPortraitPreview : MonoBehaviour // 지렁이 선택 3D 초상화
    {
        [Header("Source")]
        public MetaProgressionManager Meta; // 선택 데이터

        [Header("Render")]
        public RawImage TargetImage; // 웜 UI 출력
        public RawImage StarterBodyTargetImage; // 스타터 UI 출력
        public Camera PortraitCamera; // 초상화 카메라
        public Camera StarterBodyPortraitCamera; // 스타터 초상화 카메라
        public Transform PreviewRoot; // 프리뷰 루트
        public Transform WormAnchor; // 지렁이 위치
        public Transform StarterBodyAnchor; // 스타터 바디 위치
        [Min(64)] public int TextureWidth = 768; // 렌더 텍스처 폭
        [Min(64)] public int TextureHeight = 512; // 렌더 텍스처 높이
        public bool MatchTargetImageAspect = true; // UI 칸 비율 맞춤
        [Range(1, 8)] public int RenderTextureAntiAliasing = 4; // 외곽 부드럽게
        public bool UseHdrRenderTexture; // 조명 표현 강화
        public Color CameraBackground = new Color(0.78f, 0.88f, 0.92f, 1f); // 단색 배경
        public bool UseTransparentBackground = true; // UI 뒤 배경이 비치도록 알파 사용
        [Range(0f, 1f)] public float TransparentBackgroundAlpha = 0f; // 투명 배경 알파
        public Rect WormUvRect = new Rect(0f, 0f, 0.5f, 1f); // 웜 표시 영역
        public Rect StarterBodyUvRect = new Rect(0.5f, 0f, 0.5f, 1f); // 스타터 표시 영역
        public bool UseSingleCombinedOutput = true; // one shared portrait region
        [Range(0, 31)] public int WormPreviewLayer = 30; // 웜 전용 렌더 레이어
        [Range(0, 31)] public int StarterBodyPreviewLayer = 29; // 스타터 전용 렌더 레이어

        [Header("Layout")]
        public Vector3 CameraLocalPosition = new Vector3(0f, 1.2f, -6.2f); // 카메라 위치
        public Vector3 CameraLookAtLocalPosition = new Vector3(-2f, 0.37f, 0.25f); // 시선 위치
        public Vector3 CameraLocalEulerAngles = new Vector3(7.63f, 0f, 0f); // 카메라 회전
        public bool UseCameraLookAt = true; // 시선 위치로 회전 계산
        [Min(0.1f)] public float OrthographicSize = 1.15f; // 화면 크기
        public Vector3 WormLocalPosition = Vector3.zero; // 앞쪽 캐릭터
        public Vector3 WormLocalEulerAngles = Vector3.zero; // 캐릭터 각도
        [Min(0.1f)] public float WormTargetHeight = 1f; // 캐릭터 높이
        public Vector3 StarterBodyLocalPosition = Vector3.zero; // 뒤쪽 바디
        public Vector3 StarterBodyLocalEulerAngles = new Vector3(0f, -90f, 0f); // 바디 각도
        [Min(0.1f)] public float StarterBodyTargetHeight = 1f; // 바디 높이
        public Vector3 CombinedWormLocalPosition = Vector3.zero; // merged view worm position
        public Vector3 CombinedStarterBodyLocalPosition = new Vector3(-1f, 0f, -0.4f); // merged view starter position
        public float PreviewRootYaw = 248.28f; // 마우스로 돌린 프리뷰 Y 회전
        public bool EnableIdleRotation = false; // 자동 회전 사용 여부
        [Range(0f, 180f)] public float IdleRotationSpeed = 8f; // 약한 회전

        [Header("Shadow")]
        public bool EnablePreviewShadow = true; // 프리뷰 바닥 그림자
        public Color PreviewShadowColor = new Color(0f, 0f, 0f, 0.28f); // 그림자 색상
        public Vector2 PreviewShadowSize = new Vector2(2.4f, 0.65f); // 그림자 크기
        public Vector3 PreviewShadowLocalOffset = new Vector3(0f, -0.03f, 0f); // bounds 기준 보정
        [Range(0.05f, 0.95f)] public float PreviewShadowSoftness = 0.72f; // 가장자리 흐림
        [Range(16, 256)] public int PreviewShadowTextureSize = 96; // 그림자 텍스처 해상도

        [Header("Mouse Control")]
        [Min(0.1f)] public float MinOrthographicSize = 1.25f; // 최대 확대
        [Min(0.1f)] public float MaxOrthographicSize = 2.45f; // 최대 축소
        [Min(0f)] public float IdleResumeDelay = 1.25f; // 조작 후 자동 회전 대기

        [Header("Runtime Camera Tuning")]
        public bool EnableRuntimeCameraTuning = true; // 플레이 중 카메라 값 찾기
        [Min(0.001f)] public float TuningPositionStep = 0.05f; // 화살표/Page 이동량
        [Min(0.001f)] public float TuningZoomStep = 0.05f; // +/- 줌 조절량
        public bool LogTuningValuesOnChange = true; // 바뀐 값 콘솔 출력

        [Header("Worm Prefabs")]
        public GameObject BasicWormPrefab; // 기본형
        public GameObject AttackWormPrefab; // 공격형
        public GameObject MobilityWormPrefab; // 이속형
        public GameObject SupportWormPrefab; // 지원형
        public GameObject MagicWormPrefab; // 마법형

        [Header("Starter Body Prefabs")]
        public GameObject BasicStarterBodyPrefab; // 기본 스타터
        public GameObject AttackStarterBodyPrefab; // 공격형 스타터
        public GameObject MobilityStarterBodyPrefab; // 이속형 스타터
        public GameObject SupportStarterBodyPrefab; // 지원형 스타터
        public GameObject MagicStarterBodyPrefab; // 마법형 스타터

        private RenderTexture renderTexture; // 웜 런타임 텍스처
        private RenderTexture starterBodyRenderTexture; // 스타터 런타임 텍스처
        private GameObject activeWorm; // 현재 지렁이
        private GameObject activeStarterBody; // 현재 스타터 바디
        private string activeWormId; // 현재 표시 ID
        private bool activeSingleCombinedOutput; // current output layout
        private bool cachedLayoutState; // inspector-driven layout cache
        private bool cachedUseSingleCombinedOutput; // cached output mode
        private Vector3 cachedWormLocalPosition; // cached split worm position
        private Vector3 cachedWormLocalEulerAngles; // cached worm rotation
        private float cachedWormTargetHeight; // cached worm size
        private Vector3 cachedStarterBodyLocalPosition; // cached split starter position
        private Vector3 cachedStarterBodyLocalEulerAngles; // cached starter rotation
        private float cachedStarterBodyTargetHeight; // cached starter size
        private Vector3 cachedCombinedWormLocalPosition; // cached merged worm position
        private Vector3 cachedCombinedStarterBodyLocalPosition; // cached merged starter position
        private float manualYaw; // 사용자가 돌린 각도
        private float idleResumeTime; // 자동 회전 재개 시각
        private GameObject previewShadowObject; // 런타임 그림자
        private Material previewShadowMaterial; // 그림자 머티리얼
        private Mesh previewShadowMesh; // 그림자 평면
        private Texture2D previewShadowTexture; // 그림자 텍스처
        private Color cachedPreviewShadowColor; // 텍스처 캐시 색
        private float cachedPreviewShadowSoftness = -1f; // 텍스처 캐시 흐림
        private int cachedPreviewShadowTextureSize; // 텍스처 캐시 크기

        private void Awake() // 초기화
        {
            manualYaw = PreviewRootYaw; // 저장된 프리뷰 회전에서 시작
            ResolveReferences(); // 참조 보정
            EnsureRenderTexture(); // 출력 텍스처
            Refresh(); // 첫 표시
        }

        private void OnEnable() // 이벤트 연결
        {
            ResolveReferences(); // 재활성 보정
            if (Meta != null)
            {
                Meta.SelectedWormChanged += OnSelectedWormChanged; // 선택 변경
            }

            Refresh(); // 즉시 갱신
        }

        private void OnDisable() // 이벤트 해제
        {
            if (Meta != null)
            {
                Meta.SelectedWormChanged -= OnSelectedWormChanged; // 선택 변경 해제
            }
        }

        private void OnDestroy() // 텍스처 정리
        {
            if (PortraitCamera != null && PortraitCamera.targetTexture == renderTexture)
            {
                PortraitCamera.targetTexture = null; // 카메라 해제
            }

            if (TargetImage != null && TargetImage.texture == renderTexture)
            {
                TargetImage.texture = null; // UI 해제
            }

            if (StarterBodyTargetImage != null && StarterBodyTargetImage.texture == renderTexture)
            {
                StarterBodyTargetImage.texture = null; // 스타터 UI 해제
            }

            if (StarterBodyTargetImage != null && StarterBodyTargetImage.texture == starterBodyRenderTexture)
            {
                StarterBodyTargetImage.texture = null; // 스타터 UI 해제
            }

            if (renderTexture != null)
            {
                renderTexture.Release(); // GPU 해제
                Destroy(renderTexture); // 오브젝트 해제
                renderTexture = null;
            }

            if (starterBodyRenderTexture != null)
            {
                starterBodyRenderTexture.Release(); // GPU 해제
                Destroy(starterBodyRenderTexture); // 오브젝트 해제
                starterBodyRenderTexture = null;
            }

            DestroyPreviewShadow(); // 그림자 해제
        }

        private void LateUpdate() // 약한 생동감
        {
            EnsureRenderTexture(); // UI 비율 변화 반영
            RebuildModelsIfLayoutChanged(); // inspector changes
            HandleRuntimeCameraTuning(); // 플레이 중 카메라 값 조절

            OrthographicSize = Mathf.Clamp(OrthographicSize, MinOrthographicSize, MaxOrthographicSize); // 줌 제한
            ApplyCamera(PortraitCamera, renderTexture, CameraLookAtLocalPosition, MainCameraCullingMask); // 웜 카메라
            if (IndependentStarterOutput)
            {
                ApplyCamera(StarterBodyPortraitCamera, starterBodyRenderTexture, CameraLookAtLocalPosition, 1 << StarterBodyPreviewLayer); // 스타터 카메라
            }

            if (PreviewRoot != null)
            {
                if (EnableIdleRotation && IdleRotationSpeed > 0f && Time.unscaledTime >= idleResumeTime)
                {
                    manualYaw += IdleRotationSpeed * Time.unscaledDeltaTime; // 자동 회전
                    PreviewRootYaw = NormalizeEulerAngle(manualYaw); // 현재 회전값 노출
                }

                PreviewRoot.localRotation = Quaternion.Euler(0f, manualYaw, 0f); // 전체 프리뷰 회전
                UpdatePreviewShadow(); // 그림자 위치/크기 갱신
            }
        }

        public void Refresh() // 현재 선택 표시
        {
            ResolveReferences(); // 참조 보정
            EnsureRenderTexture(); // 출력 준비

            string wormId = Meta != null ? Meta.SelectedWormId : MetaWormIds.Basic; // 현재 선택
            ShowWorm(wormId); // 모델 갱신
        }

        public void PreviewWorm(string wormId) // 외부 미리보기
        {
            ShowWorm(wormId); // 선택 전 표시용
        }

        public void AddManualYaw(float deltaYaw) // 마우스 회전
        {
            manualYaw += deltaYaw; // 누적 회전
            PreviewRootYaw = NormalizeEulerAngle(manualYaw); // 저장/로그용 회전값
            PauseIdleMotion(); // 자동 회전 잠시 정지
        }

        public void ZoomBy(float deltaSize) // 마우스 휠 줌
        {
            OrthographicSize = Mathf.Clamp(OrthographicSize + deltaSize, MinOrthographicSize, MaxOrthographicSize); // 줌 반영
            PauseIdleMotion(); // 자동 회전 잠시 정지
        }

        public void PauseIdleMotion() // 조작 중 자동 회전 정지
        {
            idleResumeTime = Time.unscaledTime + IdleResumeDelay; // 재개 예약
        }

        private void OnSelectedWormChanged(string wormId) // 선택 변경 이벤트
        {
            ShowWorm(wormId); // 모델 교체
        }

        private void ShowWorm(string wormId) // 지렁이/스타터 표시
        {
            string normalized = MetaWormIds.Normalize(wormId); // ID 보정
            if (string.Equals(activeWormId, normalized, StringComparison.OrdinalIgnoreCase)
                && activeWorm != null
                && activeStarterBody != null
                && activeSingleCombinedOutput == UseSingleCombinedOutput)
            {
                return; // 이미 표시 중
            }

            ClearActiveModels(); // 기존 제거

            GameObject wormPrefab = ResolveWormPrefab(normalized); // 지렁이 프리팹
            GameObject starterPrefab = ResolveStarterBodyPrefab(normalized); // 스타터 바디 프리팹

            if (wormPrefab != null && WormAnchor != null)
            {
                activeWorm = Instantiate(wormPrefab, WormAnchor, false); // 지렁이 생성
                activeWorm.name = $"Portrait_{wormPrefab.name}"; // 이름
                Vector3 wormPosition = UseSingleCombinedOutput ? CombinedWormLocalPosition : WormLocalPosition; // layout
                SetupPreviewInstance(activeWorm, wormPosition, WormLocalEulerAngles, WormTargetHeight); // 배치
                if (IndependentStarterOutput)
                {
                    SetLayerRecursively(activeWorm, WormPreviewLayer); // 웜 카메라 전용
                }
                else if (UseSingleCombinedOutput)
                {
                    SetLayerRecursively(activeWorm, WormPreviewLayer); // shared camera target
                }
            }

            if (starterPrefab != null && StarterBodyAnchor != null)
            {
                activeStarterBody = Instantiate(starterPrefab, StarterBodyAnchor, false); // 스타터 바디 생성
                activeStarterBody.name = $"Portrait_{starterPrefab.name}"; // 이름
                Vector3 starterPosition = UseSingleCombinedOutput ? CombinedStarterBodyLocalPosition : StarterBodyLocalPosition; // layout
                SetupPreviewInstance(activeStarterBody, starterPosition, ResolveStarterBodyEulerAngles(normalized), StarterBodyTargetHeight); // 배치
                if (IndependentStarterOutput)
                {
                    SetLayerRecursively(activeStarterBody, StarterBodyPreviewLayer); // 스타터 카메라 전용
                }
                else if (UseSingleCombinedOutput)
                {
                    SetLayerRecursively(activeStarterBody, WormPreviewLayer); // shared camera target
                }
            }

            activeWormId = normalized; // 상태 저장
            activeSingleCombinedOutput = UseSingleCombinedOutput; // layout cache
            UpdatePreviewShadow(); // 새 모델 기준 그림자 배치
            CacheLayoutState(); // inspector edit baseline
        }

        private void RebuildModelsIfLayoutChanged() // live inspector layout changes
        {
            if (!Application.isPlaying || !cachedLayoutState || !HasLayoutChanged())
            {
                return;
            }

            string rebuildWormId = !string.IsNullOrWhiteSpace(activeWormId)
                ? activeWormId
                : (Meta != null ? Meta.SelectedWormId : MetaWormIds.Basic);

            ClearActiveModels();
            activeWormId = null;
            cachedLayoutState = false;
            ShowWorm(rebuildWormId);
        }

        private bool HasLayoutChanged() // compare fields that affect spawned model transforms
        {
            return cachedUseSingleCombinedOutput != UseSingleCombinedOutput
                || cachedWormLocalPosition != WormLocalPosition
                || cachedWormLocalEulerAngles != WormLocalEulerAngles
                || !Mathf.Approximately(cachedWormTargetHeight, WormTargetHeight)
                || cachedStarterBodyLocalPosition != StarterBodyLocalPosition
                || cachedStarterBodyLocalEulerAngles != StarterBodyLocalEulerAngles
                || !Mathf.Approximately(cachedStarterBodyTargetHeight, StarterBodyTargetHeight)
                || cachedCombinedWormLocalPosition != CombinedWormLocalPosition
                || cachedCombinedStarterBodyLocalPosition != CombinedStarterBodyLocalPosition;
        }

        private void CacheLayoutState() // remember last applied inspector values
        {
            cachedLayoutState = true;
            cachedUseSingleCombinedOutput = UseSingleCombinedOutput;
            cachedWormLocalPosition = WormLocalPosition;
            cachedWormLocalEulerAngles = WormLocalEulerAngles;
            cachedWormTargetHeight = WormTargetHeight;
            cachedStarterBodyLocalPosition = StarterBodyLocalPosition;
            cachedStarterBodyLocalEulerAngles = StarterBodyLocalEulerAngles;
            cachedStarterBodyTargetHeight = StarterBodyTargetHeight;
            cachedCombinedWormLocalPosition = CombinedWormLocalPosition;
            cachedCombinedStarterBodyLocalPosition = CombinedStarterBodyLocalPosition;
        }

        private Vector3 ResolveStarterBodyEulerAngles(string normalizedWormId) // per-worm starter preview rotation
        {
            Vector3 eulerAngles = StarterBodyLocalEulerAngles;
            switch (MetaWormIds.Normalize(normalizedWormId))
            {
                case MetaWormIds.Attack:
                case MetaWormIds.Mobility:
                case MetaWormIds.Support:
                    eulerAngles.y = 0f;
                    break;
            }

            return eulerAngles;
        }

        private void ResolveReferences() // 기본 참조 찾기
        {
            if (Meta == null)
            {
                Meta = FindFirstObjectByType<MetaProgressionManager>(); // 씬 메타
            }
        }

        private void EnsureRenderTexture() // 렌더 텍스처 준비
        {
            if (PortraitCamera == null || TargetImage == null)
            {
                return; // 필수 참조 없음
            }

            if (UseSingleCombinedOutput)
            {
                SetTargetImageFullRegion(true); // aspect match uses merged region
            }

            renderTexture = EnsureRenderTextureFor(TargetImage, renderTexture, "RT_TitleWormPortrait"); // 웜 텍스처
            ApplyOutputImage(TargetImage, renderTexture, new Rect(0f, 0f, 1f, 1f)); // 웜 UI 연결

            if (IndependentStarterOutput)
            {
                SetTargetImageFullRegion(false); // keep authored split layout
                starterBodyRenderTexture = EnsureRenderTextureFor(StarterBodyTargetImage, starterBodyRenderTexture, "RT_TitleStarterBodyPortrait"); // 스타터 텍스처
                ApplyOutputImage(StarterBodyTargetImage, starterBodyRenderTexture, new Rect(0f, 0f, 1f, 1f)); // 스타터 UI 연결
                SetStarterOutputVisible(true); // split output
            }
            else
            {
                SetTargetImageFullRegion(UseSingleCombinedOutput); // merged output fills parent
                SetStarterOutputVisible(false); // secondary region hidden
                ReleaseStarterBodyRenderTexture(); // no extra RT
            }
        }

        private RenderTexture EnsureRenderTextureFor(RawImage image, RenderTexture current, string textureName) // 렌더 텍스처 준비
        {
            if (image == null)
            {
                return current; // 대상 없음
            }

            int width = Mathf.Max(64, TextureWidth); // 폭 보정
            int height = Mathf.Max(64, TextureHeight); // 높이 보정
            if (MatchTargetImageAspect && TryGetImageAspect(image, out float targetAspect))
            {
                height = Mathf.Max(64, Mathf.RoundToInt(width / targetAspect)); // UI 비율 맞춤
            }

            int antiAliasing = Mathf.ClosestPowerOfTwo(Mathf.Clamp(RenderTextureAntiAliasing, 1, 8)); // 샘플 보정
            RenderTextureFormat colorFormat = UseHdrRenderTexture && !UseTransparentBackground ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.ARGB32; // 투명 배경은 알파 포맷 고정
            if (current == null || current.width != width || current.height != height || current.antiAliasing != antiAliasing || current.format != colorFormat)
            {
                if (current != null)
                {
                    current.Release(); // 기존 해제
                    Destroy(current);
                }

                current = new RenderTexture(width, height, 24, colorFormat)
                {
                    name = textureName,
                    useMipMap = false,
                    autoGenerateMips = false,
                    antiAliasing = antiAliasing
                };
                current.Create(); // 생성
            }

            return current; // 결과
        }

        private bool TryGetImageAspect(RawImage image, out float aspect) // UI 출력 비율
        {
            aspect = 0f; // 기본값
            RectTransform rectTransform = image != null ? image.rectTransform : null; // UI Rect
            if (rectTransform == null)
            {
                return false; // 없음
            }

            Rect rect = rectTransform.rect; // 현재 크기
            float width = rect.width; // 출력 폭
            float height = rect.height; // 출력 높이
            if (width <= 1f || height <= 1f)
            {
                return false; // 아직 레이아웃 전
            }

            aspect = Mathf.Clamp(width / height, 0.25f, 4f); // 비율 제한
            return true; // 성공
        }

        private int MainCameraCullingMask => UseSingleCombinedOutput ? 1 << WormPreviewLayer : (IndependentStarterOutput ? 1 << WormPreviewLayer : -1); // main camera layers

        private bool IndependentStarterOutput => !UseSingleCombinedOutput && StarterBodyTargetImage != null && StarterBodyPortraitCamera != null; // 독립 출력 여부

        private void SetTargetImageFullRegion(bool fullRegion) // runtime UI region merge
        {
            if (!fullRegion || TargetImage == null)
            {
                return;
            }

            RectTransform rectTransform = TargetImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
        }

        private void SetStarterOutputVisible(bool visible) // secondary portrait region
        {
            if (StarterBodyTargetImage != null)
            {
                StarterBodyTargetImage.enabled = visible;
                StarterBodyTargetImage.raycastTarget = visible;
                if (!visible)
                {
                    StarterBodyTargetImage.texture = null;
                }
            }

            if (StarterBodyPortraitCamera != null)
            {
                StarterBodyPortraitCamera.enabled = visible;
                if (!visible)
                {
                    StarterBodyPortraitCamera.targetTexture = null;
                }
            }
        }

        private void ReleaseStarterBodyRenderTexture() // split output cleanup
        {
            if (starterBodyRenderTexture == null)
            {
                return;
            }

            starterBodyRenderTexture.Release();
            Destroy(starterBodyRenderTexture);
            starterBodyRenderTexture = null;
        }

        private void ApplyOutputImage(RawImage image, RenderTexture texture, Rect uvRect) // UI 출력 연결
        {
            if (image == null)
            {
                return; // 대상 없음
            }

            image.texture = texture; // 텍스처 연결
            image.uvRect = uvRect; // 좌우 분할
        }

        private void HandleRuntimeCameraTuning() // 플레이 중 프리뷰 카메라 수치 조절
        {
            if (!Application.isPlaying || !EnableRuntimeCameraTuning)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool changed = false;
            bool editLookAt = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            Vector3 delta = Vector3.zero;
            float positionStep = Mathf.Max(0.001f, TuningPositionStep);
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                delta.x -= positionStep;
            }

            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                delta.x += positionStep;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                delta.y += positionStep;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                delta.y -= positionStep;
            }

            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                delta.z += positionStep;
            }

            if (keyboard.pageDownKey.wasPressedThisFrame)
            {
                delta.z -= positionStep;
            }

            if (delta != Vector3.zero)
            {
                if (editLookAt)
                {
                    CameraLookAtLocalPosition += delta; // Shift+방향키는 시선 위치
                }
                else
                {
                    CameraLocalPosition += delta; // 방향키는 카메라 위치
                }

                changed = true;
            }

            float zoomStep = Mathf.Max(0.001f, TuningZoomStep);
            if (keyboard.equalsKey.wasPressedThisFrame)
            {
                OrthographicSize -= zoomStep; // 더 크게 보이기
                changed = true;
            }

            if (keyboard.minusKey.wasPressedThisFrame)
            {
                OrthographicSize += zoomStep; // 더 작게 보이기
                changed = true;
            }

            OrthographicSize = Mathf.Clamp(OrthographicSize, MinOrthographicSize, MaxOrthographicSize);
            if (keyboard.pKey.wasPressedThisFrame)
            {
                LogTuningValues();
                return;
            }

            if (changed)
            {
                PauseIdleMotion();
                if (LogTuningValuesOnChange)
                {
                    LogTuningValues();
                }
            }
        }

        private void LogTuningValues() // 찾은 값을 고정하기 위한 콘솔 출력
        {
            Debug.Log(
                $"[TitleWormPortraitPreview] CameraLocalPosition={CameraLocalPosition}, "
                + $"CameraLookAtLocalPosition={CameraLookAtLocalPosition}, "
                + $"CameraLocalEulerAngles={ResolveCameraEulerAnglesForLog()}, "
                + $"PreviewRootYaw={NormalizeEulerAngle(manualYaw):0.###}, "
                + $"OrthographicSize={OrthographicSize:0.###}",
                this);
        }

        private void ApplyCamera(Camera camera, RenderTexture texture, Vector3 focusLocalPosition, int cullingMask) // 카메라 설정
        {
            if (camera == null || PreviewRoot == null || texture == null)
            {
                return; // 대상 없음
            }

            camera.transform.localPosition = CameraLocalPosition + new Vector3(focusLocalPosition.x, 0f, focusLocalPosition.z); // 대상 앞 배치
            camera.orthographic = true; // UI용
            camera.allowHDR = UseHdrRenderTexture; // 밝은 조명
            camera.allowMSAA = RenderTextureAntiAliasing > 1; // 외곽 보정
            camera.orthographicSize = OrthographicSize; // 크기 반영
            camera.targetTexture = texture; // 출력 연결
            ApplyCameraClearMode(camera, texture); // 투명 배경/단색 배경 적용
            if (cullingMask >= 0)
            {
                camera.cullingMask = cullingMask; // 전용 레이어만 출력
            }

            ApplyCameraRotation(camera, focusLocalPosition); // 회전 반영
        }

        private void ApplyCameraRotation(Camera camera, Vector3 focusLocalPosition) // 카메라 회전 적용
        {
            if (UseCameraLookAt)
            {
                camera.transform.LookAt(PreviewRoot.TransformPoint(focusLocalPosition)); // 대상 고정
                CameraLocalEulerAngles = NormalizeEulerAngles(camera.transform.localEulerAngles); // 로그/고정용 저장
                return;
            }

            camera.transform.localRotation = Quaternion.Euler(CameraLocalEulerAngles); // 직접 회전값 사용
        }

        private Vector3 ResolveCameraEulerAnglesForLog() // 최신 로그용 카메라 회전
        {
            if (!UseCameraLookAt)
            {
                return NormalizeEulerAngles(CameraLocalEulerAngles);
            }

            Vector3 cameraLocalPosition = CameraLocalPosition + new Vector3(CameraLookAtLocalPosition.x, 0f, CameraLookAtLocalPosition.z);
            Vector3 direction = CameraLookAtLocalPosition - cameraLocalPosition;
            if (direction.sqrMagnitude <= 0.000001f)
            {
                return NormalizeEulerAngles(CameraLocalEulerAngles);
            }

            return NormalizeEulerAngles(Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles);
        }

        private static Vector3 NormalizeEulerAngles(Vector3 eulerAngles) // 보기 좋은 0~360 회전값
        {
            return new Vector3(
                NormalizeEulerAngle(eulerAngles.x),
                NormalizeEulerAngle(eulerAngles.y),
                NormalizeEulerAngle(eulerAngles.z));
        }

        private static float NormalizeEulerAngle(float angle) // 음수/360 초과 보정
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        private void ApplyCameraClearMode(Camera camera, RenderTexture texture) // 렌더 텍스처 배경 처리
        {
            Color background = GetEffectiveCameraBackground();
            camera.backgroundColor = background; // 배경 반영
            if (!UseTransparentBackground)
            {
                camera.clearFlags = CameraClearFlags.SolidColor; // 단색 배경 고정
                return;
            }

            ClearRenderTexture(texture, background); // 색상 알파를 직접 0으로 비움
            camera.clearFlags = CameraClearFlags.Depth; // 카메라가 색 알파를 다시 채우지 않게 함
        }

        private static void ClearRenderTexture(RenderTexture texture, Color clearColor) // 투명 클리어
        {
            if (texture == null)
            {
                return;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(true, true, clearColor);
            RenderTexture.active = previous;
        }

        private Color GetEffectiveCameraBackground() // 투명 배경 적용
        {
            if (!UseTransparentBackground)
            {
                return CameraBackground;
            }

            Color background = CameraBackground;
            background.a = Mathf.Clamp01(TransparentBackgroundAlpha);
            return background;
        }

        private void SetupPreviewInstance(GameObject instance, Vector3 localPosition, Vector3 localEuler, float targetHeight) // 프리뷰 배치
        {
            if (instance == null)
            {
                return; // 대상 없음
            }

            instance.transform.localPosition = localPosition; // 위치
            instance.transform.localRotation = Quaternion.Euler(localEuler); // 회전
            instance.transform.localScale = Vector3.one; // 초기화
            DisableGameplayComponents(instance); // 프리뷰 전용
            FitToHeight(instance, targetHeight); // 크기 정규화
        }

        private void FitToHeight(GameObject instance, float targetHeight) // 모델 높이 맞춤
        {
            Bounds bounds;
            if (!TryGetRendererBounds(instance, out bounds) || bounds.size.y <= 0.0001f)
            {
                return; // 렌더러 없음
            }

            float scale = Mathf.Max(0.01f, targetHeight) / bounds.size.y; // 목표 높이
            instance.transform.localScale *= scale; // 스케일 적용

            if (!TryGetRendererBounds(instance, out bounds))
            {
                return; // 재계산 실패
            }

            Vector3 offset = instance.transform.position - bounds.center; // 중심 보정
            instance.transform.position += offset; // 앵커 중심으로 이동
        }

        private static bool TryGetRendererBounds(GameObject root, out Bounds bounds) // 렌더러 bounds
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>(); // 렌더러
            bool hasBounds = false; // 유효 여부
            bounds = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue; // 없음
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds; // 첫 bounds
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderer.bounds); // 합치기
            }

            return hasBounds; // 결과
        }

        private void UpdatePreviewShadow() // 프리뷰 소프트 그림자
        {
            if (!EnablePreviewShadow || PreviewRoot == null || activeWorm == null)
            {
                SetPreviewShadowVisible(false);
                return;
            }

            EnsurePreviewShadow();
            if (previewShadowObject == null)
            {
                return;
            }

            if (!TryGetActiveModelBounds(out Bounds bounds))
            {
                SetPreviewShadowVisible(false);
                return;
            }

            EnsurePreviewShadowTexture();
            SetPreviewShadowVisible(true);
            SetLayerRecursively(previewShadowObject, WormPreviewLayer);

            Vector3 worldPosition = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Vector3 localPosition = PreviewRoot.InverseTransformPoint(worldPosition) + PreviewShadowLocalOffset;
            previewShadowObject.transform.localPosition = localPosition;
            previewShadowObject.transform.localRotation = Quaternion.identity;
            previewShadowObject.transform.localScale = new Vector3(
                Mathf.Max(0.01f, PreviewShadowSize.x),
                1f,
                Mathf.Max(0.01f, PreviewShadowSize.y));
        }

        private bool TryGetActiveModelBounds(out Bounds bounds) // 그림자 기준 bounds
        {
            bool hasBounds = false;
            bounds = default;
            if (TryGetRendererBounds(activeWorm, out Bounds wormBounds))
            {
                bounds = wormBounds;
                hasBounds = true;
            }

            if (TryGetRendererBounds(activeStarterBody, out Bounds starterBounds))
            {
                if (hasBounds)
                {
                    bounds.Encapsulate(starterBounds);
                }
                else
                {
                    bounds = starterBounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        private void EnsurePreviewShadow() // 그림자 오브젝트 생성
        {
            if (previewShadowObject != null)
            {
                return;
            }

            previewShadowObject = new GameObject("PortraitShadow")
            {
                hideFlags = HideFlags.DontSave
            };
            previewShadowObject.transform.SetParent(PreviewRoot, false);

            MeshFilter meshFilter = previewShadowObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = previewShadowObject.AddComponent<MeshRenderer>();
            previewShadowMesh = CreatePreviewShadowMesh();
            meshFilter.sharedMesh = previewShadowMesh;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Transparent");
            }

            previewShadowMaterial = new Material(shader)
            {
                name = "Runtime_TitleWormPortraitShadow",
                hideFlags = HideFlags.DontSave,
                renderQueue = 3000
            };
            meshRenderer.sharedMaterial = previewShadowMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private Mesh CreatePreviewShadowMesh() // 양면 수평 평면
        {
            Mesh mesh = new Mesh
            {
                name = "Runtime_TitleWormPortraitShadowMesh",
                hideFlags = HideFlags.DontSave
            };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(-0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(0.5f, 0f, -0.5f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void EnsurePreviewShadowTexture() // 원형 알파 텍스처
        {
            int textureSize = Mathf.Clamp(PreviewShadowTextureSize, 16, 256);
            if (previewShadowTexture != null
                && cachedPreviewShadowTextureSize == textureSize
                && Mathf.Approximately(cachedPreviewShadowSoftness, PreviewShadowSoftness)
                && cachedPreviewShadowColor == PreviewShadowColor)
            {
                return;
            }

            if (previewShadowTexture != null)
            {
                Destroy(previewShadowTexture);
            }

            previewShadowTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime_TitleWormPortraitShadowTexture",
                hideFlags = HideFlags.DontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float innerRadius = Mathf.Clamp01(1f - PreviewShadowSoftness);
            float edgeRange = Mathf.Max(0.001f, 1f - innerRadius);
            Color[] pixels = new Color[textureSize * textureSize];
            for (int y = 0; y < textureSize; y++)
            {
                float v = (y + 0.5f) / textureSize * 2f - 1f;
                for (int x = 0; x < textureSize; x++)
                {
                    float u = (x + 0.5f) / textureSize * 2f - 1f;
                    float radius = Mathf.Sqrt(u * u + v * v);
                    float fade = 1f - Mathf.Clamp01((radius - innerRadius) / edgeRange);
                    fade = fade * fade * (3f - 2f * fade);
                    Color color = PreviewShadowColor;
                    color.a *= fade;
                    pixels[y * textureSize + x] = color;
                }
            }

            previewShadowTexture.SetPixels(pixels);
            previewShadowTexture.Apply(false, true);
            cachedPreviewShadowTextureSize = textureSize;
            cachedPreviewShadowSoftness = PreviewShadowSoftness;
            cachedPreviewShadowColor = PreviewShadowColor;

            if (previewShadowMaterial != null)
            {
                previewShadowMaterial.mainTexture = previewShadowTexture;
            }
        }

        private void SetPreviewShadowVisible(bool visible) // 그림자 표시
        {
            if (previewShadowObject != null && previewShadowObject.activeSelf != visible)
            {
                previewShadowObject.SetActive(visible);
            }
        }

        private void DestroyPreviewShadow() // 그림자 리소스 해제
        {
            if (previewShadowObject != null)
            {
                Destroy(previewShadowObject);
                previewShadowObject = null;
            }

            if (previewShadowMaterial != null)
            {
                Destroy(previewShadowMaterial);
                previewShadowMaterial = null;
            }

            if (previewShadowTexture != null)
            {
                Destroy(previewShadowTexture);
                previewShadowTexture = null;
            }

            if (previewShadowMesh != null)
            {
                Destroy(previewShadowMesh);
                previewShadowMesh = null;
            }
        }

        private static void DisableGameplayComponents(GameObject root) // 게임 로직 비활성
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true); // 콜라이더
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false; // UI 프리뷰
            }

            Rigidbody[] rigidbodies = root.GetComponentsInChildren<Rigidbody>(true); // 물리
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                rigidbodies[i].isKinematic = true; // 물리 정지
                rigidbodies[i].detectCollisions = false; // 충돌 차단
            }

            MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true); // 런타임 스크립트
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                {
                    behaviours[i].enabled = false; // 초상화 전용
                }
            }
        }

        private static void SetLayerRecursively(GameObject root, int layer) // 렌더 레이어 적용
        {
            if (root == null)
            {
                return; // 대상 없음
            }

            root.layer = Mathf.Clamp(layer, 0, 31); // 현재 오브젝트
            Transform rootTransform = root.transform; // 루트
            for (int i = 0; i < rootTransform.childCount; i++)
            {
                SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer); // 자식
            }
        }

        private void ClearActiveModels() // 기존 모델 제거
        {
            if (activeWorm != null)
            {
                Destroy(activeWorm); // 지렁이 제거
                activeWorm = null;
            }

            if (activeStarterBody != null)
            {
                Destroy(activeStarterBody); // 스타터 제거
                activeStarterBody = null;
            }
        }

        private GameObject ResolveWormPrefab(string wormId) // 지렁이 프리팹
        {
            switch (MetaWormIds.Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return AttackWormPrefab != null ? AttackWormPrefab : BasicWormPrefab; // 공격형
                case MetaWormIds.Mobility:
                    return MobilityWormPrefab != null ? MobilityWormPrefab : BasicWormPrefab; // 이속형
                case MetaWormIds.Support:
                    return SupportWormPrefab != null ? SupportWormPrefab : BasicWormPrefab; // 지원형
                case MetaWormIds.Magic:
                    return MagicWormPrefab != null ? MagicWormPrefab : BasicWormPrefab; // 마법형
                default:
                    return BasicWormPrefab; // 기본형
            }
        }

        private GameObject ResolveStarterBodyPrefab(string wormId) // 스타터 바디 프리팹
        {
            switch (MetaWormIds.Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return AttackStarterBodyPrefab != null ? AttackStarterBodyPrefab : BasicStarterBodyPrefab; // 공격형
                case MetaWormIds.Mobility:
                    return MobilityStarterBodyPrefab != null ? MobilityStarterBodyPrefab : BasicStarterBodyPrefab; // 이속형
                case MetaWormIds.Support:
                    return SupportStarterBodyPrefab != null ? SupportStarterBodyPrefab : BasicStarterBodyPrefab; // 지원형
                case MetaWormIds.Magic:
                    return MagicStarterBodyPrefab != null ? MagicStarterBodyPrefab : BasicStarterBodyPrefab; // 마법형
                default:
                    return BasicStarterBodyPrefab; // 기본형
            }
        }
    }
}
