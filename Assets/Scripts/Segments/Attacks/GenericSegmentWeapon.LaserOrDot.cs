using System.Collections;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class GenericSegmentWeapon
    {
        private void FireLaser(EnemyController target, DamageData damage) // 레이저 공격
        {
            if (laserRoutine != null)
            {
                StopCoroutine(laserRoutine); // 이전 지속 피해 정리
            }

            laserRoutine = StartCoroutine(ApplyLaserDamage(target, damage)); // 지속 피해 시작
        }

        private IEnumerator ApplyLaserDamage(EnemyController target, DamageData damage) // 레이저 지속 피해
        {
            float timer = Mathf.Max(0.05f, AttackProfile.LaserDuration); // 지속 시간
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.02f, AttackProfile.LaserTickInterval)); // 피해 간격
            while (timer > 0f && target != null)
            {
                Vector3 hitPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 명중 위치
                if (Vector3.Distance(transform.position, target.transform.position) <= GetUpgrade().ApplyRange(AttackProfile.SearchRange) && IsTargetInAttackArea(target)) // 레이저 지속 피해도 공격 범위 형태 유지
                {
                    target.ApplyDamage(damage.WithHitPosition(hitPosition)); // 지속 피해
                    PlayHitVfx(hitPosition); // 명중 VFX
                }

                timer -= AttackProfile.LaserTickInterval; // 시간 감소
                yield return wait;
            }

            laserRoutine = null; // 종료
        }
    }
}
