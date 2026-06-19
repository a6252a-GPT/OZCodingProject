using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class CoreTestDamageFloatingFontButton : MonoBehaviour //전찬우추가 - CoreTest 데미지 폰트 변경 버튼
    {
        public Button Button; //전찬우추가 - 클릭 버튼
        public Text Label; //전찬우추가 - 버튼 글자

        private void Awake() //전찬우추가 - 버튼 연결
        {
            ResolveReferences(); //전찬우추가 - 참조 보장
            if (Button != null)
            {
                Button.onClick.RemoveListener(CycleFont); //전찬우추가 - 중복 방지
                Button.onClick.AddListener(CycleFont); //전찬우추가 - 클릭 연결
            }

            RefreshLabel(); //전찬우추가 - 라벨 초기화
        }

        private void OnDestroy() //전찬우추가 - 연결 해제
        {
            if (Button != null)
            {
                Button.onClick.RemoveListener(CycleFont); //전찬우추가 - 해제
            }
        }

        public void CycleFont() //전찬우추가 - 폰트 순환
        {
            string fontName = DamageFloatingSpawner.CycleFontAndSpawnSample(); //전찬우추가 - 폰트 변경 + 샘플
            RefreshLabel(); //전찬우추가 - 라벨 유지
            Debug.Log($"[CoreTest] 데미지 플로팅 폰트 변경: {fontName}", this); //전찬우추가 - 확인 로그
        }

        private void ResolveReferences() //전찬우추가 - 참조 보정
        {
            if (Button == null)
            {
                Button = GetComponent<Button>(); //전찬우추가 - 같은 오브젝트
            }

            if (Label == null && Button != null)
            {
                Label = Button.GetComponentInChildren<Text>(true); //전찬우추가 - 자식 텍스트
            }
        }

        private void RefreshLabel() //전찬우추가 - 버튼 글자
        {
            if (Label != null)
            {
                Label.text = "데미지폰트"; //전찬우추가 - 짧은 라벨
            }
        }
    }
}
