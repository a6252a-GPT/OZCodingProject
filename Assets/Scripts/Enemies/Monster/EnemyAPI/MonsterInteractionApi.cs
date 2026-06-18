using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    ////// 전찬우추가 - 몬스터/컨보이/월드 상호작용을 한 파일에서 찾을 수 있게 만든 공용 API입니다.
    ////// 전찬우추가 - 소유권은 Core/Convoy 쪽이고, 몬스터 담당자는 이 API를 통해 요청만 합니다.
    ////// 전찬우추가 - 몬스터 코드는 ConvoyController, SegmentBlocker를 직접 호출하지 않는 것을 기준으로 합니다.
    public static class MonsterInteractionApi
    {
        ////// 전찬우추가 - 자폭/폭발 몬스터가 컨보이에게 요청하는 넉백 데이터를 저장하는 내부 구조체입니다.
        private struct KnockbackRequest
        {
            public Vector3 Center; // 전찬우추가 - 폭발 또는 넉백 발생 중심 위치입니다.
            public float Radius; // 전찬우추가 - 넉백이 적용되는 반경입니다.
            public float Distance; // 전찬우추가 - 컨보이가 밀려날 거리입니다.
            public float Duration; // 전찬우추가 - 컨보이가 밀려나는 시간입니다.
            public float Height; // 전찬우추가 - 컨보이가 공중으로 뜨는 높이입니다.
            public float ExpireTime; // 전찬우추가 - 요청이 소비되지 않았을 때 자동 폐기되는 시간입니다.
        }

        private static readonly List<KnockbackRequest> knockbackRequests = new List<KnockbackRequest>(16); // 전찬우추가 - 아직 컨보이가 소비하지 않은 넉백 요청 목록입니다.
        private static Transform convoyTarget; // 전찬우추가 - 현재 플레이어 컨보이 Transform입니다. ConvoyController가 등록하고 몬스터는 조회만 합니다.

        ////// 전찬우추가 - Target 섹션: 몬스터가 GameObject.Find("PlayerConvoy")를 직접 쓰지 않도록 합니다.
        public static void RegisterConvoyTarget(Transform target)
        {
            if (target == null) // 전찬우추가 - 잘못된 타겟 등록 요청은 무시합니다.
            {
                return; // 전찬우추가 - null을 저장하지 않고 종료합니다.
            }

            convoyTarget = target; // 전찬우추가 - 현재 컨보이 타겟으로 등록합니다.
        }

        public static void UnregisterConvoyTarget(Transform target)
        {
            if (convoyTarget == target) // 전찬우추가 - 현재 등록된 컨보이와 해제 요청 대상이 같을 때만 처리합니다.
            {
                convoyTarget = null; // 전찬우추가 - 비활성화된 컨보이를 몬스터가 계속 추적하지 않게 비웁니다.
            }
        }

        public static bool TryGetConvoyTarget(out Transform target)
        {
            if (convoyTarget != null && convoyTarget.gameObject.activeInHierarchy) // 전찬우추가 - 등록된 컨보이가 있고 씬에서 활성 상태인지 확인합니다.
            {
                target = convoyTarget; // 전찬우추가 - 몬스터가 사용할 컨보이 Transform을 반환합니다.
                return true; // 전찬우추가 - 타겟 조회 성공을 알립니다.
            }

            target = null; // 전찬우추가 - 사용할 수 있는 컨보이 타겟이 없으면 null을 반환합니다.
            return false; // 전찬우추가 - 타겟 조회 실패를 알립니다.
        }

        ////// 전찬우추가 - Convoy Motion 섹션: 몬스터는 이동 효과를 요청하고 실제 적용은 ConvoyController가 합니다.
        public static void RequestConvoyKnockback(Vector3 center, float radius, float distance, float duration)
        {
            RequestConvoyKnockback(center, radius, distance, duration, 0.0f); // 전찬우추가 - 높이가 없는 기존 넉백 요청은 height 0으로 처리합니다.
        }

        public static void RequestConvoyKnockback(Vector3 center, float radius, float distance, float duration, float height)
        {
            center.y = 0.0f; // 전찬우추가 - 넉백 범위 판정은 바닥 평면 기준으로 맞춥니다.

            KnockbackRequest request = new KnockbackRequest(); // 전찬우추가 - 이번 프레임에 등록할 넉백 요청 데이터를 만듭니다.
            request.Center = center; // 전찬우추가 - 폭발 중심 위치를 저장합니다.
            request.Radius = Mathf.Max(0.1f, radius); // 전찬우추가 - 반경은 최소 0.1 이상으로 보정합니다.
            request.Distance = Mathf.Max(0.0f, distance); // 전찬우추가 - 밀림 거리는 음수가 되지 않게 보정합니다.
            request.Duration = Mathf.Max(0.01f, duration); // 전찬우추가 - 지속 시간은 0이 되지 않게 보정합니다.
            request.Height = Mathf.Max(0.0f, height); // 전찬우추가 - 공중 높이는 음수가 되지 않게 보정합니다.
            request.ExpireTime = Time.time + 0.25f; // 전찬우추가 - 짧은 시간 안에 소비되지 않은 요청은 버리기 위해 만료 시간을 저장합니다.

            knockbackRequests.Add(request); // 전찬우추가 - 컨보이가 Update에서 소비할 수 있도록 요청 목록에 추가합니다.
        }

        public static bool TryConsumeConvoyKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration)
        {
            float height; // 전찬우추가 - 높이를 사용하지 않는 호출부를 위해 임시 변수로 받습니다.
            return TryConsumeConvoyKnockback(targetPosition, out direction, out distance, out duration, out height); // 전찬우추가 - height 포함 버전을 호출하고 height만 버립니다.
        }

        public static bool TryConsumeConvoyKnockback(Vector3 targetPosition, out Vector3 direction, out float distance, out float duration, out float height)
        {
            targetPosition.y = 0.0f; // 전찬우추가 - 컨보이 위치도 바닥 평면 기준으로 맞춥니다.

            for (int i = knockbackRequests.Count - 1; i >= 0; i--) // 전찬우추가 - 제거가 쉬운 뒤쪽부터 넉백 요청을 검사합니다.
            {
                KnockbackRequest request = knockbackRequests[i]; // 전찬우추가 - 현재 검사할 넉백 요청입니다.

                if (Time.time > request.ExpireTime) // 전찬우추가 - 이미 만료된 요청인지 확인합니다.
                {
                    knockbackRequests.RemoveAt(i); // 전찬우추가 - 만료된 요청은 목록에서 제거합니다.
                    continue; // 전찬우추가 - 다음 요청을 검사합니다.
                }

                Vector3 offset = targetPosition - request.Center; // 전찬우추가 - 폭발 중심에서 컨보이까지의 방향과 거리를 구합니다.
                offset.y = 0.0f; // 전찬우추가 - 높이 차이는 넉백 판정에서 제외합니다.

                if (offset.sqrMagnitude > request.Radius * request.Radius) // 전찬우추가 - 컨보이가 넉백 반경 밖에 있는지 확인합니다.
                {
                    continue; // 전찬우추가 - 범위 밖 요청은 적용하지 않고 다음 요청으로 넘어갑니다.
                }

                direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector3.forward; // 전찬우추가 - 폭발 중심에서 바깥쪽으로 밀 방향을 정합니다.
                distance = request.Distance; // 전찬우추가 - 컨보이에 적용할 밀림 거리를 반환합니다.
                duration = request.Duration; // 전찬우추가 - 컨보이에 적용할 밀림 시간을 반환합니다.
                height = request.Height; // 전찬우추가 - 컨보이에 적용할 공중 높이를 반환합니다.

                knockbackRequests.RemoveAt(i); // 전찬우추가 - 한 번 적용한 요청은 중복 적용되지 않게 제거합니다.
                return true; // 전찬우추가 - 컨보이가 받을 넉백 요청이 있음을 알립니다.
            }

            direction = Vector3.zero; // 전찬우추가 - 적용할 넉백 방향이 없을 때 기본값입니다.
            distance = 0.0f; // 전찬우추가 - 적용할 넉백 거리가 없을 때 기본값입니다.
            duration = 0.0f; // 전찬우추가 - 적용할 넉백 시간이 없을 때 기본값입니다.
            height = 0.0f; // 전찬우추가 - 적용할 넉백 높이가 없을 때 기본값입니다.
            return false; // 전찬우추가 - 소비할 넉백 요청이 없음을 알립니다.
        }

        public static void ClearConvoyKnockbackRequests()
        {
            knockbackRequests.Clear(); // 전찬우추가 - 씬 종료나 컨보이 비활성화 시 남은 넉백 요청을 모두 비웁니다.
        }

        ////// 전찬우추가 - Slow 섹션: 컨보이는 EnemySlowZone 내부 목록을 직접 알지 않고 속도 배율만 조회합니다.
        public static float GetConvoySpeedMultiplier(Vector3 convoyPosition)
        {
            return EnemySlowZone.GetSpeedMultiplier(convoyPosition); // 전찬우추가 - 현재 위치에 적용될 가장 강한 슬로우 배율을 반환합니다.
        }

        ////// 전찬우추가 - World Blocker 섹션: 이동 보정은 이 API를 통해서만 조회하게 만듭니다.
        public static Vector3 ResolveConvoyPosition(Vector3 currentPosition, Vector3 desiredPosition, float moverRadius)
        {
            return EnemyObstacle.ResolvePosition(currentPosition, desiredPosition, moverRadius); // 전찬우추가 - 컨보이가 적 장애물과 겹치지 않게 위치를 보정합니다.
        }

        public static Vector3 ResolveMonsterPosition(Vector3 currentPosition, Vector3 desiredPosition, float monsterRadius)
        {
            return SegmentBlocker.ResolveMonsterPosition(currentPosition, desiredPosition, monsterRadius); // 전찬우추가 - 몬스터가 컨보이 세그먼트와 겹치지 않게 위치를 보정합니다.
        }
    }
}
