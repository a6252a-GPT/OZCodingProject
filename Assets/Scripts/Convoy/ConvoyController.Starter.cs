using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class ConvoyController
    {
        private const string StarterSegmentResourceRoot = "StarterSegments/"; // 스타터 세그먼트 경로
        private const string StarterBodyResourceRoot = "StarterBodies/"; // 스타터 바디 경로

        [Header("Starter Segment")]
        public bool EnableStarterSegment; // 선택 지렁이 기본 무기 세그먼트
        public StarterCatalogAsset StarterCatalog; // 지렁이별 시작 무기 데이터에셋
        public GameObject StarterSegmentPrefab; // 공통 fallback 시작 세그먼트
        [Min(0.1f)] public float StarterSegmentDistanceBehindHead = 1.7f; // 머리와 스타터 사이 거리
        [Min(0.1f)] public float StarterSegmentVisualClearanceDistance = 2.7f; // 큰 바디 여유 거리
        public WormStarterSegmentEntry[] StarterSegmentsByWorm; // 지렁이별 시작 세그먼트

        private Transform starterSegment; // 현재 시작 세그먼트
        private GameObject activeStarterSegmentPrefab; // 현재 시작 프리팹
        private string activeStarterWormId; // 현재 시작 지렁이

        private bool HasActiveStarterSegment => starterSegment != null
            && segments.Count > 0
            && segments[0] == starterSegment; // 체인 맨 앞 스타터 여부

        public bool ApplySelectedWormStarterSegment(string wormId) // 타이틀 선택 지렁이 반영
        {
            return EnsureStarterSegment(wormId, true); // 이미 있으면 교체/정렬
        }

        private void EnsureStarterSegmentFromCurrentLoadout() // 런 시작 기본 스타터
        {
            if (!EnableStarterSegment)
            {
                ClearStarterTracking(); // 비활성 씬에서는 추적 초기화
                return;
            }

            string wormId = RunLoadoutContext.CurrentStartBonus.SelectedWormId; // 타이틀 선택값
            EnsureStarterSegment(wormId, false); // Start 마지막에 일괄 정렬됨
        }

        private bool EnsureStarterSegment(string wormId, bool snapToPath) // 스타터 생성/교체
        {
            if (!EnableStarterSegment)
            {
                return false; // 기능 비활성
            }

            string normalizedWormId = NormalizeStarterWormId(wormId); // 기본 지렁이 보정
            ApplySelectedWormVisual(normalizedWormId); // 캐릭터 외형 동기화
            GameObject prefab = ResolveStarterSegmentPrefab(normalizedWormId); // 지렁이별 프리팹
            if (prefab == null)
            {
                return false; // 연결할 프리팹 없음
            }

            if (HasActiveStarterSegment
                && activeStarterSegmentPrefab == prefab
                && string.Equals(activeStarterWormId, normalizedWormId, StringComparison.OrdinalIgnoreCase))
            {
                if (snapToPath)
                {
                    SnapSegmentsToPath(); // 보너스 적용 시 위치만 보정
                }

                return true; // 이미 같은 스타터
            }

            RemoveStarterSegmentIfPresent(); // 다른 지렁이 선택 시 기존 스타터 제거
            Transform segment = CreateSegment(0, prefab); // 런타임 세그먼트 생성
            if (segment == null)
            {
                ClearStarterTracking();
                return false;
            }

            segment.name = "ConvoyStarterSegment"; // 일반 몸통과 구분
            // ApplyStarterBodyVisual(segment, normalizedWormId); // 스타터 전용 프리팹에 조립
            segments.Insert(0, segment); // 항상 맨 앞
            segmentGroundChecks.Insert(0, GetSegmentGroundCheck(segment));
            segmentRuntimes.Insert(0, GetSegmentRuntime(segment, 0, true));

            starterSegment = segment;
            activeStarterSegmentPrefab = prefab;
            activeStarterWormId = normalizedWormId;
            SyncSegmentRuntimes(true); // 뒤 세그먼트 인덱스 재보정

            if (snapToPath)
            {
                SnapSegmentsToPath();
            }

            NotifySegmentCountChanged();
            return true;
        }

        private GameObject ResolveStarterSegmentPrefab(string wormId) // 지렁이별 스타터 선택
        {
            if (StarterCatalog != null && StarterCatalog.TryGetStarterPrefab(wormId, out GameObject catalogPrefab))
            {
                return catalogPrefab; // 데이터에셋 우선
            }

            if (StarterSegmentsByWorm != null)
            {
                for (int i = 0; i < StarterSegmentsByWorm.Length; i++)
                {
                    WormStarterSegmentEntry entry = StarterSegmentsByWorm[i];
                    if (entry.Prefab == null || string.IsNullOrWhiteSpace(entry.WormId))
                    {
                        continue;
                    }

                    if (string.Equals(NormalizeStarterWormId(entry.WormId), wormId, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Prefab; // 지렁이 전용
                    }
                }
            }

            GameObject resourcePrefab = ResolveStarterSegmentResource(wormId); // 기본 내장 매핑
            if (resourcePrefab != null)
            {
                return resourcePrefab; // Resources fallback
            }

            return StarterSegmentPrefab; // 아직 전용 무기가 없으면 기본 대포
        }

        private GameObject ResolveStarterSegmentResource(string wormId) // 내장 스타터 프리팹
        {
            string resourceName;
            switch (NormalizeStarterWormId(wormId))
            {
                case MetaWormIds.Attack:
                    resourceName = "SG00_StarterAttack"; // 공격형 스타터
                    break;
                case MetaWormIds.Mobility:
                    resourceName = "SG00_StarterMobility"; // 이속형 스타터
                    break;
                case MetaWormIds.Support:
                    resourceName = "SG00_StarterSupport"; // 지원형 스타터
                    break;
                case MetaWormIds.Magic:
                    resourceName = "SG00_StarterMagic"; // 마법형 스타터
                    break;
                default:
                    resourceName = "SG00_StarterCannon"; // 대포
                    break;
            }

            return Resources.Load<GameObject>(StarterSegmentResourceRoot + resourceName); // Resources 로드
        }

        private void ApplyStarterBodyVisual(Transform segment, string wormId) // 전용 바디 교체
        {
            GameObject bodyPrefab = ResolveStarterBodyResource(wormId); // 지렁이별 바디
            if (segment == null || bodyPrefab == null)
            {
                return; // 교체 불가
            }

            Transform previousBody = FindStarterBodyRoot(segment); // 기존 몸통
            Vector3 localPosition = previousBody != null ? previousBody.localPosition : Vector3.zero; // 위치 유지
            Quaternion localRotation = previousBody != null ? previousBody.localRotation : Quaternion.identity; // 회전 유지
            Vector3 localScale = previousBody != null ? previousBody.localScale : Vector3.one; // 크기 유지
            int siblingIndex = previousBody != null ? previousBody.GetSiblingIndex() : 0; // 순서 유지
            string bodyName = previousBody != null ? previousBody.name : "Body"; // 기존 이름 유지

            if (previousBody != null)
            {
                previousBody.gameObject.SetActive(false); // 즉시 숨김
                DestroyUnityObject(previousBody.gameObject); // 기존 제거
            }

            GameObject body = Instantiate(bodyPrefab, segment, false); // 새 바디
            body.name = bodyName; // 기존 참조명 유지
            body.transform.localPosition = localPosition; // 위치
            body.transform.localRotation = localRotation; // 회전
            body.transform.localScale = localScale; // 크기
            body.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, segment.childCount - 1)); // 순서
        }

        private GameObject ResolveStarterBodyResource(string wormId) // 내장 스타터 바디
        {
            string resourceName;
            switch (NormalizeStarterWormId(wormId))
            {
                case MetaWormIds.Attack:
                    resourceName = "SegmentBody_AttackWormStarter"; // 공격형
                    break;
                case MetaWormIds.Mobility:
                    resourceName = "SegmentBody_MobilityWormStarter"; // 이속형
                    break;
                case MetaWormIds.Support:
                    resourceName = "SegmentBody_SupportWormStarter"; // 지원형
                    break;
                case MetaWormIds.Magic:
                    resourceName = "SegmentBody_MagicWormStarter"; // 마법형
                    break;
                default:
                    resourceName = "SegmentBody_StarterCannon"; // 기본형
                    break;
            }

            return Resources.Load<GameObject>(StarterBodyResourceRoot + resourceName); // Resources 로드
        }

        private static Transform FindStarterBodyRoot(Transform segment) // 바디 자식 찾기
        {
            if (segment == null)
            {
                return null; // 대상 없음
            }

            for (int i = 0; i < segment.childCount; i++)
            {
                Transform child = segment.GetChild(i);
                if (child != null && string.Equals(child.name, "Body", StringComparison.OrdinalIgnoreCase))
                {
                    return child; // 표준 이름
                }
            }

            for (int i = 0; i < segment.childCount; i++)
            {
                Transform child = segment.GetChild(i);
                if (child != null && child.name.StartsWith("SegmentBody_", StringComparison.OrdinalIgnoreCase))
                {
                    return child; // 프리팹 이름
                }
            }

            return null; // 바디 없음
        }

        private void RemoveStarterSegmentIfPresent() // 기존 스타터 제거
        {
            int index = starterSegment != null ? segments.IndexOf(starterSegment) : -1;
            if (index < 0)
            {
                ClearStarterTracking();
                return;
            }

            Transform segment = starterSegment;
            segments.RemoveAt(index);
            RemoveSegmentGroundCheck(index);
            RemoveSegmentRuntime(index);

            if (segment != null)
            {
                DestroyUnityObject(segment.gameObject);
            }

            ClearStarterTracking();
            SyncSegmentRuntimes(true);
        }

        private void ClearStarterTracking() // 스타터 상태 초기화
        {
            starterSegment = null;
            activeStarterSegmentPrefab = null;
            activeStarterWormId = string.Empty;
        }

        private int GetRegularSegmentCount() // 스타터 제외 일반 세그먼트 수
        {
            return Mathf.Max(0, segments.Count - (HasActiveStarterSegment ? 1 : 0));
        }

        private int GetFirstDetachableSegmentIndex() // 꼬리 절단 가능 시작점
        {
            return HasActiveStarterSegment ? 1 : 0;
        }

        private float GetEffectiveStarterSegmentDistance() // 실제 스타터 간격
        {
            return Mathf.Max(0.1f, StarterSegmentDistanceBehindHead, StarterSegmentVisualClearanceDistance); // 겹침 방지
        }

        private static string NormalizeStarterWormId(string wormId) // 지렁이 ID 보정
        {
            return MetaWormIds.Normalize(wormId); // 공용 보정
        }
    }
}
