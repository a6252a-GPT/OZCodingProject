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

        [Header("Default Values")]
        [Min(1)] public int UpgradeCostMultiplier = 2;
        [Min(1)] public int ShieldUpgradeCostMultiplier = 2;

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

        private void EnsureReferences()
        {
            if (CoreStats == null)
            {
                CoreStats = CoreStatProvider.Active != null ? CoreStatProvider.Active : FindFirstObjectByType<CoreStatProvider>();
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

            if (skill.CostOnUse && !SpendGold(skill.BaseCost))
            {
                return false;
            }

            ApplySkillEffect(skill, state);
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

            int cost = GetRepeatPurchaseCost(skill, state);
            if (!SpendGold(cost))
            {
                return false;
            }

            state.Purchased = true;
            state.RepeatPurchaseCount++;
            state.UpgradeLevel = Mathf.Max(1, state.RepeatPurchaseCount);
            Debug.Log($"[GoldActionHud] Nexus shield upgrade reserved: level {state.RepeatPurchaseCount}, cost {cost}", this);
            return true;
        }

        private void ApplySkillEffect(SkillDefinition skill, SkillState state)
        {
            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                Debug.Log($"[GoldActionHud] Nexus heal reserved: {skill.BaseCost}G, cooldown {skill.CooldownSeconds:0}s", this);
                return;
            }

            Debug.Log($"[GoldActionHud] {skill.DisplayName} reserved: Lv{Mathf.Max(1, state.UpgradeLevel)}", this);
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
            bool buttonEnabled = CanPressButton(skill, state, stats, unlocked, coolingDown);

            Slots[index].Refresh(
                skill.Icon,
                skill.KeyNumber.ToString(),
                skill.DisplayName,
                string.Empty,
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

        private bool CanPressButton(SkillDefinition skill, SkillState state, CoreStatData stats, bool unlocked, bool coolingDown)
        {
            if (!unlocked)
            {
                return false;
            }

            if (skill.Kind == GoldActionSkillKind.NexusHeal)
            {
                return !coolingDown && stats.Gold >= skill.BaseCost;
            }

            if (skill.RepeatPurchase)
            {
                return stats.Gold >= GetRepeatPurchaseCost(skill, state);
            }

            if (!state.Purchased)
            {
                return stats.Gold >= skill.BaseCost;
            }

            return skill.CanUpgrade && stats.Gold >= GetUpgradeCost(skill, state);
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
                lines.Add(stats.Gold >= skill.BaseCost ? "상태: 회복 가능" : "상태: 골드 부족");
            }
            else if (skill.RepeatPurchase)
            {
                int cost = GetRepeatPurchaseCost(skill, state);
                lines.Add(stats.Gold >= cost ? "상태: 보호막 강화 가능" : "상태: 골드 부족");
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
                actionText = $"강화 비용 {GetRepeatPurchaseCost(skill, state)}G";
            }
            else if (!state.Purchased)
            {
                actionText = $"구매 비용 {skill.BaseCost}G";
            }
            else if (skill.CanUpgrade)
            {
                actionText = $"다음 강화 {GetUpgradeCost(skill, state)}G";
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
                    return "광역 메테오: 넓은 범위에 피해를 주는 공격형 액션입니다.";
                case GoldActionSkillKind.Shockwave:
                    return "광역 넉백충격파: 주변 적을 밀어내는 방어형 액션입니다.";
                case GoldActionSkillKind.TimeStop:
                    return "타임스탑: 일정 시간 전장 흐름을 멈추는 제어형 액션입니다.";
                case GoldActionSkillKind.NexusHeal:
                    return "넥서스회복: 골드를 소모해 넥서스 체력을 회복합니다.";
                case GoldActionSkillKind.NexusShieldUpgrade:
                    return "넥서스보호막 업그레이드: 구매할 때마다 보호막 최대치를 올립니다.";
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

        private static SkillDefinition[] CreateDefaultSkills()
        {
            return new[]
            {
                new SkillDefinition { Kind = GoldActionSkillKind.Meteor, DisplayName = "Meteor", KeyNumber = 1, UnlockLevel = 10, BaseCost = 200, CooldownSeconds = 120f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.Shockwave, DisplayName = "Shockwave", KeyNumber = 2, UnlockLevel = 15, BaseCost = 500, CooldownSeconds = 90f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.TimeStop, DisplayName = "Time Stop", KeyNumber = 3, UnlockLevel = 30, BaseCost = 800, CooldownSeconds = 150f, RequiresPurchase = true, CanUpgrade = true },
                new SkillDefinition { Kind = GoldActionSkillKind.NexusHeal, DisplayName = "Nexus Heal", KeyNumber = 4, UnlockLevel = 40, BaseCost = 1000, CooldownSeconds = 240f, RequiresPurchase = false, CanUpgrade = false, CostOnUse = true },
                new SkillDefinition { Kind = GoldActionSkillKind.NexusShieldUpgrade, DisplayName = "Shield Upgrade", KeyNumber = 5, UnlockLevel = 40, BaseCost = 500, CooldownSeconds = 0f, RequiresPurchase = false, CanUpgrade = false, RepeatPurchase = true }
            };
        }
    }
}
