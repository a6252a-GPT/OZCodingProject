using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using DG.Tweening;
using TeamProject01.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class CardUI
{
    // 안건준 추가 - 0623 : 세그먼트 ID + 레벨로 SegmentDefinition 아이콘 스프라이트 조회
    private static Sprite GetSegmentIconSprite(string segmentId, int level)
    {
        SegmentCatalogAsset catalog = CoreStatProvider.Active?.SegmentCatalogAsset;
        if (catalog == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return null; // 카탈로그 없음
        }

        if (!catalog.TryFind(segmentId, out SegmentDefinition def))
        {
            return null; // 정의 없음
        }

        return def.GetIconSpriteForLevel(level); // 레벨별 스프라이트
    }

    // 안건준 추가 - 0623 : 세그먼트 카드 아이콘 크기 조절값 — CardUI 인스펙터값 우선, 없으면 SegmentDefinition 값 사용
    private float GetSegmentIconSizeOffset(string segmentId)
    {
        if (!Mathf.Approximately(segmentCardIconSizeOffset, 0f))
        {
            return segmentCardIconSizeOffset; // CardUI 인스펙터 값 우선
        }

        SegmentCatalogAsset catalog = CoreStatProvider.Active?.SegmentCatalogAsset;
        if (catalog == null || string.IsNullOrWhiteSpace(segmentId))
        {
            return 0f; // 기본값
        }

        return catalog.TryFind(segmentId, out SegmentDefinition def) ? def.CardIconSizeOffset : 0f;
    }

    // 안건준 추가 - 0623 : SegmentUpgradeCard 같은 커스텀 프리팹에 Card_Text / DescText / Image 직접 주입
    private static void ApplyCardTextsDirectly(GameObject root, string title, string desc, Sprite iconSprite = null, float iconSizeOffset = 0f)
    {
        if (root == null)
        {
            return;
        }

        TMPro.TMP_Text[] texts = root.GetComponentsInChildren<TMPro.TMP_Text>(true);
        TMPro.TMP_Text cardText = null;
        TMPro.TMP_Text descText = null;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "Card_Text")
            {
                cardText = texts[i];
            }
            else if (texts[i].gameObject.name == "DescText")
            {
                descText = texts[i];
            }
        }

        if (cardText != null && !string.IsNullOrWhiteSpace(title))
        {
            ApplyDirectSingleLineSizing(cardText, title);
            cardText.text = title; // 세그먼트 이름 (캐논, 미사일 등)
        }

        if (descText != null && !string.IsNullOrWhiteSpace(desc))
        {
            string displayDesc = SegmentCardTagPresenter.Apply(root, desc, descText);
            descText.richText = true;
            ApplyDirectDescriptionSizing(descText, displayDesc);
            descText.text = displayDesc; // WeaponDefinition Description
        }

        // 안건준 추가 - 0623 : "Image" 오브젝트에 세그먼트 Lv1 아이콘 적용
        if (iconSprite != null)
        {
            Transform imageTransform = root.transform.Find("Image");
            if (imageTransform != null && imageTransform.TryGetComponent(out UnityEngine.UI.Image img))
            {
                img.sprite = iconSprite;
                img.enabled = true;
                img.color = Color.white;
                img.type = UnityEngine.UI.Image.Type.Simple;
                img.preserveAspect = false;
                img.SetNativeSize(); // 원본 크기로 설정
                // 크기 조절 적용 (0=원본, -50=절반, 100=두배)
                if (!Mathf.Approximately(iconSizeOffset, 0f))
                {
                    float scale = Mathf.Max(0.01f, 1f + Mathf.Clamp(iconSizeOffset, -100f, 100f) / 100f);
                    img.rectTransform.sizeDelta *= scale;
                }
            }
            else
            {
                Debug.LogWarning($"[CardUI] 'Image' 자식 오브젝트를 찾지 못했습니다. root={root.name}, 자식 수={root.transform.childCount}");
            }
        }
    }

    private static void ApplyStatUpgradeCardTextsDirectly(GameObject root, string title, string desc)
    {
        if (root == null)
        {
            return;
        }

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text cardText = null;
        TMP_Text descText = null;
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i].gameObject.name == "Card_Text")
            {
                cardText = texts[i];
            }
            else if (texts[i].gameObject.name == "DescText")
            {
                descText = texts[i];
            }
        }

        if (cardText != null && !string.IsNullOrWhiteSpace(title))
        {
            ApplyStatCardTextStyle(cardText, 24f); // 공통카드 제목은 한 줄 유지
            cardText.text = title;
        }

        if (descText != null && !string.IsNullOrWhiteSpace(desc))
        {
            ApplyStatCardTextStyle(descText, 20f); // 공통카드 설명은 줄바꿈 대신 축소
            descText.richText = true;
            descText.text = desc;
        }
    }

    private static void ApplyStatUpgradeCardIcon(GameObject root, StatUpgradeDefinition definition)
    {
        if (root == null || definition == null)
        {
            return;
        }

        Transform imageTransform = root.transform.Find("Image");
        if (imageTransform == null || !imageTransform.TryGetComponent(out Image img))
        {
            return;
        }

        Vector2 slotSize = img.rectTransform.sizeDelta; // 프리팹 아이콘 슬롯 크기 유지
        Sprite icon = definition.CardIconSprite;
        img.sprite = icon;
        img.overrideSprite = null;
        img.enabled = icon != null;
        img.color = Color.white;
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        if (icon == null)
        {
            return;
        }

        if (slotSize.sqrMagnitude <= 0.0001f)
        {
            img.SetNativeSize(); // 슬롯 정보가 없을 때만 fallback
            slotSize = img.rectTransform.sizeDelta;
        }

        img.rectTransform.sizeDelta = slotSize; // 원본 PNG 크기 대신 기존 UI 크기 사용
        if (!Mathf.Approximately(definition.CardIconSizeOffset, 0f))
        {
            float scale = Mathf.Max(0.01f, 1f + Mathf.Clamp(definition.CardIconSizeOffset, -100f, 100f) / 100f);
            img.rectTransform.sizeDelta = slotSize * scale;
        }
    }

    private static void ApplyStatCardTextStyle(TMP_Text text, float maxFontSize)
    {
        if (text == null)
        {
            return;
        }

        float resolvedMax = Mathf.Min(maxFontSize, text.fontSize > 0f ? text.fontSize : maxFontSize);
        resolvedMax = Mathf.Max(8f, resolvedMax);
        text.enableAutoSizing = true;
        text.fontSizeMax = resolvedMax;
        text.fontSizeMin = Mathf.Max(8f, resolvedMax * 0.5f);
        text.fontSize = resolvedMax;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static void ApplyDirectDescriptionSizing(TMP_Text descText, string description)
    {
        if (descText == null)
        {
            return;
        }

        float baseSize = descText.fontSizeMax > 0f ? Mathf.Max(descText.fontSizeMax, descText.fontSize) : descText.fontSize;
        float maxSize = CountDescriptionLines(description) >= 3 ? baseSize * 0.86f : baseSize;
        ConfigureDirectAutoSize(descText, maxSize, true);
    }

    private static void ApplyDirectSingleLineSizing(TMP_Text text, string value)
    {
        if (text == null)
        {
            return;
        }

        float baseSize = text.fontSizeMax > 0f ? Mathf.Max(text.fontSizeMax, text.fontSize) : text.fontSize;
        ConfigureDirectAutoSize(text, baseSize, false);
    }

    private static void ConfigureDirectAutoSize(TMP_Text text, float maxSize, bool allowWrapping)
    {
        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = Mathf.Max(8f, maxSize * 0.62f);
        text.fontSize = maxSize;
        text.textWrappingMode = allowWrapping ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
    }

    private static int CountDescriptionLines(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return 0;
        }

        string normalized = description.Replace("\r\n", "\n").Replace('\r', '\n');
        return Mathf.Max(1, normalized.Split('\n').Length);
    }
}
