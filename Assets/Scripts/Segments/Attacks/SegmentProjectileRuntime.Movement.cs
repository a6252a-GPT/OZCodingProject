using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class SegmentProjectileRuntime
    {
        private void UpdateStraightProjectile() // 직선 이동
        {
            transform.position += direction * (profile.ProjectileSpeed * Time.deltaTime); // 이동
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 방향
            }

            TryApplyHitAt(transform.position); // 명중 확인
        }

        private void UpdateHomingProjectile() // 추적 이동
        {
            if (target != null)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * profile.TargetAimHeight; // 목표 중심
                Vector3 offset = targetPosition - transform.position; // 목표 방향
                if (offset.sqrMagnitude > 0.0001f)
                {
                    direction = offset.normalized; // 방향 갱신
                }
            }

            UpdateStraightProjectile(); // 이동 공유
        }
    }
}
