using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("레벨업UI")]
    [SerializeField] private LevelUpUi levelUpUi; // 선택: 비워두면 씬에서 자동 검색
    [Header("카드 투명도")]
    [SerializeField] private CanvasGroup cardCanvasGroup;
    [Header("등장 시작 위치")]
    [SerializeField] private float startYOffset = -80.0f;
    [Header("마우스 오버 크기")]
    [SerializeField] private float hoverScale = 1.09f;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector3 originalScale;
    private bool isClickable;
    private Button cardButton;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        originalScale = transform.localScale;

        if (cardCanvasGroup == null)
        {
            cardCanvasGroup = GetComponent<CanvasGroup>();
        }

        cardButton = GetComponent<Button>();
        if (cardButton != null)
        {
            cardButton.onClick.RemoveListener(OnCardClicked);
            cardButton.onClick.AddListener(OnCardClicked);
        }
    }

    private void Reset()
    {
        if (cardCanvasGroup == null)
        {
            cardCanvasGroup = GetComponent<CanvasGroup>();
        }
    }

    public void HideInstant()
    {
        isClickable = false;
        transform.DOKill();
        cardCanvasGroup.DOKill();
        rectTransform.DOKill();
        cardCanvasGroup.alpha = 0.0f;
        cardCanvasGroup.blocksRaycasts = false;
        cardCanvasGroup.interactable = false;
        rectTransform.anchoredPosition = originalPosition + new Vector2(0.0f, startYOffset);
        transform.localScale = Vector3.zero;
    }

    public void PlayOpenTween()
    {
        isClickable = true;
        cardCanvasGroup.blocksRaycasts = true;
        cardCanvasGroup.interactable = true;

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(cardCanvasGroup.DOFade(1.0f, 0.25f));
        sequence.Join(rectTransform.DOAnchorPos(originalPosition, 0.35f).SetEase(Ease.OutCubic));
        sequence.Join(transform.DOScale(originalScale, 0.35f).SetEase(Ease.OutBack));
    }

    public Tween PlaySelectTween()
    {
        isClickable = false;
        transform.DOKill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Append(transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack));
        sequence.Append(transform.DOScale(originalScale, 0.15f));
        return sequence;
    }

    public Tween PlayHideTween()
    {
        isClickable = false;
        transform.DOKill();
        cardCanvasGroup.DOKill();

        Sequence sequence = DOTween.Sequence().SetUpdate(true);
        sequence.Join(cardCanvasGroup.DOFade(0.0f, 0.2f));
        sequence.Join(transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        return sequence;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isClickable)
        {
            return;
        }

        transform.DOKill();
        transform.DOScale(originalScale * hoverScale, 0.15f).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isClickable)
        {
            return;
        }

        transform.DOKill();
        transform.DOScale(originalScale, 0.15f).SetEase(Ease.InQuad).SetUpdate(true);
    }

    private void OnCardClicked()
    {
        if (!isClickable)
        {
            return;
        }

        LevelUpUi ui = ResolveLevelUpUi();
        if (ui != null)
        {
            ui.SelectCard(this);
        }
    }

    // 레벨업 할 때 카드 UI 호출
    public void PlayLevelUpTween()
    {
        LevelUpUi ui = ResolveLevelUpUi();
        if (ui != null)
        {
            ui.Open();
        }
    }

    private LevelUpUi ResolveLevelUpUi()
    {
        return levelUpUi != null ? levelUpUi : FindFirstObjectByType<LevelUpUi>();
    }
}
