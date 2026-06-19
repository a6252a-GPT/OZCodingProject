using UnityEngine;

namespace TeamProject01.Gameplay
{
    public sealed class EnemyImpactDebugVisual : MonoBehaviour // VFX가 없을 때 사용할 임시 임팩트 표시
    {
        [Min(0.1f)]
        [SerializeField] private float lifeTime = 0.7f; // 임팩트 오브젝트가 유지될 시간

        [Min(0.0f)]
        [SerializeField] private float startScaleMultiplier = 0.3f; // 처음 생성될 때 크기 배율

        [Min(0.0f)]
        [SerializeField] private float endScaleMultiplier = 1.3f; // 사라지기 직전 크기 배율

        [Min(0.01f)]
        [SerializeField] private float blinkInterval = 0.08f; // 깜빡이는 간격

        [SerializeField] private Renderer targetRenderer; // 깜빡임을 적용할 Renderer

        private Vector3 baseScale; // Prefab 원래 크기
        private float lifeTimer; // 생성 후 지난 시간

        private void Awake()
        {
            baseScale = transform.localScale; // Prefab의 원래 크기를 저장한다.

            if (targetRenderer == null) // Inspector에서 Renderer가 연결되지 않았다면
            {
                targetRenderer = GetComponentInChildren<Renderer>(); // 자식까지 포함해서 Renderer를 찾는다.
            }

            transform.localScale = baseScale * startScaleMultiplier; // 처음에는 작게 보이게 한다.
        }

        private void Update()
        {
            lifeTimer += Time.deltaTime; // 지난 시간만큼 유지 시간을 증가시킨다.

            float progress = lifeTimer / lifeTime; // 현재 진행률을 계산한다.
            progress = Mathf.Clamp01(progress); // 진행률을 0~1 사이로 제한한다.

            float scaleMultiplier = Mathf.Lerp(startScaleMultiplier, endScaleMultiplier, progress); // 시작 크기에서 끝 크기까지 점점 커지게 한다.
            transform.localScale = baseScale * scaleMultiplier; // 계산된 크기를 적용한다.

            ApplyBlink(); // 깜빡임을 적용한다.

            if (lifeTimer >= lifeTime) // 유지 시간이 끝났다면
            {
                Destroy(gameObject); // 임팩트 오브젝트를 제거한다.
            }
        }

        public void Configure(float newLifeTime) // 외부에서 유지 시간을 설정하는 함수
        {
            lifeTime = Mathf.Max(0.1f, newLifeTime); // 유지 시간을 최소 0.1초로 제한해서 저장한다.
            lifeTimer = 0.0f; // 유지 시간 타이머를 초기화한다.
        }

        private void ApplyBlink() // Renderer를 켜고 끄며 번쩍이는 느낌을 주는 함수
        {
            if (targetRenderer == null) // Renderer가 없다면
            {
                return; // 깜빡임을 적용하지 않는다.
            }

            int blinkIndex = Mathf.FloorToInt(lifeTimer / blinkInterval); // 현재 시간이 몇 번째 깜빡임 구간인지 계산한다.
            bool visible = blinkIndex % 2 == 0; // 짝수 구간이면 보이고, 홀수 구간이면 안 보이게 한다.

            targetRenderer.enabled = visible; // Renderer 표시 상태를 적용한다.
        }
    }
}