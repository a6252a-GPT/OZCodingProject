using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class ConvoyController
    {
        [Header("Starter Segment")]
        public bool EnableStarterSegment; // 선택 지렁이 기본 무기 세그먼트
        public GameObject StarterSegmentPrefab; // 공통 fallback 시작 세그먼트
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
            if (StarterSegmentsByWorm != null)
            {
                for (int i = 0; i < StarterSegmentsByWorm.Length; i++)
                {
                    WormStarterSegmentEntry entry = StarterSegmentsByWorm[i];
                    if (entry.Prefab == null || string.IsNullOrWhiteSpace(entry.WormId))
                    {
                        continue;
                    }

                    if (string.Equals(entry.WormId.Trim(), wormId, StringComparison.OrdinalIgnoreCase))
                    {
                        return entry.Prefab; // 지렁이 전용
                    }
                }
            }

            return StarterSegmentPrefab; // 아직 전용 무기가 없으면 기본 대포
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

        private static string NormalizeStarterWormId(string wormId) // 지렁이 ID 보정
        {
            return string.IsNullOrWhiteSpace(wormId) ? MetaWormIds.Basic : wormId.Trim();
        }
    }
}
