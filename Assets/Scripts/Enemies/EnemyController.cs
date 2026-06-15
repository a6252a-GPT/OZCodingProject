using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyController : MonoBehaviour // 몬스터
    {
        private static readonly List<EnemyController> ActiveMonsters = new List<EnemyController>(128); // 타겟 목록
        private static int nextEnemyId; // 몬스터 ID 발급

        public Transform Nexus; // 이동 목표
        public Material MonsterMaterial; // 표시 재질
        [Min(0)] public int ExperienceReward = 1; // 처치 경험치
        [Min(0)] public int GoldReward = 1; // 처치 골드
        [Min(0.1f)] public float MoveSpeed = 1.25f; // 이동 속도
        [Min(0.1f)] public float StopRadius = 1.6f; // 넥서스 도달 거리
        [Min(0.05f)] public float BodyRadius = 0.46f; // 몸통 반경
        [Min(0f)] public float GroundHeight = 0.72f; // 바닥 오프셋
        [Min(0)] public int NexusDamage = 1; // 넥서스 피해량
        public EnemyGrade Grade = EnemyGrade.Monster; // 몬스터 등급

        public int EnemyId { get; private set; } // 외부 식별값

        private bool dead; // 사망 처리됨

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
            EnemyTags.TryApplyTag(gameObject, Grade); // 태그 보장

            if (Nexus == null)
            {
                GameObject nexusObject = GameObject.Find("Nexus_Core"); // 넥서스 검색
                Nexus = nexusObject != null ? nexusObject.transform : null; // 목표 연결
            }
        }

        private void Update() // 이동 루프
        {
            if (dead || Nexus == null)
            {
                return; // 처리 불가
            }

            Vector3 offset = Nexus.position - transform.position; // 넥서스 방향
            offset.y = 0f; // 평면 이동
            if (offset.sqrMagnitude <= StopRadius * StopRadius)
            {
                NexusController.TryApplyDamage(Nexus, NexusDamage); // 넥서스 피해 요청
                Kill(); // 넥서스 도달 제거
                return; // 종료
            }

            Vector3 direction = offset.normalized; // 이동 방향
            Vector3 desiredPosition = transform.position + direction * (MoveSpeed * Time.deltaTime); // 다음 위치
            desiredPosition = GroundService.ProjectToGround(desiredPosition, GroundHeight); // 바닥 보정
            Vector3 position = SegmentBlocker.ResolveMonsterPosition(transform.position, desiredPosition, BodyRadius); // 세그먼트 차단
            transform.position = position; // 위치 적용
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up); // 목표 바라보기
        }

        public void Configure(Transform nexus, Material material, float moveSpeed, float stopRadius, float groundHeight, EnemyGrade grade = EnemyGrade.Monster) // 스폰 설정
        {
            Nexus = nexus; // 목표 저장
            MonsterMaterial = material; // 재질 저장
            MoveSpeed = moveSpeed; // 속도 적용
            StopRadius = stopRadius; // 도달 거리
            GroundHeight = groundHeight; // 높이 적용
            Grade = grade; // 등급 저장
            EnemyTags.TryApplyTag(gameObject, Grade); // 태그 적용
            ApplyMaterial(); // 표시 적용
        }

        public void ApplyDamage(DamageData damage) // 피해 받기
        {
            if (!damage.IsValid)
            {
                return; // 피해 없음
            }

            KillByDamage(damage); // 현재 MVP 한 방 처치
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

            RewardData reward = RewardData.Create(ExperienceReward, GoldReward, EnemyId, transform.position); // 보상 생성
            GrowthRewardReceiver.SubmitReward(reward); // 보상 입구 전달
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
            if (renderer != null && MonsterMaterial != null)
            {
                renderer.sharedMaterial = MonsterMaterial; // 몬스터 재질
            }
        }

        private static void CleanupActiveList() // 목록 정리
        {
            ActiveMonsters.RemoveAll(monster => monster == null || monster.dead); // 죽은 대상 제거
        }
    }
}

