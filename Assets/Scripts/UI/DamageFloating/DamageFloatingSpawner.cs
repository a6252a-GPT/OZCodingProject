using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TeamProject01.Gameplay
{
    public sealed class DamageFloatingSpawner : MonoBehaviour // 데미지 숫자 풀
    {
        private const string FontResourcesPath = "UI/Fonts/DamageFloating"; // 테스트 폰트 경로
        private const string SourceFontResourcesPath = "UI/Fonts/DamageFloatingSource"; // 원본 TTF 경로

        [SerializeField] private DamageFloatingPopup popupPrefab; // 선택 프리팹
        [SerializeField] private int initialPoolSize = 24; // 초기 풀
        [SerializeField] private bool allowPoolExpansion = true; // 풀 확장
        [SerializeField] private TMP_FontAsset[] fontCatalog; // 폰트 후보
        [SerializeField] private int activeFontIndex; // 현재 폰트

        private static DamageFloatingSpawner instance; // singleton
        private readonly Queue<DamageFloatingPopup> pool = new Queue<DamageFloatingPopup>(); // 팝업 풀

        private void Awake() // 풀 준비
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); // 중복 제거
                return; // 기존 사용
            }

            instance = this; // 등록
            ResolveFontCatalog(); // 폰트 목록 로드
            PrewarmPool(); // 풀 예열
        }

        public static void SpawnEnemyDamage(DamageData damage, float actualDamage, Vector3 fallbackPosition) // 몬스터 피해 표시
        {
            if (actualDamage <= 0f)
            {
                return; // 표시할 피해 없음
            }

            Instance.SpawnDamage(damage, actualDamage, fallbackPosition); // 표시 요청
        }

        public static string CycleFontAndSpawnSample() // 테스트 폰트 순환
        {
            DamageFloatingSpawner spawner = Instance; // 스포너 확보
            spawner.ResolveFontCatalog(); // 목록 최신화
            spawner.CycleFont(); // 다음 폰트
            spawner.SpawnSample(); // 샘플 표시
            return spawner.GetActiveFontName(); // 현재 폰트명
        }

        public static string GetActiveFontDisplayName() // 현재 폰트명 조회
        {
            return Instance.GetActiveFontName(); // 표시명 반환
        }

        private static DamageFloatingSpawner Instance // 런타임 보장
        {
            get
            {
                if (instance != null)
                {
                    return instance; // 기존 사용
                }

                DamageFloatingSpawner found = FindFirstObjectByType<DamageFloatingSpawner>(FindObjectsInactive.Include); // 씬 검색
                if (found != null)
                {
                    instance = found; // 기존 등록
                    return instance; // 반환
                }

                GameObject root = new GameObject("DamageFloatingSpawner"); // 런타임 생성
                instance = root.AddComponent<DamageFloatingSpawner>(); // 컴포넌트 생성
                return instance; // 반환
            }
        }

        private void SpawnDamage(DamageData damage, float actualDamage, Vector3 fallbackPosition) // 숫자 생성
        {
            DamageFloatingPopup popup = GetPopup(); // 풀에서 확보
            Vector3 position = ResolveHitPosition(damage, fallbackPosition) + Vector3.up * 0.85f; // 표시 위치
            popup.Initialize(FormatDamage(actualDamage), ResolveColor(damage.Type), position, ResolveFontSize(damage.Type), GetActiveFont(), ReleasePopup); // 팝업 시작
        }

        private void SpawnSample() // 폰트 확인용 샘플
        {
            DamageFloatingPopup popup = GetPopup(); // 풀에서 확보
            Vector3 position = ResolveSamplePosition(); // 샘플 위치
            popup.Initialize("123", new Color(1f, 0.9f, 0.25f, 1f), position, 2.9f, GetActiveFont(), ReleasePopup); // 샘플 표시
        }

        private void PrewarmPool() // 풀 예열
        {
            int count = Mathf.Max(0, initialPoolSize); // 수량 보정
            for (int i = pool.Count; i < count; i++)
            {
                ReleasePopup(CreatePopup()); // 생성 후 풀 보관
            }
        }

        private DamageFloatingPopup GetPopup() // 팝업 확보
        {
            while (pool.Count > 0)
            {
                DamageFloatingPopup popup = pool.Dequeue(); // 재사용
                if (popup != null)
                {
                    return popup; // 반환
                }
            }

            return allowPoolExpansion ? CreatePopup() : CreateStandalonePopup(); // 부족분 처리
        }

        private DamageFloatingPopup CreatePopup() // 풀용 팝업 생성
        {
            DamageFloatingPopup popup = popupPrefab != null ? Instantiate(popupPrefab, transform) : CreateStandalonePopup(); // 프리팹/기본
            popup.transform.SetParent(transform, false); // 스포너 하위
            popup.gameObject.SetActive(false); // 대기 상태
            return popup; // 반환
        }

        private DamageFloatingPopup CreateStandalonePopup() // 기본 팝업 생성
        {
            GameObject popupObject = new GameObject("DamageFloatingPopup"); // 오브젝트
            TextMeshPro text = popupObject.AddComponent<TextMeshPro>(); // TMP
            text.raycastTarget = false; // 입력 차단 방지
            return popupObject.AddComponent<DamageFloatingPopup>(); // 팝업 컴포넌트
        }

        private void ReleasePopup(DamageFloatingPopup popup) // 풀 반환
        {
            if (popup == null)
            {
                return; // 대상 없음
            }

            popup.gameObject.SetActive(false); // 비활성화
            popup.transform.SetParent(transform, false); // 풀 하위 정리
            pool.Enqueue(popup); // 큐 보관
        }

        private void ResolveFontCatalog() // 폰트 목록 로드
        {
            if (fontCatalog != null && fontCatalog.Length > 0)
            {
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); // 범위 보정
                return; // 이미 있음
            }

            TMP_FontAsset[] loaded = Resources.LoadAll<TMP_FontAsset>(FontResourcesPath); // Resources 폰트
            if (loaded != null && loaded.Length > 0)
            {
                Array.Sort(loaded, CompareFontNames); // 순서 고정
                fontCatalog = loaded; // 후보 등록
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); // 범위 보정
                return; // TMP 에셋 우선
            }

            Font[] sourceFonts = Resources.LoadAll<Font>(SourceFontResourcesPath); // 원본 TTF 로드
            if (sourceFonts != null && sourceFonts.Length > 0)
            {
                Array.Sort(sourceFonts, CompareFontNames); // 순서 고정
                List<TMP_FontAsset> generatedFonts = new List<TMP_FontAsset>(sourceFonts.Length); // 런타임 TMP 폰트
                for (int i = 0; i < sourceFonts.Length; i++)
                {
                    if (sourceFonts[i] == null)
                    {
                        continue; // 누락 방지
                    }

                    TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(sourceFonts[i]); // 런타임 TMP 생성
                    generated.name = sourceFonts[i].name + " Runtime SDF"; // 표시명
                    generated.atlasPopulationMode = AtlasPopulationMode.Dynamic; // 동적 글리프
                    generated.isMultiAtlasTexturesEnabled = true; // atlas 확장
                    generatedFonts.Add(generated); // 후보 등록
                }

                if (generatedFonts.Count > 0)
                {
                    fontCatalog = generatedFonts.ToArray(); // 런타임 후보 적용
                    activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); // 범위 보정
                }
            }
        }

        private void CycleFont() // 다음 폰트
        {
            ResolveFontCatalog(); // 목록 보장
            if (fontCatalog == null || fontCatalog.Length == 0)
            {
                return; // 후보 없음
            }

            activeFontIndex = (activeFontIndex + 1) % fontCatalog.Length; // 순환
        }

        private TMP_FontAsset GetActiveFont() // 현재 폰트
        {
            ResolveFontCatalog(); // 목록 보장
            if (fontCatalog != null && fontCatalog.Length > 0)
            {
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); // 범위 보정
                return fontCatalog[activeFontIndex]; // 현재 폰트
            }

            return TMP_Settings.defaultFontAsset; // fallback
        }

        private string GetActiveFontName() // 표시명
        {
            TMP_FontAsset font = GetActiveFont(); // 현재 폰트
            return FormatFontDisplayName(font != null ? font.name : "Default"); // UI 표시명 반환
        }

        private static string FormatFontDisplayName(string fontName) // 버튼용 폰트명 정리
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return "Default"; // fallback
            }

            string displayName = fontName.Replace(" Runtime SDF", string.Empty); // 런타임 접미사 제거
            displayName = displayName.Replace(" SDF", string.Empty); // TMP 접미사 제거
            displayName = displayName.Replace("_", " "); // 파일명 구분자 정리
            return displayName.Trim(); // 표시명
        }

        private static Vector3 ResolveHitPosition(DamageData damage, Vector3 fallbackPosition) // 명중 위치 결정
        {
            if (damage.HitPosition.sqrMagnitude > 0.0001f)
            {
                return damage.HitPosition; // 전달 위치 우선
            }

            return fallbackPosition; // 몬스터 위치 fallback
        }

        private static Vector3 ResolveSamplePosition() // 샘플 위치
        {
            ConvoyController convoy = FindFirstObjectByType<ConvoyController>(); // 플레이어 컨보이
            if (convoy != null)
            {
                return convoy.transform.position + Vector3.up * 2.2f; // 플레이어 위
            }

            Camera camera = Camera.main; // 카메라 fallback
            if (camera != null)
            {
                return camera.transform.position + camera.transform.forward * 6f; // 화면 앞
            }

            return Vector3.up * 2f; // 최후 fallback
        }

        private static string FormatDamage(float damage) // 데미지 문자열
        {
            float rounded = Mathf.Round(damage); // 정수 후보
            if (damage < 10f && !Mathf.Approximately(damage, rounded))
            {
                return damage.ToString("0.#"); // 소수 피해 표시
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(); // 정수 표시
        }

        private static Color ResolveColor(DamageType type) // 타입별 색
        {
            switch (type)
            {
                case DamageType.Fire:
                    return new Color(1f, 0.32f, 0.12f, 1f); // 화염
                case DamageType.Laser:
                    return new Color(0.35f, 0.88f, 1f, 1f); // 레이저
                case DamageType.Explosion:
                    return new Color(1f, 0.58f, 0.18f, 1f); // 폭발
                case DamageType.Electric:
                    return new Color(0.62f, 0.92f, 1f, 1f); // 전기
                default:
                    return Color.white; // 기본
            }
        }

        private static float ResolveFontSize(DamageType type) // 타입별 크기
        {
            return type == DamageType.Explosion ? 2.95f : 2.6f; // 폭발 강조
        }

        private static int CompareFontNames(UnityEngine.Object left, UnityEngine.Object right) // 테스트 폰트 순서
        {
            string leftName = left != null ? left.name : string.Empty; // 왼쪽 이름
            string rightName = right != null ? right.name : string.Empty; // 오른쪽 이름
            int leftOrder = GetFontSortOrder(leftName); // 왼쪽 순서
            int rightOrder = GetFontSortOrder(rightName); // 오른쪽 순서
            if (leftOrder != rightOrder)
            {
                return leftOrder.CompareTo(rightOrder); // 지정 순서 우선
            }

            return string.Compare(leftName, rightName, StringComparison.Ordinal); // 이름 fallback
        }

        private static int GetFontSortOrder(string fontName) // 4종 폰트 순서
        {
            if (fontName.Contains("Tium"))
            {
                return 0; // Tium
            }

            if (fontName.Contains("Pretendard"))
            {
                return 1; // Pretendard
            }

            if (fontName.Contains("Gwangyang"))
            {
                return 2; // Gwangyang
            }

            if (fontName.Contains("Elice"))
            {
                return 3; // Elice
            }

            return 100; // 기타
        }
    }
}
