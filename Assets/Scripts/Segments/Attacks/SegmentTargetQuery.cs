using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    internal static class SegmentTargetQuery // 세그먼트 타겟 공용 검색
    {
        public static bool TryPickMidToLongRandomTarget(
            Vector3 origin,
            float range,
            float minDistanceRatio,
            int excludedEnemyId,
            Func<EnemyController, bool> isValidTarget,
            float targetAimHeight,
            out EnemyController target) // 중장거리 랜덤 후보
        {
            target = null;
            if (range <= 0f)
            {
                return false; // 사거리 없음
            }

            List<TargetCandidate> candidates = new List<TargetCandidate>(); // 전체 후보
            Collider[] hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Collide); // 범위 검색
            float farthestDistance = 0f; // 가장 먼 후보 거리
            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i]; // 충돌체
                if (hit == null)
                {
                    continue; // 빈 콜라이더
                }

                EnemyController enemy = hit.GetComponentInParent<EnemyController>(); // 몬스터
                if (enemy == null || enemy.EnemyId == excludedEnemyId || ContainsCandidate(candidates, enemy.EnemyId))
                {
                    continue; // 대상 아님/제외/중복
                }

                if (isValidTarget != null && !isValidTarget(enemy))
                {
                    continue; // 무기별 범위 조건 실패
                }

                Vector3 center = GetEnemyHitPosition(enemy, origin, targetAimHeight); // 중심 위치
                float distance = GetHorizontalDistance(origin, center); // 수평 거리
                candidates.Add(new TargetCandidate(enemy, enemy.EnemyId, distance)); // 후보 등록
                farthestDistance = Mathf.Max(farthestDistance, distance); // 최대 거리 갱신
            }

            if (candidates.Count == 0)
            {
                return false; // 후보 없음
            }

            List<TargetCandidate> pickSource = FilterMidToLongCandidates(candidates, farthestDistance, minDistanceRatio); // 중장거리 우선
            int index = UnityEngine.Random.Range(0, pickSource.Count); // 균등 랜덤
            target = pickSource[index].Enemy; // 선택
            return target != null;
        }

        public static Vector3 GetEnemyHitPosition(EnemyController enemy, Vector3 fallbackPosition, float targetAimHeight) // 몬스터 중심
        {
            if (enemy == null)
            {
                return fallbackPosition; // fallback
            }

            Collider targetCollider = enemy.GetComponentInChildren<Collider>();
            if (targetCollider != null)
            {
                return targetCollider.bounds.center; // 콜라이더 중심
            }

            return enemy.transform.position + Vector3.up * targetAimHeight; // 높이 보정
        }

        public static bool IsPositionInSideCones(Transform reference, Vector3 fallbackRight, Vector3 worldPosition, float sideConeAngle) // 좌우 부채꼴
        {
            if (reference == null)
            {
                return true; // 기준 없음
            }

            Vector3 toTarget = worldPosition - reference.position; // 기준 -> 목표
            toTarget.y = 0f; // 수평 판정
            if (toTarget.sqrMagnitude <= 0.0001f)
            {
                return true; // 거의 같은 위치
            }

            Vector3 right = GetHorizontalDirection(reference.right, fallbackRight, Vector3.right); // 오른쪽 중심
            Vector3 targetDirection = toTarget.normalized; // 목표 방향
            float halfAngle = Mathf.Clamp(sideConeAngle, 1f, 180f) * 0.5f; // 한쪽 반각
            return Vector3.Angle(right, targetDirection) <= halfAngle
                || Vector3.Angle(-right, targetDirection) <= halfAngle; // 좌우 중 하나
        }

        public static float GetHorizontalDistance(Vector3 from, Vector3 to) // 수평 거리
        {
            from.y = 0f;
            to.y = 0f;
            return Vector3.Distance(from, to);
        }

        private static List<TargetCandidate> FilterMidToLongCandidates(List<TargetCandidate> candidates, float farthestDistance, float minDistanceRatio) // 중장거리 필터
        {
            float minDistance = farthestDistance * Mathf.Clamp01(minDistanceRatio); // 기준 거리
            List<TargetCandidate> distantCandidates = new List<TargetCandidate>(); // 중장거리 후보
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Distance >= minDistance)
                {
                    distantCandidates.Add(candidates[i]); // 기준 통과
                }
            }

            return distantCandidates.Count > 0 ? distantCandidates : candidates; // fallback
        }

        private static bool ContainsCandidate(List<TargetCandidate> candidates, int enemyId) // 후보 중복 확인
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (candidates[i].Id == enemyId)
                {
                    return true; // 이미 있음
                }
            }

            return false;
        }

        private static Vector3 GetHorizontalDirection(Vector3 primary, Vector3 secondary, Vector3 fallback) // 수평 방향
        {
            primary.y = 0f; // 수평화
            if (primary.sqrMagnitude > 0.0001f)
            {
                return primary.normalized; // 1순위
            }

            secondary.y = 0f; // 수평화
            if (secondary.sqrMagnitude > 0.0001f)
            {
                return secondary.normalized; // 2순위
            }

            fallback.y = 0f; // 수평화
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.right; // 최종
        }

        private readonly struct TargetCandidate // 타겟 후보
        {
            public readonly EnemyController Enemy; // 대상 몬스터
            public readonly int Id; // 중복 방지 ID
            public readonly float Distance; // 기준 거리

            public TargetCandidate(EnemyController enemy, int id, float distance)
            {
                Enemy = enemy; // 대상
                Id = id; // ID
                Distance = distance; // 거리
            }
        }
    }
}
