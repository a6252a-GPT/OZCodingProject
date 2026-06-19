using TMPro;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public static class DamageFloatingTextStyle //전찬우추가 - 데미지 숫자 TMP 스타일
    {
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.72f); //전찬우추가 - 그림자 색

        public static void Apply(TMP_Text text, ref Material runtimeMaterial) //전찬우추가 - 외곽/그림자 적용
        {
            if (text == null)
            {
                return; //전찬우추가 - 대상 없음
            }

            text.outlineWidth = 0f; //전찬우추가 - 외곽선 끔
            text.outlineColor = Color.clear; //전찬우추가 - 외곽선 색 초기화

            Material source = text.fontSharedMaterial != null ? text.fontSharedMaterial : text.fontMaterial; //전찬우추가 - 기준 재질
            if (source == null)
            {
                return; //전찬우추가 - 재질 없음
            }

            if (runtimeMaterial == null)
            {
                runtimeMaterial = new Material(source) { name = source.name + "_DamageFloating" }; //전찬우추가 - 팝업 전용 재질
            }

            text.fontMaterial = runtimeMaterial; //전찬우추가 - 런타임 재질 적용
            ApplyMaterial(runtimeMaterial); //전찬우추가 - 그림자 값 적용
        }

        private static void ApplyMaterial(Material material) //전찬우추가 - TMP underlay 설정
        {
            if (material == null)
            {
                return; //전찬우추가 - 재질 없음
            }

            material.EnableKeyword("UNDERLAY_ON"); //전찬우추가 - 그림자 활성화
            SetFloat(material, ShaderUtilities.ID_OutlineWidth, 0f); //전찬우추가 - 외곽선 제거
            SetColor(material, ShaderUtilities.ID_OutlineColor, Color.clear); //전찬우추가 - 외곽선 투명
            SetColor(material, ShaderUtilities.ID_UnderlayColor, ShadowColor); //전찬우추가 - 그림자 색
            SetFloat(material, ShaderUtilities.ID_UnderlayOffsetX, 0.18f); //전찬우추가 - 그림자 X
            SetFloat(material, ShaderUtilities.ID_UnderlayOffsetY, -0.18f); //전찬우추가 - 그림자 Y
            SetFloat(material, ShaderUtilities.ID_UnderlaySoftness, 0.2f); //전찬우추가 - 그림자 부드러움
        }

        private static void SetFloat(Material material, int id, float value) //전찬우추가 - float 안전 적용
        {
            if (material.HasProperty(id))
            {
                material.SetFloat(id, value); //전찬우추가 - 값 적용
            }
        }

        private static void SetColor(Material material, int id, Color value) //전찬우추가 - color 안전 적용
        {
            if (material.HasProperty(id))
            {
                material.SetColor(id, value); //전찬우추가 - 값 적용
            }
        }
    }
}
