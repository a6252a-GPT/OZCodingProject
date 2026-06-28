using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed partial class ConvoyController //partial 하나의 클래스를 여러 Script 파일로 나눠 작성할 수 있다. ConvoyController클래스 이름으로 사용한다. 그래야 segments, HeadVisual 같은 private에 접근 가능하다.
    {
        private readonly HashSet<Transform> reservedSegmentCutTargets = new HashSet<Transform>();

        public bool TryGetRandomAttachedWeaponSegment(out Transform targetSegment)
        {
            targetSegment = null;

            CleanupReservedSegmentCutTargets();

            int weaponSegmentCount = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];

                if (!IsAvailableSegmentCutTarget(segment))
                {
                    continue;
                }

                weaponSegmentCount++;
            }

            if (weaponSegmentCount <= 0)
            {
                return false;
            }

            int randomWeaponIndex = Random.Range(0, weaponSegmentCount);
            int currentWeaponIndex = 0;

            for (int i = 0; i < segments.Count; i++)
            {
                Transform segment = segments[i];

                if (!IsAvailableSegmentCutTarget(segment))
                {
                    continue;
                }

                if (currentWeaponIndex == randomWeaponIndex)
                {
                    targetSegment = segment;
                    reservedSegmentCutTargets.Add(targetSegment);
                    return true;
                }

                currentWeaponIndex++;
            }

            return false;
        }

        public bool HasAvailableSegmentCutTarget()
        {
            CleanupReservedSegmentCutTargets();

            for (int i = 0; i < segments.Count; i++) 
            {
                if (IsAvailableSegmentCutTarget(segments[i])) 
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAttachedSegmentCutTarget(Transform targetSegment)
        {
            if (targetSegment == null)
            {
                return false;
            }

            int segmentIndex = segments.IndexOf(targetSegment);

            if (segmentIndex < 0)
            {
                return false;
            }

            if (segmentIndex < GetFirstDetachableSegmentIndex())
            {
                return false;
            }

            return IsAttachedWeaponSegment(targetSegment);
        }

        public void ReleaseSegmentCutTarget(Transform targetSegment)
        {
            reservedSegmentCutTargets.Remove(targetSegment);
        }

        public bool IsConvoyHeadCollider(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            Transform hitTransform = other.transform;

            if (hitTransform == transform)
            {
                return true;
            }

            if (HeadVisual != null && (hitTransform == HeadVisual || hitTransform.IsChildOf(HeadVisual)))
            {
                return true;
            }

            return false;
        }

        public bool IsTargetSegmentCollider(Collider other, Transform targetSegment)
        {
            if (other == null || targetSegment == null)
            {
                return false;
            }

            Transform hitTransform = other.transform;

            return hitTransform == targetSegment || hitTransform.IsChildOf(targetSegment);
        }

        public bool TryCutTailFromTargetSegment(Transform targetSegment)
        {
            if (!IsAttachedSegmentCutTarget(targetSegment))
            {
                return false;
            }

            if (tailCutCooldownRemaining > 0.0f)
            {
                return false;
            }

            int segmentIndex = segments.IndexOf(targetSegment);
            Vector3 burstCenter = targetSegment.position;

            CutTailFromIndex(segmentIndex, burstCenter);

            CleanupReservedSegmentCutTargets();

            return true;
        }

        private bool IsAvailableSegmentCutTarget(Transform segment)
        {
            if (!IsAttachedSegmentCutTarget(segment))
            {
                return false;
            }

            return !reservedSegmentCutTargets.Contains(segment);
        }

        private void CleanupReservedSegmentCutTargets()
        {
            reservedSegmentCutTargets.RemoveWhere(segment => !IsAttachedSegmentCutTarget(segment));
        }

        private bool IsAttachedWeaponSegment(Transform segment)
        {
            if (segment == null)
            {
                return false;
            }

            if (!segment.gameObject.activeInHierarchy)
            {
                return false;
            }

            SegmentWeaponBehaviour weapon = segment.GetComponent<SegmentWeaponBehaviour>();

            if (weapon == null)
            {
                weapon = segment.GetComponentInChildren<SegmentWeaponBehaviour>();
            }

            return weapon != null;
        }
    }
}