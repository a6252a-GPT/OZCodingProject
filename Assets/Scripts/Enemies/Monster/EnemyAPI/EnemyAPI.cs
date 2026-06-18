using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public static class EnemyApi // 플레이어/컨보이가 몬스터 효과를 조회하는 공용 API
    {
        private struct KnockbackRequest // 자폭이나 폭발 몬스터가 남기는 넉백 요청 정보
        {
            public Vector3 Center; // 폭발 중심 위치
            public float Radius; // 폭발 판정 반경
            public float Distance; // 밀려날 거리
            public float Duration; // 밀려나는 시간
            public float Height; // 공중으로 뜨는 높이
            public float ExpireTime; // 요청이 자동 제거될 시간
        }

        private static readonly List<KnockbackRequest> knockbackRequests = new List<KnockbackRequest>(16); // 현재 대기 중인 넉백 요청 목록

        public static Vector3 ResolveObstaclePosition(Vector3 currentPosition, Vector3 desiredPosition, float moverRadius) // 장애물 위치 보정 API
        {
            return EnemyObstacle.ResolvePosition(currentPosition, desiredPosition, moverRadius); // 실제 장애물 Script의 보정 함수를 호출한다.
        }

        public static float GetSlowMultiplier(Vector3 targetPosition) // 슬로우 장판 속도 배율 API
        {
            return EnemySlowZone.GetSpeedMultiplier(targetPosition); // 실제 슬로우 장판 Script의 속도 배율 함수를 호출한다.
        }

        public static void RequestKnockback(Vector3 center, float radius, float distance, float duration) // 기존 수평 넉백 요청 함수
        {
            RequestKnockback(center, radius, distance, duration, 0.0f); // 높이 0으로 처리해서 기존 코드가 그대로 동작하게 한다.
        }

        public static void RequestKnockback(Vector3 center, float radius, float distance, float duration, float height) // 몬스터가 폭발 넉백을 요청하는 함수
        {
            KnockbackRequest request = new KnockbackRequest(); // 새 넉백 요청 데이터를 만든다.

            center.y = 0.0f; // 폭발 판정은 바닥 평면 기준으로 처리한다.

            request.Center = center; // 폭발 중심 위치를 저장한다.
            request.Radius = Mathf.Max(0.1f, radius); // 폭발 반경을 저장한다.
            request.Distance = Mathf.Max(0.0f, distance); // 밀려날 거리를 저장한다.
            request.Duration = Mathf.Max(0.01f, duration); // 밀려나는 시간을 저장한다.
            request.Height = Mathf.Max(0.0f, height); // 공중으로 뜨는 높이를 저장한다.
            request.ExpireTime = Time.time + 0.25f; // 짧은 시간 안에 소비되지 않으면 요청을 버린다.

            knockbackRequests.Add(request); // 넉백 요청 목록에 등록한다.
        }

        public static bool TryConsumeKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration) // 기존 수평 넉백 소비 함수
        {
            float height; // 기존 방식에서는 높이를 사용하지 않는다.
            return TryConsumeKnockback(targetPosition, out direction, out distance, out duration, out height); // 새 함수로 넘기고 height만 버린다.
        }

        public static bool TryConsumeKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration, out float height) // 컨보이가 받을 넉백 요청이 있는지 확인한다.
        {
            targetPosition.y = 0.0f; // 판정은 바닥 평면 기준으로 한다.

            for (int i = knockbackRequests.Count - 1; i >= 0; i--) // 뒤에서부터 넉백 요청 목록을 확인한다.
            {
                KnockbackRequest request = knockbackRequests[i]; // 현재 넉백 요청을 가져온다.

                if (Time.time > request.ExpireTime) // 요청 시간이 지났다면
                {
                    knockbackRequests.RemoveAt(i); // 오래된 요청을 제거한다.
                    continue; // 다음 요청을 확인한다.
                }

                Vector3 offset = targetPosition - request.Center; // 폭발 중심에서 대상까지의 방향과 거리 벡터를 구한다.
                offset.y = 0.0f; // 높이 차이는 제거한다.

                if (offset.sqrMagnitude > request.Radius * request.Radius) // 대상이 폭발 범위 밖이라면
                {
                    continue; // 이 요청은 적용하지 않는다.
                }

                direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.forward; // 폭발 중심에서 바깥쪽으로 밀 방향을 만든다.
                distance = request.Distance; // 밀려날 거리를 반환한다.
                duration = request.Duration; // 밀려나는 시간을 반환한다.
                height = request.Height; // 공중으로 뜨는 높이를 반환한다.

                knockbackRequests.RemoveAt(i); // 한 번 적용한 넉백 요청은 제거한다.
                return true; // 받을 넉백 요청이 있다고 알린다.
            }

            direction = Vector3.zero; // 받을 넉백 방향 없음
            distance = 0.0f; // 받을 넉백 거리 없음
            duration = 0.0f; // 받을 넉백 시간 없음
            height = 0.0f; // 받을 넉백 높이 없음

            return false; // 받을 넉백 요청이 없다.
        }

        public static void ClearKnockbackRequests() // 남아 있는 넉백 요청을 모두 제거한다.
        {
            knockbackRequests.Clear(); // 넉백 요청 목록을 비운다.
        }
    }
}