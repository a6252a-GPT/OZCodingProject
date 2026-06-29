using TMPro;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    // Display-only controller for WaveHudRoot.
    // It only switches UI groups and updates text from WaveController state.
    public sealed class WaveHudPanel : MonoBehaviour
    {
        private WaveController waveController;
        private BossWaveController bossWaveController;

        private GameObject normalGroup;
        private GameObject bossGroup;
        private GameObject bonusGroup;

        private TMP_Text normalTitleText;
        private TMP_Text normalEnemyCountText;
        private TMP_Text normalTimeText;

        private TMP_Text bossTitleText;
        private GameObject bossBattleMessageObject;
        private GameObject bossRewardMessageObject;

        private TMP_Text bonusTitleText;
        private GameObject bonusCollectMessageObject;
        private GameObject bonusRewardMessageObject;
        private TMP_Text bonusTimeText;

        [Header("Display Text")]
        [SerializeField] private string normalTitleFormat = "WAVE {0}";
        [SerializeField] private string normalEnemyFormat = "{0} Enemies Left";
        [SerializeField] private string bossTitle = "BOSS STAGE";
        [SerializeField] private string bonusTitle = "BONUS STAGE";
        [SerializeField] private string missingControllerText = "WaveController Missing";

        private void Reset()
        {
            ResolveReferences();
        }

        private void Awake()
        {
            ResolveReferences();
            Refresh();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Refresh();
        }

        private void Update()
        {
            if (waveController == null)
            {
                ResolveReferences();
            }

            Refresh();
        }

        private void ResolveReferences()
        {
            waveController = waveController != null ? waveController : FindFirstObjectByType<WaveController>();
            bossWaveController = bossWaveController != null ? bossWaveController : FindFirstObjectByType<BossWaveController>();

            normalGroup = normalGroup != null ? normalGroup : FindChildObject("NormalGroup");
            bossGroup = bossGroup != null ? bossGroup : FindChildObject("BossGroup");
            bonusGroup = bonusGroup != null ? bonusGroup : FindChildObject("BonusGroup");

            normalTitleText = normalTitleText != null ? normalTitleText : FindGroupText(normalGroup, "TitleText");
            normalEnemyCountText = normalEnemyCountText != null ? normalEnemyCountText : FindGroupText(normalGroup, "EnemyCountText");
            normalTimeText = normalTimeText != null ? normalTimeText : FindGroupText(normalGroup, "TimeText");

            bossTitleText = bossTitleText != null ? bossTitleText : FindGroupText(bossGroup, "TitleText");
            bossBattleMessageObject = bossBattleMessageObject != null ? bossBattleMessageObject : FindGroupObject(bossGroup, "BattleMessageText");
            bossRewardMessageObject = bossRewardMessageObject != null ? bossRewardMessageObject : FindGroupObject(bossGroup, "RewardMessageText");

            bonusTitleText = bonusTitleText != null ? bonusTitleText : FindGroupText(bonusGroup, "TitleText");
            bonusCollectMessageObject = bonusCollectMessageObject != null ? bonusCollectMessageObject : FindGroupObject(bonusGroup, "CollectMessageText");
            bonusRewardMessageObject = bonusRewardMessageObject != null ? bonusRewardMessageObject : FindGroupObject(bonusGroup, "RewardMessageText");
            bonusTimeText = bonusTimeText != null ? bonusTimeText : FindGroupText(bonusGroup, "TimeText");
        }

        private void Refresh()
        {
            if (waveController == null)
            {
                ShowNormal();
                SetText(normalTitleText, "WAVE");
                SetText(normalEnemyCountText, missingControllerText);
                SetText(normalTimeText, "00:00");
                return;
            }

            switch (waveController.CurrentState)
            {
                case WaveController.WaveRunState.Boss:
                    RefreshBoss();
                    break;
                case WaveController.WaveRunState.Special:
                    RefreshBonus();
                    break;
                default:
                    RefreshNormal();
                    break;
            }
        }

        private void RefreshNormal()
        {
            ShowNormal();
            SetText(normalTitleText, string.Format(normalTitleFormat, waveController.CurrentStage));
            SetText(normalEnemyCountText, string.Format(normalEnemyFormat, waveController.CurrentStageRemainingEnemyCount));
            SetText(normalTimeText, FormatTime(waveController.RemainingStageSeconds));
        }

        private void RefreshBoss()
        {
            ShowBoss();
            SetText(bossTitleText, bossTitle);

            bool hasActiveBoss = bossWaveController != null && bossWaveController.HasActiveBoss;
            SetActive(bossBattleMessageObject, hasActiveBoss);
            SetActive(bossRewardMessageObject, !hasActiveBoss);
        }

        private void RefreshBonus()
        {
            ShowBonus();
            SetText(bonusTitleText, bonusTitle);

            ManaOrbCollectSpecialWave manaOrbWave = waveController.CurrentManaOrbCollectSpecialWave;
            bool isRewardStage = manaOrbWave != null && manaOrbWave.IsRewardStageActive;
            bool isCollectStage = !isRewardStage;

            SetActive(bonusCollectMessageObject, isCollectStage);
            SetActive(bonusRewardMessageObject, isRewardStage);
            SetActive(bonusTimeText, isCollectStage);

            if (isCollectStage)
            {
                float seconds = manaOrbWave != null ? manaOrbWave.RemainingCollectSeconds : waveController.RemainingStageSeconds;
                SetText(bonusTimeText, FormatTime(seconds));
            }
        }

        private void ShowNormal()
        {
            SetActive(normalGroup, true);
            SetActive(bossGroup, false);
            SetActive(bonusGroup, false);
        }

        private void ShowBoss()
        {
            SetActive(normalGroup, false);
            SetActive(bossGroup, true);
            SetActive(bonusGroup, false);
        }

        private void ShowBonus()
        {
            SetActive(normalGroup, false);
            SetActive(bossGroup, false);
            SetActive(bonusGroup, true);
        }

        private GameObject FindChildObject(string childName)
        {
            Transform child = FindChild(transform, childName);
            return child != null ? child.gameObject : null;
        }

        private static TMP_Text FindGroupText(GameObject group, string childName)
        {
            GameObject child = FindGroupObject(group, childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static GameObject FindGroupObject(GameObject group, string childName)
        {
            if (group == null)
            {
                return null;
            }

            Transform child = FindChild(group.transform, childName);
            return child != null ? child.gameObject : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                {
                    return children[i];
                }
            }

            return null;
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0.0f, seconds));
            int minutes = totalSeconds / 60;
            int remainSeconds = totalSeconds % 60;
            return $"{minutes:00}:{remainSeconds:00}";
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null && text.text != value)
            {
                text.text = value;
            }
        }

        private static void SetActive(TMP_Text text, bool active)
        {
            if (text != null)
            {
                SetActive(text.gameObject, active);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
