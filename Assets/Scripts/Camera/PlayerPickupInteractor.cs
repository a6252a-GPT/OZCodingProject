using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class PlayerPickupInteractor : MonoBehaviour // 플레이어 픽업 상호작용
    {
        private const string LootingCenterName = "Looting";
        private const float MinimumRewardMagnetRadius = 4f;
        private const float MinimumRewardPullStrength = 570f;
        private const float MinimumRewardMaxPullSpeed = 252f;

        [SerializeField] private Transform pickupCenter; // 흡수 기준
        [Min(0.1f)]
        [SerializeField] private float rewardMagnetRadius = MinimumRewardMagnetRadius; // 기본 자석 반경
        [Min(0f)]
        [SerializeField] private float rewardPullStrength = MinimumRewardPullStrength; // 기본 흡수 가속
        [Min(0.1f)]
        [SerializeField] private float rewardMaxPullSpeed = MinimumRewardMaxPullSpeed; // 기본 흡수 최대 속도
        [Min(0.05f)]
        [SerializeField] private float rewardCollectDistance = 0.6f; // 획득 거리

        private Transform cachedPickupCenter; // 검색 캐시
        private Transform debugLastResolvedCenter; // 시작 선택권 원인 추적용 수집 중심
        private float debugNextAttractLogTime; // 후보 감지 로그 스로틀

        public bool HasActivePickupCandidates => false; // 월드 보상은 카메라 줌 입력을 막지 않는다.

        private void Update()
        {
            Transform center = ResolvePickupCenter();
            if (center == null)
            {
                return;
            }

            if (StartingSegmentChoiceTicketDebug.ShouldLog && debugLastResolvedCenter != center)
            {
                debugLastResolvedCenter = center;
                StartingSegmentChoiceTicketDebug.Log(
                    $"PlayerPickupInteractor.CenterResolved scene={StartingSegmentChoiceTicketDebug.SceneName}, center={(center != null ? center.name : "null")}, "
                    + $"position={StartingSegmentChoiceTicketDebug.Format(center.position)}, effectiveRadius={GetEffectiveRewardMagnetRadius():0.00}, collectDistance={rewardCollectDistance:0.00}",
                    this);
            }

            bool attracted = WorldRewardPickup.AttractInRange(
                center.position,
                GetEffectiveRewardMagnetRadius(),
                GetEffectiveRewardPullStrength(),
                GetEffectiveRewardMaxPullSpeed(),
                rewardCollectDistance,
                Time.deltaTime);
            if (StartingSegmentChoiceTicketDebug.ShouldLog && attracted && Time.time >= debugNextAttractLogTime)
            {
                debugNextAttractLogTime = Time.time + 0.25f;
                StartingSegmentChoiceTicketDebug.Log(
                    $"PlayerPickupInteractor.AttractTick center={center.name}, position={StartingSegmentChoiceTicketDebug.Format(center.position)}, "
                    + $"radius={GetEffectiveRewardMagnetRadius():0.00}, pull={GetEffectiveRewardPullStrength():0.00}, maxSpeed={GetEffectiveRewardMaxPullSpeed():0.00}",
                    this);
            }
        }

        private Transform ResolvePickupCenter()
        {
            if (pickupCenter != null && pickupCenter.gameObject.activeInHierarchy)
            {
                return pickupCenter;
            }

            if (cachedPickupCenter != null
                && cachedPickupCenter.gameObject.activeInHierarchy
                && cachedPickupCenter.name == LootingCenterName)
            {
                return cachedPickupCenter;
            }

            if (TryResolveLootingCenter(out Transform lootingCenter))
            {
                cachedPickupCenter = lootingCenter;
                return cachedPickupCenter;
            }

            if (cachedPickupCenter != null && cachedPickupCenter.gameObject.activeInHierarchy)
            {
                return cachedPickupCenter;
            }

            if (MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget))
            {
                cachedPickupCenter = convoyTarget;
                return cachedPickupCenter;
            }

            ConvoyController controller = FindFirstObjectByType<ConvoyController>();
            cachedPickupCenter = controller != null ? controller.transform : null;
            return cachedPickupCenter;
        }

        public static bool TryResolveLootingCenter(out Transform lootingCenter)
        {
            lootingCenter = null;
            if (MonsterInteractionApi.TryGetConvoyTarget(out Transform convoyTarget)
                && TryFindActiveChildRecursive(convoyTarget, LootingCenterName, out lootingCenter))
            {
                return true;
            }

            ConvoyController controller = UnityEngine.Object.FindFirstObjectByType<ConvoyController>();
            return controller != null && TryFindActiveChildRecursive(controller.transform, LootingCenterName, out lootingCenter);
        }

        private static bool TryFindActiveChildRecursive(Transform root, string childName, out Transform found)
        {
            found = null;
            if (root == null || string.IsNullOrWhiteSpace(childName))
            {
                return false;
            }

            if (root.name == childName && root.gameObject.activeInHierarchy)
            {
                found = root;
                return true;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (TryFindActiveChildRecursive(child, childName, out found))
                {
                    return true;
                }
            }

            return false;
        }

        private float GetEffectiveRewardMagnetRadius()
        {
            float baseRadius = Mathf.Max(rewardMagnetRadius, MinimumRewardMagnetRadius);
            return CoreStatProvider.ApplyRunPickupRangeBonusOrDefault(baseRadius);
        }

        private float GetEffectiveRewardPullStrength()
        {
            return Mathf.Max(rewardPullStrength, MinimumRewardPullStrength);
        }

        private float GetEffectiveRewardMaxPullSpeed()
        {
            return Mathf.Max(rewardMaxPullSpeed, MinimumRewardMaxPullSpeed);
        }
    }
}
