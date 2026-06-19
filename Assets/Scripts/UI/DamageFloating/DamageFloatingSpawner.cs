using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace TeamProject01.Gameplay
{
    public sealed class DamageFloatingSpawner : MonoBehaviour //전찬우추가 - 데미지 숫자 풀
    {
        private const string FontResourcesPath = "UI/Fonts/DamageFloating"; //전찬우추가 - 테스트 폰트 경로
        private const string SourceFontResourcesPath = "UI/Fonts/DamageFloatingSource"; //전찬우추가 - 원본 TTF 경로

        [SerializeField] private DamageFloatingPopup popupPrefab; //전찬우추가 - 선택 프리팹
        [SerializeField] private int initialPoolSize = 24; //전찬우추가 - 초기 풀
        [SerializeField] private bool allowPoolExpansion = true; //전찬우추가 - 풀 확장
        [SerializeField] private TMP_FontAsset[] fontCatalog; //전찬우추가 - 폰트 후보
        [SerializeField] private int activeFontIndex; //전찬우추가 - 현재 폰트

        private static DamageFloatingSpawner instance; //전찬우추가 - singleton
        private readonly Queue<DamageFloatingPopup> pool = new Queue<DamageFloatingPopup>(); //전찬우추가 - 팝업 풀

        private void Awake() //전찬우추가 - 풀 준비
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject); //전찬우추가 - 중복 제거
                return; //전찬우추가 - 기존 사용
            }

            instance = this; //전찬우추가 - 등록
            ResolveFontCatalog(); //전찬우추가 - 폰트 목록 로드
            PrewarmPool(); //전찬우추가 - 풀 예열
        }

        public static void SpawnEnemyDamage(DamageData damage, float actualDamage, Vector3 fallbackPosition) //전찬우추가 - 몬스터 피해 표시
        {
            if (actualDamage <= 0f)
            {
                return; //전찬우추가 - 표시할 피해 없음
            }

            Instance.SpawnDamage(damage, actualDamage, fallbackPosition); //전찬우추가 - 표시 요청
        }

        public static string CycleFontAndSpawnSample() //전찬우추가 - 테스트 폰트 순환
        {
            DamageFloatingSpawner spawner = Instance; //전찬우추가 - 스포너 확보
            spawner.ResolveFontCatalog(); //전찬우추가 - 목록 최신화
            spawner.CycleFont(); //전찬우추가 - 다음 폰트
            spawner.SpawnSample(); //전찬우추가 - 샘플 표시
            return spawner.GetActiveFontName(); //전찬우추가 - 현재 폰트명
        }

        public static string GetActiveFontDisplayName() //전찬우추가 - 현재 폰트명 조회
        {
            return Instance.GetActiveFontName(); //전찬우추가 - 표시명 반환
        }

        private static DamageFloatingSpawner Instance //전찬우추가 - 런타임 보장
        {
            get
            {
                if (instance != null)
                {
                    return instance; //전찬우추가 - 기존 사용
                }

                DamageFloatingSpawner found = FindFirstObjectByType<DamageFloatingSpawner>(FindObjectsInactive.Include); //전찬우추가 - 씬 검색
                if (found != null)
                {
                    instance = found; //전찬우추가 - 기존 등록
                    return instance; //전찬우추가 - 반환
                }

                GameObject root = new GameObject("DamageFloatingSpawner"); //전찬우추가 - 런타임 생성
                instance = root.AddComponent<DamageFloatingSpawner>(); //전찬우추가 - 컴포넌트 생성
                return instance; //전찬우추가 - 반환
            }
        }

        private void SpawnDamage(DamageData damage, float actualDamage, Vector3 fallbackPosition) //전찬우추가 - 숫자 생성
        {
            DamageFloatingPopup popup = GetPopup(); //전찬우추가 - 풀에서 확보
            Vector3 position = ResolveHitPosition(damage, fallbackPosition) + Vector3.up * 0.85f; //전찬우추가 - 표시 위치
            popup.Initialize(FormatDamage(actualDamage), ResolveColor(damage.Type), position, ResolveFontSize(damage.Type), GetActiveFont(), ReleasePopup); //전찬우추가 - 팝업 시작
        }

        private void SpawnSample() //전찬우추가 - 폰트 확인용 샘플
        {
            DamageFloatingPopup popup = GetPopup(); //전찬우추가 - 풀에서 확보
            Vector3 position = ResolveSamplePosition(); //전찬우추가 - 샘플 위치
            popup.Initialize("123", new Color(1f, 0.9f, 0.25f, 1f), position, 2.9f, GetActiveFont(), ReleasePopup); //전찬우추가 - 샘플 표시
        }

        private void PrewarmPool() //전찬우추가 - 풀 예열
        {
            int count = Mathf.Max(0, initialPoolSize); //전찬우추가 - 수량 보정
            for (int i = pool.Count; i < count; i++)
            {
                ReleasePopup(CreatePopup()); //전찬우추가 - 생성 후 풀 보관
            }
        }

        private DamageFloatingPopup GetPopup() //전찬우추가 - 팝업 확보
        {
            while (pool.Count > 0)
            {
                DamageFloatingPopup popup = pool.Dequeue(); //전찬우추가 - 재사용
                if (popup != null)
                {
                    return popup; //전찬우추가 - 반환
                }
            }

            return allowPoolExpansion ? CreatePopup() : CreateStandalonePopup(); //전찬우추가 - 부족분 처리
        }

        private DamageFloatingPopup CreatePopup() //전찬우추가 - 풀용 팝업 생성
        {
            DamageFloatingPopup popup = popupPrefab != null ? Instantiate(popupPrefab, transform) : CreateStandalonePopup(); //전찬우추가 - 프리팹/기본
            popup.transform.SetParent(transform, false); //전찬우추가 - 스포너 하위
            popup.gameObject.SetActive(false); //전찬우추가 - 대기 상태
            return popup; //전찬우추가 - 반환
        }

        private DamageFloatingPopup CreateStandalonePopup() //전찬우추가 - 기본 팝업 생성
        {
            GameObject popupObject = new GameObject("DamageFloatingPopup"); //전찬우추가 - 오브젝트
            TextMeshPro text = popupObject.AddComponent<TextMeshPro>(); //전찬우추가 - TMP
            text.raycastTarget = false; //전찬우추가 - 입력 차단 방지
            return popupObject.AddComponent<DamageFloatingPopup>(); //전찬우추가 - 팝업 컴포넌트
        }

        private void ReleasePopup(DamageFloatingPopup popup) //전찬우추가 - 풀 반환
        {
            if (popup == null)
            {
                return; //전찬우추가 - 대상 없음
            }

            popup.gameObject.SetActive(false); //전찬우추가 - 비활성화
            popup.transform.SetParent(transform, false); //전찬우추가 - 풀 하위 정리
            pool.Enqueue(popup); //전찬우추가 - 큐 보관
        }

        private void ResolveFontCatalog() //전찬우추가 - 폰트 목록 로드
        {
            if (fontCatalog != null && fontCatalog.Length > 0)
            {
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); //전찬우추가 - 범위 보정
                return; //전찬우추가 - 이미 있음
            }

            TMP_FontAsset[] loaded = Resources.LoadAll<TMP_FontAsset>(FontResourcesPath); //전찬우추가 - Resources 폰트
            if (loaded != null && loaded.Length > 0)
            {
                Array.Sort(loaded, CompareFontNames); //전찬우추가 - 순서 고정
                fontCatalog = loaded; //전찬우추가 - 후보 등록
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); //전찬우추가 - 범위 보정
                return; //전찬우추가 - TMP 에셋 우선
            }

            Font[] sourceFonts = Resources.LoadAll<Font>(SourceFontResourcesPath); //전찬우추가 - 원본 TTF 로드
            if (sourceFonts != null && sourceFonts.Length > 0)
            {
                Array.Sort(sourceFonts, CompareFontNames); //전찬우추가 - 순서 고정
                List<TMP_FontAsset> generatedFonts = new List<TMP_FontAsset>(sourceFonts.Length); //전찬우추가 - 런타임 TMP 폰트
                for (int i = 0; i < sourceFonts.Length; i++)
                {
                    if (sourceFonts[i] == null)
                    {
                        continue; //전찬우추가 - 누락 방지
                    }

                    TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(sourceFonts[i]); //전찬우추가 - 런타임 TMP 생성
                    generated.name = sourceFonts[i].name + " Runtime SDF"; //전찬우추가 - 표시명
                    generated.atlasPopulationMode = AtlasPopulationMode.Dynamic; //전찬우추가 - 동적 글리프
                    generated.isMultiAtlasTexturesEnabled = true; //전찬우추가 - atlas 확장
                    generatedFonts.Add(generated); //전찬우추가 - 후보 등록
                }

                if (generatedFonts.Count > 0)
                {
                    fontCatalog = generatedFonts.ToArray(); //전찬우추가 - 런타임 후보 적용
                    activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); //전찬우추가 - 범위 보정
                }
            }
        }

        private void CycleFont() //전찬우추가 - 다음 폰트
        {
            ResolveFontCatalog(); //전찬우추가 - 목록 보장
            if (fontCatalog == null || fontCatalog.Length == 0)
            {
                return; //전찬우추가 - 후보 없음
            }

            activeFontIndex = (activeFontIndex + 1) % fontCatalog.Length; //전찬우추가 - 순환
        }

        private TMP_FontAsset GetActiveFont() //전찬우추가 - 현재 폰트
        {
            ResolveFontCatalog(); //전찬우추가 - 목록 보장
            if (fontCatalog != null && fontCatalog.Length > 0)
            {
                activeFontIndex = Mathf.Clamp(activeFontIndex, 0, fontCatalog.Length - 1); //전찬우추가 - 범위 보정
                return fontCatalog[activeFontIndex]; //전찬우추가 - 현재 폰트
            }

            return TMP_Settings.defaultFontAsset; //전찬우추가 - fallback
        }

        private string GetActiveFontName() //전찬우추가 - 표시명
        {
            TMP_FontAsset font = GetActiveFont(); //전찬우추가 - 현재 폰트
            return font != null ? font.name : "Default"; //전찬우추가 - fallback
        }

        private static Vector3 ResolveHitPosition(DamageData damage, Vector3 fallbackPosition) //전찬우추가 - 명중 위치 결정
        {
            if (damage.HitPosition.sqrMagnitude > 0.0001f)
            {
                return damage.HitPosition; //전찬우추가 - 전달 위치 우선
            }

            return fallbackPosition; //전찬우추가 - 몬스터 위치 fallback
        }

        private static Vector3 ResolveSamplePosition() //전찬우추가 - 샘플 위치
        {
            ConvoyController convoy = FindFirstObjectByType<ConvoyController>(); //전찬우추가 - 플레이어 컨보이
            if (convoy != null)
            {
                return convoy.transform.position + Vector3.up * 2.2f; //전찬우추가 - 플레이어 위
            }

            Camera camera = Camera.main; //전찬우추가 - 카메라 fallback
            if (camera != null)
            {
                return camera.transform.position + camera.transform.forward * 6f; //전찬우추가 - 화면 앞
            }

            return Vector3.up * 2f; //전찬우추가 - 최후 fallback
        }

        private static string FormatDamage(float damage) //전찬우추가 - 데미지 문자열
        {
            float rounded = Mathf.Round(damage); //전찬우추가 - 정수 후보
            if (damage < 10f && !Mathf.Approximately(damage, rounded))
            {
                return damage.ToString("0.#"); //전찬우추가 - 소수 피해 표시
            }

            return Mathf.Max(1, Mathf.RoundToInt(damage)).ToString(); //전찬우추가 - 정수 표시
        }

        private static Color ResolveColor(DamageType type) //전찬우추가 - 타입별 색
        {
            switch (type)
            {
                case DamageType.Fire:
                    return new Color(1f, 0.32f, 0.12f, 1f); //전찬우추가 - 화염
                case DamageType.Laser:
                    return new Color(0.35f, 0.88f, 1f, 1f); //전찬우추가 - 레이저
                case DamageType.Explosion:
                    return new Color(1f, 0.58f, 0.18f, 1f); //전찬우추가 - 폭발
                case DamageType.Electric:
                    return new Color(0.62f, 0.92f, 1f, 1f); //전찬우추가 - 전기
                default:
                    return Color.white; //전찬우추가 - 기본
            }
        }

        private static float ResolveFontSize(DamageType type) //전찬우추가 - 타입별 크기
        {
            return type == DamageType.Explosion ? 2.95f : 2.6f; //전찬우추가 - 폭발 강조
        }

        private static int CompareFontNames(UnityEngine.Object left, UnityEngine.Object right) //전찬우추가 - 테스트 폰트 순서
        {
            string leftName = left != null ? left.name : string.Empty; //전찬우추가 - 왼쪽 이름
            string rightName = right != null ? right.name : string.Empty; //전찬우추가 - 오른쪽 이름
            int leftOrder = GetFontSortOrder(leftName); //전찬우추가 - 왼쪽 순서
            int rightOrder = GetFontSortOrder(rightName); //전찬우추가 - 오른쪽 순서
            if (leftOrder != rightOrder)
            {
                return leftOrder.CompareTo(rightOrder); //전찬우추가 - 지정 순서 우선
            }

            return string.Compare(leftName, rightName, StringComparison.Ordinal); //전찬우추가 - 이름 fallback
        }

        private static int GetFontSortOrder(string fontName) //전찬우추가 - 4종 폰트 순서
        {
            if (fontName.Contains("Tium"))
            {
                return 0; //전찬우추가 - Tium
            }

            if (fontName.Contains("Pretendard"))
            {
                return 1; //전찬우추가 - Pretendard
            }

            if (fontName.Contains("Gwangyang"))
            {
                return 2; //전찬우추가 - Gwangyang
            }

            if (fontName.Contains("Elice"))
            {
                return 3; //전찬우추가 - Elice
            }

            return 100; //전찬우추가 - 기타
        }
    }
}
