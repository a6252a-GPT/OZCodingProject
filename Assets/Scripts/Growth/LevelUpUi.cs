using System.Collections;
using DG.Tweening;
using TeamProject01.Gameplay;
using UnityEngine;

public class LevelUpUi : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject levelUpPanel;
    [Header("투명도")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [Header("타이틀")]
    [SerializeField] private RectTransform levelUpText;
    [Header("디버그")]
    [SerializeField] private bool logCoreStats = true; // 코어 경험치 로그 출력 여부

    private bool isOpen;
    private float previousTimeScale = 1f;
    private CoreStatProvider subscribedCore; // 구독 중인 코어
    private int lastLoggedExp = -1; // 마지막 로그 경험치
    private int lastLoggedLevel = -1; // 마지막 로그 레벨
    private CoreStatData pendingLogStats; // 디바운스 대기값
    private Coroutine debouncedLogRoutine; // 경험치 변경 묶음 로그

    private void Reset()
    {
        levelUpPanel = gameObject;
        panelCanvasGroup = GetComponent<CanvasGroup>();
        if (panelCanvasGroup == null)
        {
            panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        Transform title = transform.Find("LevelUpText");
        if (title != null)
        {
            levelUpText = title as RectTransform;
        }
    }

    private void Start()
    {
        CloseInstant();
        TrySubscribeCore(); // 코어 이벤트 연결
        LogCoreStats(); // 시작 시 1회 출력
    }

    private void OnEnable()
    {
        TrySubscribeCore(); // 활성화 시 코어 연결
    }

    private void OnDisable()
    {
        if (subscribedCore != null)
        {
            subscribedCore.StatsChanged -= OnCoreStatsChanged; // 이벤트 해제
            subscribedCore = null; // 참조 제거
        }

        if (debouncedLogRoutine != null)
        {
            StopCoroutine(debouncedLogRoutine); // 대기 중 로그 취소
            debouncedLogRoutine = null;
        }
    }

    private void TrySubscribeCore()
    {
        if (subscribedCore != null || CoreStatProvider.Active == null)
        {
            return; // 이미 연결 또는 코어 없음
        }

        subscribedCore = CoreStatProvider.Active; // 현재 코어 저장
        subscribedCore.StatsChanged += OnCoreStatsChanged; // 경험치 변경 즉시 로그
    }

    private void OnCoreStatsChanged(CoreStatData stats)
    {
        if (!logCoreStats)
        {
            return; // 로그 비활성
        }

        pendingLogStats = stats; // 최신값 저장
        if (debouncedLogRoutine != null)
        {
            StopCoroutine(debouncedLogRoutine); // 같은 프레임/연속 변경은 1번만
        }

        debouncedLogRoutine = StartCoroutine(LogCoreStatsDebounced()); // 경험치 획득 시 1회 출력
    }

    private IEnumerator LogCoreStatsDebounced()
    {
        yield return null; // 같은 프레임 변경 묶기
        debouncedLogRoutine = null;

        if (!logCoreStats)
        {
            yield break;
        }

        if (pendingLogStats.CurrentExperience == lastLoggedExp && pendingLogStats.Level == lastLoggedLevel)
        {
            yield break; // 변화 없음
        }

        lastLoggedExp = pendingLogStats.CurrentExperience;
        lastLoggedLevel = pendingLogStats.Level;
        LogCoreStats(pendingLogStats); // 경험치/레벨 변경 1회 출력
    }

    private void LogCoreStats()
    {
        CoreStatData stats = CoreStatProvider.GetCurrentOrDefault(); // 코어 없으면 기본값
        lastLoggedExp = stats.CurrentExperience;
        lastLoggedLevel = stats.Level;
        LogCoreStats(stats); // 공통 로그 출력
    }

    private void LogCoreStats(CoreStatData stats)
    {
        Debug.Log(
            $"[LevelUpUi] Level={$"레벨 : "+stats.Level}, Exp={$"경험치 : "+stats.CurrentExperience+"/"+stats.ExperienceToNextLevel}, CanLevelUp={stats.CanLevelUp}",
            this); // 현재 레벨 / 현재 경험치 / 필요 경험치
    }

    private void OnDestroy()
    {
        if (isOpen)
        {
            ResumeGame();
        }
    }

    public void Open()
    {
        if (isOpen || panelCanvasGroup == null)
        {
            return;
        }

        isOpen = true;
        PauseGame();

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true);
        }

        panelCanvasGroup.alpha = 0.0f;
        panelCanvasGroup.blocksRaycasts = true;
        panelCanvasGroup.interactable = true;
        panelCanvasGroup.DOFade(1.0f, 0.25f).SetUpdate(true);

        PlayTitleTween();
    }

    private void PauseGame()
    {
        previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
    }

    private void ResumeGame()
    {
        Time.timeScale = previousTimeScale > 0f ? previousTimeScale : 1f;
    }

    public void Close()
    {
        if (panelCanvasGroup == null)
        {
            CloseInstant();
            return;
        }

        panelCanvasGroup.DOFade(0.0f, 0.25f).SetUpdate(true).OnComplete(CloseInstant);
    }

    private void PlayTitleTween()
    {
        if (levelUpText == null)
        {
            return;
        }

        levelUpText.localScale = Vector3.zero;
        levelUpText.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack).SetUpdate(true);
    }

    private void CloseInstant()
    {
        isOpen = false;
        ResumeGame();

        if (levelUpPanel != null && levelUpPanel != gameObject)
        {
            levelUpPanel.SetActive(false);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0.0f;
            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;
        }
    }
}
