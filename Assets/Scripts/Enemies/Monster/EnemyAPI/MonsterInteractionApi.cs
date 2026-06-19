// 전찬우생성
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public static class MonsterInteractionApi
    {
        private struct KnockbackRequest
        {
            public Vector3 Center;
            public float Radius;
            public float Distance;
            public float Duration;
            public float Height;
            public float ExpireTime;
        }

        private static readonly List<KnockbackRequest> knockbackRequests = new List<KnockbackRequest>(16); // 소비 대기 넉백
        private static Transform convoyTarget; // 컨보이 타겟 캐시

        public static void RegisterConvoyTarget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            convoyTarget = target;
        }

        public static void UnregisterConvoyTarget(Transform target)
        {
            if (convoyTarget == target)
            {
                convoyTarget = null;
            }
        }

        public static bool TryGetConvoyTarget(out Transform target)
        {
            if (convoyTarget != null && convoyTarget.gameObject.activeInHierarchy)
            {
                target = convoyTarget;
                return true;
            }

            target = null;
            return false;
        }

        public static void RequestConvoyKnockback(Vector3 center, float radius, float distance, float duration)
        {
            RequestConvoyKnockback(center, radius, distance, duration, 0.0f);
        }

        public static void RequestConvoyKnockback(Vector3 center, float radius, float distance, float duration, float height)
        {
            center.y = 0.0f; // 바닥 평면 기준

            KnockbackRequest request = new KnockbackRequest();
            request.Center = center;
            request.Radius = Mathf.Max(0.1f, radius);
            request.Distance = Mathf.Max(0.0f, distance);
            request.Duration = Mathf.Max(0.01f, duration);
            request.Height = Mathf.Max(0.0f, height);
            request.ExpireTime = Time.time + 0.25f;

            knockbackRequests.Add(request);
        }

        public static bool TryConsumeConvoyKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration)
        {
            return TryConsumeConvoyKnockback(targetPosition, out direction, out distance, out duration, out _);
        }

        public static bool TryConsumeConvoyKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration, out float height)
        {
            targetPosition.y = 0.0f; // 바닥 평면 기준

            for (int i = knockbackRequests.Count - 1; i >= 0; i--)
            {
                KnockbackRequest request = knockbackRequests[i];

                if (Time.time > request.ExpireTime)
                {
                    knockbackRequests.RemoveAt(i);
                    continue;
                }

                Vector3 offset = targetPosition - request.Center;
                offset.y = 0.0f;

                if (offset.sqrMagnitude > request.Radius * request.Radius)
                {
                    continue;
                }

                direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.forward;
                distance = request.Distance;
                duration = request.Duration;
                height = request.Height;

                knockbackRequests.RemoveAt(i);
                return true;
            }

            direction = Vector3.zero;
            distance = 0.0f;
            duration = 0.0f;
            height = 0.0f;
            return false;
        }

        public static void ClearConvoyKnockbackRequests()
        {
            knockbackRequests.Clear();
        }

        public static float GetConvoySpeedMultiplier(Vector3 convoyPosition)
        {
            return EnemySlowZone.GetSpeedMultiplier(convoyPosition);
        }

        public static Vector3 ResolveConvoyPosition(Vector3 currentPosition, Vector3 desiredPosition, float moverRadius)
        {
            return EnemyObstacle.ResolvePosition(currentPosition, desiredPosition, moverRadius);
        }

        public static Vector3 ResolveMonsterPosition(Vector3 currentPosition, Vector3 desiredPosition, float monsterRadius)
        {
            return SegmentBlocker.ResolveMonsterPosition(currentPosition, desiredPosition, monsterRadius);
        }
    }
}
