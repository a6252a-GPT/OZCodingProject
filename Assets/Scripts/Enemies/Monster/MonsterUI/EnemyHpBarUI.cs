using UnityEngine;
using UnityEngine.UI;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyHpBarUI : MonoBehaviour // 몬스터 머리 위 HP바 UI를 제어
    {
        [Header("Target")]
        [SerializeField] private EnemyHealth health; // HP 값을 읽어올 EnemyHealth 스크립트

        [Header("UI")]
        [SerializeField] private RectTransform barRoot; // HP바 전체 검은 프레임 RectTransform
        [SerializeField] private Image fillImage; // 현재 체력 비율을 표시할 빨간 Fill Image
        [SerializeField] private RectTransform segmentLineRoot; // 자동 생성된 칸 구분선들이 들어갈 부모
        [SerializeField] private Image segmentLinePrefab; // 칸 구분선 복사용 원본 Image

        [Header("Size Setting")]
        [Min(1.0f)]
        [SerializeField] private float fixedFillWidth = 180.0f; // 빨간 HP Fill의 고정 너비

        [Min(1.0f)]
        [SerializeField] private float barHeight = 18.0f; // HP바 전체 높이

        [Header("Segment Setting")]
        [Min(1.0f)]
        [SerializeField] private float hpPerSegment = 10.0f; // HP 몇 당 칸 1개로 볼지

        [Min(1)]
        [SerializeField] private int maxVisibleSegmentCount = 10; // 화면에 표시할 최대 칸 수

        [Header("Frame Padding")]
        [Min(0.0f)]
        [SerializeField] private float fillHorizontalPadding = 3.0f; // 검은 프레임 안쪽 좌우 여백

        [Min(0.0f)]
        [SerializeField] private float fillVerticalPadding = 3.0f; // 검은 프레임 안쪽 위아래 여백

        private float cachedMaxHp = -1.0f; // 마지막으로 UI에 반영한 MaxHp 값
        private float cachedFixedFillWidth = -1.0f; // 마지막으로 반영한 고정 Fill 너비
        private float cachedBarHeight = -1.0f; // 마지막으로 반영한 HP바 높이
        private float cachedHpPerSegment = -1.0f; // 마지막으로 반영한 칸 계산 기준
        private int cachedMaxVisibleSegmentCount = -1; // 마지막으로 반영한 최대 표시 칸 수
        private float cachedFillHorizontalPadding = -1.0f; // 마지막으로 반영한 좌우 여백
        private float cachedFillVerticalPadding = -1.0f; // 마지막으로 반영한 위아래 여백

        private void Awake()
        {
            if (health == null) // EnemyHealth가 연결되지 않았다면
            {
                health = GetComponentInParent<EnemyHealth>(); // 부모 오브젝트에서 EnemyHealth를 찾는다.
            }

            if (segmentLinePrefab != null) // 칸 구분선 원본이 있다면
            {
                segmentLinePrefab.gameObject.SetActive(false); // 원본 Template은 화면에 보이지 않게 숨긴다.
            }
        }

        private void Update()
        {
            if (health == null) // EnemyHealth를 찾지 못했다면
            {
                return; // HP를 표시할 수 없으므로 종료한다.
            }

            UpdateBarLayoutIfNeeded(); // MaxHp 또는 UI 설정이 바뀌면 칸과 크기를 다시 계산한다.
            UpdateFillAmount(); // CurrentHp / MaxHp 비율로 빨간 Fill을 갱신한다.
        }

        private void UpdateBarLayoutIfNeeded() // HP바 크기와 칸 구분선을 갱신하는 함수
        {
            float maxHp = health.MaxHp; // 현재 최대 체력을 가져온다.

            if (Mathf.Approximately(cachedMaxHp, maxHp) &&
                Mathf.Approximately(cachedFixedFillWidth, fixedFillWidth) &&
                Mathf.Approximately(cachedBarHeight, barHeight) &&
                Mathf.Approximately(cachedHpPerSegment, hpPerSegment) &&
                cachedMaxVisibleSegmentCount == maxVisibleSegmentCount &&
                Mathf.Approximately(cachedFillHorizontalPadding, fillHorizontalPadding) &&
                Mathf.Approximately(cachedFillVerticalPadding, fillVerticalPadding))
            {
                return; // 이전 값들과 모두 같으면 다시 계산하지 않는다.
            }

            cachedMaxHp = maxHp; // 현재 MaxHp를 저장한다.
            cachedFixedFillWidth = fixedFillWidth; // 현재 고정 Fill 너비를 저장한다.
            cachedBarHeight = barHeight; // 현재 HP바 높이를 저장한다.
            cachedHpPerSegment = hpPerSegment; // 현재 칸 계산 기준을 저장한다.
            cachedMaxVisibleSegmentCount = maxVisibleSegmentCount; // 현재 최대 표시 칸 수를 저장한다.
            cachedFillHorizontalPadding = fillHorizontalPadding; // 현재 좌우 여백을 저장한다.
            cachedFillVerticalPadding = fillVerticalPadding; // 현재 위아래 여백을 저장한다.

            int rawSegmentCount = Mathf.CeilToInt(maxHp / hpPerSegment); // MaxHp 기준 실제 칸 수를 계산한다.
            rawSegmentCount = Mathf.Max(1, rawSegmentCount); // 최소 1칸은 보장한다.

            int visibleSegmentCount = Mathf.Min(rawSegmentCount, maxVisibleSegmentCount); // 화면에 표시할 칸 수를 제한한다.
            visibleSegmentCount = Mathf.Max(1, visibleSegmentCount); // 최소 1칸은 보장한다.

            float fillWidth = fixedFillWidth; // HP바 Fill 길이는 MaxHp와 상관없이 고정한다.
            float fillHeight = Mathf.Max(1.0f, barHeight - fillVerticalPadding * 2.0f); // 빨간 Fill 높이를 계산한다.

            float barWidth = fillWidth + fillHorizontalPadding * 2.0f; // 검은 프레임 전체 너비를 계산한다.

            if (barRoot != null) // 검은 프레임 Root가 연결되어 있다면
            {
                barRoot.sizeDelta = new Vector2(barWidth, barHeight); // 검은 프레임 크기를 고정 크기로 설정한다.
            }

            if (fillImage != null) // 빨간 Fill Image가 연결되어 있다면
            {
                RectTransform fillRect = fillImage.rectTransform; // Fill의 RectTransform을 가져온다.

                fillRect.anchoredPosition = Vector2.zero; // Fill을 프레임 중앙에 둔다.
                fillRect.sizeDelta = new Vector2(fillWidth, fillHeight); // Fill 크기를 고정 크기로 설정한다.
                fillRect.localScale = Vector3.one; // Fill 스케일을 기본값으로 맞춘다.
            }

            if (segmentLineRoot != null) // 칸 구분선 부모가 연결되어 있다면
            {
                segmentLineRoot.anchoredPosition = Vector2.zero; // 구분선 부모를 중앙에 둔다.
                segmentLineRoot.sizeDelta = new Vector2(fillWidth, fillHeight); // 구분선 영역을 Fill 영역과 맞춘다.
                segmentLineRoot.localScale = Vector3.one; // 구분선 부모 스케일을 기본값으로 맞춘다.
            }

            RebuildSegmentLines(visibleSegmentCount, fillWidth, fillHeight); // 표시할 칸 수 기준으로 구분선을 다시 만든다.
        }

        private void UpdateFillAmount() // 현재 체력 비율로 Fill을 갱신하는 함수
        {
            if (fillImage == null) // Fill Image가 없다면
            {
                return; // 표시할 수 없으므로 종료한다.
            }

            float maxHp = Mathf.Max(1.0f, health.MaxHp); // 0으로 나누는 상황을 막기 위해 MaxHp를 최소 1로 보정한다.
            float hpRate = Mathf.Clamp01(health.CurrentHp / maxHp); // 현재 체력 비율을 0~1 사이로 계산한다.

            fillImage.fillAmount = hpRate; // 빨간 Fill의 채워진 양을 체력 비율로 설정한다.
        }

        private void RebuildSegmentLines(int visibleSegmentCount, float fillWidth, float fillHeight) // 칸 구분선을 다시 생성하는 함수
        {
            if (segmentLineRoot == null) // 구분선 부모가 없다면
            {
                return; // 구분선을 만들 수 없으므로 종료한다.
            }

            if (segmentLinePrefab == null) // 구분선 원본이 없다면
            {
                return; // 구분선을 만들 수 없으므로 종료한다.
            }

            for (int i = segmentLineRoot.childCount - 1; i >= 0; i--) // 기존 자동 생성 구분선을 뒤에서부터 순회한다.
            {
                Destroy(segmentLineRoot.GetChild(i).gameObject); // 기존 구분선을 제거한다.
            }

            segmentLinePrefab.gameObject.SetActive(false); // 원본 Template은 계속 숨겨둔다.

            float displayedSegmentWidth = fillWidth / visibleSegmentCount; // 고정된 Fill 길이 안에서 칸 하나의 표시 너비를 계산한다.

            for (int i = 1; i < visibleSegmentCount; i++) // 표시할 칸 사이에 구분선을 만든다.
            {
                Image line = Instantiate(segmentLinePrefab, segmentLineRoot); // 구분선 원본을 복사한다.
                line.gameObject.SetActive(true); // 복사된 구분선은 화면에 보이게 한다.

                RectTransform lineRect = line.rectTransform; // 생성된 구분선의 RectTransform을 가져온다.

                float x = -fillWidth * 0.5f + displayedSegmentWidth * i; // 고정된 Fill 영역 안에서 i번째 칸 경계 위치를 계산한다.

                lineRect.anchoredPosition = new Vector2(x, 0.0f); // 구분선 위치를 설정한다.
                lineRect.sizeDelta = new Vector2(1.0f, fillHeight); // 구분선을 얇은 세로선으로 만든다.
                lineRect.localScale = Vector3.one; // 스케일을 기본값으로 맞춘다.
            }
        }
    }
}