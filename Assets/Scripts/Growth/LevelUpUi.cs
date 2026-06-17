using DG.Tweening;
using UnityEngine;

public class LevelUpUi : MonoBehaviour
{
    [Header("패널")]
    [SerializeField] private GameObject levelUpPanel;
    [Header("투명도")]
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [Header("타이틀")]
    [SerializeField] private RectTransform levelUpText;
    [Header("스킬카드")]
    [SerializeField] private CardUI[] skillCards;

    private bool isOpen;
    private bool isSelected;
    private float previousTimeScale = 1f;

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

        skillCards = GetComponentsInChildren<CardUI>(true);
    }

    private void Start()
    {
        CloseInstant();
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
        isSelected = false;
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
        PlayCardOpenTween();
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

    private void PlayCardOpenTween()
    {
        if (skillCards == null)
        {
            return;
        }

        for (int i = 0; i < skillCards.Length; i++)
        {
            if (skillCards[i] != null)
            {
                skillCards[i].HideInstant();
            }
        }

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        for (int i = 0; i < skillCards.Length; i++)
        {
            int index = i;
            sequence.AppendCallback(() =>
            {
                if (skillCards[index] != null)
                {
                    skillCards[index].PlayOpenTween();
                }
            });
            sequence.AppendInterval(0.12f);
        }
    }

    public void SelectCard(CardUI selectedCard)
    {
        if (isSelected || skillCards == null)
        {
            return;
        }

        isSelected = true;
        Sequence sequence = DOTween.Sequence().SetUpdate(true);

        for (int i = 0; i < skillCards.Length; i++)
        {
            CardUI card = skillCards[i];
            if (card == null)
            {
                continue;
            }

            if (card == selectedCard)
            {
                sequence.Join(card.PlaySelectTween());
            }
            else
            {
                sequence.Join(card.PlayHideTween());
            }
        }

        sequence.AppendInterval(0.5f);
        sequence.OnComplete(Close);
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
        isSelected = false;
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
