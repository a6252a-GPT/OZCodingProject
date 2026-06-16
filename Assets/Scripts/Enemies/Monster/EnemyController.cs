using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyController : MonoBehaviour // 몬스터
    {
        private static readonly List<EnemyController> ActiveMonsters = new List<EnemyController>(128); // 타겟 목록
        private static int nextEnemyId; // 몬스터 ID 발급

        [SerializeField] private Transform nexus; // 이동 목표
        [SerializeField] private Material monsterMaterial; // 표시 재질

        [Min(0)]
        [SerializeField] private int experienceReward = 1; // 처치 경험치 EnemyReward 기준이 되면 추후 삭제

        [Min(0)]
        [SerializeField] private int goldReward = 1; // 처치 골드 EnemyReward 기준이 되면 추후 삭제

        [Min(0.1f)]
        [SerializeField] private float moveSpeed = 1.25f; // 이동 속도

        [Min(0.1f)]
        [SerializeField] private float stopRadius = 1.6f; // 넥서스 도달 거리

        [Min(0.05f)]
        [SerializeField] private float bodyRadius = 0.46f; // 몸통 반경

        [Min(0f)]
        [SerializeField] private float groundHeight = 0.72f; // 바닥 오프셋

        [Min(0)]
        [SerializeField] private int nexusDamage = 1; // 넥서스 피해량

        [SerializeField] private EnemyGrade grade = EnemyGrade.Monster; // 몬스터 등급

        public int EnemyId { get; private set; } // 외부 식별값

        private bool dead; // 사망 처리됨

        private EnemyHealth health; // EnemyController가 EnemyHealth Script Component를 사용하기 위해 저장하는 참조
        private EnemyReward reward; // EnemyController가 EnemyReward Script Component를 사용하기 위해 저장하는 참조
        private EnemyMovement movement; // EnemyController가 EnemyMovement Script Component를 사용하기 위해 저장하는 참조

        public static int ActiveCount // 활성 수
        {
            get
            {
                CleanupActiveList(); // null 정리
                return ActiveMonsters.Count; // 현재 수
            }
        }

        private void OnEnable() // 목록 등록
        {
            if (!ActiveMonsters.Contains(this))
            {
                ActiveMonsters.Add(this); // 타겟 등록
            }
        }

        private void OnDisable() // 목록 해제
        {
            ActiveMonsters.Remove(this); // 타겟 제거
        }

        private void Awake() // 기본 보강
        {
            EnemyId = ++nextEnemyId; // ID 부여
            EnemyTags.TryApplyTag(gameObject, grade); // 태그 보장

            health = GetComponent<EnemyHealth>(); // 같은 GameObject에 붙은 EnemyHealth Script Component를 찾는다.
            reward = GetComponent<EnemyReward>(); // 같은 GameObject에 붙은 EnemyReward Script Component를 찾는다.
            movement = GetComponent<EnemyMovement>(); // 같은 GameObject에 붙은 EnemyMovement Script Component를 찾는다.

            if (nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                nexus = nexusObject != null ? nexusObject.transform : null; // 목표 연결
            }
        }

        private void Update()
        {
            if (dead) // 이미 사망 처리된 몬스터라면
            {
                return; // 작동하지 않는다.
            }

            if (movement != null) // EnemyMovement Script Component가 붙어 있다면
            {
                return; // 이동하지 않는다.
            }

            if (nexus == null) // 이동 목표가 없다면
            {
                return; // 이동 계산을 하지 않고 종료한다.
            }

            Vector3 offset = nexus.position - transform.position; // 넥서스 방향
            offset.y = 0f; // 평면 이동

            if (offset.sqrMagnitude <= stopRadius * stopRadius)
            {
                NexusController.TryApplyDamage(nexus, nexusDamage); // 기존 예시 구조에서는 넥서스 도달 시 피해 요청
                Kill(); // 기존 예시 구조에서는 넥서스 도달 시 제거
                return; // 종료
            }

            Vector3 direction = offset.normalized; // 이동 방향
            Vector3 desiredPosition = transform.position + direction * (moveSpeed * Time.deltaTime); // 다음 위치
            desiredPosition = GroundService.ProjectToGround(desiredPosition, groundHeight); // 바닥 보정
            Vector3 position = SegmentBlocker.ResolveMonsterPosition(transform.position, desiredPosition, bodyRadius); // 세그먼트 차단
            transform.position = position; // 위치 적용
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 목표 바라보기
        }

        // EnemyMovement 없으면 EnemyController.Update가 이동, EnemyMovement 있으면 EnemyMovement.Update가 이동
        public void Configure(Transform nexus, Material material, float moveSpeed, float stopRadius, float groundHeight, EnemyGrade grade = EnemyGrade.Monster) // 스포너가 생성된 몬스터에 초기값을 넣어주는 함수
        {
            this.nexus = nexus; // 이동 목표 Nexus를 EnemyController에 저장한다.
            monsterMaterial = material; // 표시 재질을 저장한다.
            this.moveSpeed = moveSpeed; // 기존 예시 이동 구조에서 사용할 이동 속도를 저장한다.
            this.stopRadius = stopRadius; // 기존 예시 이동 구조에서 사용할 도달 거리를 저장한다.
            this.groundHeight = groundHeight; // 기존 예시 이동 구조에서 사용할 바닥 높이 오프셋을 저장한다.
            this.grade = grade; // 몬스터 등급을 저장한다.

            EnemyTags.TryApplyTag(gameObject, this.grade); // 몬스터 등급에 맞는 Unity Tag를 적용한다.
            ApplyMaterial(); // Renderer가 있다면 표시 재질을 적용한다.

            if (movement != null) // 같은 GameObject에 EnemyMovement Script Component가 붙어 있다면
            {
                movement.Configure(this.nexus, this.moveSpeed, this.stopRadius, this.groundHeight); // 실제 이동 담당 Script Component에도 이동 초기값을 전달한다.
            }
        }

        public void ApplyDamage(DamageData damage) // 피해 요청 입구
        {
            if (!damage.IsValid)
            {
                return; // 피해 없음
            }

            if (health == null)
            {
                KillByDamage(damage); // EnemyHealth가 없는 기존 예시 몬스터라면 기존처럼 한 방 처치로 처리한다.
                return;
            }

            health.TakeDamage(damage.Amount); // 실제 HP 감소는 EnemyHealth가 담당

            if (health.IsDead)
            {
                KillByDamage(damage); // HP가 0 이하가 된 경우에만 보상 지급 후 제거
            }
        }

        public void Kill() // 즉시 사망
        {
            if (dead)
            {
                return; // 중복 방지
            }

            dead = true; // 사망 표시
            Destroy(gameObject); // 몬스터 제거
        }

        private void KillByDamage(DamageData damage) // 피해 사망
        {
            if (dead)
            {
                return; // 중복 방지
            }

            if (reward != null)
            {
                reward.GiveReward(EnemyId, transform.position); // 보상 처리는 EnemyReward가 담당
            }
            else
            {
                RewardData rewardData = RewardData.Create(experienceReward, goldReward, EnemyId, transform.position); // EnemyReward가 없으면 기존 RewardData 방식 사용
                RewardGateway.SubmitReward(rewardData); // 보상 입구 전달
            }

            Kill(); // 공통 제거
        }

        public static bool TryFindNearest(Vector3 origin, float range, out EnemyController target) // 가까운 적 검색
        {
            CleanupActiveList(); // null 정리
            target = null; // 기본값
            float bestDistance = range * range; // 사거리 제곱

            string[] tags = EnemyTags.TargetTags; // 탐색 태그
            bool foundRegisteredTag = false; // 태그 등록 여부

            for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
            {
                GameObject[] candidates = FindObjectsByTag(tags[tagIndex], out bool tagRegistered); // 태그 후보
                foundRegisteredTag |= tagRegistered; // 등록 확인

                for (int i = 0; i < candidates.Length; i++)
                {
                    EnemyController monster = candidates[i].GetComponentInParent<EnemyController>(); // 몬스터 확인
                    TryPickNearest(monster, origin, ref bestDistance, ref target); // 최단 갱신
                }
            }

            if (!foundRegisteredTag)
            {
                for (int i = 0; i < ActiveMonsters.Count; i++)
                {
                    TryPickNearest(ActiveMonsters[i], origin, ref bestDistance, ref target); // 태그 세팅 전 임시 탐색
                }
            }

            return target != null; // 발견 여부
        }

        private static GameObject[] FindObjectsByTag(string tagName, out bool tagRegistered) // 태그 검색
        {
            try
            {
                tagRegistered = true; // 등록됨
                return GameObject.FindGameObjectsWithTag(tagName); // Unity 태그 검색
            }
            catch (UnityException)
            {
                tagRegistered = false; // 미등록
                return System.Array.Empty<GameObject>(); // 태그 미등록
            }
        }

        private static void TryPickNearest(EnemyController monster, Vector3 origin, ref float bestDistance, ref EnemyController target) // 최단 후보
        {
            if (monster == null || monster.dead)
            {
                return; // 대상 아님
            }

            Vector3 offset = monster.transform.position - origin; // 후보 거리
            offset.y = 0f; // 평면 거리
            float distance = offset.sqrMagnitude; // 제곱 거리

            if (distance > bestDistance)
            {
                return; // 사거리 밖
            }

            bestDistance = distance; // 최단 갱신
            target = monster; // 대상 갱신
        }

        private void ApplyMaterial() // 재질 적용
        {
            Renderer renderer = GetComponent<Renderer>(); // 표시 renderer

            if (renderer != null && monsterMaterial != null)
            {
                renderer.sharedMaterial = monsterMaterial; // 몬스터 재질
            }
        }

        private static void CleanupActiveList() // 목록 정리
        {
            ActiveMonsters.RemoveAll(monster => monster == null || monster.dead); // 죽은 대상 제거
        }
    }
}