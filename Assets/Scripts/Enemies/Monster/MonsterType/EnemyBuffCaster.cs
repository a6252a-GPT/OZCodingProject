using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyBuffCaster : MonoBehaviour //버프 부여 몬스터
    {
        [Min(0.1f)]
        [SerializeField] private float buffRange = 8.0f; //버프 범위

        [Min(0.1f)]
        [SerializeField] private float buffInterval = 8.0f; //몇 초마다 버프를 부여할지

        [Range(0.0f, 1.0f)]
        [SerializeField] private float buffChance = 0.4f; //범위 안 몬스터에게 버프를 줄 확률

        [Min(1)]
        [SerializeField] private int maxBuffCountPerCast = 5; //최대 버프를 받을 몬스터 숫자

        [Min(1.0f)]
        [SerializeField] private float attackPowerMultiplier = 1.5f; //공격력 버프 배율
        [Min(1.0f)]
        [SerializeField] private float moveSpeedMultiplier = 1.3f; //이동속도 버프 배율
        [Min(1.0f)]
        [SerializeField] private float attackSpeedMultiplier = 1.5f; //공격속도 버프 배율

        [Min(1.0f)]
        [SerializeField] private float buffDuration = 5.0f; //버프 유지 시간

        [SerializeField] private bool canBuffSelf; //자기 자신도 버프 대상에 포함 할지

        private EnemyController ownerController;

        private float buffTimer; //다음 버프까지 대기 시간

        private void Awake()
        {
            ownerController = GetComponent<EnemyController>(); //GameObject에 붙은 EnemyController를 찾는다.
        }

        private void OnEnable()
        {
            buffTimer = buffInterval; //처음 버프 시간을 설정한다.
        }

        private void Update()
        {
            buffTimer -= Time.deltaTime; //버프 대기 시간을 감소시킨다.

            if(buffTimer > 0.0f) //다음 버프 시전까지 시간이 남았다면
            {
                return; //종료한다.
            }

            CastBuff();
            buffTimer = buffInterval; //다음 버프 시간을 설정한다.
        }

        private void CastBuff() //주변 몬스터를 찾아 버프를 부여하는 함수
        {
            List<EnemyBuffReceiver> candidates = FindBuffCandidates(); //목록에서 버프를 부여할 몬스터를 찾는다.

            int appliedCount = 0; //버프를 부여한 몬스터 숫자

            for(int i = 0; i < candidates.Count; i++) //몬스터 목록을 본다.
            {
                if(appliedCount >= maxBuffCountPerCast) // 버프를 부여한 몬스터 수가 최대수보다 크거나 같으면
                {
                    return; //종료한다.
                }

                if(Random.value > buffChance) //랜덤값이 버프 확률보다 크면 실패한다.
                {
                    continue; //버프를 부여하지 않는다.
                }

                ApplyRandomBuff(candidates[i]); //몬스터에게 랜덤으로 버프를 적용한다.
                appliedCount++; //버프를 부여한 몬스터 숫자를 증가시킨다.
            }
        }
        
        private List<EnemyBuffReceiver> FindBuffCandidates() //범위 안 몬스터를 리스트에 저장하는 함수
        {
            List<EnemyBuffReceiver> result = new List<EnemyBuffReceiver>(); //몬스터를 리스트에 저장한다.

            string[] targetTags = EnemyTags.TargetTags; //몬스터 Tag목록

            for (int tagIndex = 0; tagIndex < targetTags.Length; tagIndex++) //tag목록을 본다.
            {
                GameObject[] taggedObjects = FindObjectsByTagSafe(targetTags[tagIndex]); // 해당 Tag를 가진 GameObject들을 찾는다.

                for(int i = 0; i < taggedObjects.Length; i++)//목록을 본다.
                {
                    EnemyBuffReceiver receiver = taggedObjects[i].GetComponentInParent<EnemyBuffReceiver>(); // 버프를 받을 수 있는 Component를 찾는다.

                    if(receiver == null) // EnemyBuffReceiver가 없다면
                    {
                        continue; //버프 대상에서 제외한다.
                    }

                    if (result.Contains(receiver))//이미 목록에 있는 몬스터는
                    {
                        continue; // 등록하지 않는다.
                    }

                    EnemyController receiverController = receiver.GetComponent<EnemyController>(); // 후보 몬스터의 EnemyController를 찾는다.

                    if(!canBuffSelf && ownerController != null && receiverController == ownerController) //자기 자신 버프가 꺼져 있고 대상이 자기 자신이라면
                    {
                        continue; //버프 대상에서 제외한다.
                    }

                    Vector3 offset = receiver.transform.position - transform.position; // 버프 몬스터에서 후보 몬스터까지의 거리 벡터
                    offset.y = 0.0f; //높이는 제거한다.

                    if(offset.sqrMagnitude > buffRange * buffRange) //버프 범위 밖이라면
                    {
                        continue; //제외한다.
                    }

                    result.Add(receiver); //버프 목록에 추가한다.
                }
            }

            return result; //최종 몬스터 목록을 반환한다.
        }

        private void ApplyRandomBuff(EnemyBuffReceiver receiver) //몬스터에게 랜덤 버프를 적용할 함수
        {
            if(receiver == null) //대상이 없다면
            {
                return; //종료한다.
            }

            EnemyBuffType buffType = PickRandomBuffType(); //적용할 버프 종류를 랜덤으로 선택한다.
            float multiplier = GetMultiplier(buffType); //버프 종류에 맞는 배율을 선택한다.

            receiver.ApplyBuff(buffType, multiplier, buffDuration); //대상 몬스터에게 버프를 부여한다.
        }
        private EnemyBuffType PickRandomBuffType() // 랜덤으로 버프 종류를 선택하는 함수
        {
            int randomIndex = Random.Range(0, 3); // 0, 1, 2 랜덤으로 고른다.

            if (randomIndex == 0) // 0 이라면
            {
                return EnemyBuffType.AttackPower; // 공격 버프를 반환한다.
            }

            if (randomIndex == 1) // 1 이라면
            {
                return EnemyBuffType.MoveSpeed; // 이동속도 버프를 반환한다.
            }

            return EnemyBuffType.AttackSpeed; // 나머지는 공격속도 버프를 반환한다.
        }

        private float GetMultiplier(EnemyBuffType buffType) // 버프 종류에 맞는 고정 배율을 반환하는 함수
        {
            if (buffType == EnemyBuffType.AttackPower) // 공격력 버프라면
            {
                return attackPowerMultiplier; // 공격 버프 배율을 반환한다.
            }

            if (buffType == EnemyBuffType.MoveSpeed) // 이동속도 버프라면
            {
                return moveSpeedMultiplier; // 이동속도 버프 배율을 반환한다.
            }

            if (buffType == EnemyBuffType.AttackSpeed) // 공격속도 버프라면
            {
                return attackSpeedMultiplier; // 공격속도 버프 배율을 반환한다.
            }

            return 1.0f; // 버프 없음이면 기본 배율을 반환한다.
        }

        private GameObject[] FindObjectsByTagSafe(string targetTag) // 등록되지 않은 Tag 오류를 막고 안전하게 검색하는 함수
        {
            try // Tag가 등록되어 있는지 시도한다.
            {
                return GameObject.FindGameObjectsWithTag(targetTag); // 해당 Tag를 가진 GameObject 배열을 반환한다.
            }
            catch (UnityException) // Tag가 등록되어 있지 않다면
            {
                return new GameObject[0]; // 빈 배열을 반환한다.
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, buffRange); // Scene에서 선택했을 때 버프 범위를 표시한다.
        }
    }
}
