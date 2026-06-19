using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class GenericSegmentWeapon
    {
        private bool TryFindTarget(out EnemyController target) // 대상 탐색
        {
            float range = GetUpgrade().ApplyRange(AttackProfile.SearchRange); // 강화 사거리
            if (AttackProfile.MoveType == SegmentAttackMoveType.SawBounceProjectile)
            {
                return TryFindLockedSawTarget(range, out target); // 톱날은 조준 대상 고정
            }

            ClearSawTargetLock(); // 다른 무기는 톱날 락 해제
            return EnemyController.TryFindNearest(transform.position, range, IsTargetInAttackArea, out target); // 데이터에셋의 공격 범위 형태까지 통과한 가까운 몬스터
        }

        private bool TryFindLockedSawTarget(float range, out EnemyController target) // 톱날 고정 대상 탐색
        {
            if (IsSawTargetLockValid(range))
            {
                target = lockedSawTarget; // 기존 대상 유지
                return true;
            }

            ClearSawTargetLock(); // 무효 대상 해제
            if (!TryFindDistantRandomTarget(transform.position, range, out target))
            {
                return false; // 새 대상 없음
            }

            lockedSawTarget = target; // 새 대상 고정
            return true;
        }

        private bool IsSawTargetLockValid(float range) // 톱날 대상 유지 조건
        {
            if (lockedSawTarget == null)
            {
                return false; // 고정 대상 없음
            }

            Vector3 center = GetEnemyHitPosition(lockedSawTarget); // 대상 중심
            float distance = SegmentTargetQuery.GetHorizontalDistance(transform.position, center); // 수평 거리
            return distance <= range && IsTargetInAttackArea(lockedSawTarget); // 사거리/범위 유지
        }

        private void ClearSawTargetLock() // 톱날 대상 해제
        {
            lockedSawTarget = null; // 다음 검색에서 재선택
        }

        private bool TryFindDistantRandomTarget(Vector3 origin, float range, out EnemyController target) // 중거리~장거리 랜덤 대상
        {
            float aimHeight = AttackProfile != null ? AttackProfile.TargetAimHeight : 0.45f; // 조준 높이
            return SegmentTargetQuery.TryPickMidToLongRandomTarget(origin, range, GetSawTargetMinDistanceRatio(), 0, IsTargetInAttackArea, aimHeight, out target); // 공용 후보 선택
        }

        // 원형/양옆 부채꼴 공격 범위 조건 확인
        private bool IsTargetInAttackArea(EnemyController target)
        {
            if (target == null)
            {
                return false; // 대상 없음
            }

            if (AttackProfile == null || AttackProfile.AttackAreaMode == SegmentAttackAreaMode.FullCircle)
            {
                return true; // 기존 원형 범위는 추가 각도 제한 없음
            }

            if (AttackProfile.AttackAreaMode == SegmentAttackAreaMode.SideCones)
            {
                return IsPositionInSideCones(target.transform.position); // 양옆 부채꼴 판정
            }

            return true; // 새 모드가 추가됐는데 아직 처리 전이면 기존 방식 유지
        }

        // 세그먼트 바디 기준 좌우 각각 SideConeAngle 안에 있는지 확인
        private bool IsPositionInSideCones(Vector3 worldPosition)
        {
            Transform reference = Segment != null ? Segment.transform : transform; // 머리 회전축이 아니라 세그먼트 바디 기준
            return SegmentTargetQuery.IsPositionInSideCones(reference, transform.right, worldPosition, AttackProfile.SideConeAngle); // 공용 부채꼴 판정
        }

        private Vector3 GetEnemyHitPosition(EnemyController enemy) // 몬스터 중심 위치
        {
            float aimHeight = AttackProfile != null ? AttackProfile.TargetAimHeight : 0.45f; // 조준 높이
            return SegmentTargetQuery.GetEnemyHitPosition(enemy, transform.position, aimHeight); // 공용 중심 계산
        }

        private float GetSawTargetMinDistanceRatio() // 톱날 중장거리 후보 기준
        {
            return AttackProfile != null ? Mathf.Clamp01(AttackProfile.SawTargetMinDistanceRatio) : 0.5f; // 기본 절반 이상
        }

    }
}
