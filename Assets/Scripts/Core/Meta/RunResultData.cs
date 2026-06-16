using System;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [Serializable]
    public struct RunResultData // 한 판 결과 요약
    {
        [Min(0)] public int ReachedWave; // 도달 웨이브
        [Min(0f)] public float SurviveTime; // 생존 시간
        [Min(0)] public int KillCount; // 처치 수
        public bool IsClear; // 클리어 여부
        [Min(0)] public int EarnedDiamond; // 지급 다이아
        [Min(0)] public int EarnedGoldInRun; // 한 판 골드
        public string SelectedWormId; // 사용 지렁이

        public static RunResultData Create(int reachedWave, float surviveTime, int killCount, bool isClear, int earnedDiamond, int earnedGoldInRun, string selectedWormId) // 생성
        {
            RunResultData data = default; // 값 준비
            data.ReachedWave = Mathf.Max(0, reachedWave); // 웨이브
            data.SurviveTime = Mathf.Max(0f, surviveTime); // 시간
            data.KillCount = Mathf.Max(0, killCount); // 처치
            data.IsClear = isClear; // 클리어
            data.EarnedDiamond = Mathf.Max(0, earnedDiamond); // 다이아
            data.EarnedGoldInRun = Mathf.Max(0, earnedGoldInRun); // 골드
            data.SelectedWormId = string.IsNullOrWhiteSpace(selectedWormId) ? MetaWormIds.Basic : selectedWormId; // 지렁이
            return data; // 결과 반환
        }
    }
}
