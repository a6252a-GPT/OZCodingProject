using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class ConvoyController
    {
        private void EnsureHeadVisual() // 머리 보장
        {
            if (HeadVisual != null)
            {
                ApplyMaterial(HeadVisual, HeadMaterial); // 재질 보정
                return; // 기존 사용
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube); // fallback 머리
            visual.name = "ConvoyHeadVisual";
            visual.transform.SetParent(transform, false); // 머리 자식
            visual.transform.localPosition = new Vector3(0f, VisualCenterHeight, 0f); // 표시 높이
            visual.transform.localScale = HeadScale; // 머리 크기
            DestroyUnityObject(visual.GetComponent<Collider>()); // 표시 전용
            HeadVisual = visual.transform; // 참조 저장
            ApplyMaterial(HeadVisual, HeadMaterial); // 재질 적용
        }

        private void EnsureHeadPhysicsCollider() // 머리 물리 보장
        {
            if (!EnableHeadPhysicsCollider || HeadVisual == null)
            {
                return; // 사용 안 함
            }

            BoxCollider collider = HeadVisual.GetComponent<BoxCollider>(); // 머리 콜라이더
            if (collider == null)
            {
                collider = HeadVisual.gameObject.AddComponent<BoxCollider>(); // 충돌체 추가
            }

            collider.enabled = true; // 충돌 사용
            collider.isTrigger = false; // 물리 충돌
            collider.center = Vector3.zero; // 중심 정렬
            collider.size = Vector3.one; // 스케일 기준

            Rigidbody rigidbody = HeadVisual.GetComponent<Rigidbody>(); // 머리 바디
            if (rigidbody == null)
            {
                rigidbody = HeadVisual.gameObject.AddComponent<Rigidbody>(); // 바디 추가
            }

            rigidbody.isKinematic = true; // 이동 스크립트 우선
            rigidbody.useGravity = false; // 중력 제외
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate; // 보간
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // 관통 완화
        }

        private void ConfigureGroundChecks() // 바닥 체크 연결
        {
            if (HeadGroundCheck == null)
            {
                Transform checkTransform = transform.Find("GroundCheck"); // 머리 체크 오브젝트
                HeadGroundCheck = checkTransform != null ? checkTransform.GetComponent<GroundCheck>() : null; // 컴포넌트 연결
            }

            ConfigureGroundCheck(HeadGroundCheck, 0f); // 머리 높이
        }

        private void ConfigureGroundCheck(GroundCheck groundCheck, float offset) // 체크값 설정
        {
            if (groundCheck == null)
            {
                return; // 체크 없음
            }

            groundCheck.GroundOffset = offset; // 바닥 위 높이
            GroundService service = GroundService.Active; // 월드 바닥
            if (service != null && groundCheck.GroundCollider == null)
            {
                groundCheck.GroundCollider = service.GroundCollider; // 씬 바닥 연결
            }
        }

        private void EnsureSegmentRoot() // 몸통 루트 보장
        {
            if (SegmentRoot != null)
            {
                return; // 기존 사용
            }

            GameObject root = new GameObject("ConvoySegments"); // fallback 루트
            SegmentRoot = root.transform; // 참조 저장
        }

        private void EnsureDetachedTailRoot() // 분리 루트 보장
        {
            if (DetachedTailRoot != null)
            {
                return; // 기존 사용
            }

            GameObject root = new GameObject("DetachedTails"); // fallback 루트
            Transform parent = FindWorldParent(); // 배치 기준
            root.transform.SetParent(parent); // 월드 계층
            DetachedTailRoot = root.transform; // 참조 저장
        }

        private void EnsureProjectileRoot() // 투사체 루트 보장
        {
            if (ProjectileRoot != null)
            {
                return; // 기존 사용
            }

            GameObject existing = GameObject.Find("Projectiles"); // 정식 루트 검색
            if (existing != null)
            {
                ProjectileRoot = existing.transform; // 기존 루트 사용
                return; // 완료
            }

            GameObject root = new GameObject("Projectiles"); // fallback 루트
            Transform parent = FindWorldParent(); // 월드 기준
            root.transform.SetParent(parent); // 월드 계층
            ProjectileRoot = root.transform; // 참조 저장
        }

        public Transform GetProjectileRoot() // 투사체 부모 제공
        {
            EnsureProjectileRoot(); // 루트 보장
            return ProjectileRoot; // 현재 루트
        }

        private Transform FindWorldParent() // 월드 부모
        {
            GameObject worldRoot = GameObject.Find("World"); // 월드 루트
            if (worldRoot != null)
            {
                return worldRoot.transform; // 정식 부모
            }

            return SegmentRoot != null ? SegmentRoot.parent : transform.parent; // 기본 부모
        }

        private void CollectExistingSegments() // 배치 몸통 수집
        {
            segments.Clear(); // 목록 초기화
            segmentGroundChecks.Clear(); // 바닥 체크 초기화
            segmentRuntimes.Clear(); // 런타임 초기화

            if (SegmentRoot == null)
            {
                return; // 루트 없음
            }

            for (int i = 0; i < SegmentRoot.childCount; i++)
            {
                Transform child = SegmentRoot.GetChild(i); // 후보 몸통
                if (child.name.StartsWith("ConvoySegment"))
                {
                    segments.Add(child); // 체인 등록
                    segmentGroundChecks.Add(GetSegmentGroundCheck(child)); // 바닥 체크 등록
                    segmentRuntimes.Add(GetSegmentRuntime(child, segments.Count - 1, true)); // 런타임 등록
                    ApplySegmentMaterial(child, i); // 교차 재질
                    DisableAttachedSegmentPhysics(child); // 붙은 몸통 물리 끔
                    EnsureSegmentMonsterBlocker(child); // 몬스터 막기
                }
            }

            SyncSegmentRuntimes(true); // 런타임 보정
        }

        private Transform CreateSegment(int index, GameObject segmentPrefab) // 몸통 생성
        {
            if (segmentPrefab == null || SegmentRoot == null)
            {
                return null; // 프리팹 필요
            }

            GameObject segment = Instantiate(segmentPrefab); // 프리팹 생성
            segment.name = $"ConvoySegment_{index + 1:00}"; // 체인 이름
            segment.transform.SetParent(SegmentRoot, false); // 몸통 루트
            segment.transform.localScale = SegmentScale; // 몸통 크기
            DisableAttachedSegmentPhysics(segment.transform); // 붙은 몸통 상태
            ApplySegmentMaterial(segment.transform, index); // 교차 재질
            ConfigureGroundCheck(GetSegmentGroundCheck(segment.transform), VisualCenterHeight); // 바닥 체크
            GetSegmentRuntime(segment.transform, index, true); // 런타임 연결
            return segment.transform; // 생성 결과
        }

        private void UpdateSegmentWeapons(float deltaTime) // 자동 사격 갱신
        {
            if (!EnableSegmentAutoFire)
            {
                return; // 사용 안 함
            }

            SyncSegmentRuntimes(true); // 런타임 보정

            for (int i = 0; i < segments.Count; i++)
            {
                ConvoySegmentRuntime runtime = segmentRuntimes[i]; // 세그먼트 런타임
                if (runtime != null)
                {
                    runtime.Tick(deltaTime); // 세그먼트 무기
                }
            }
        }

        private void SyncSegmentGroundChecks() // 바닥 체크 길이 보정
        {
            while (segmentGroundChecks.Count < segments.Count)
            {
                segmentGroundChecks.Add(GetSegmentGroundCheck(segments[segmentGroundChecks.Count])); // 부족분 추가
            }

            while (segmentGroundChecks.Count > segments.Count)
            {
                segmentGroundChecks.RemoveAt(segmentGroundChecks.Count - 1); // 초과분 제거
            }

            for (int i = 0; i < segments.Count; i++)
            {
                GroundCheck groundCheck = segmentGroundChecks[i]; // 현재 체크
                Transform segment = segments[i]; // 현재 몸통
                if (groundCheck == null || segment == null || !groundCheck.transform.IsChildOf(segment))
                {
                    segmentGroundChecks[i] = GetSegmentGroundCheck(segment); // 참조 복구
                }
            }
        }

        private void RemoveSegmentGroundCheck(int index) // 단일 체크 제거
        {
            if (index < 0 || index >= segmentGroundChecks.Count)
            {
                return; // 범위 밖
            }

            segmentGroundChecks.RemoveAt(index); // 체크 제거
        }

        private void RemoveSegmentGroundChecks(int index, int count) // 절단 체크 제거
        {
            if (count <= 0 || index < 0 || index >= segmentGroundChecks.Count)
            {
                return; // 제거 없음
            }

            int safeCount = Mathf.Min(count, segmentGroundChecks.Count - index); // 범위 보정
            segmentGroundChecks.RemoveRange(index, safeCount); // 체크 절단
        }

        private void SyncSegmentRuntimes(bool attached) // 런타임 길이 보정
        {
            while (segmentRuntimes.Count < segments.Count)
            {
                int index = segmentRuntimes.Count; // 추가 순번
                segmentRuntimes.Add(GetSegmentRuntime(segments[index], index, attached)); // 부족분 추가
            }

            while (segmentRuntimes.Count > segments.Count)
            {
                segmentRuntimes.RemoveAt(segmentRuntimes.Count - 1); // 초과분 제거
            }

            for (int i = 0; i < segments.Count; i++)
            {
                ConvoySegmentRuntime runtime = segmentRuntimes[i]; // 현재 런타임
                Transform segment = segments[i]; // 현재 몸통
                if (runtime == null || segment == null || runtime.transform != segment)
                {
                    runtime = GetSegmentRuntime(segment, i, attached); // 참조 복구
                    segmentRuntimes[i] = runtime; // 목록 갱신
                }

                if (runtime != null)
                {
                    runtime.Configure(this, i, attached); // 순번 보정
                }
            }
        }

        private void RemoveSegmentRuntime(int index) // 단일 런타임 제거
        {
            if (index < 0 || index >= segmentRuntimes.Count)
            {
                return; // 범위 밖
            }

            ConvoySegmentRuntime runtime = segmentRuntimes[index]; // 제거 대상
            if (runtime != null)
            {
                runtime.SetAttached(false); // 무기 정지
            }

            segmentRuntimes.RemoveAt(index); // 목록 제거
        }

        private void RemoveSegmentRuntimes(int index, int count) // 절단 런타임 제거
        {
            if (count <= 0 || index < 0 || index >= segmentRuntimes.Count)
            {
                return; // 제거 없음
            }

            int safeCount = Mathf.Min(count, segmentRuntimes.Count - index); // 범위 보정
            for (int i = index; i < index + safeCount; i++)
            {
                ConvoySegmentRuntime runtime = segmentRuntimes[i]; // 분리 대상
                if (runtime != null)
                {
                    runtime.SetAttached(false); // 무기 정지
                }
            }

            segmentRuntimes.RemoveRange(index, safeCount); // 런타임 절단
        }

        private ConvoySegmentRuntime GetSegmentRuntime(Transform segment, int index, bool attached) // 런타임 찾기
        {
            if (segment == null)
            {
                return null; // 대상 없음
            }

            ConvoySegmentRuntime runtime = segment.GetComponent<ConvoySegmentRuntime>(); // 루트 런타임
            if (runtime != null)
            {
                runtime.Configure(this, index, attached); // 체인 연결
            }

            return runtime; // 결과 반환
        }

        private GroundCheck GetSegmentGroundCheck(Transform segment) // 몸통 체크 찾기
        {
            if (segment == null)
            {
                return null; // 대상 없음
            }

            GroundCheck groundCheck = segment.GetComponentInChildren<GroundCheck>(true); // 자식 체크
            ConfigureGroundCheck(groundCheck, VisualCenterHeight); // 값 보정
            return groundCheck; // 결과 반환
        }

        private Vector3 SnapHeadToGround(Vector3 position) // 머리 바닥 보정
        {
            if (HeadGroundCheck != null)
            {
                return HeadGroundCheck.Snap(position, 0f); // 체크 기준
            }

            position.y = 0f; // 평면 fallback
            return position; // 결과 반환
        }

        private Vector3 SnapSegmentToGround(int index, Vector3 position) // 몸통 바닥 보정
        {
            GroundCheck groundCheck = index >= 0 && index < segmentGroundChecks.Count ? segmentGroundChecks[index] : null; // 체크 참조
            if (groundCheck != null)
            {
                return groundCheck.Snap(position, VisualCenterHeight); // 체크 기준
            }

            position.y = VisualCenterHeight; // 평면 fallback
            return position; // 결과 반환
        }

        private void DisableAttachedSegmentPhysics(Transform segment) // 붙은 몸통 물리 해제
        {
            if (segment == null)
            {
                return; // 대상 없음
            }

            ClearDetachedSegmentJoints(segment); // 링크 제거
            DestroyUnityObject(segment.GetComponent<Rigidbody>()); // 바디 제거
            DestroyUnityObject(segment.GetComponent<Collider>()); // 콜라이더 제거
            EnsureSegmentMonsterBlocker(segment); // 몬스터 막기
        }

        private void EnsureSegmentMonsterBlocker(Transform segment) // 몬스터 차단 보장
        {
            if (segment == null)
            {
                return; // 대상 없음
            }

            SegmentBlocker blocker = segment.GetComponent<SegmentBlocker>(); // 차단 컴포넌트
            if (blocker == null)
            {
                blocker = segment.gameObject.AddComponent<SegmentBlocker>(); // 차단 추가
            }

            blocker.Configure(TailCollisionRadius); // 반경 적용
        }

        private void ApplySegmentMaterial(Transform segment, int index) // 몸통 재질
        {
            Material material = index % 2 == 0 ? SegmentMaterial : SegmentAltMaterial; // 교차 선택
            ApplyMaterial(segment, material != null ? material : SegmentMaterial); // fallback 포함
        }

        private void ApplyMaterial(Transform target, Material material) // 재질 적용
        {
            if (target == null || material == null)
            {
                return; // 대상 없음
            }

            Renderer renderer = target.GetComponent<Renderer>(); // 표시 renderer
            if (renderer != null)
            {
                renderer.sharedMaterial = material; // 공유 재질
            }
        }
    }
}
