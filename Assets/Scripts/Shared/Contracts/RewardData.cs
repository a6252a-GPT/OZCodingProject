using UnityEngine;

namespace TeamProject01.Gameplay
{
    [System.Serializable]
    public struct RewardData // 몬스터 → 보상 입구 → 코어 전달값
    {
        public int Experience; // 경험치
        public int Gold; // 골드
        public int EnemyId; // 몬스터 ID
        public Vector3 Position; // 보상 위치

        public bool IsValid => Experience > 0 || Gold > 0; // 지급 여부

        public static RewardData Create(int experience, int gold, int enemyId, Vector3 position) // 생성
        {
            RewardData data = default; // 값 준비
            data.Experience = Mathf.Max(0, experience); // 경험치 보정
            data.Gold = Mathf.Max(0, gold); // 골드 보정
            data.EnemyId = enemyId; // 몬스터 저장
            data.Position = position; // 위치 저장
            return data; // 결과 반환
        }
    }
}
