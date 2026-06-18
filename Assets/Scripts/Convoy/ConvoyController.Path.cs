using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class ConvoyController
    {
        private void ResetPath() // 경로 초기화
        {
            path.Clear(); // 기존 경로 제거

            float starterExtraDistance = EnableStarterSegment ? Mathf.Max(0f, StarterSegmentDistanceBehindHead - SegmentSpacing) : 0f; // 스타터 추가 거리
            float requiredDistance = Mathf.Max((MaxSegmentCount + 4) * SegmentSpacing + starterExtraDistance, 24f); // 필요 길이
            float sampleStep = Mathf.Max(MinPathSampleDistance * 4f, 0.25f); // 초기 간격

            for (float distance = requiredDistance; distance >= 0f; distance -= sampleStep)
            {
                path.Add(transform.position - transform.forward * distance); // 뒤쪽 경로
            }

            if (path.Count == 0 || Vector3.Distance(path[path.Count - 1], transform.position) > 0.001f)
            {
                path.Add(transform.position); // 현재 위치
            }
        }

        private void SamplePathIfNeeded() // 경로 샘플
        {
            if (path.Count == 0)
            {
                path.Add(transform.position); // 첫 샘플
                return; // 완료
            }

            Vector3 last = path[path.Count - 1]; // 마지막 샘플
            if (Vector3.Distance(last, transform.position) >= MinPathSampleDistance)
            {
                path.Add(transform.position); // 새 샘플
            }
        }

        private void UpdateHeadVisual(float deltaTime) // 머리 표시
        {
            if (HeadVisual == null)
            {
                return; // 표시 없음
            }

            HeadVisual.localPosition = Vector3.Lerp(
                HeadVisual.localPosition,
                new Vector3(0f, VisualCenterHeight, 0f),
                ExpLerpFactor(18f, deltaTime)); // 높이 보간

            Quaternion targetRotation = Quaternion.Euler(0f, 0f, -currentTurnInput * HeadVisualLean); // 기울기
            HeadVisual.localRotation = Quaternion.Slerp(
                HeadVisual.localRotation,
                targetRotation,
                ExpLerpFactor(18f, deltaTime)); // 회전 보간
        }

        private void UpdateSegments(float deltaTime) // 몸통 추적
        {
            SyncSegmentGroundChecks(); // 체크 목록 보정
            float moveFactor = ExpLerpFactor(SegmentFollowResponse, deltaTime); // 위치 보간값
            float turnFactor = ExpLerpFactor(SegmentTurnResponse, deltaTime); // 회전 보간값

            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i]; // 현재 몸통
                if (segment == null)
                {
                    continue; // 삭제됨
                }

                GetPoseBehindHead(GetSegmentDistanceBehindHead(i), out Vector3 targetPosition, out Vector3 targetForward);
                targetPosition = SnapSegmentToGround(i, targetPosition); // 몸통 바닥 유지

                segment.position = Vector3.Lerp(segment.position, targetPosition, moveFactor); // 위치 추적

                if (targetForward.sqrMagnitude > 0.0001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(targetForward, Vector3.up); // 목표 회전
                    segment.rotation = Quaternion.Slerp(segment.rotation, targetRotation, turnFactor); // 회전 추적
                }
            }
        }

        private void SnapSegmentsToPath() // 몸통 즉시 정렬
        {
            SyncSegmentGroundChecks(); // 체크 목록 보정
            for (int i = 0; i < segments.Count; i++)
            {
                SnapSegmentToPath(segments[i], i); // 순번별 배치
            }
        }

        private void SnapSegmentToPath(Transform segment, int segmentIndex) // 단일 몸통 정렬
        {
            if (segment == null)
            {
                return; // 대상 없음
            }

            GetPoseBehindHead(GetSegmentDistanceBehindHead(segmentIndex), out Vector3 targetPosition, out Vector3 targetForward);
            targetPosition = SnapSegmentToGround(segmentIndex, targetPosition); // 몸통 바닥 유지
            segment.SetPositionAndRotation(targetPosition, Quaternion.LookRotation(targetForward, Vector3.up)); // pose 적용
        }

        private float GetSegmentDistanceBehindHead(int segmentIndex) // 머리에서 세그먼트까지의 경로 거리
        {
            int safeIndex = Mathf.Max(0, segmentIndex); // 음수 방지
            if (HasActiveStarterSegment)
            {
                float starterDistance = Mathf.Max(0.1f, StarterSegmentDistanceBehindHead); // 스타터 전용 간격
                if (safeIndex == 0)
                {
                    return starterDistance; // 스타터는 머리에서 조금 더 떨어진다.
                }

                return starterDistance + safeIndex * SegmentSpacing; // 스타터 뒤는 기존 간격 유지
            }

            return (safeIndex + 1) * SegmentSpacing; // 일반 체인 기존 규칙
        }

        private void GetPoseBehindHead(float distanceBehindHead, out Vector3 position, out Vector3 forward) // 뒤쪽 pose
        {
            Vector3 previous = transform.position; // 시작점
            float accumulated = 0f; // 누적 거리

            for (int i = path.Count - 1; i >= 0; i--)
            {
                Vector3 current = path[i]; // 경로 점
                float length = Vector3.Distance(previous, current); // 구간 길이

                if (length <= 0.0001f)
                {
                    previous = current; // 중복점 스킵
                    continue; // 다음 점
                }

                if (accumulated + length >= distanceBehindHead)
                {
                    float t = (distanceBehindHead - accumulated) / length; // 구간 비율
                    position = Vector3.Lerp(previous, current, t); // 보간 위치
                    forward = (previous - current).normalized; // 진행 방향
                    return; // pose 확정
                }

                accumulated += length; // 거리 누적
                previous = current; // 이전점 이동
            }

            float remaining = distanceBehindHead - accumulated; // 부족 거리
            forward = transform.forward; // fallback 방향
            position = previous - forward * remaining; // 외삽 위치
        }

        private void PrunePath() // 경로 제한
        {
            int overflow = path.Count - PathSampleLimit; // 초과 수
            if (overflow > 0)
            {
                path.RemoveRange(0, overflow); // 오래된 경로 제거
            }
        }
    }
}
