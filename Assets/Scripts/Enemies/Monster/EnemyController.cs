using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyController : MonoBehaviour // 몬스터 관리
    {
        private static readonly List<EnemyController> ActiveMonsters = new List<EnemyController>(128); // 타겟 목록
        private static int nextEnemyId; // 몬스터 ID 발급용 번호

        [SerializeField] private EnemyGrade grade = EnemyGrade.Monster; // 몬스터 등급

        public int EnemyId { get; private set; } // 외부 식별값

        public EnemyGrade Grade // 외부에서 몬스터 등급을 읽기 위한 property
        {
            get
            {
                return grade; // 현재 몬스터 등급을 반환
            }
        }

        private bool dead; //몬스터가 사망 처리되었는지 확인

        private EnemyHealth health; // 체력 처리를 담당하는 EnemyHealth Script Component 참조
        private EnemyReward reward; // 보상 처리를 담당하는 EnemyReward Script Component 참조

        public static int ActiveCount // 현재 활성 몬스터 수
        {
            get
            {
                CleanupActiveList(); // null이거나 죽은 몬스터를 목록에서 정리
                return ActiveMonsters.Count; // 현재 살아있는 몬스터 수를 반환
            }
        }

        private void Awake() 
        {
            EnemyId = ++nextEnemyId; // 몬스터마다 고유 ID를 부여

            health = GetComponent<EnemyHealth>(); // 같은 GameObject에 붙은 EnemyHealth Script Component를 찾는다.
            reward = GetComponent<EnemyReward>(); // 같은 GameObject에 붙은 EnemyReward Script Component를 찾는다.

            EnemyTags.TryApplyTag(gameObject, grade); // 몬스터 등급에 맞는 Unity Tag를 적용한다.
        }

        private void OnEnable() // 목록 등록
        {
            if (!ActiveMonsters.Contains(this)) // 이미 목록에 등록되어 있지 않다면
            {
                ActiveMonsters.Add(this); // 타겟 등록
            }
        }

        private void OnDisable() // 목록 해제
        {
            ActiveMonsters.Remove(this); // 타겟 제거
        }

        public void Configure(Transform nexus, Material material, float moveSpeed, float stopRadius, float groundHeight, EnemyGrade grade = EnemyGrade.Monster) // 스폰 설정
        {
            this.grade = grade;  // 등급 저장
            EnemyTags.TryApplyTag(gameObject, this.grade); // 태그 적용
        }

        public void ApplyDamage(DamageData damage) // 피해 받기
        {
            if (!damage.IsValid) // 유효하지 않은 피해라면
            {
                return; // 피해 없음
            }

            if (dead) // 이미 사망 처리된 몬스터라면
            {
                return; // 중복 방지
            }

            if (health == null) // EnemyHealth가 붙어 있지 않다면
            {
                KillByDamage(); // 체력 계산 없이 즉시 사망 처리한다.
                return; // 더 이상 처리하지 않는다.
            }

            health.TakeDamage(damage.Amount); // 실제 HP 감소는 EnemyHealth가 담당한다.

            if (health.IsDead) // HP가 0 이하가 되었다면
            {
                KillByDamage(); // 보상 지급 후 사망 처리한다.
            }
        }

        public void Kill() // 보상 없이 즉시 제거하는 함수
        {
            if (dead) // 이미 사망 처리되었다면
            {
                return; // 중복 방지
            }

            dead = true; // 사망 표시
            Destroy(gameObject);  // 몬스터 제거
        }

        private void KillByDamage()  // 피해 사망
        {
            if (dead) // 이미 사망 처리되었다면
            {
                return; // 중복 방지
            }

            if (reward != null) // EnemyReward Script Component가 있다면
            {
                reward.GiveReward(EnemyId, transform.position); // 보상 생성.
            }

            Kill(); // 공통 제거
        }

        public static bool TryFindNearest(Vector3 origin, float range, out EnemyController target) // 가까운 적 검색
        {
            CleanupActiveList(); // 목록에서 null이나 죽은 몬스터를 정리한다.

            target = null; // 찾지 못했을 때의 기본값
            float bestDistance = range * range; // 사거리 제곱

            string[] tags = EnemyTags.TargetTags;  // 탐색 태그
            bool foundRegisteredTag = false;  // 태그 등록 여부

            for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++) // 태그 목록을 순회한다.
            {
                GameObject[] candidates = FindObjectsByTag(tags[tagIndex], out bool tagRegistered); /// 태그 대상
                foundRegisteredTag |= tagRegistered; // 등록 확인

                for (int i = 0; i < candidates.Length; i++) // 찾은 후보들을 순회한다.
                {
                    EnemyController enemy = candidates[i].GetComponentInParent<EnemyController>(); // 몬스터 확인
                    TryPickNearest(enemy, origin, ref bestDistance, ref target); // 최단 갱신
                }
            }

            if (!foundRegisteredTag) // 태그가 아직 Unity에 등록되지 않은 경우
            {
                for (int i = 0; i < ActiveMonsters.Count; i++) // 등록 목록을 직접 순회한다.
                {
                    TryPickNearest(ActiveMonsters[i], origin, ref bestDistance, ref target); // 가장 가까운 대상인지 확인한다.
                }
            }

            return target != null; // 대상을 찾았다면 true를 반환한다.
        }

        private static GameObject[] FindObjectsByTag(string tagName, out bool tagRegistered) // 태그 검색
        {
            try
            {
                tagRegistered = true; // 등록됨
                return GameObject.FindGameObjectsWithTag(tagName); // 해당 태그를 가진 GameObject들을 찾는다.
            }
            catch (UnityException)
            {
                tagRegistered = false; // 미등록
                return System.Array.Empty<GameObject>();  // 태그 미등록
            }
        }

        private static void TryPickNearest(EnemyController enemy, Vector3 origin, ref float bestDistance, ref EnemyController target) // 최단 대상
        {
            if (enemy == null || enemy.dead) // 대상이 없거나 이미 죽었다면
            {
                return; // 대상에서 제외한다.
            }

            Vector3 offset = enemy.transform.position - origin; // 대상 거리
            offset.y = 0f; // 평면 거리

            float distance = offset.sqrMagnitude;  // 제곱 거리

            if (distance > bestDistance) // 현재 최고 대상보다 멀다면
            {
                return; // 갱신하지 않는다.
            }

            bestDistance = distance; // 최단 갱신
            target = enemy;  // 대상 갱신
        }

        private static void CleanupActiveList() // 목록 정리
        {
            ActiveMonsters.RemoveAll(enemy => enemy == null || enemy.dead); // 죽은 대상 제거
        }
    }
}