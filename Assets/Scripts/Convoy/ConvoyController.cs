using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TeamProject01.Gameplay
{
    public enum ConvoyControlMode // 조작 모드
    {
        RelativeTurn, // 진행방향 좌우 턴
        WasdDirection, // 자동전진 WASD
        MousePointer, // 마우스 추적
        WasdManualForward // 수동전진 WASD
    }

    public sealed partial class ConvoyController : MonoBehaviour // 컨보이 본체
    {
        [Header("Scene References")]
        public Transform SegmentRoot; // 세그먼트 부모
        public Transform DetachedTailRoot; // 분리 꼬리 부모
        public Transform ProjectileRoot; // 투사체 부모
        public Transform HeadVisual; // 머리 표시
        public GroundCheck HeadGroundCheck; // 머리 바닥 체크
        public GameObject SegmentPrefab; // 세그먼트 프리팹
        public Material HeadMaterial; // 머리 재질
        public Material SegmentMaterial; // 몸통 재질 A
        public Material SegmentAltMaterial; // 몸통 재질 B

        [Header("Movement Feel")]
        [Min(0f)] public float BaseSpeed = 6f; // 전진 속도
        [Min(1f)] public float TurnSpeed = 138f; // 최대 회전
        [Min(1f)] public float TurnResponse = 11f; // 회전 반응
        [Min(1f)] public float TurnReleaseResponse = 17f; // 회전 복귀
        [Min(1f)] public float DirectionSteerFullTurnAngle = 42f; // 최대 조향각
        public ConvoyControlMode ControlMode = ConvoyControlMode.RelativeTurn; // 현재 모드

        [Header("Body Follow")]
        [Range(1, 40)] public int StartingSegmentCount = 9; // 시작 길이
        [Range(1, 100)] public int MaxSegmentCount = 100; // 최대 길이
        [Min(0.1f)] public float SegmentSpacing = 1.18f; // 몸통 간격
        [Min(0.01f)] public float MinPathSampleDistance = 0.08f; // 경로 샘플 간격
        [Min(1f)] public float SegmentFollowResponse = 24f; // 추적 반응
        [Min(1f)] public float SegmentTurnResponse = 22f; // 회전 추적
        [Min(128)] public int PathSampleLimit = 2048; // 경로 보관량
        public Vector3 HeadScale = new Vector3(1.25f, 0.6f, 1.45f); // 머리 크기
        public Vector3 SegmentScale = new Vector3(1.05f, 0.48f, 1.05f); // 몸통 크기
        [Min(0f)] public float VisualCenterHeight = 0.32f; // 표시 높이
        [Min(0f)] public float HeadVisualLean = 8f; // 회전 기울기

        [Header("Tail Collision")]
        public bool EnableTailCollision = true; // 꼬리 충돌 사용
        [Range(1, 12)] public int TailCollisionSafeSegmentCount = 4; // 앞쪽 안전칸
        [Min(0.1f)] public float TailCollisionRadius = 0.82f; // 충돌 반경
        [Min(0f)] public float TailCutCooldown = 0.45f; // 재절단 대기

        [Header("Detached Tail Physics")]
        public bool EnableHeadPhysicsCollider = true; // 머리 밀기 콜라이더
        public bool EnableDetachedTailPhysics = true; // 분리 물리 사용
        [Min(0.01f)] public float DetachedTailMass = 0.8f; // 분리 질량
        [Min(0f)] public float DetachedTailLinearDamping = 0.75f; // 이동 감쇠
        [Min(0f)] public float DetachedTailAngularDamping = 1.5f; // 회전 감쇠
        [Min(0f)] public float TailBurstForce = 16.5f; // 절단 폭발 힘
        [Min(0f)] public float TailBurstRadius = 6.4f; // 폭발 반경
        [Min(0f)] public float TailBurstUpward = 1.7f; // 위쪽 힘
        [Min(0f)] public float TailBurstTorque = 8.5f; // 회전 힘
        [Range(1f, 85f)] public float DetachedTailJointAngle = 38f; // 링크 굽힘각
        [Min(0f)] public float DetachedTailJointProjection = 0.18f; // 링크 보정 거리

        [Header("Detached Tail Rejoin")]
        public bool EnableDetachedTailRejoin = true; // 재결합 사용
        [Min(0.01f)] public float DetachedTailSettleSpeed = 0.08f; // 안착 속도
        [Min(0.01f)] public float DetachedTailSettleAngularSpeed = 0.55f; // 안착 회전속도
        [Min(0f)] public float DetachedTailSettleTime = 1.1f; // 안착 유지시간
        [Min(0f)] public float DetachedTailMinRejoinAge = 1.8f; // 최소 재결합 대기
        [Min(0.1f)] public float RejoinAreaRadius = 1.15f; // 재결합 반경
        [Min(0f)] public float RejoinAreaForwardOffset = 1.65f; // 머리 앞 거리
        [Min(0f)] public float RejoinAreaHeight = 0.08f; // 표시 높이
        [Range(12, 96)] public int RejoinAreaSegments = 48; // 원 세그먼트
        public Color RejoinAreaColor = new Color(0.35f, 1f, 0.78f, 0.82f); // 재결합 색

        [Header("Segment Weapons")]
        public bool EnableSegmentAutoFire = true; // 자동 발사 사용

        private readonly List<Transform> segments = new List<Transform>(128); // 연결 몸통
        private readonly List<GroundCheck> segmentGroundChecks = new List<GroundCheck>(128); // 몸통 바닥 체크
        private readonly List<ConvoySegmentRuntime> segmentRuntimes = new List<ConvoySegmentRuntime>(128); // 몸통 런타임
        private readonly List<DetachedTailGroup> detachedTails = new List<DetachedTailGroup>(16); // 분리 꼬리
        private readonly List<Vector3> path = new List<Vector3>(2048); // 머리 경로
        private Material rejoinAreaMaterial; // 재결합 재질
        private Vector3 startPosition; // 시작 위치
        private Quaternion startRotation; // 시작 회전
        private float currentTurnVelocity; // 현재 회전속도
        private float currentTurnInput; // 현재 회전입력
        private float currentForwardSpeed; // 현재 전진속도
        private float tailCutCooldownRemaining; // 절단 쿨타임
        private int detachedTailSerial; // 분리 그룹 번호

        public int SegmentCount => segments.Count; // 표시 길이
        public int MaxSegments => MaxSegmentCount; // 외부 최대 길이
        public bool CanAddSegment => CanAddSegmentPrefab(SegmentPrefab); // 기본 추가 가능
        public CoreStatData CurrentCoreStats => CoreStatProvider.GetCurrentOrDefault(); // 현재 성장값
        public float CurrentSpeed => currentForwardSpeed; // HUD 속도
        public float CurrentTurnVelocity => currentTurnVelocity; // HUD 회전
        public float CurrentTurnInput => currentTurnInput; // 머리 기울기
        public ConvoyControlMode CurrentControlMode => ControlMode; // HUD 모드
        public string CurrentControlModeLabel => GetControlModeLabel(ControlMode); // HUD 모드명
        public event Action<int> SegmentCountChanged; // 세그먼트 수 변경

        private void Awake() // 참조 준비
        {
            startPosition = transform.position; // 리셋 위치
            startRotation = transform.rotation; // 리셋 회전
            EnsureHeadVisual(); // 머리 보강
            ConfigureGroundChecks(); // 바닥 체크 연결
            EnsureHeadPhysicsCollider(); // 머리 충돌 보강
            EnsureSegmentRoot(); // 몸통 루트 보강
            EnsureDetachedTailRoot(); // 분리 루트 보강
            EnsureProjectileRoot(); // 투사체 루트 보강
            CollectExistingSegments(); // 씬 배치 몸통 수집
        }

        private void Start() // 시작 세팅
        {
            startPosition = SnapHeadToGround(startPosition); // 시작 바닥 보정
            transform.position = SnapHeadToGround(transform.position); // 현재 바닥 보정
            currentForwardSpeed = BaseSpeed; // 초기 속도
            ResetPath(); // 경로 초기화

            while (segments.Count < StartingSegmentCount)
            {
                if (!AddSegment(SegmentPrefab, false))
                {
                    break; // 최대치 도달
                }
            }

            SnapSegmentsToPath(); // 몸통 정렬
        }

        private void Update() // 이동 루프
        {
            float deltaTime = Time.deltaTime; // 프레임 시간
            if (deltaTime <= 0f)
            {
                return; // 정지 프레임
            }

            WormInput input = ReadInput(); // 입력 수집

            if (input.Reset)
            {
                ResetWorm(); // 시작 위치
                return; // 이번 프레임 종료
            }

            if (input.AddSegment)
            {
                AddSegment(SegmentPrefab, true); // 테스트 추가
            }

            if (input.RemoveSegment)
            {
                RemoveSegment(); // 테스트 제거
            }

            ApplyControl(input, deltaTime); // 모드별 조향
            transform.position += transform.forward * (currentForwardSpeed * deltaTime); // 전진
            transform.position = SnapHeadToGround(transform.position); // 머리 바닥 유지

            SamplePathIfNeeded(); // 경로 기록
            UpdateHeadVisual(deltaTime); // 머리 표시
            UpdateSegments(deltaTime); // 몸통 추적
            UpdateSegmentWeapons(deltaTime); // 세그먼트 사격
            UpdateTailCollision(deltaTime); // 자기 충돌
            UpdateDetachedTailGroups(deltaTime); // 분리 꼬리 갱신
            PrunePath(); // 경로 정리
        }

        private bool AddSegment(GameObject segmentPrefab, bool snapToPath) // 몸통 추가
        {
            if (segments.Count >= MaxSegmentCount)
            {
                return false; // 최대 길이
            }

            Transform segment = CreateSegment(segments.Count, segmentPrefab); // 새 몸통
            if (segment == null)
            {
                return false; // 프리팹 없음
            }

            segments.Add(segment); // 체인 등록
            segmentGroundChecks.Add(GetSegmentGroundCheck(segment)); // 바닥 체크 등록
            segmentRuntimes.Add(GetSegmentRuntime(segment, segments.Count - 1, true)); // 런타임 등록

            if (snapToPath)
            {
                SnapSegmentToPath(segment, segments.Count); // 끝 위치 정렬
            }

            NotifySegmentCountChanged(); // 길이 변경 알림
            return true; // 추가 성공
        }

        public bool TryAddSegment() // 외부 추가 입구
        {
            return AddSegment(SegmentPrefab, true); // 기본 정렬 추가
        }

        public bool TryAddSegment(GameObject segmentPrefab) // 프리팹 지정 추가 입구
        {
            return AddSegment(segmentPrefab, true); // 지정 세그먼트 추가
        }

        public int AddSegments(int count, bool snapToPath) // 여러 세그먼트 추가
        {
            int added = 0; // 추가 수
            int targetCount = Mathf.Max(0, count); // 음수 방지
            for (int i = 0; i < targetCount; i++)
            {
                if (!AddSegment(SegmentPrefab, snapToPath))
                {
                    break; // 더 이상 추가 불가
                }

                added++; // 성공 누적
            }

            return added; // 실제 추가 수
        }

        public bool CanAddSegmentPrefab(GameObject segmentPrefab) // 지정 프리팹 추가 가능
        {
            return segments.Count < MaxSegmentCount && segmentPrefab != null; // 길이와 프리팹 확인
        }

        public int GetSegmentCount() // 외부 길이 조회
        {
            return segments.Count; // 현재 연결 길이
        }

        public void RemoveSegment() // 몸통 제거
        {
            if (segments.Count <= 1)
            {
                return; // 최소 길이
            }

            int index = segments.Count - 1; // 마지막 순번
            Transform segment = segments[index]; // 마지막 몸통
            segments.RemoveAt(index); // 체인 해제
            RemoveSegmentGroundCheck(index); // 바닥 체크 해제
            RemoveSegmentRuntime(index); // 런타임 해제

            if (segment != null)
            {
                DestroyUnityObject(segment.gameObject); // 오브젝트 제거
            }

            NotifySegmentCountChanged(); // 길이 변경 알림
        }

        public void ResetWorm() // 위치 리셋
        {
            transform.SetPositionAndRotation(startPosition, startRotation); // 시작 pose
            transform.position = SnapHeadToGround(transform.position); // 머리 바닥 보정
            currentTurnVelocity = 0f; // 회전 초기화
            currentTurnInput = 0f; // 입력 초기화
            currentForwardSpeed = GetAutoForwardSpeed(); // 속도 복구
            tailCutCooldownRemaining = 0f; // 절단 쿨 초기화
            ClearDetachedTailGroups(); // 분리 꼬리 제거
            SyncSegmentRuntimes(true); // 런타임 보정
            ResetPath(); // 경로 재생성
            SnapSegmentsToPath(); // 몸통 정렬
            UpdateHeadVisual(1f); // 머리 정렬
        }

        public void SetControlMode(ConvoyControlMode mode) // 모드 변경
        {
            if (ControlMode == mode)
            {
                return; // 같은 모드
            }

            ControlMode = mode; // 모드 적용
            currentTurnVelocity = 0f; // 관성 제거
            currentTurnInput = 0f; // 입력 제거
        }

        private void NotifySegmentCountChanged() // 길이 변경 알림
        {
            SegmentCountChanged?.Invoke(segments.Count); // 현재 길이 전달
        }

        private float GetEffectiveTurnSpeed() // 성장 반영 회전력
        {
            CoreStatData stats = CoreStatProvider.GetCurrentOrDefault(); // 코어 성장값
            return Mathf.Max(1f, TurnSpeed + stats.TurnSpeedBonus); // 보너스 적용
        }

        private float GetEffectiveRejoinAreaRadius() // 성장 반영 재결합 반경
        {
            CoreStatData stats = CoreStatProvider.GetCurrentOrDefault(); // 코어 성장값
            return Mathf.Max(0.1f, RejoinAreaRadius + stats.RejoinRangeBonus); // 보너스 적용
        }





        private static float ExpLerp(float current, float target, float sharpness, float deltaTime) // 지수 보간
        {
            return Mathf.Lerp(current, target, ExpLerpFactor(sharpness, deltaTime)); // 값 보간
        }

        private static float ExpLerpFactor(float sharpness, float deltaTime) // 보간 계수
        {
            return 1f - Mathf.Exp(-sharpness * deltaTime); // 프레임 독립
        }

        private static void DestroyUnityObject(UnityEngine.Object target) // Unity 제거
        {
            if (target == null)
            {
                return; // 대상 없음
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target); // PlayMode 제거
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target); // Editor 제거
            }
        }

        private void OnDrawGizmosSelected() // 경로 gizmo
        {
            Gizmos.color = new Color(1f, 1f, 1f, 0.35f); // 경로 색

            for (int i = 1; i < path.Count; i++)
            {
                Gizmos.DrawLine(path[i - 1], path[i]); // 샘플 연결
            }
        }

        private struct WormInput // 입력 묶음
        {
            public float Turn; // 좌우 턴
            public Vector2 Move; // 목표 방향
            public bool AddSegment; // 테스트 추가
            public bool RemoveSegment; // 테스트 제거
            public bool Reset; // 리셋
            public bool HasMouseWorld; // 마우스 유효
            public Vector3 MouseWorld; // 마우스 위치
        }

        private sealed class DetachedTailGroup // 분리 꼬리 묶음
        {
            public readonly Transform Root; // 그룹 루트
            public readonly List<Transform> Segments = new List<Transform>(32); // 포함 몸통
            public Transform RejoinArea; // 재결합 영역
            public LineRenderer RejoinLine; // 재결합 원
            public Vector3 RejoinCenter; // 재결합 중심
            public float Age; // 분리 시간
            public float SettledTime; // 안착 시간
            public bool RejoinReady; // 재결합 가능

            public DetachedTailGroup(Transform root) // 생성자
            {
                Root = root; // 루트 저장
            }
        }
    }
}

