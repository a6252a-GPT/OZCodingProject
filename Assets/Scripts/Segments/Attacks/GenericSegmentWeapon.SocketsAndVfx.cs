using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class GenericSegmentWeapon
    {
        private Vector3 GetFireDirection(EnemyController target, Vector3 spawnPosition) // 발사 방향
        {
            if (target != null)
            {
                Vector3 targetPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 목표 중심
                Vector3 direction = targetPosition - spawnPosition; // 포구 -> 목표
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction.normalized; // 목표 방향
                }
            }

            Transform muzzle = ResolveMuzzle(); // 포구 fallback
            return muzzle != null ? muzzle.forward : transform.forward; // 현재 방향
        }

        private bool AimHeadAtTarget(EnemyController target, float deltaTime) // 머리 조준
        {
            Transform pivot = ResolveHeadYawPivot(); // 회전축
            if (pivot == null || target == null)
            {
                return true; // 회전축 없음
            }

            Transform muzzle = ResolveMuzzle(); // 포구
            if (!TryGetHorizontalAim(target, pivot, muzzle, out Vector3 currentDirection, out Vector3 targetDirection))
            {
                return true; // 방향 없음
            }

            float signedAngle = Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up); // 목표 각도
            float maxStep = AttackProfile.HeadTurnSpeed * deltaTime; // 회전량
            float step = Mathf.Clamp(signedAngle, -maxStep, maxStep); // 과회전 방지
            pivot.Rotate(Vector3.up, step, Space.World); // 회전

            if (!TryGetHorizontalAim(target, pivot, muzzle, out currentDirection, out targetDirection))
            {
                return true; // 방향 없음
            }

            return Mathf.Abs(Vector3.SignedAngle(currentDirection, targetDirection, Vector3.up)) <= AttackProfile.FireAngleTolerance; // 조준 완료
        }

        private bool TryGetHorizontalAim(EnemyController target, Transform pivot, Transform muzzle, out Vector3 currentDirection, out Vector3 targetDirection) // 수평 조준
        {
            currentDirection = Vector3.zero; // 현재 방향
            targetDirection = Vector3.zero; // 목표 방향
            Vector3 aimOrigin = muzzle != null ? muzzle.position : pivot.position; // 포구 우선
            Vector3 targetPosition = target.transform.position + Vector3.up * AttackProfile.TargetAimHeight; // 목표 중심
            targetDirection = targetPosition - aimOrigin; // 목표 벡터
            targetDirection.y = 0f; // 수평 회전만
            if (targetDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 방향 없음
            }

            currentDirection = GetCurrentMuzzleDirection(pivot, muzzle); // 투석기는 머즐 위치가 아닌 머즐 방향 기준 조준
            if (currentDirection.sqrMagnitude <= 0.0001f)
            {
                return false; // 기준 없음
            }

            currentDirection.Normalize(); // 정규화
            targetDirection.Normalize(); // 정규화
            return true; // 계산 가능
        }

        private Vector3 GetCurrentMuzzleDirection(Transform pivot, Transform muzzle) // 현재 포신 방향
        {
            if (ShouldAimByMuzzleForward())
            {
                Vector3 muzzleForwardDirection = GetTrebuchetAimDirection(muzzle, pivot); // SG03은 머즐 X축을 정면 조준축으로 사용
                if (muzzleForwardDirection.sqrMagnitude > 0.0001f)
                {
                    return muzzleForwardDirection; // 머즐 Z+ 방향을 조준 기준으로 사용
                }
            }

            if (muzzle != null)
            {
                Vector3 pivotToMuzzle = muzzle.position - pivot.position; // 피벗 -> 포구
                pivotToMuzzle.y = 0f; // 수평
                if (pivotToMuzzle.sqrMagnitude > 0.0001f)
                {
                    return pivotToMuzzle; // 모델 기준
                }

                Vector3 muzzleForward = muzzle.forward; // 포구 방향
                muzzleForward.y = 0f;
                if (muzzleForward.sqrMagnitude > 0.0001f)
                {
                    return muzzleForward;
                }
            }

            Vector3 pivotForward = pivot.forward; // 피벗 방향
            pivotForward.y = 0f;
            return pivotForward;
        }

        // 투석기처럼 머즐 위치와 조준 정면이 다른 무기는 머즐 방향으로 조준한다.
        private bool ShouldAimByMuzzleForward()
        {
            return ResolveTrebuchetFireMotion() != null; // SG03 투석기 전용 보정
        }

        // 투석기 머즐 X+ 방향을 수평 조준 벡터로 변환
        private static Vector3 GetTrebuchetAimDirection(Transform primary, Transform fallback)
        {
            if (primary != null)
            {
                Vector3 direction = primary.right; // 투석기 머즐의 빨간 X축
                direction.y = 0f; // 좌우 회전만 사용
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return direction; // 머즐 X축 방향
                }
            }

            Vector3 fallbackDirection = fallback != null ? fallback.forward : Vector3.zero; // 피벗 fallback
            fallbackDirection.y = 0f; // 수평화
            return fallbackDirection; // 결과
        }

        private Transform ResolveHeadYawPivot() // 회전축 찾기
        {
            if (HeadYawPivot != null)
            {
                return HeadYawPivot; // 수동 연결
            }

            Transform root = Segment != null ? Segment.transform : transform; // 검색 루트
            HeadYawPivot = FindChildRecursive(root, "YawPivot"); // 머리 프리팹 기준 회전축
            if (HeadYawPivot == null)
            {
                HeadYawPivot = FindChildRecursive(root, "Joint_HeadMount"); // 기존 조립 기준 fallback
            }

            if (HeadYawPivot == null)
            {
                HeadYawPivot = FindChildRecursive(root, "Joint"); // 구버전 fallback
            }

            return HeadYawPivot;
        }

        private Transform ResolveMuzzle() // 포구 찾기
        {
            if (Muzzle != null)
            {
                return Muzzle; // 수동 연결
            }

            Transform pivot = ResolveHeadYawPivot(); // 회전축
            Transform root = pivot != null ? pivot : (Segment != null ? Segment.transform : transform); // 검색 루트
            Muzzle = FindChildRecursive(root, "Muzzle"); // 포구
            return Muzzle;
        }

        private Transform ResolveMuzzleVfxSocket(Transform muzzle) // 발사 VFX 기준점
        {
            if (MuzzleVfxSocket != null)
            {
                return MuzzleVfxSocket; // 수동 연결
            }

            Transform root = muzzle != null ? muzzle : ResolveMuzzle(); // 포구 기준
            MuzzleVfxSocket = FindChildRecursive(root, "VFX_Muzzle"); // 정식 이름
            if (MuzzleVfxSocket == null)
            {
                MuzzleVfxSocket = FindChildRecursive(root, "MuzzleVFX"); // fallback
            }

            return MuzzleVfxSocket;
        }


        private void PlayMuzzleVfx(Transform muzzle) // 발사 VFX
        {
            Transform socket = ResolveMuzzleVfxSocket(muzzle); // 기준점
            Vector3 position = socket != null ? socket.position : (muzzle != null ? muzzle.position : transform.position + Vector3.up * AttackProfile.AttackSpawnHeight); // 위치
            Quaternion rotation = socket != null ? socket.rotation : (muzzle != null ? muzzle.rotation : transform.rotation); // 방향
            SegmentAttackVfxPlayer.Play(AttackProfile.MuzzleVfxPrefab, position, rotation, AttackProfile.MuzzleVfxLifetime); // 공용 생성
        }

        private void PlayHitVfx(Vector3 position) // 명중 VFX
        {
            SegmentAttackVfxPlayer.Play(AttackProfile.HitVfxPrefab, position, Quaternion.identity, AttackProfile.HitVfxLifetime); // 공용 생성
        }

        private static Transform FindDirectChild(Transform root, string childName)
        {
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return null; // 검색 불가
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i); // 직계 자식
                if (child.name == childName)
                {
                    return child; // 발견
                }
            }

            return null; // 없음
        }

        // 특정 하위 오브젝트를 포함하는 직계 자식 찾기
        private static Transform FindDirectChildContaining(Transform root, Transform descendant)
        {
            if (root == null || descendant == null)
            {
                return null; // 검색 불가
            }

            Transform current = descendant; // 시작점
            while (current != null && current.parent != null && current.parent != root)
            {
                current = current.parent; // 직계 자식까지 상승
            }

            return current != null && current.parent == root ? current : null; // 직계 자식이면 반환
        }

        private static Transform FindChildRecursive(Transform root, string childName) // 이름 검색
        {
            if (root == null)
            {
                return null; // 검색 불가
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i); // 하위
                if (child.name == childName)
                {
                    return child; // 발견
                }

                Transform found = FindChildRecursive(child, childName); // 재귀
                if (found != null)
                {
                    return found; // 발견
                }
            }

            return null; // 없음
        }
    }
}
