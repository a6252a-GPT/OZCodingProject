// 안건준 추가 - 0623
// 카드 등급(레어/유니크)에 따라 VFX 이팩트를 카드에 적용하는 컴포넌트
// CardUI.cs 와 동일 오브젝트에 추가해서 사용

using System.Collections.Generic;
using UnityEngine;
using TeamProject01.Gameplay;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CardEffect : MonoBehaviour
{
    // 레어 카드 이팩트 종류 (_G 계열, 초록색)
    // 이름 = Assets/.../Prefabs/URP/FX_{이름}.prefab 과 1:1 대응
    public enum RareEffectKind
    {
        None = 0,
        CardBrush_G,
        CardBrushLine_G,
        CardCubeUp_G,
        CardDissolve_G,
        CardEdgeFlow_G,
        CardFeather_G,
        CardFlare_G,
        CardFlash_G,
        CardFlashAir_G,
        CardFlowLoop_G,
        CardLight_Loop_G,
        CardRimLine_G,
        CardRimStar_G,
        CardShock_Loop_G,
        CardStar_Loop_G,
        CardSunLight_Loop_G
    }

    // 유니크 카드 이팩트 종류 (_Y 계열, 노란색)
    public enum UniqueEffectKind
    {
        None = 0,
        CardBrush_Y,
        CardBrushLine_Y,
        CardCubeUp_Y,
        CardDissolve_Y,
        CardEdgeFlow_Y,
        CardFeather_Y,
        CardFlare_Y,
        CardFlash_Y,
        CardFlashAir_Y,
        CardFlowLoop_Y,
        CardLight_Loop_Y,
        CardRimLine_Y,
        CardRimStar_Y,
        CardShock_Loop_Y,
        CardStar_Loop_Y,
        CardSunLight_Loop_Y
    }

    [Header("이팩트 종류 선택 (레어 카드)")]
    [SerializeField] private RareEffectKind rareEffect = RareEffectKind.CardLight_Loop_G;
    [Header("이팩트 종류 선택 (유니크 카드)")]
    [SerializeField] private UniqueEffectKind uniqueEffect = UniqueEffectKind.CardLight_Loop_Y;

    [Header("이팩트 프리팹 (자동 등록방식)")]
    [SerializeField] private GameObject rareEffectPrefab;   // 레어 등급 프리팹
    [SerializeField] private GameObject uniqueEffectPrefab; // 유니크 등급 프리팹
    [Tooltip("일반 등급 이팩트 (없으면 미적용)")]
    [SerializeField] private GameObject normalEffectPrefab; // 일반 등급 프리팹 (선택)

    [Header("이팩트 설정")]
    [Range(0f, 1f)]
    [Tooltip("이팩트 밝기 (0=완전 어둡게, 1=원본 밝기)")]
    public float EffectBrightness = 1f;
    [Tooltip("카드 크기 대비 가로 배율 (1.0 = 카드 가로와 동일)")]
    [Min(0.01f)] public float EffectWidth = 1f;
    [Tooltip("카드 크기 대비 세로 배율 (1.0 = 카드 세로와 동일)")]
    [Min(0.01f)] public float EffectHeight = 1f;

    // 카드 루트 → 활성화된 이팩트 인스턴스 매핑
    private readonly Dictionary<GameObject, GameObject> activeEffects = new();

    // 카드 루트에 등급에 맞는 이팩트를 부착
    public void ApplyEffect(GameObject cardRoot, StatUpgrade.StatCardTier tier)
    {
        if (cardRoot == null) return;

        ClearEffect(cardRoot); // 기존 이팩트 제거

        GameObject prefab = ResolvePrefab(tier);
        if (prefab == null) return;

        // Canvas 레이아웃 강제 갱신 → GetWorldCorners 의 위치가 0 이 되는 문제 방지
        Canvas.ForceUpdateCanvases();

        GameObject effect = Instantiate(prefab); // 월드 스페이스에서 먼저 생성

        Canvas.ForceUpdateCanvases();

        RectTransform rt = cardRoot.GetComponent<RectTransform>();
        if (rt != null)
        {
            // 카드 4 모서리 월드 좌표로 중심·크기 계산 (카드가 완전히 열린 뒤 호출되므로 정확)
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);

            Vector3 worldCenter = (corners[0] + corners[2]) * 0.5f;
            float worldW = Vector3.Distance(corners[0], corners[3]) * EffectWidth;
            float worldH = Vector3.Distance(corners[0], corners[1]) * EffectHeight;

                // 혹시 GetWorldCorners 가 0 이면 lossyScale fallback
            if (worldW < 0.0001f) worldW = rt.rect.width  * Mathf.Abs(rt.lossyScale.x) * EffectWidth;
            if (worldH < 0.0001f) worldH = rt.rect.height * Mathf.Abs(rt.lossyScale.y) * EffectHeight;

            // 카드의 자식으로 배치, z = -0.1 로 카드 앞에 렌더링
            effect.transform.SetParent(cardRoot.transform, false);
            effect.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            effect.transform.localRotation = Quaternion.identity;

            // lossyScale 을 고려해 로컬 스케일로 변환
            Vector3 ls = rt.lossyScale;
            float localSX = Mathf.Abs(ls.x) > 0.0001f ? worldW / ls.x : worldW;
            float localSY = Mathf.Abs(ls.y) > 0.0001f ? worldH / ls.y : worldH;
            effect.transform.localScale = new Vector3(localSX, localSY, 1f);
        }
        else
        {
            effect.transform.SetParent(cardRoot.transform, false);
            effect.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            effect.transform.localScale    = new Vector3(EffectWidth, EffectHeight, 1f);
        }

        ApplyBrightness(effect, EffectBrightness); // 밝기 적용
        ApplySortingFront(effect, cardRoot);        // 카드 앞에 렌더링
        PlayAllParticles(effect);                   // 파티클 강제 재생

        activeEffects[cardRoot] = effect;
        Debug.Log($"[CardEffect] {tier} 이팩트 생성: {effect.name}  worldScale=({effect.transform.lossyScale.x:F4}, {effect.transform.lossyScale.y:F4})", effect);
    }

    // 특정 카드의 이팩트 제거
    public void ClearEffect(GameObject cardRoot)
    {
        if (cardRoot == null) return;
        if (activeEffects.TryGetValue(cardRoot, out GameObject existing) && existing != null)
            Destroy(existing);
        activeEffects.Remove(cardRoot);
    }

    // 전체 이팩트 제거 (카드 패널 닫힐 때 호출)
    public void ClearAll()
    {
        foreach (KeyValuePair<GameObject, GameObject> kvp in activeEffects)
        {
            if (kvp.Value != null) Destroy(kvp.Value);
        }
        activeEffects.Clear();
    }

    // 등급에 맞는 프리팹 반환
    private GameObject ResolvePrefab(StatUpgrade.StatCardTier tier)
    {
        return tier switch
        {
            StatUpgrade.StatCardTier.Unique => uniqueEffectPrefab,
            StatUpgrade.StatCardTier.Rare   => rareEffectPrefab,
            _                               => normalEffectPrefab
        };
    }

    // 이팩트 내부의 모든 파티클 시스템을 강제로 재생
    // (Play on Awake 비활성화 상태, 또는 동적 생성 시 재생이 안 되는 경우 대응)
    private static void PlayAllParticles(GameObject effect)
    {
        // 루트 ParticleSystem 에 withChildren=true 로 한 번에 재생
        ParticleSystem root = effect.GetComponent<ParticleSystem>();
        if (root != null)
        {
            root.Play(withChildren: true);
            return;
        }

        // 루트에 없으면 자식 전체 개별 재생
        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ps.Play(withChildren: false);
        }
    }

    // 카드 Canvas 보다 앞에 렌더링되도록 Sorting 설정
    private static void ApplySortingFront(GameObject effect, GameObject cardRoot)
    {
        int baseSortingOrder = 0;
        string baseSortingLayer = "Default";

        Canvas cardCanvas = cardRoot.GetComponentInParent<Canvas>();
        if (cardCanvas != null)
        {
            baseSortingLayer = cardCanvas.sortingLayerName;
            baseSortingOrder = cardCanvas.sortingOrder;
        }

        // 파티클 렌더러 전체에 카드 sortingOrder + 10 설정
        foreach (Renderer r in effect.GetComponentsInChildren<Renderer>(true))
        {
            r.sortingLayerName = baseSortingLayer;
            r.sortingOrder = baseSortingOrder + 10;
        }
    }

    // 이팩트 인스턴스의 밝기 조절
    private static void ApplyBrightness(GameObject effect, float brightness)
    {
        if (Mathf.Approximately(brightness, 1f)) return;

        foreach (ParticleSystem ps in effect.GetComponentsInChildren<ParticleSystem>(true))
        {
            ParticleSystem.MainModule main = ps.main;
            Color c = main.startColor.color;
            c.r *= brightness; c.g *= brightness; c.b *= brightness;
            main.startColor = c;
        }

        foreach (Light lt in effect.GetComponentsInChildren<Light>(true))
            lt.intensity *= brightness;

        foreach (Renderer r in effect.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.r *= brightness; c.g *= brightness; c.b *= brightness;
                    mat.color = c;
                }
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.r *= brightness; c.g *= brightness; c.b *= brightness;
                    mat.SetColor("_BaseColor", c);
                }
            }
        }
    }

#if UNITY_EDITOR
    // 이팩트 프리팹 경로 (URP 버전)
    private const string PrefabRoot =
        "Assets/ThirdParty/03_LevelSystem/Game VFX - Card Effects Collection/Game VFX - Card Effects Collection/Prefabs/URP/FX_";

    // 인스펙터에서 열거형 변경 시 자동으로 프리팹 할당
    private void OnValidate()
    {
        UpdateRarePrefab();
        UpdateUniquePrefab();
    }

    private void UpdateRarePrefab()
    {
        if (rareEffect == RareEffectKind.None)
        {
            rareEffectPrefab = null;
            return;
        }

        string prefabName = rareEffect.ToString(); // 예: "CardLight_Loop_G"
        GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}{prefabName}.prefab");
        if (loaded != null)
        {
            rareEffectPrefab = loaded;
        }
        else
        {
            Debug.LogWarning($"[CardEffect] 레어 이팩트 프리팹 없음: FX_{prefabName}.prefab");
        }
    }

    private void UpdateUniquePrefab()
    {
        if (uniqueEffect == UniqueEffectKind.None)
        {
            uniqueEffectPrefab = null;
            return;
        }

        string prefabName = uniqueEffect.ToString(); // 예: "CardLight_Loop_Y"
        GameObject loaded = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}{prefabName}.prefab");
        if (loaded != null)
        {
            uniqueEffectPrefab = loaded;
        }
        else
        {
            Debug.LogWarning($"[CardEffect] 유니크 이팩트 프리팹 없음: FX_{prefabName}.prefab");
        }
    }

    [ContextMenu("이팩트 프리팹 재할당 (강제)")]
    private void ForceReassign()
    {
        UpdateRarePrefab();
        UpdateUniquePrefab();
        EditorUtility.SetDirty(this);
        Debug.Log($"[CardEffect] 재할당 결과\n" +
                  $"  레어({rareEffect}): {(rareEffectPrefab != null ? rareEffectPrefab.name : "NULL — 경로를 확인하세요!")}\n" +
                  $"  유니크({uniqueEffect}): {(uniqueEffectPrefab != null ? uniqueEffectPrefab.name : "NULL — 경로를 확인하세요!")}");
    }
#endif
}
