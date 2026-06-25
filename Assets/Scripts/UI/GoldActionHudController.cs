using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TeamProject01.Gameplay
{
    public sealed class GoldActionHudController : MonoBehaviour
    {
        public enum GoldActionSkillKind
        {
            Meteor,
            Shockwave,
            TimeStop,
            NexusHeal,
            NexusShieldUpgrade
        }

        [Serializable]
        public sealed class SkillDefinition
        {
            public GoldActionSkillKind Kind;
            public string DisplayName;
            [Range(1, 5)] public int KeyNumber = 1;
            [Min(1)] public int UnlockLevel = 1;
            [Min(0)] public int BaseCost;
            [Min(1)] public int MaxLevel = 5;
            [Min(0f)] public float CooldownSeconds = 90f;
            public bool RequiresPurchase = true;
            public bool CanUpgrade = true;
            public bool CostOnUse;
            public bool RepeatPurchase;
            public Sprite Icon;
        }

        [Serializable]
        private sealed class SkillState
        {
            public bool Purchased;
            public int UpgradeLevel;
            public int RepeatPurchaseCount;
            public float CooldownEndsAt;
        }

        public SkillDefinition[] Skills = Array.Empty<SkillDefinition>();
        public GoldActionHudSlot[] Slots = Array.Empty<GoldActionHudSlot>();
        public CoreStatProvider CoreStats;
        public GoldActionMeteorSkill MeteorSkill; // 1번 메테오 실제 효과
        public GoldActionShieldShockwaveSkill ShockwaveSkill; // 2번 보호막 충격파 실제 효과
        public GoldActionTimeStopSkill TimeStopSkill; // 3번 타임스탑 실제 효과
        public GoldActionNexusHealSkill NexusHealSkill; // 4번 넥서스 회복 실제 효과
        public GoldActionNexusShieldUpgradeSkill ShieldUpgradeSkill; // 5번 실드 강화 실제 효과

        [Header("Default Values")]
        [Min(1)] public int UpgradeCostMultiplier = 2;
        [Min(1)] public int ShieldUpgradeCostMultiplier = 2;
        [Min(1)] public int DefaultMaxSkillLevel = 5;

        private SkillState[] states = Array.Empty<SkillState>();

        private void Awake()
        {
            EnsureDefaults();
            EnsureReferences();
            WireButtons();
        }

        private void OnEnable()
        {
            EnsureDefaults();
            EnsureReferences();
            WireButtons();
            RefreshAll();
        }

        private void Update()
        {
            HandleKeyboardInput();
            RefreshAll();
        }

        private void EnsureDefaults()
        {
            if (Skills == null || Skills.Length != 5)
            {
                Skills = CreateDefaultSkills();
            }

            NormalizeSkillDefinitions(); // 기존 씬 직렬화값 보정

            if (states == null || states.Length != Skills.Length)
            {
                SkillState[] next = new SkillState[Skills.Length];
                for (int i = 0; i < next.Length; i++)
                {
                    next[i] = states != null && i < states.Length && states[i] != null ? states[i] : new SkillState();
                }

                states = next;
            }
        }

        private void NormalizeSkillDefinitions() // 스킬 정의 보정
        {
            if (Skills == null)
            {
                return; // 대상 없음
            }

            for (int i = 0; i < Skills.Length; i++)
            {
                SkillDefinition skill = Skills[i]; // 현재 스킬
                if (skill == null)
                {
                    continue; // 비어 있음
                }

                if (skill.MaxLevel <= 0)
                {
                    skill.MaxLevel = GetDefaultMaxLevel(skill.Kind); // 기존 씬 0값 보정
                }
            }
        }

        private void EnsureReferences()
        {
            if (CoreStats == null)
            {
                CoreStats = CoreStatProvider.Active != null ? CoreStatProvider.Active : FindFirstObjectByType<CoreStatProvider>();
            }

            if (MeteorSkill == null)
            {
                MeteorSkill = GetComponent<GoldActionMeteorSkill>(); // 같은 HUD 루트 우선
            }

            if (MeteorSkill == null)
            {
                MeteorSkill = FindFirstObjectByType<GoldActionMeteorSkill>(); // 씬 배치 fallback
            }

            if (ShockwaveSkill == null)
            {
                ShockwaveSkill = GetComponent<GoldActionShieldShockwaveSkill>(); // 같은 HUD 루트 우선
            }

            if (ShockwaveSkill == null)
            {
                ShockwaveSkill = FindFirstObjectByType<GoldActionShieldShockwaveSkill>(); // 씬 배치 fallback
            }

            if (ShockwaveSkill == null)
            {
                ShockwaveSkill = gameObject.AddComponent<GoldActionShieldShockwaveSkill>(); // 임시 씬 연결 없이도 발동
            }

            if (TimeStopSkill == null)
            {
                TimeStopSkill = GetComponent<GoldActionTimeStopSkill>(); // 같은 HUD 루트 우선
            }

            if (TimeStopSkill == null)
            {
                TimeStopSkill = FindFirstObjectByType<GoldActionTimeStopSkill>(); // 씬 배치 fallback
            }

            if (TimeStopSkill == null)
            {
                TimeStopSkill = gameObject.AddComponent<GoldActionTimeStopSkill>(); // 임시 씬 연결 없이도 발동
            }

            if (NexusHealSkill == null)
            {
                NexusHealSkill = GetComponent<GoldActionNexusHealSkill>(); // 같은 HUD 루트 우선
            }

            if (NexusHealSkill == null)
            {
                NexusHealSkill = FindFirstObjectByType<GoldActionNexusHealSkill>(); // 씬 배치 fallback
            }

            if (NexusHealSkill == null)
            {
                NexusHealSkill = gameObject.AddComponent<GoldActionNexusHealSkill>(); // 임시 씬 연결 없이도 발동
            }

            if (ShieldUpgradeSkill == null)
            {
                ShieldUpgradeSkill = GetComponent<GoldActionNexusShieldUpgradeSkill>(); // 같은 HUD 루트 우선
            }

            if (ShieldUpgradeSkill == null)
            {
                ShieldUpgradeSkill = FindFirstObjectByType<GoldActionNexusShieldUpgradeSkill>(); // 씬 배치 fallback
            }

            if (ShieldUpgradeSkill == null)
            {
                ShieldUpgradeSkill = gameObject.AddComponent<GoldActionNexusShieldUpgradeSkill>(); // 임시 씬 연결 없이도 발동
            }
        }

        private void WireButtons()
        {
            if (Slots == null)
            {
                return;
            }

            for (int i = 0; i < Slots.Length; i++)
            {
                int index = i;
                if (Slots[i] != null)
                {
                    Slots[i].BindButton(() => HandleSlotButton(index));
                }
            }
        }

        private void HandleKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || GameplayInputBlocker.IsGameplayInputBlocked)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) TryUseSkillByKey(1);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) TryUseSkillByKey(2);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) TryUseSkillByKey(3);
            if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) TryUseSkillByKey(4);
            if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame) TryUseSkillByKey(5);
        }

        private void TryUseSkillByKey(int keyNumber)
        {
            for (int i = 0; i < Skills.Length; i++)
            {
                if (Skills[i] != null && Skills[i].KeyNumber == keyNumber)
                {
                    TryUseSkill(i);
                    return;
                }
            }
        }

        private void HandleSlotButton(int index)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || !IsUnlocked(skill))
            {
                return;
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                TryUseSkill(index);
                return;
            }

            if (skill.RepeatPurchase)
            {
                TryRepeatPurchase(index);
                return;
            }

            if (!state.Purchased)
            {
                TryPurchase(index);
                return;
            }

            TryUpgrade(index);
        }

        private bool TryUseSkill(int index)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || !IsUnlocked(skill))
            {
                return false;
            }

            if (skill.RepeatPurchase)
            {
                return TryRepeatPurchase(index);
            }

            if (skill.RequiresPurchase && !state.Purchased && skill.Kind != GoldActionSkillKind.NexusHeal)
            {
                return false;
            }

            if (Time.time < state.CooldownEndsAt)
            {
                return false;
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal && !CanPlayNexusHeal())
            {
                return false; // 회복 불필요/불가 시 골드 소모 방지
            }

            if (skill.CostOnUse && !SpendGold(skill.BaseCost))
            {
                return false;
            }

            if (!ApplySkillEffect(skill, state))
            {
                return false;
            }

            if (skill.CooldownSeconds > 0f)
            {
                state.CooldownEndsAt = Time.time + skill.CooldownSeconds;
            }

            return true;
        }

        private bool TryPurchase(int index)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || state.Purchased || !IsUnlocked(skill))
            {
                return false;
            }

            if (!SpendGold(skill.BaseCost))
            {
                return false;
            }

            state.Purchased = true;
            state.UpgradeLevel = 1;
            return true;
        }

        private bool TryUpgrade(int index)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || !state.Purchased || !skill.CanUpgrade)
            {
                return false;
            }

            if (IsMaxLevel(skill, state))
            {
                return false; // 최대 레벨
            }

            int cost = GetUpgradeCost(skill, state);
            if (!SpendGold(cost))
            {
                return false;
            }

            state.UpgradeLevel++;
            return true;
        }

        private bool TryRepeatPurchase(int index)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || !IsUnlocked(skill))
            {
                return false;
            }

            bool isShieldUpgrade = skill.Kind == GoldActionSkillKind.NexusShieldUpgrade; // 5번 반복 강화
            if (IsMaxLevel(skill, state))
            {
                return false; // 최대 레벨
            }

            if (isShieldUpgrade && !CanPlayShieldUpgrade())
            {
                return false; // 보호막 VFX 없음
            }

            int cost = GetRepeatPurchaseCost(skill, state);
            if (!SpendGold(cost))
            {
                return false;
            }

            state.Purchased = true;
            state.RepeatPurchaseCount++;
            state.UpgradeLevel = Mathf.Max(1, state.RepeatPurchaseCount);
            if (isShieldUpgrade && !TryPlayShieldUpgrade(state))
            {
                state.RepeatPurchaseCount = Mathf.Max(0, state.RepeatPurchaseCount - 1); // 실패 시 상태 복구
                state.UpgradeLevel = Mathf.Max(0, state.RepeatPurchaseCount); // 레벨 복구
                return false; // 적용 실패
            }

            Debug.Log($"[GoldActionHud] Nexus shield upgrade: level {state.RepeatPurchaseCount}, cost {cost}", this);
            return true;
        }

        private bool ApplySkillEffect(SkillDefinition skill, SkillState state)
        {
            if (skill.Kind == GoldActionSkillKind.Meteor)
            {
                return TryPlayMeteor(state); // 메테오 발동
            }

            if (skill.Kind == GoldActionSkillKind.Shockwave)
            {
                return TryPlayShockwave(state); // 보호막 충격파 발동
            }

            if (skill.Kind == GoldActionSkillKind.TimeStop)
            {
                return TryPlayTimeStop(state); // 타임스탑 발동
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                return TryPlayNexusHeal(state); // 넥서스 회복 발동
            }

            Debug.Log($"[GoldActionHud] {skill.DisplayName} reserved: Lv{Mathf.Max(1, state.UpgradeLevel)}", this);
            return true;
        }

        private bool TryPlayMeteor(SkillState state) // 1번 액션 HUD 메테오
        {
            EnsureReferences();

            if (MeteorSkill == null)
            {
                Debug.LogWarning("[GoldActionHud] GoldActionMeteorSkill 컴포넌트가 없어 메테오를 발동할 수 없습니다.", this);
                return false; // 정식 스킬 컴포넌트 필요
            }

            int level = Mathf.Max(1, state.UpgradeLevel); // 구매 후 강화 레벨
            bool played = MeteorSkill.Play(level); // 메테오 실제 발동
            if (played)
            {
                Debug.Log($"[GoldActionHud] Meteor cast: Lv{level}", this); // 발동 로그
            }

            return played; // 발동 성공 여부
        }

        private bool TryPlayShieldUpgrade(SkillState state) // 5번 액션 HUD 실드 강화
        {
            EnsureReferences();

            if (ShieldUpgradeSkill == null)
            {
                Debug.LogWarning("[GoldActionHud] GoldActionNexusShieldUpgradeSkill 컴포넌트가 없어 실드 강화를 발동할 수 없습니다.", this);
                return false; // 정식 스킬 컴포넌트 필요
            }

            int level = Mathf.Max(1, state.UpgradeLevel); // 강화 레벨
            bool played = ShieldUpgradeSkill.Play(level); // 실드 강화 실제 발동
            if (played)
            {
                Debug.Log($"[GoldActionHud] Nexus shield upgrade cast: Lv{level}", this); // 발동 로그
            }

            return played; // 발동 성공 여부
        }

        private bool TryPlayNexusHeal(SkillState state) // 4번 액션 HUD 넥서스 회복
        {
            EnsureReferences();

            if (NexusHealSkill == null)
            {
                Debug.LogWarning("[GoldActionHud] GoldActionNexusHealSkill 컴포넌트가 없어 넥서스 회복을 발동할 수 없습니다.", this);
                return false; // 정식 스킬 컴포넌트 필요
            }

            int level = Mathf.Max(1, state.UpgradeLevel); // 회복 레벨
            bool played = NexusHealSkill.Play(level); // 넥서스 회복 실제 발동
            if (played)
            {
                Debug.Log($"[GoldActionHud] Nexus heal cast: Lv{level}", this); // 발동 로그
            }

            return played; // 발동 성공 여부
        }

        private bool TryPlayTimeStop(SkillState state) // 3번 액션 HUD 타임스탑
        {
            EnsureReferences();

            if (TimeStopSkill == null)
            {
                Debug.LogWarning("[GoldActionHud] GoldActionTimeStopSkill 컴포넌트가 없어 타임스탑을 발동할 수 없습니다.", this);
                return false; // 정식 스킬 컴포넌트 필요
            }

            int level = Mathf.Max(1, state.UpgradeLevel); // 구매 후 강화 레벨
            bool played = TimeStopSkill.Play(level); // 타임스탑 실제 발동
            if (played)
            {
                Debug.Log($"[GoldActionHud] Time stop cast: Lv{level}", this); // 발동 로그
            }

            return played; // 발동 성공 여부
        }

        private bool TryPlayShockwave(SkillState state) // 2번 액션 HUD 보호막 충격파
        {
            EnsureReferences();

            if (ShockwaveSkill == null)
            {
                Debug.LogWarning("[GoldActionHud] GoldActionShieldShockwaveSkill 컴포넌트가 없어 충격파를 발동할 수 없습니다.", this);
                return false; // 정식 스킬 컴포넌트 필요
            }

            int level = Mathf.Max(1, state.UpgradeLevel); // 구매 후 강화 레벨
            bool played = ShockwaveSkill.Play(level); // 보호막 충격파 실제 발동
            if (played)
            {
                Debug.Log($"[GoldActionHud] Shield shockwave cast: Lv{level}", this); // 발동 로그
            }

            return played; // 발동 성공 여부
        }

        private void RefreshAll()
        {
            EnsureDefaults();
            CoreStatData stats = CoreStats != null ? CoreStats.CurrentStats : CoreStatProvider.GetCurrentOrDefault();
            for (int i = 0; i < Skills.Length && Slots != null && i < Slots.Length; i++)
            {
                RefreshSlot(i, stats);
            }
        }

        private void RefreshSlot(int index, CoreStatData stats)
        {
            if (!TryGetSkill(index, out SkillDefinition skill, out SkillState state) || Slots[index] == null)
            {
                return;
            }

            bool unlocked = stats.Level >= skill.UnlockLevel;
            bool coolingDown = Time.time < state.CooldownEndsAt;
            float remaining = Mathf.Max(0f, state.CooldownEndsAt - Time.time);
            float cooldownRatio = skill.CooldownSeconds <= 0f ? 0f : remaining / skill.CooldownSeconds;
            bool iconActive = IsIconActive(skill, state, stats, unlocked, coolingDown);
            string cooldownLabel = coolingDown ? FormatSeconds(remaining) : string.Empty;
            string buttonLabel = BuildButtonLabel(skill, state, unlocked);
            string levelLabel = BuildLevelLabel(skill, state, unlocked);
            bool buttonEnabled = CanPressButton(skill, state, stats, unlocked, coolingDown);

            Slots[index].Refresh(
                skill.Icon,
                skill.KeyNumber.ToString(),
                skill.DisplayName,
                levelLabel,
                unlocked ? string.Empty : $"Lv{skill.UnlockLevel}",
                cooldownLabel,
                cooldownRatio,
                buttonLabel,
                buttonEnabled,
                !unlocked,
                iconActive,
                coolingDown);

            Slots[index].SetTooltipContent(
                BuildTooltipTitle(skill),
                BuildTooltipBody(skill, state, stats, unlocked, coolingDown, remaining),
                BuildTooltipFooter(skill, state, stats, unlocked));
        }

        private static bool IsIconActive(SkillDefinition skill, SkillState state, CoreStatData stats, bool unlocked, bool coolingDown)
        {
            if (!unlocked || coolingDown)
            {
                return false;
            }

            if (state.Purchased)
            {
                return true;
            }

            if (skill.CostOnUse && !skill.RequiresPurchase)
            {
                return stats.Gold >= skill.BaseCost;
            }

            return false;
        }

        private string BuildButtonLabel(SkillDefinition skill, SkillState state, bool unlocked)
        {
            if (!unlocked)
            {
                return $"Lv{skill.UnlockLevel}";
            }

            if (IsMaxLevel(skill, state) && skill.Kind != GoldActionSkillKind.NexusHeal)
            {
                return "MAX";
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                return $"회복 {skill.BaseCost}G";
            }

            if (skill.RepeatPurchase)
            {
                return $"강화 {GetRepeatPurchaseCost(skill, state)}G";
            }

            if (!state.Purchased)
            {
                return $"구매 {skill.BaseCost}G";
            }

            return skill.CanUpgrade ? $"강화 {GetUpgradeCost(skill, state)}G" : string.Empty;
        }

        private string BuildLevelLabel(SkillDefinition skill, SkillState state, bool unlocked) // 슬롯 레벨 표시
        {
            if (!unlocked)
            {
                return $"Lv0/{GetMaxLevel(skill)}"; // 잠금 전
            }

            return $"Lv{GetCurrentSkillLevel(skill, state)}/{GetMaxLevel(skill)}"; // 현재/최대
        }

        private bool CanPressButton(SkillDefinition skill, SkillState state, CoreStatData stats, bool unlocked, bool coolingDown)
        {
            if (!unlocked)
            {
                return false;
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                return !coolingDown && stats.Gold >= skill.BaseCost && CanPlayNexusHeal();
            }

            if (skill.RepeatPurchase)
            {
                if (IsMaxLevel(skill, state))
                {
                    return false; // 최대 레벨
                }

                if (skill.Kind == GoldActionSkillKind.NexusShieldUpgrade && !CanPlayShieldUpgrade())
                {
                    return false; // 보호막 VFX 없음
                }

                return stats.Gold >= GetRepeatPurchaseCost(skill, state);
            }

            if (!state.Purchased)
            {
                return stats.Gold >= skill.BaseCost;
            }

            return skill.CanUpgrade && !IsMaxLevel(skill, state) && stats.Gold >= GetUpgradeCost(skill, state);
        }

        private string BuildTooltipTitle(SkillDefinition skill)
        {
            return $"{skill.KeyNumber}. {skill.DisplayName}";
        }

        private string BuildTooltipBody(SkillDefinition skill, SkillState state, CoreStatData stats, bool unlocked, bool coolingDown, float remaining)
        {
            List<string> lines = new List<string>
            {
                GetSkillSummary(skill.Kind)
            };

            if (!unlocked)
            {
                lines.Add($"상태: 잠김 - Lv{skill.UnlockLevel} 필요");
            }
            else if (coolingDown)
            {
                lines.Add($"상태: 쿨타임 {FormatSeconds(remaining)} 남음");
            }
            else if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                if (!CanPlayNexusHeal())
                {
                    lines.Add("상태: 넥서스 체력 가득 참");
                }
                else
                {
                    lines.Add(stats.Gold >= skill.BaseCost ? "상태: 회복 가능" : "상태: 골드 부족");
                }
            }
            else if (skill.RepeatPurchase)
            {
                int cost = GetRepeatPurchaseCost(skill, state);
                if (skill.Kind == GoldActionSkillKind.NexusShieldUpgrade && !CanPlayShieldUpgrade())
                {
                    lines.Add("상태: Nexus_ManaWall_Shield 없음");
                }
                else
                {
                    lines.Add(stats.Gold >= cost ? "상태: 보호막 강화 가능" : "상태: 골드 부족");
                }
            }
            else if (!state.Purchased)
            {
                lines.Add(stats.Gold >= skill.BaseCost ? "상태: 구매 가능" : "상태: 골드 부족");
            }
            else
            {
                lines.Add("상태: 사용 가능");
                if (skill.CanUpgrade)
                {
                    lines.Add($"현재 강화: Lv{Mathf.Max(1, state.UpgradeLevel)}");
                }
            }

            if (unlocked)
            {
                lines.Add($"스킬 레벨: Lv{GetCurrentSkillLevel(skill, state)}/{GetMaxLevel(skill)}"); // 현재 레벨
                if (IsMaxLevel(skill, state) && skill.Kind != GoldActionSkillKind.NexusHeal)
                {
                    lines.Add("상태: 최대 레벨"); // 강화 완료
                }
            }

            if (skill.CooldownSeconds > 0f)
            {
                lines.Add($"쿨타임: {FormatSeconds(skill.CooldownSeconds)}");
            }

            lines.Add("세부 효과 수치는 추후 기획 확정");
            return string.Join("\n", lines);
        }

        private string BuildTooltipFooter(SkillDefinition skill, SkillState state, CoreStatData stats, bool unlocked)
        {
            string actionText;
            if (!unlocked)
            {
                actionText = $"Lv{skill.UnlockLevel}부터 구매 가능";
            }
            else if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                actionText = $"사용 비용 {skill.BaseCost}G";
            }
            else if (skill.RepeatPurchase)
            {
                actionText = IsMaxLevel(skill, state) ? "최대 레벨" : $"강화 비용 {GetRepeatPurchaseCost(skill, state)}G";
            }
            else if (!state.Purchased)
            {
                actionText = $"구매 비용 {skill.BaseCost}G";
            }
            else if (skill.CanUpgrade)
            {
                actionText = IsMaxLevel(skill, state) ? "최대 레벨" : $"다음 강화 {GetUpgradeCost(skill, state)}G";
            }
            else
            {
                actionText = "추가 구매 없음";
            }

            return $"보유 골드 {stats.Gold}G / {actionText}";
        }

        private string GetSkillSummary(GoldActionSkillKind kind)
        {
            switch (kind)
            {
                case GoldActionSkillKind.Meteor:
                    return "광역 메테오: 1회 사용으로 2.5초 간격 3웨이브를 발동해 넥서스 주변 우선 운석비를 떨어뜨립니다.";
                case GoldActionSkillKind.Shockwave:
                    return "보호막 충격파: 넥서스 실드가 X/Z로 크게 퍼지며 주변 적을 대규모로 밀어냅니다.";
                case GoldActionSkillKind.TimeStop:
                    return "타임스탑: 넥서스 주변 전장 범위의 몬스터 이동과 공격을 일정 시간 정지시킵니다.";
                case GoldActionSkillKind.NexusHeal:
                    return "넥서스회복: 골드를 소모해 넥서스 체력을 회복하고 VFX_Heal_Cast를 넥서스에서 재생합니다.";
                case GoldActionSkillKind.NexusShieldUpgrade:
                    return "넥서스보호막 업그레이드: VFX_Heal_Cast를 재생하고 보호막 ManaWall의 Y 스케일을 2씩 키웁니다.";
                default:
                    return "HUD 액션: 세부 설명은 추후 확정됩니다.";
            }
        }

        private bool IsUnlocked(SkillDefinition skill)
        {
            CoreStatData stats = CoreStats != null ? CoreStats.CurrentStats : CoreStatProvider.GetCurrentOrDefault();
            return stats.Level >= skill.UnlockLevel;
        }

        private bool SpendGold(int amount)
        {
            EnsureReferences();
            return CoreStats != null && CoreStats.TrySpendGold(amount);
        }

        private bool CanPlayNexusHeal() // 4번 회복 가능 여부
        {
            EnsureReferences();
            return NexusHealSkill != null && NexusHealSkill.CanHeal(); // 체력 부족 상태
        }

        private bool CanPlayShieldUpgrade() // 5번 실드 강화 가능 여부
        {
            EnsureReferences();
            return ShieldUpgradeSkill != null && ShieldUpgradeSkill.CanUpgradeShieldVisual(); // 보호막 VFX 존재
        }

        private int GetCurrentSkillLevel(SkillDefinition skill, SkillState state) // 현재 스킬 레벨
        {
            if (skill == null || state == null)
            {
                return 0; // 무효
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                return 1; // 사용형 회복은 기본 Lv1
            }

            if (skill.RepeatPurchase)
            {
                return Mathf.Clamp(state.RepeatPurchaseCount, 0, GetMaxLevel(skill)); // 반복 강화 레벨
            }

            return state.Purchased ? Mathf.Clamp(Mathf.Max(1, state.UpgradeLevel), 1, GetMaxLevel(skill)) : 0; // 구매형 레벨
        }

        private int GetMaxLevel(SkillDefinition skill) // 최대 스킬 레벨
        {
            if (skill == null)
            {
                return Mathf.Max(1, DefaultMaxSkillLevel); // 기본값
            }

            return Mathf.Max(1, skill.MaxLevel > 0 ? skill.MaxLevel : GetDefaultMaxLevel(skill.Kind)); // 직렬화 0 보정
        }

        private bool IsMaxLevel(SkillDefinition skill, SkillState state) // 최대 레벨 여부
        {
            return GetCurrentSkillLevel(skill, state) >= GetMaxLevel(skill); // 현재 >= 최대
        }

        private int GetDefaultMaxLevel(GoldActionSkillKind kind) // 기본 최대 레벨
        {
            return kind == GoldActionSkillKind.NexusHeal ? 1 : Mathf.Max(1, DefaultMaxSkillLevel); // 회복은 사용형
        }

        private bool TryGetSkill(int index, out SkillDefinition skill, out SkillState state)
        {
            skill = null;
            state = null;
            if (Skills == null || states == null || index < 0 || index >= Skills.Length || index >= states.Length)
            {
                return false;
            }

            skill = Skills[index];
            state = states[index];
            return skill != null && state != null;
        }

        private int GetUpgradeCost(SkillDefinition skill, SkillState state)
        {
            int level = Mathf.Max(1, state.UpgradeLevel);
            return Mathf.Max(0, skill.BaseCost * Mathf.Max(1, UpgradeCostMultiplier) * level);
        }

        private int GetRepeatPurchaseCost(SkillDefinition skill, SkillState state)
        {
            int multiplier = Mathf.Max(1, ShieldUpgradeCostMultiplier);
            int cost = Mathf.Max(0, skill.BaseCost);
            for (int i = 0; i < state.RepeatPurchaseCount; i++)
            {
                cost *= multiplier;
            }

            return cost;
        }

        private static string FormatSeconds(float seconds)
        {
            return $"{Mathf.Max(0f, seconds):0.0}s";
        }

#if UNITY_EDITOR
        private void OnValidate() // 에디터 표시값 보정
        {
            NormalizeSkillDefinitions(); // MaxLevel 0 방지
        }
#endif

        private static SkillDefinition[] CreateDefaultSkills()
        {
            return new[]
            {
                new SkillDefinition { Kind = GoldActionSkillKind.Meteor, DisplayName = "Meteor", KeyNumber = 1, UnlockLevel = 10, BaseCost = 200, MaxLevel = 5, CooldownSeconds = 120f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.Shockwave, DisplayName = "Shield Shockwave", KeyNumber = 2, UnlockLevel = 15, BaseCost = 500, MaxLevel = 5, CooldownSeconds = 90f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.TimeStop, DisplayName = "Time Stop", KeyNumber = 3, UnlockLevel = 30, BaseCost = 800, MaxLevel = 5, CooldownSeconds = 150f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.NexusHeal, DisplayName = "Nexus Heal", KeyNumber = 4, UnlockLevel = 40, BaseCost = 1000, MaxLevel = 1, CooldownSeconds = 240f, RequiresPurchase = false, CanUpgrade = false, CostOnUse = true },
                new SkillDefinition { Kind = GoldActionSkillKind.NexusShieldUpgrade, DisplayName = "Shield Upgrade", KeyNumber = 5, UnlockLevel = 40, BaseCost = 500, MaxLevel = 5, CooldownSeconds = 0f, RequiresPurchase = false, CanUpgrade = false, RepeatPurchase = true }
            };
        }
    }
}
