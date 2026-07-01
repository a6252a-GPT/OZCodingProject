using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    // GoldActionHudRoot — 액션 HUD 구매/강화 SFX (GoldActionHudController 는 수정하지 않음)
    [DefaultExecutionOrder(-100)] // 키 SFX는 컨트롤러 Update(구매/강화)보다 먼저
    public sealed class ActionHudFeature : MonoBehaviour
    {
        private enum SlotButtonSfxKind
        {
            None,
            Purchase,
            Upgrade
        }

        [Header("컨트롤러")]
        [SerializeField] private GoldActionHudController hudController;

        [Header("구매 클립 (Bling05)")]
        [SerializeField] private AudioClip skillPurchaseClip;
        [Range(0f, 1f)] [SerializeField] private float skillPurchaseVolume = 1f;

        [Header("강화 클립 (chimes_magic_bell_ding_1)")]
        [SerializeField] private AudioClip skillUpgradeClip;
        [Range(0f, 1f)] [SerializeField] private float skillUpgradeVolume = 1f;

        private FieldInfo statesField;
        private FieldInfo purchasedField;

        private bool slotHooksApplied;
        private int slotHookRetryFrames;

        private SlotButtonSfxKind[] pendingClickSfxBySlot;

        private global::LevelUpUi cachedLevelUpUi;

        private UnityEngine.Events.UnityAction[] iconClickSfxActions;
        private UnityEngine.Events.UnityAction[] actionClickSfxActions;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureFeatureOnHudControllers()
        {
            GoldActionHudController[] controllers = FindObjectsByType<GoldActionHudController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < controllers.Length; i++)
            {
                GoldActionHudController controller = controllers[i];
                if (controller == null || controller.GetComponent<ActionHudFeature>() != null)
                {
                    continue;
                }

                controller.gameObject.AddComponent<ActionHudFeature>();
            }
        }

        private void Awake()
        {
            EnsureHudController();
            EnsureReflection();
            EnsureClipDefaults();
        }

        private void OnEnable()
        {
            EnsureHudController();
            slotHooksApplied = false;
            slotHookRetryFrames = 30;
            StartCoroutine(HookSlotButtonsNextFrame());
        }

        private void Update()
        {
            HandleKeyboardSfx();
        }

        private void LateUpdate()
        {
            if (hudController == null)
            {
                return;
            }

            if (!slotHooksApplied && slotHookRetryFrames > 0)
            {
                TryApplySlotHooks();
                slotHookRetryFrames--;
            }
        }

        private void EnsureHudController()
        {
            if (hudController != null)
            {
                return;
            }

            hudController = GetComponent<GoldActionHudController>();
            if (hudController == null)
            {
                hudController = GetComponentInChildren<GoldActionHudController>(true);
            }
        }

        private void EnsureReflection()
        {
            if (statesField != null && purchasedField != null)
            {
                return;
            }

            const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            Type controllerType = typeof(GoldActionHudController);
            statesField = controllerType.GetField("states", instance);

            Type stateType = controllerType.GetNestedType("SkillState", BindingFlags.NonPublic);
            if (stateType == null)
            {
                Debug.LogWarning("[ActionHudFeature] SkillState 타입을 찾지 못했습니다.", this);
                return;
            }

            purchasedField = stateType.GetField("Purchased", instance);
            if (statesField == null || purchasedField == null)
            {
                Debug.LogWarning("[ActionHudFeature] states / Purchased reflection 실패 — SFX 단계 판별 불가.", this);
            }
        }

        private void EnsureClipDefaults()
        {
            if (skillPurchaseClip == null)
            {
                skillPurchaseClip = ResolveCatalogClip(GameplaySfxCue.GoodPickup);
            }

            if (skillUpgradeClip == null)
            {
                skillUpgradeClip = ResolveCatalogClip(GameplaySfxCue.ManaOrbPickup);
            }
        }

        private static AudioClip ResolveCatalogClip(GameplaySfxCue cue)
        {
            GameplaySfxCatalog catalog = Resources.Load<GameplaySfxCatalog>(GameplaySfxCatalog.ResourcePath);
            if (catalog == null || !catalog.TryGetEntry(cue, out GameplaySfxCatalogEntry entry)
                || entry.Clips == null || entry.Clips.Length == 0)
            {
                return null;
            }

            return entry.Clips[0];
        }

        private IEnumerator HookSlotButtonsNextFrame()
        {
            yield return null;
            TryApplySlotHooks();
        }

        private void TryApplySlotHooks()
        {
            if (hudController == null || hudController.Slots == null || hudController.Slots.Length == 0)
            {
                return;
            }

            int slotCount = hudController.Slots.Length;
            EnsurePendingSfxCache(slotCount);
            EnsureClickActionCache(slotCount);

            GoldActionHudSlot[] slots = hudController.Slots;
            for (int i = 0; i < slots.Length; i++)
            {
                HookSlotPointerDown(slots[i], i);
                HookSlotClickSfxAdditive(slots[i], i);
            }

            slotHooksApplied = true;
        }

        private void EnsurePendingSfxCache(int slotCount)
        {
            if (pendingClickSfxBySlot == null || pendingClickSfxBySlot.Length != slotCount)
            {
                pendingClickSfxBySlot = new SlotButtonSfxKind[slotCount];
            }
        }

        private void EnsureClickActionCache(int slotCount)
        {
            if (iconClickSfxActions != null && iconClickSfxActions.Length == slotCount)
            {
                return;
            }

            iconClickSfxActions = new UnityEngine.Events.UnityAction[slotCount];
            actionClickSfxActions = new UnityEngine.Events.UnityAction[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                int capturedIndex = i;
                iconClickSfxActions[i] = () => PlayPendingOrResolvedClickSfx(capturedIndex);
                actionClickSfxActions[i] = () => PlayPendingOrResolvedClickSfx(capturedIndex);
            }
        }

        private void HookSlotPointerDown(GoldActionHudSlot slot, int slotIndex)
        {
            if (slot == null)
            {
                return;
            }

            if (slot.ActionButton != null)
            {
                EnsurePointerDownHandler(slot.ActionButton.gameObject, slotIndex);
            }

            if (slot.IconImage != null)
            {
                EnsurePointerDownHandler(slot.IconImage.gameObject, slotIndex);
            }
        }

        private void EnsurePointerDownHandler(GameObject target, int slotIndex)
        {
            SlotSfxPointerDownHandler handler = target.GetComponent<SlotSfxPointerDownHandler>();
            if (handler == null)
            {
                handler = target.AddComponent<SlotSfxPointerDownHandler>();
            }

            handler.Initialize(this, slotIndex);
        }

        private void HookSlotClickSfxAdditive(GoldActionHudSlot slot, int slotIndex)
        {
            if (slot == null || iconClickSfxActions == null || slotIndex >= iconClickSfxActions.Length)
            {
                return;
            }

            if (slot.IconImage != null)
            {
                Button iconButton = slot.IconImage.GetComponent<Button>();
                if (iconButton != null)
                {
                    iconButton.onClick.RemoveListener(iconClickSfxActions[slotIndex]);
                    iconButton.onClick.AddListener(iconClickSfxActions[slotIndex]);
                }
            }

            if (slot.ActionButton != null)
            {
                slot.ActionButton.onClick.RemoveListener(actionClickSfxActions[slotIndex]);
                slot.ActionButton.onClick.AddListener(actionClickSfxActions[slotIndex]);
            }
        }

        internal void CachePendingSfxForSlot(int slotIndex)
        {
            EnsurePendingSfxCache(hudController != null && hudController.Slots != null ? hudController.Slots.Length : 0);
            if (pendingClickSfxBySlot == null || slotIndex < 0 || slotIndex >= pendingClickSfxBySlot.Length)
            {
                return;
            }

            pendingClickSfxBySlot[slotIndex] = ResolveSfxKindFromSlot(slotIndex);
        }

        private void PlayPendingOrResolvedClickSfx(int slotIndex)
        {
            SlotButtonSfxKind kind = SlotButtonSfxKind.None;
            if (pendingClickSfxBySlot != null && slotIndex >= 0 && slotIndex < pendingClickSfxBySlot.Length
                && pendingClickSfxBySlot[slotIndex] != SlotButtonSfxKind.None)
            {
                kind = pendingClickSfxBySlot[slotIndex];
                pendingClickSfxBySlot[slotIndex] = SlotButtonSfxKind.None;
            }
            else
            {
                kind = ResolveSfxKindFromSlot(slotIndex);
            }

            PlaySfxForKind(kind);
        }

        internal void PlaySlotSfxFromState(int slotIndex)
        {
            PlaySfxForKind(ResolveSfxKindFromSlot(slotIndex));
        }

        private void HandleKeyboardSfx()
        {
            if (hudController == null || IsHudKeyboardInputBlocked())
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (WasDigitKeyPressed(keyboard, 1)) PlayKeyboardSfxForKey(1);
            if (WasDigitKeyPressed(keyboard, 2)) PlayKeyboardSfxForKey(2);
            if (WasDigitKeyPressed(keyboard, 3)) PlayKeyboardSfxForKey(3);
            if (WasDigitKeyPressed(keyboard, 4)) PlayKeyboardSfxForKey(4);
            if (WasDigitKeyPressed(keyboard, 5)) PlayKeyboardSfxForKey(5);
        }

        private static bool WasDigitKeyPressed(Keyboard keyboard, int digit)
        {
            return digit switch
            {
                1 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
                2 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
                3 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
                4 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
                5 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
                _ => false
            };
        }

        private void PlayKeyboardSfxForKey(int keyNumber)
        {
            int slotIndex = FindSlotIndexByKeyNumber(keyNumber);
            if (slotIndex < 0)
            {
                return;
            }

            PlaySlotSfxFromState(slotIndex);
        }

        // 1~3: 미활성(미구매)=구매음 / 활성(구매됨)=강화음 · 4~5: 항상 강화음
        private SlotButtonSfxKind ResolveSfxKindFromSlot(int slotIndex)
        {
            if (TryReadPurchased(slotIndex, out GoldActionHudController.SkillDefinition skill, out bool purchased))
            {
                if (!IsSkillUnlocked(skill))
                {
                    return SlotButtonSfxKind.None;
                }

                if (IsPurchaseThenUpgradeSkill(skill))
                {
                    return purchased ? SlotButtonSfxKind.Upgrade : SlotButtonSfxKind.Purchase;
                }

                return SlotButtonSfxKind.Upgrade;
            }

            return ResolveSfxKindFromLabel(GetSlotButtonLabel(slotIndex));
        }

        private static bool IsPurchaseThenUpgradeSkill(GoldActionHudController.SkillDefinition skill)
        {
            return skill != null && skill.KeyNumber >= 1 && skill.KeyNumber <= 3;
        }

        private static SlotButtonSfxKind ResolveSfxKindFromLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return SlotButtonSfxKind.None;
            }

            if (label.StartsWith("구매", StringComparison.Ordinal))
            {
                return SlotButtonSfxKind.Purchase;
            }

            if (label.StartsWith("강화", StringComparison.Ordinal) || label.StartsWith("회복", StringComparison.Ordinal))
            {
                return SlotButtonSfxKind.Upgrade;
            }

            return SlotButtonSfxKind.None;
        }

        private string GetSlotButtonLabel(int slotIndex)
        {
            GoldActionHudSlot slot = GetSlot(slotIndex);
            return slot != null && slot.ActionButtonText != null ? slot.ActionButtonText.text : string.Empty;
        }

        private void PlaySfxForKind(SlotButtonSfxKind kind)
        {
            switch (kind)
            {
                case SlotButtonSfxKind.Purchase:
                    PlaySkillPurchaseSfx();
                    break;
                case SlotButtonSfxKind.Upgrade:
                    PlaySkillUpgradeSfx();
                    break;
            }
        }

        private int FindSlotIndexByKeyNumber(int keyNumber)
        {
            if (hudController == null || hudController.Skills == null)
            {
                return -1;
            }

            GoldActionHudController.SkillDefinition[] skills = hudController.Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                if (skills[i] != null && skills[i].KeyNumber == keyNumber)
                {
                    return i;
                }
            }

            return -1;
        }

        private GoldActionHudSlot GetSlot(int slotIndex)
        {
            if (hudController == null || hudController.Slots == null
                || slotIndex < 0 || slotIndex >= hudController.Slots.Length)
            {
                return null;
            }

            return hudController.Slots[slotIndex];
        }

        private bool IsSkillUnlocked(GoldActionHudController.SkillDefinition skill)
        {
            if (skill == null)
            {
                return false;
            }

            CoreStatData stats = hudController.CoreStats != null
                ? hudController.CoreStats.CurrentStats
                : CoreStatProvider.GetCurrentOrDefault();
            return stats.Level >= skill.UnlockLevel;
        }

        private bool IsHudKeyboardInputBlocked()
        {
            if (GameplayInputBlocker.IsGameplayInputBlocked || Time.timeScale <= 0f)
            {
                return true;
            }

            if (cachedLevelUpUi == null)
            {
                cachedLevelUpUi = FindFirstObjectByType<global::LevelUpUi>(FindObjectsInactive.Include);
            }

            return cachedLevelUpUi != null
                   && (cachedLevelUpUi.IsPanelOpen || cachedLevelUpUi.IsPanelVisible);
        }

        private bool TryReadPurchased(
            int slotIndex,
            out GoldActionHudController.SkillDefinition skill,
            out bool purchased)
        {
            skill = null;
            purchased = false;

            if (hudController == null || hudController.Skills == null
                || slotIndex < 0 || slotIndex >= hudController.Skills.Length)
            {
                return false;
            }

            skill = hudController.Skills[slotIndex];
            if (skill == null || statesField == null || purchasedField == null)
            {
                return false;
            }

            object statesObject = statesField.GetValue(hudController);
            if (statesObject is not Array states || slotIndex >= states.Length)
            {
                return false;
            }

            object state = states.GetValue(slotIndex);
            if (state == null)
            {
                return false;
            }

            purchased = (bool)purchasedField.GetValue(state);
            return true;
        }

        private void PlaySkillPurchaseSfx()
        {
            if (skillPurchaseClip == null)
            {
                return;
            }

            AudioManager.PlayUiSfxClip(skillPurchaseClip, skillPurchaseVolume);
        }

        private void PlaySkillUpgradeSfx()
        {
            if (skillUpgradeClip == null)
            {
                return;
            }

            AudioManager.PlayUiSfxClip(skillUpgradeClip, skillUpgradeVolume);
        }

        private sealed class SlotSfxPointerDownHandler : MonoBehaviour, IPointerDownHandler
        {
            private ActionHudFeature owner;
            private int slotIndex;

            public void Initialize(ActionHudFeature feature, int index)
            {
                owner = feature;
                slotIndex = index;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                owner?.CachePendingSfxForSlot(slotIndex);
            }
        }
    }
}
