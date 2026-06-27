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
    // 안건준 추가 - 0622 ======
    // 자동모드 카드 자동선택 ─────────────────────────────────────────────────

    private void TryStartAutoSelect()
    {
        if (!autoSelectInAutoOrbit || !IsAutoOrbitActive())
        {
            return; // 자동모드가 아니거나 기능 꺼짐
        }

        StopAutoSelect();
        autoSelectRoutine = StartCoroutine(AutoSelectRoutine());
    }

    // 안건준 추가 - 0622 : 세그먼트 추가/레벨업 2차 카드 자동선택
    private void TryStartAutoSelectSegmentAction(bool canAdd, bool canLevelUp)
    {
        if (!autoSelectInAutoOrbit || !IsAutoOrbitActive())
        {
            return;
        }

        StopAutoSelect();
        autoSelectRoutine = StartCoroutine(AutoSelectSegmentActionRoutine(canAdd, canLevelUp));
    }

    private void StopAutoSelect()
    {
        if (autoSelectRoutine != null)
        {
            StopCoroutine(autoSelectRoutine);
            autoSelectRoutine = null;
        }
    }

    private IEnumerator AutoSelectRoutine()
    {
        // 안건준 추가 - 0622 : WaitForSecondsRealtime — timeScale = 0 상태에서도 작동
        float waitTime = 0.4f + autoSelectDelay;
        yield return new WaitForSecondsRealtime(waitTime);

        if (isProcessingSelection || spawnedCards == null || spawnedCards.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        List<SpawnedCardEntry> selectable = new List<SpawnedCardEntry>();
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i];
            if (card != null && card.CanSelect && card.IsClickable)
            {
                selectable.Add(card);
            }
        }

        if (selectable.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 안건준 수정 - 0622 : 랜덤 → 최고 등급 우선 선택
        SpawnedCardEntry picked = PickHighestTierCard(selectable);

        NotifySpawnedCardPointerEnter(picked);
        yield return new WaitForSecondsRealtime(0.2f);

        NotifySpawnedCardClicked(picked);
        autoSelectRoutine = null;
    }

    // 안건준 추가 - 0622 : 추가/레벨업 2차 카드 자동선택 — 선택 불가 카드 제외 후 랜덤
    private IEnumerator AutoSelectSegmentActionRoutine(bool canAdd, bool canLevelUp)
    {
        // 카드 등장 연출 대기
        yield return new WaitForSecondsRealtime(0.4f + autoSelectDelay);

        if (isProcessingSelection || spawnedCards == null || spawnedCards.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 선택 가능한 카드만 수집 (CanSelect 기준 — 레벨업 불가면 LevelUpAction이 CanSelect=false)
        List<SpawnedCardEntry> selectable = new List<SpawnedCardEntry>();
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            SpawnedCardEntry card = spawnedCards[i];
            if (card == null || !card.IsClickable)
            {
                continue;
            }

            // 추가만 가능한 경우 AddAction만 허용
            // 레벨업만 가능한 경우 LevelUpAction만 허용
            // 둘 다 가능한 경우 둘 다 허용
            bool isAdd = card.SegmentRole == SegmentCardRole.AddAction;
            bool isLevelUp = card.SegmentRole == SegmentCardRole.LevelUpAction;

            if (isAdd && canAdd)
            {
                selectable.Add(card);
            }
            else if (isLevelUp && canLevelUp)
            {
                selectable.Add(card);
            }
            else if (!isAdd && !isLevelUp && card.CanSelect)
            {
                selectable.Add(card); // 기타 선택 가능 카드 fallback
            }
        }

        if (selectable.Count == 0)
        {
            autoSelectRoutine = null;
            yield break;
        }

        // 안건준 수정 - 0622 : 랜덤 → 최고 등급 우선 선택
        SpawnedCardEntry picked = PickHighestTierCard(selectable);

        NotifySpawnedCardPointerEnter(picked);
        yield return new WaitForSecondsRealtime(0.2f);

        NotifySpawnedCardClicked(picked);
        autoSelectRoutine = null;
    }

    // 안건준 추가 - 0622 : 카드 등급(티어) 반환 — 스탯/무기강화는 실제 등급, 세그먼트 계열은 Normal
    private StatUpgrade.StatCardTier GetCardTier(SpawnedCardEntry entry)
    {
        if (entry == null)
        {
            return StatUpgrade.StatCardTier.Normal;
        }

        if (entry.SegmentRole == SegmentCardRole.EnhanceChoice)
        {
            return entry.WeaponEnhancementTier; // 무기 강화 카드 등급
        }

        if (entry.StatUpgrade != null)
        {
            return entry.StatUpgrade.CurrentTier; // 스탯 카드 등급
        }

        return StatUpgrade.StatCardTier.Normal; // 세그먼트 추가/레벨업 등 등급 없는 카드
    }

    // 안건준 추가 - 0622 : 후보 목록에서 가장 높은 등급의 카드를 반환 — 동급이면 랜덤
    private SpawnedCardEntry PickHighestTierCard(List<SpawnedCardEntry> candidates)
    {
        StatUpgrade.StatCardTier best = StatUpgrade.StatCardTier.Normal;
        for (int i = 0; i < candidates.Count; i++)
        {
            StatUpgrade.StatCardTier t = GetCardTier(candidates[i]);
            if (t > best)
            {
                best = t;
            }
        }

        List<SpawnedCardEntry> topTier = new List<SpawnedCardEntry>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (GetCardTier(candidates[i]) == best)
            {
                topTier.Add(candidates[i]);
            }
        }

        return topTier[UnityEngine.Random.Range(0, topTier.Count)];
    }
}
