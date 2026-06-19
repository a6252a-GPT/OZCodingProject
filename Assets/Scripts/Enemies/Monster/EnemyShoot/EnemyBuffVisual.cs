using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyBuffVisual : MonoBehaviour //받은 버프 종류에 따라 구분
    {
        private EnemyBuffReceiver buffReceiver;//버프 상태를 읽을 EnemyBuffReceiver Component

        [SerializeField] private GameObject attackPowerAura; //공격력 버프 표시
        [SerializeField] private GameObject moveSpeedAura; //이동속도 버프 표시
        [SerializeField] private GameObject attackSpeedAura; //공격속도 버프 표시

        private EnemyBuffType visibleBuffType = EnemyBuffType.None; //현재 적용중인 버프 표시(None)

        private void Awake()
        {
            if(buffReceiver == null) //현재 buffReceiver가 연결되어 있지 않다면
            {
                buffReceiver = GetComponent<EnemyBuffReceiver>(); //연결한다.
            }

            ClearVisual(); //처음 시작할 떄 모든 아우라를 끈다.
        }

        private void Update()
        {
            EnemyBuffType targetBuffType = EnemyBuffType.None; //이번 프레임에 표시할 버프 종류를 None로 저장한다.

            if (buffReceiver != null && buffReceiver.HasActiveBuff) //현재 버프가 적용중이라면
            {
                targetBuffType = buffReceiver.ActiveBuffType; //현재 아우라 표시할 대상으로 저장한다.
            }

            if(visibleBuffType == targetBuffType) //같은 아우라 버프가 표시 중이라면
            {
                return; //종료한다.
            }

            ApplyVisual(targetBuffType); //버프 종류에 맞게 아우라 표시를 갱신한다.
        }

        private void OnDisable()
        {
            ClearVisual(); //아우라를 끈다.
        }

        public void ApplyVisual(EnemyBuffType buffType) //아우라 효과를 켜는 함수
        {
            visibleBuffType = buffType; //현재 적용중인 버프 상태에 맞게 아우라를 적용한다.

            SetAuraActive(attackPowerAura, buffType == EnemyBuffType.AttackPower); //공격 증가 아우라를 켠다.
            SetAuraActive(moveSpeedAura, buffType == EnemyBuffType.MoveSpeed); //이동속도 증가 아우라를 켠다.
            SetAuraActive(attackSpeedAura, buffType == EnemyBuffType.AttackSpeed); //공격속도 증가 아우라를 켠다.
        }

        public void ClearVisual() //아우라 효과를 끄는 함수
        {
            visibleBuffType = EnemyBuffType.None; //현재 적용중인 버프 상태를 None로 한다.

            SetAuraActive(attackPowerAura, false); //공격 증가 아우라를 종료한다.
            SetAuraActive(moveSpeedAura, false); //이동속도 증가 아우라를 종료한다.
            SetAuraActive(attackSpeedAura, false); //공격속도 증가 아우라를 종료한다.
        }

        private void SetAuraActive(GameObject auraObject, bool active) //GameObject 활성화 상태를 바꾸는 함수
        {
            if(auraObject == null) // auraObject가 연결되어 있지 않다면
            {
                return; //종료한다.
            }

            if(auraObject.activeSelf == active) //auraObject가 활성화 상태라면
            {
                return; //종료한다.
            }

            auraObject.SetActive(active); //auraObject GameObject를 켜거나 끈다.
        }
    }    
}