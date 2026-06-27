using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemySupportDebuffState : MonoBehaviour
    {
        private float freezeTimer;
        private float incomingDamageMultiplier = 1f;
        private float incomingDamageTimer;
        private float moveSpeedSlowMultiplier = 1f;
        private float moveSpeedSlowTimer;
        private readonly List<Behaviour> disabledByFreezeBehaviours = new List<Behaviour>(6);
        private bool freezeBehavioursDisabled;

        public bool IsFrozen => freezeTimer > 0f;
        public float MoveSpeedMultiplier => moveSpeedSlowTimer > 0f ? Mathf.Clamp(moveSpeedSlowMultiplier, 0.05f, 1f) : 1f;

        public static EnemySupportDebuffState GetOrAdd(EnemyController enemy)
        {
            if (enemy == null)
            {
                return null;
            }

            if (!enemy.TryGetComponent(out EnemySupportDebuffState state))
            {
                state = enemy.gameObject.AddComponent<EnemySupportDebuffState>();
            }

            return state;
        }

        public static bool IsEnemyFrozen(EnemyController enemy) //조성원추가-0622 동결 몬스터 상태 확인
        {
            if (enemy == null) //조성원추가-0622 확인할 몬스터가 없으면 동결상태가 아니다.
            {
                return false; //조성원추가-0622 동결되지 않음으로 반환
            }

            if (!enemy.TryGetComponent(out EnemySupportDebuffState state)) //조성원추가-0622 디버프 상태 확인
            {
                return false; //조성원추가-0622 디버프상태가 없다면 동결되지 않은것으로 반환
            }
            return state.IsFrozen; //조성원추가-0622 현재 동결상태를 반환
        }

        public void ApplyFreeze(float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            EnemySuicideCharger suicideCharger = GetComponent<EnemySuicideCharger>(); // 조성원추가-0626 - 자폭 몬스터의 현재 충전 상태를 확인한다.

            if (suicideCharger != null && suicideCharger.IsCharging) // 조성원추가-0626 - 이미 자폭 준비 중이라면
            {
                return; // 조성원추가-0626 - 동결 상태와 동결 시간을 적용하지 않아 자폭이 중단되지 않게 한다.
            }

            bool wasFrozen = IsFrozen;
            freezeTimer = Mathf.Max(freezeTimer, Mathf.Max(0f, duration));
            if (!wasFrozen)
            {
                DisableFreezeBehaviours();
            }
        }

        public void ApplyIncomingDamageMultiplier(float multiplier, float duration)
        {
            if (multiplier <= 1f || duration <= 0f)
            {
                return;
            }

            incomingDamageMultiplier = Mathf.Max(incomingDamageMultiplier, multiplier);
            incomingDamageTimer = Mathf.Max(incomingDamageTimer, duration);
        }

        public void ApplyMoveSpeedSlow(float multiplier, float duration)
        {
            if (multiplier >= 1f || duration <= 0f)
            {
                return;
            }

            EnemySuicideCharger suicideCharger = GetComponent<EnemySuicideCharger>(); // 조성원추가-0626 - 자폭 몬스터의 현재 충전 상태를 확인한다.

            if (suicideCharger != null && suicideCharger.IsCharging) // 조성원추가-0626 - 이미 자폭 준비 중이라면
            {
                return; // 조성원추가-0626 - 이동속도 감소를 적용하지 않아 자폭 진행 상태를 유지한다.
            }

            moveSpeedSlowMultiplier = Mathf.Min(moveSpeedSlowMultiplier, Mathf.Clamp(multiplier, 0.05f, 1f));
            moveSpeedSlowTimer = Mathf.Max(moveSpeedSlowTimer, duration);
        }

        public DamageData ApplyIncomingDamageBonus(DamageData damage)
        {
            if (incomingDamageTimer <= 0f || incomingDamageMultiplier <= 1f)
            {
                return damage;
            }

            return damage.WithAmount(damage.Amount * incomingDamageMultiplier);
        }

        private void Update()
        {
            if (freezeTimer > 0f)
            {
                freezeTimer -= Time.deltaTime;
                if (freezeTimer <= 0f)
                {
                    RestoreFreezeBehaviours();
                }
            }

            if (incomingDamageTimer <= 0f)
            {
                UpdateMoveSpeedSlowTimer();
                return;
            }

            incomingDamageTimer -= Time.deltaTime;
            if (incomingDamageTimer <= 0f)
            {
                incomingDamageMultiplier = 1f;
            }

            UpdateMoveSpeedSlowTimer();
        }

        private void OnDisable()
        {
            RestoreFreezeBehaviours();
        }

        private void UpdateMoveSpeedSlowTimer()
        {
            if (moveSpeedSlowTimer <= 0f)
            {
                return;
            }

            moveSpeedSlowTimer -= Time.deltaTime;
            if (moveSpeedSlowTimer <= 0f)
            {
                moveSpeedSlowMultiplier = 1f;
            }
        }

        private void DisableFreezeBehaviours()
        {
            if (freezeBehavioursDisabled)
            {
                return;
            }

            disabledByFreezeBehaviours.Clear();
            DisableIfEnabled(GetComponent<EnemyMeleeAttack>());
            DisableIfEnabled(GetComponent<EnemyRangedAttack>());
            DisableIfEnabled(GetComponent<EnemySlowZoneThrower>());
            DisableIfEnabled(GetComponent<EnemyObstacleSummoner>());
            DisableIfEnabled(GetComponent<EnemyBuffCaster>());
            DisableIfEnabled(GetComponent<EnemySuicideCharger>());
            freezeBehavioursDisabled = true;
        }

        private void DisableIfEnabled(Behaviour behaviour)
        {
            if (behaviour == null || !behaviour.enabled)
            {
                return;
            }

            behaviour.enabled = false;
            disabledByFreezeBehaviours.Add(behaviour);
        }

        private void RestoreFreezeBehaviours()
        {
            if (!freezeBehavioursDisabled)
            {
                return;
            }

            for (int i = 0; i < disabledByFreezeBehaviours.Count; i++)
            {
                Behaviour behaviour = disabledByFreezeBehaviours[i];
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            disabledByFreezeBehaviours.Clear();
            freezeBehavioursDisabled = false;
        }
    }
}