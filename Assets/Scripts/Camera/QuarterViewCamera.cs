using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamProject01.Gameplay
{
    public class QuarterViewCamera : MonoBehaviour // 쿼터뷰 카메라
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset;

        [Header("View")]
        [SerializeField] private float distance = 15f;
        [SerializeField] private float pitch = 55f;
        [SerializeField] private float yaw;

        [Header("Mouse Wheel Zoom")]
        [SerializeField] private bool enableMouseWheelZoom = true;
        [SerializeField] private float zoomSpeed = 1.25f;
        [SerializeField] private float minDistance = 6f;
        [SerializeField] private float maxDistance = 22f;
        [SerializeField] private float zoomSharpness = 18f;
        [SerializeField] private PlayerPickupInteractor pickupInteractor;

        [Header("Manual Rotate")]
        [SerializeField] private Key rotateLeftKey = Key.Q;
        [SerializeField] private Key rotateRightKey = Key.E;
        [SerializeField] private float rotateSpeed = 90f;

        private Vector3 focusPosition; // 추적 위치
        private bool hasFocusPosition; // 추적 초기화
        private float targetDistance; // 목표 거리

        private void Awake()
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance); // 줌 초기값

            if (pickupInteractor == null)
                pickupInteractor = FindFirstObjectByType<PlayerPickupInteractor>(); // 픽업 UI
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            UpdateYawInput();
            UpdateZoomInput();
            UpdateZoomDistance();
            UpdateFocusPosition();
            ApplyCameraTransform();
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget; // 추적 대상
            hasFocusPosition = false; // 위치 재초기화
        }

        public void SetYaw(float newYaw)
        {
            yaw = newYaw; // yaw 고정
            hasFocusPosition = false; // 즉시 재정렬
        }

        private void UpdateYawInput()
        {
            Keyboard keyboard = Keyboard.current; // 입력
            if (keyboard == null)
                return;

            float input = 0f;

            if (keyboard[rotateLeftKey].isPressed)
                input -= 1f;

            if (keyboard[rotateRightKey].isPressed)
                input += 1f;

            if (Mathf.Approximately(input, 0f))
                return;

            yaw += input * rotateSpeed * Time.deltaTime; // Q/E 회전
        }

        private void UpdateZoomInput()
        {
            if (!enableMouseWheelZoom || GameplayInputBlocker.IsGameplayInputBlocked)
                return;

            if (pickupInteractor == null)
                pickupInteractor = FindFirstObjectByType<PlayerPickupInteractor>(); // 픽업 UI

            if (pickupInteractor != null && pickupInteractor.HasActivePickupCandidates)
                return; // 픽업 선택 우선

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            float scroll = mouse.scroll.ReadValue().y; // 휠 입력
            if (Mathf.Abs(scroll) <= 0.01f)
                return;

            targetDistance = Mathf.Clamp(targetDistance - Mathf.Sign(scroll) * zoomSpeed, minDistance, maxDistance); // 목표 줌
        }

        private void UpdateZoomDistance()
        {
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance); // 범위 보정
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, zoomSharpness) * Time.deltaTime); // 보간값
            distance = Mathf.Lerp(distance, targetDistance, t); // 거리 보간
        }

        private void UpdateFocusPosition()
        {
            Vector3 targetPosition = target.position + targetOffset; // 목표 위치

            if (!hasFocusPosition)
            {
                focusPosition = targetPosition; // 즉시 이동
                hasFocusPosition = true; // 초기화 완료
                return;
            }

            focusPosition = targetPosition; // 추적 위치
        }

        private void ApplyCameraTransform()
        {
            Quaternion viewRotation = Quaternion.Euler(pitch, yaw, 0f); // 뷰 회전
            Vector3 cameraOffset = viewRotation * Vector3.back * Mathf.Max(0f, distance); // 카메라 offset
            Vector3 cameraPosition = focusPosition + cameraOffset; // 카메라 위치

            transform.position = cameraPosition;
            transform.rotation = Quaternion.LookRotation(focusPosition - cameraPosition, Vector3.up); // 대상 바라봄
        }
    }
}
