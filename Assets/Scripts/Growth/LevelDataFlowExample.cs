using UnityEngine;
//테스트
namespace TeamProject01.Gameplay
{
    public sealed class LevelDataFlowExample : MonoBehaviour // 레벨시스템 송수신 예시
    {
        public CoreDataFlowExample Core; // 더미 로그용 코어 예시
        public int LastCheckedLevel = 1; // 마지막 확인 레벨
        public CoreStatData LastReceivedStats; // 마지막 수신값
        public GrowthStatData LastSentGrowth; // 마지막 송신값

        private CoreStatProvider subscribedCore; // 구독 중인 코어
        private bool applyingGrowth; // 중복 적용 방지

        private void Reset() // 자동 참조
        {
            Core = FindFirstObjectByType<CoreDataFlowExample>(); // 코어 예시 찾기
        }

        private void OnEnable() // 구독 시작
        {
            TrySubscribeCore(); // 코어 변경 감지
        }

        private void Start() // 초기 상태 맞춤
        {
            TrySubscribeCore(); // Awake 순서 보정
            CoreStatProvider.TryGetCurrentStats(out CoreStatData stats); // 현재 코어값
            LastCheckedLevel = stats.Level; // 시작 레벨 기준
            LastReceivedStats = stats; // 시작 상태 저장
        }

        private void OnDisable() // 구독 해제
        {
            if (subscribedCore != null)
            {
                subscribedCore.StatsChanged -= CheckCoreStats; // 이벤트 해제
                subscribedCore = null; // 참조 제거
            }
        }

        private void TrySubscribeCore() // 코어 이벤트 연결
        {
            if (subscribedCore != null || CoreStatProvider.Active == null)
            {
                return; // 이미 연결 또는 코어 없음
            }

            subscribedCore = CoreStatProvider.Active; // 현재 코어 저장
            subscribedCore.StatsChanged += CheckCoreStats; // 데이터를 받는 곳!! 코어 → 레벨
        }

        public void CheckCoreStats(CoreStatData stats) // 데이터를 받는 곳!! 코어 → 레벨
        {
            if (applyingGrowth)
            {
                return; // 적용 중 재진입 방지
            }

            LastReceivedStats = stats; // 받은 CoreStatData
            Debug.Log($"[LevelExample] CoreStatData 수신: Level={stats.Level}, Exp={stats.CurrentExperience}/{stats.ExperienceToNextLevel}, Gold={stats.Gold}, CanLevelUp={stats.CanLevelUp}", this); // 수신 로그

            if (!stats.CanLevelUp)
            {
                Debug.Log("[LevelExample] 레벨업 조건 미충족, 성장값 송신 없음", this); // 대기 로그
                return; // 변화 없음
            }

            LastSentGrowth = CreateGrowth(); // 보낼 GrowthStatData 준비
            Debug.Log($"[LevelExample] GrowthStatData 송신: Type={LastSentGrowth.ChoiceType}, LevelDelta={LastSentGrowth.LevelDelta}, DamageBonus={LastSentGrowth.DamageMultiplierBonus:0.00}", this); // 송신 로그

            applyingGrowth = true; // 재진입 차단
            bool applied = CoreStatProvider.TryApplyGrowth(LastSentGrowth); // 데이터를 보내는 곳!! 레벨 → 코어
            applyingGrowth = false; // 차단 해제

            if (applied)
            {
                CoreStatProvider.TryGetCurrentStats(out CoreStatData currentStats); // 적용 후 확인
                LastCheckedLevel = currentStats.Level; // 확인 레벨 갱신
                Debug.Log($"[LevelExample] 성장 적용 성공: Level={currentStats.Level}, Exp={currentStats.CurrentExperience}/{currentStats.ExperienceToNextLevel}", this); // 성공 로그
            }
            else
            {
                Debug.LogWarning("[LevelExample] 성장 적용 실패: 코어 없음 또는 경험치 부족", this); // 실패 로그
            }
        }

        private GrowthStatData CreateGrowth() // 성장 계산
        {
            return GrowthStatData.CreateConvoyUpgrade(1, 0.2f, 0.1f, 0f, 0f, 0f); // 더미 컨보이 강화
        }
    }
}
