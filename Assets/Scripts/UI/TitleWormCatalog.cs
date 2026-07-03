using UnityEngine;

namespace TeamProject01.Gameplay
{
    internal static class TitleWormCatalog // 타이틀 지렁이 표시 데이터
    {
        public static string GetDisplayName(string wormId) // 지렁이 이름
        {
            switch (Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return "공격형 지렁이";
                case MetaWormIds.Mobility:
                    return "이동형 지렁이";
                case MetaWormIds.Support:
                    return "지원형 지렁이";
                case MetaWormIds.Magic:
                    return "마법형 지렁이";
                default:
                    return "기본형 지렁이";
            }
        }

        public static string GetBonusText(string wormId) // 기존 호출 호환
        {
            return GetStartingWeaponText(wormId);
        }

        public static string GetStartingWeaponText(string wormId) // 스타팅 무기
        {
            switch (Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return "스타팅 무기 : 미사일";
                case MetaWormIds.Mobility:
                    return "스타팅 무기 : 톱날";
                case MetaWormIds.Support:
                    return "스타팅 무기 : 웜홀";
                case MetaWormIds.Magic:
                    return "스타팅 무기 : 전기지지";
                default:
                    return "스타팅 무기 : 대포";
            }
        }

        public static string GetAdditionalBonusText(string wormId) // 추가 보너스
        {
            switch (Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return "추가 보너스 : 충돌힘 30% 증가";
                case MetaWormIds.Mobility:
                    return "추가 보너스 : 이동속도 10% 증가 / 회전력 10% 증가";
                case MetaWormIds.Support:
                    return "추가 보너스 : 지원 세그먼트 보너스 20%";
                case MetaWormIds.Magic:
                    return "추가 보너스 : 마법 세그먼트 공격력 15%";
                default:
                    return "추가 보너스 : 없음";
            }
        }

        public static Color GetPreviewColor(string wormId) // 프리뷰 색
        {
            switch (Normalize(wormId))
            {
                case MetaWormIds.Attack:
                    return new Color(1f, 0.48f, 0.36f, 1f); // 공격형
                case MetaWormIds.Mobility:
                    return new Color(1f, 0.86f, 0.28f, 1f); // 이동형
                case MetaWormIds.Support:
                    return new Color(0.35f, 0.75f, 1f, 1f); // 지원형
                case MetaWormIds.Magic:
                    return new Color(0.62f, 0.48f, 1f, 1f); // 마법형
                default:
                    return new Color(0.48f, 0.9f, 0.56f, 1f); // 기본형
            }
        }

        private static string Normalize(string wormId) // 지렁이 ID 보정
        {
            return MetaWormIds.Normalize(wormId); // 공용 보정
        }
    }
}
