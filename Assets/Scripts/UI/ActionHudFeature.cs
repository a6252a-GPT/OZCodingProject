using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    // GoldActionHudRoot 부모 — 액션 HUD SFX 전용 (GoldActionHudController 는 수정하지 않음)
    [DefaultExecutionOrder(100)] // 컨트롤러 Update·WireButtons 이후 클릭/키 SFX 처리
    public sealed class ActionHudFeature : MonoBehaviour
    {
        [Header("컨트롤러")]
        [SerializeField] private GoldActionHudController hudController; // 비어 있으면 같은 오브젝트·자식에서 자동 탐색

        [Header("구매 클립 (Bling05)")]
        [SerializeField] private AudioClip skillPurchaseClip; // 구매 버튼·키(미구매) 성공 시
        [Range(0f, 1f)] [SerializeField] private float skillPurchaseVolume = 1f;

        [Header("강화 클립 (chimes_magic_bell_ding_1)")]
        [SerializeField] private AudioClip skillUpgradeClip; // 강화 버튼·5번 반복강화 시
        [Range(0f, 1f)] [SerializeField] private float skillUpgradeVolume = 1f;

        // GoldActionHudController.private SkillState[] 접근용
        private FieldInfo statesField;
        private FieldInfo purchasedField;
        private FieldInfo upgradeLevelField;
        private FieldInfo repeatPurchaseCountField;
        private FieldInfo cooldownEndsAtField;

        private int slotHookFramesRemaining; // WireButtons 직후 슬롯 리스너 재연결 남은 프레임

        private global::LevelUpUi cachedLevelUpUi; // 키 입력 차단 판별 (컨트롤러와 동일 조건)

        private UnityEngine.Events.UnityAction[] iconSfxActions; // RemoveListener 용 캐시 (람다 중복 방지)
        private UnityEngine.Events.UnityAction[] actionSfxActions;

        private void Awake()
        {
            EnsureHudController();
            EnsureReflection();
            EnsureClipDefaults();
        }

        private void OnEnable()
        {
            EnsureHudController();
            slotHookFramesRemaining = 3;
            StartCoroutine(HookSlotButtonsNextFrame());
        }

        private void LateUpdate()
        {
            if (hudController == null)
            {
                return;
            }

            if (slotHookFramesRemaining > 0)
            {
                HookSlotButtons();
                slotHookFramesRemaining--;
            }

            HandleKeyboardSfx(); // 1~5 키 — 컨트롤러 Update 처리 직후 같은 프레임에 SFX
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
            if (statesField != null)
            {
                return;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            Type controllerType = typeof(GoldActionHudController);
            statesField = controllerType.GetField("states", flags);
            Type stateType = controllerType.GetNestedType("SkillState", flags);
            if (stateType == null)
            {
                Debug.LogWarning("[ActionHudFeature] GoldActionHudController.SkillState 타입을 찾지 못했습니다.", this);
                return;
            }

            purchasedField = stateType.GetField("Purchased", flags);
            upgradeLevelField = stateType.GetField("UpgradeLevel", flags);
            repeatPurchaseCountField = stateType.GetField("RepeatPurchaseCount", flags);
            cooldownEndsAtField = stateType.GetField("CooldownEndsAt", flags);
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
            HookSlotButtons();
        }

        private void HookSlotButtons()
        {
            if (hudController == null || hudController.Slots == null)
            {
                return;
            }

            GoldActionHudSlot[] slots = hudController.Slots;
            EnsureSfxActionCache(slots.Length);

            for (int i = 0; i < slots.Length; i++)
            {
                HookSlotSoundsAdditive(slots[i], i);
            }
        }

        // 슬롯별 UnityAction 캐시 — WireButtons 재호출 후에도 동일 delegate 로 RemoveListener 가능
        private void EnsureSfxActionCache(int slotCount)
        {
            if (iconSfxActions != null && iconSfxActions.Length == slotCount)
            {
                return;
            }

            iconSfxActions = new UnityEngine.Events.UnityAction[slotCount];
            actionSfxActions = new UnityEngine.Events.UnityAction[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                int capturedIndex = i;
                iconSfxActions[i] = () => OnIconButtonSfx(capturedIndex);
                actionSfxActions[i] = () => OnActionButtonSfx(capturedIndex);
            }
        }

        // 컨트롤러 리스너 뒤에 SFX 리스너만 Add (RemoveAllListeners 사용 금지)
        private void HookSlotSoundsAdditive(GoldActionHudSlot slot, int slotIndex)
        {
            if (slot == null || iconSfxActions == null || slotIndex >= iconSfxActions.Length)
            {
                return;
            }

            if (slot.IconImage != null)
            {
                Button iconButton = slot.IconImage.GetComponent<Button>();
                if (iconButton != null)
                {
                    iconButton.onClick.RemoveListener(iconSfxActions[slotIndex]);
                    iconButton.onClick.AddListener(iconSfxActions[slotIndex]);
                }
            }

            if (slot.ActionButton != null)
            {
                slot.ActionButton.onClick.RemoveListener(actionSfxActions[slotIndex]);
                slot.ActionButton.onClick.AddListener(actionSfxActions[slotIndex]);
            }
        }

        // ─── 마우스: 아이콘 클릭 (스킬 사용) ─────────────────────────────
        private void OnIconButtonSfx(int slotIndex)
        {
            if (!TryReadSlotState(slotIndex, out GoldActionHudController.SkillDefinition skill, out SkillSnapshot state))
            {
                return;
            }

            if (!IsSkillUnlocked(skill))
            {
                return;
            }

            PlayHudClickSfx();
        }

        // ─── 마우스: 하단 버튼 (구매 / 강화 / 회복) ─────────────────────
        // 컨트롤러 HandleSlotButton 이 먼저 실행된 뒤 호출 — ActionButtonText 는 아직 이전 라벨 유지
        private void OnActionButtonSfx(int slotIndex)
        {
            GoldActionHudSlot slot = GetSlot(slotIndex);
            if (slot == null || slot.ActionButtonText == null)
            {
                return;
            }

            string label = slot.ActionButtonText.text ?? string.Empty;
            if (label.StartsWith("구매", StringComparison.Ordinal))
            {
                PlaySkillPurchaseSfx();
                return;
            }

            if (label.StartsWith("강화", StringComparison.Ordinal))
            {
                PlaySkillUpgradeSfx();
                return;
            }

            // 회복 nG 등 사용형 하단 버튼
            PlayHudClickSfx();
        }

        // ─── 키보드 1~5 (컨트롤러 TryActivateSkillByKey 와 동일 분기) ───
        private void HandleKeyboardSfx()
        {
            if (IsHudKeyboardInputBlocked())
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
            if (slotIndex < 0 || !TryReadSlotState(slotIndex, out GoldActionHudController.SkillDefinition skill, out SkillSnapshot state))
            {
                return;
            }

            if (!IsSkillUnlocked(skill))
            {
                return;
            }

            if (skill.Kind == GoldActionHudController.GoldActionSkillKind.NexusHeal)
            {
                PlayHudClickSfx();
                return;
            }

            if (skill.RepeatPurchase)
            {
                PlaySkillUpgradeSfx();
                return;
            }

            if (skill.RequiresPurchase && !state.Purchased)
            {
                PlaySkillPurchaseSfx();
                return;
            }

            // 구매 완료 후 키 입력 → 스킬 사용 (아이콘 클릭과 동일 클릭음)
            PlayHudClickSfx();
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

        private struct SkillSnapshot
        {
            public bool Purchased;
            public int UpgradeLevel;
            public int RepeatPurchaseCount;
            public float CooldownEndsAt;
        }

        private bool TryReadSlotState(
            int slotIndex,
            out GoldActionHudController.SkillDefinition skill,
            out SkillSnapshot snapshot)
        {
            skill = null;
            snapshot = default;

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
            if (!(statesObject is Array states) || slotIndex >= states.Length)
            {
                return false;
            }

            object state = states.GetValue(slotIndex);
            if (state == null)
            {
                return false;
            }

            snapshot = new SkillSnapshot
            {
                Purchased = (bool)purchasedField.GetValue(state),
                UpgradeLevel = upgradeLevelField != null ? (int)upgradeLevelField.GetValue(state) : 0,
                RepeatPurchaseCount = repeatPurchaseCountField != null ? (int)repeatPurchaseCountField.GetValue(state) : 0,
                CooldownEndsAt = cooldownEndsAtField != null ? (float)cooldownEndsAtField.GetValue(state) : 0f
            };
            return true;
        }

        // 타이틀 ClickButton 과 동일 (TitleButtonHandler)
        private void PlayHudClickSfx()
        {
            AudioManager manager = AudioManager.EnsureExists();
            if (manager != null
                && manager.TryGetSfxClip(SFXType.ClickButton, out AudioClip clip, out float localVolume)
                && manager.GetEffectiveSfxVolume(localVolume) > 0.0001f)
            {
                manager.PlaySfxOneShotDirect(clip, localVolume);
                return;
            }

            AudioManager.PlayClickButtonSfx();
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
    }
}
