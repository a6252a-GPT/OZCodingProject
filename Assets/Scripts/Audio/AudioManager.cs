using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : AudioSingleton<AudioManager>
{
    [Header("AudioSource")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource; //여긴 효과음 추가할꺼 넣으면 됩니다.

    [Header("BGM List")]
    [SerializeField] private BGMClipData[] bgmClips; //인스펙터에서 등록할 BGM
    [Header("SFX List")]
    [SerializeField] private SFXClipData[] sfxClips; //인스펙터에서 등록할 효과음

    private static readonly HashSet<SfxVolumeListener> sfxListeners = new HashSet<SfxVolumeListener>();
    private static readonly Dictionary<int, float> sfxBaseVolumes = new Dictionary<int, float>();

    private Dictionary<BGMType, BGMClipData> bgmDictionary;
    private Dictionary<SFXType, SFXClipData> sfxDictionary;

    private BGMClipData currentBGMClip;
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public const string BgmVolumePrefKey = "Settings.BGMVolume";
    public const string SfxVolumePrefKey = "Settings.SFXVolume";
    public const string MasterVolumePrefKey = "Settings.MasterVolume"; //안건준 추가 - 0628
    public const float DefaultVolume = 1f; // 저장값 없을 때 기본 볼륨 100% //안건준 추가 - 0629

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;
    public float MasterVolume => masterVolume;

    public static float GlobalSfxVolume { get; private set; } = 1f;
    public static float GlobalBgmVolume { get; private set; } = 1f;
    public static float GlobalMasterVolume { get; private set; } = 1f;

    public static AudioManager EnsureExists()
    {
        AudioManager manager = Instance;
        if (manager != null)
        {
            manager.EnsureRuntimeReady();
            manager.TryRecoverClipConfiguration(); // 클립/딕셔너리 유실 시 씬 AudioManager에서 복구 //안건준 수정 - 0629
            return manager;
        }

        manager = FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (manager != null)
        {
            manager.EnsureRuntimeReady();
            return Instance ?? manager; // Awake 전이면 씬 인스턴스 반환 //안건준 수정 - 0629
        }

        Debug.LogWarning("[AudioManager] 씬에 AudioManager가 없어 런타임 생성합니다. TitleScene AudioManager를 사용하는 것을 권장합니다."); //안건준 추가 - 0629
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        manager = go.AddComponent<AudioManager>();
        manager.EnsureRuntimeReady();
        return manager;
    }

    public static void PlayClickButtonSfx() // 타이틀/UI 공통 클릭음 — 복구 후 재생 //안건준 추가 - 0629
    {
        AudioManager manager = EnsureExists();
        if (manager == null)
        {
            Debug.LogWarning("[AudioManager] AudioManager를 찾을 수 없습니다.");
            return;
        }

        if (!manager.TryGetSfxClip(SFXType.ClickButton, out AudioClip clip, out float localVolume))
        {
            Debug.LogWarning("[AudioManager] ClickButton 클립이 없습니다. TitleScene → AudioManager → SFX List를 확인하세요.");
            return;
        }

        float effectiveVolume = manager.GetEffectiveSfxVolume(localVolume);
        if (effectiveVolume <= 0.0001f)
        {
            Debug.LogWarning(
                $"[AudioManager] 클릭음 볼륨이 0입니다. 설정에서 Master/SFX 볼륨을 확인하세요. (Master={manager.masterVolume:F2}, SFX={manager.sfxVolume:F2})");
            return;
        }

        manager.PlaySfxOneShotDirect(clip, localVolume);
    }

    public static void SetGlobalSfxVolume(float volume)
    {
        GlobalSfxVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumePrefKey, GlobalSfxVolume);
        PlayerPrefs.Save();

        AudioManager manager = EnsureExists();
        manager.sfxVolume = GlobalSfxVolume;
        manager.RefreshAllSfxSources();
    }

    public static void SetGlobalBgmVolume(float volume)
    {
        GlobalBgmVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(BgmVolumePrefKey, GlobalBgmVolume);
        PlayerPrefs.Save();

        AudioManager manager = EnsureExists();
        manager.SetBGMVolume(GlobalBgmVolume);
    }

    public static void SetGlobalMasterVolume(float volume)
    {
        GlobalMasterVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumePrefKey, GlobalMasterVolume);
        PlayerPrefs.Save();

        AudioManager manager = EnsureExists();
        manager.SetMasterVolume(GlobalMasterVolume);
    }

    public static void RegisterSfxListener(SfxVolumeListener listener)
    {
        if (listener == null || !sfxListeners.Add(listener))
        {
            return;
        }

        if (Instance != null)
        {
            Instance.ApplyVolumeToListener(listener);
        }
    }

    public static void UnregisterSfxListener(SfxVolumeListener listener)
    {
        if (listener == null)
        {
            return;
        }

        sfxListeners.Remove(listener);
    }

    public static void NotifySfxSourceReady(AudioSource source, float baseVolume)
    {
        if (source == null)
        {
            return;
        }

        RegisterSfxBaseVolume(source, baseVolume);

        if (Instance != null)
        {
            Instance.ApplySfxVolumeToSource(source);
        }
    }

    public static void RegisterSfxBaseVolume(AudioSource source, float baseVolume)
    {
        if (source == null)
        {
            return;
        }

        sfxBaseVolumes[source.GetInstanceID()] = Mathf.Clamp01(baseVolume);
    }

    public float GetEffectiveSfxVolume(float localVolume = 1f)
    {
        return Mathf.Clamp01(localVolume * sfxVolume * masterVolume);
    }



    private float sfxScanAccumulator;
    private const float SfxScanInterval = 0.25f; // 런타임 생성 AudioSource 탐색 주기 //안건준 추가 - 0628
    private bool volumePreferencesLoaded; // PlayerPrefs 볼륨 로드 완료 여부 //안건준 추가 - 0629

    public static void EnsureVolumePreferencesLoaded() // 설정 UI·SFX 재생 전 볼륨 선로드 //안건준 추가 - 0629
    {
        AudioManager manager = EnsureExists();
        manager?.EnsureVolumePreferencesLoadedInternal();
    }

    protected override void Awake()
    {
        AudioManager survivor = _instance as AudioManager;
        base.Awake();

        if (!IsActiveSingleton)
        {
            survivor?.AbsorbConfiguration(this); // 씬 AudioManager 설정을 DDOL 인스턴스로 이전 //안건준 수정 - 0628
            return;
        }

        EnsureRuntimeReady();
        EnsureVolumePreferencesLoadedInternal(); // Start() 전에도 볼륨 적용 //안건준 추가 - 0629
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }

    internal void AbsorbConfiguration(AudioManager donor)
    {
        if (donor == null)
        {
            return;
        }

        bool bgmMerged = MergeClipArrayIfNeeded(
            ref bgmClips,
            donor.bgmClips,
            HasAssignedBgmClips(),
            donor.HasAssignedBgmClips());

        bool sfxMerged = MergeClipArrayIfNeeded(
            ref sfxClips,
            donor.sfxClips,
            HasAssignedSfxClips(),
            donor.HasAssignedSfxClips());

        if (bgmMerged || sfxMerged || NeedsDictionaryRebuild())
        {
            InitializDictionary(); // 흡수 후 딕셔너리 강제 재구성 //안건준 수정 - 0629
        }

        EnsureRuntimeReady();
    }

    private void TryRecoverClipConfiguration() // DDOL 인스턴스에 클립이 없을 때 씬 AudioManager에서 복구 //안건준 추가 - 0629
    {
        if (HasAssignedSfxClips() && CanPlaySfx(SFXType.ClickButton))
        {
            return; // 이미 클릭음 재생 가능
        }

        AudioManager[] managers = FindObjectsByType<AudioManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AudioManager bestDonor = null;
        int bestSfxCount = CountAssignedClips(sfxClips);

        for (int i = 0; i < managers.Length; i++)
        {
            AudioManager candidate = managers[i];
            if (candidate == null || candidate == this)
            {
                continue;
            }

            int candidateCount = CountAssignedClips(candidate.sfxClips);
            if (candidateCount > bestSfxCount)
            {
                bestDonor = candidate;
                bestSfxCount = candidateCount;
            }
        }

        if (bestDonor != null)
        {
            AbsorbConfiguration(bestDonor);
        }
    }

    private static bool MergeClipArrayIfNeeded(
        ref BGMClipData[] survivorClips,
        BGMClipData[] donorClips,
        bool survivorHasClips,
        bool donorHasClips)
    {
        if (!donorHasClips)
        {
            return false;
        }

        if (!survivorHasClips || CountAssignedClips(survivorClips) < CountAssignedClips(donorClips))
        {
            survivorClips = donorClips;
            return true;
        }

        return false;
    }

    private static bool MergeClipArrayIfNeeded(
        ref SFXClipData[] survivorClips,
        SFXClipData[] donorClips,
        bool survivorHasClips,
        bool donorHasClips)
    {
        if (!donorHasClips)
        {
            return false;
        }

        if (!survivorHasClips || CountAssignedClips(survivorClips) < CountAssignedClips(donorClips))
        {
            survivorClips = donorClips;
            return true;
        }

        return false;
    }

    private bool NeedsDictionaryRebuild() // Inspector 클립은 있는데 Dictionary만 비어 있는 경우 //안건준 추가 - 0629
    {
        if (bgmDictionary == null || sfxDictionary == null)
        {
            return true;
        }

        if (HasAssignedBgmClips() && bgmDictionary.Count == 0)
        {
            return true;
        }

        if (HasAssignedSfxClips() && !sfxDictionary.ContainsKey(SFXType.ClickButton))
        {
            return true;
        }

        return false;
    }

    private bool CanPlaySfx(SFXType type)
    {
        EnsureRuntimeReady();
        return sfxSource != null && sfxDictionary != null && sfxDictionary.ContainsKey(type);
    }

    public bool TryGetSfxClip(SFXType type, out AudioClip clip, out float localVolume) // SFX 클립 조회 (Dictionary + 배열 fallback) //안건준 추가 - 0629
    {
        clip = null;
        localVolume = 1f;
        EnsureRuntimeReady();
        TryRecoverClipConfiguration();

        if (sfxDictionary != null && sfxDictionary.TryGetValue(type, out SFXClipData clipData) && clipData.clip != null)
        {
            clip = clipData.clip;
            localVolume = clipData.volume;
            return true;
        }

        if (sfxClips == null)
        {
            return false;
        }

        for (int i = 0; i < sfxClips.Length; i++)
        {
            SFXClipData entry = sfxClips[i];
            if (entry == null || entry.type != type || entry.clip == null)
            {
                continue;
            }

            clip = entry.clip;
            localVolume = entry.volume;
            InitializDictionary();
            return true;
        }

        return false;
    }

    public void PlaySfxOneShotDirect(AudioClip clip, float localVolume = 1f) // Dictionary 없이 클립 직접 재생 //안건준 추가 - 0629
    {
        if (clip == null)
        {
            return;
        }

        EnsureRuntimeReady();
        PrepareSfxSourceForUi();
        if (sfxSource == null)
        {
            Debug.LogWarning("[AudioManager] SFX AudioSource가 없습니다.");
            return;
        }

        sfxSource.PlayOneShot(clip, GetEffectiveSfxVolume(localVolume));
    }

    private void PrepareSfxSourceForUi() // UI 효과음용 SFX Source 상태 보정 //안건준 추가 - 0629
    {
        EnsureAudioSources();
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.enabled = true;
        sfxSource.mute = false;
        sfxSource.ignoreListenerPause = true;
        sfxSource.spatialBlend = 0f;
    }

    private void EnsureRuntimeReady()
    {
        EnsureAudioSources();
        EnsureDictionaries();
    }

    private bool HasAssignedBgmClips()
    {
        return CountAssignedClips(bgmClips) > 0;
    }

    private bool HasAssignedSfxClips()
    {
        return CountAssignedClips(sfxClips) > 0;
    }

    private static int CountAssignedClips(BGMClipData[] clips)
    {
        if (clips == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].clip != null)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountAssignedClips(SFXClipData[] clips)
    {
        if (clips == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null && clips[i].clip != null)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureDictionaries()
    {
        bool needsRebuild = bgmDictionary == null
            || sfxDictionary == null
            || (HasAssignedBgmClips() && bgmDictionary.Count == 0)
            || (HasAssignedSfxClips() && sfxDictionary.Count == 0);

        if (needsRebuild)
        {
            InitializDictionary();
        }
    }

    private void EnsureAudioSources()
    {
        if (!IsValidAudioSource(bgmSource))
        {
            bgmSource = FindChildAudioSource("BGM Source");
            if (!IsValidAudioSource(bgmSource))
            {
                bgmSource = CreateChildAudioSource("BGM Source", loop: true);
            }
        }

        if (!IsValidAudioSource(sfxSource))
        {
            sfxSource = FindChildAudioSource("SFX Source");
            if (!IsValidAudioSource(sfxSource))
            {
                sfxSource = CreateChildAudioSource("SFX Source", loop: false);
            }
        }
    }

    private static bool IsValidAudioSource(AudioSource source)
    {
        return source != null;
    }

    private AudioSource FindChildAudioSource(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<AudioSource>() : null;
    }

    private AudioSource CreateChildAudioSource(string childName, bool loop)
    {
        GameObject sourceObject = new GameObject(childName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true; // 게임 오버/일시정지 중 UI 효과음 재생 //안건준 추가 - 0629
        return source;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        EnsureRuntimeReady();
        EnsureVolumePreferencesLoadedInternal();
        PlayBGMForActiveScene();
        BindSceneSfxSources(SceneManager.GetActiveScene());
    }

    private void Update()
    {
        sfxScanAccumulator += Time.unscaledDeltaTime;
        if (sfxScanAccumulator < SfxScanInterval)
        {
            return;
        }

        sfxScanAccumulator = 0f;
        ScanUnboundSfxSources();
    }

    private void EnsureVolumePreferencesLoadedInternal() // PlayerPrefs → 전역·인스턴스 볼륨 (1회) //안건준 추가 - 0629
    {
        if (volumePreferencesLoaded)
        {
            return;
        }

        LoadVolumePreferences();
        volumePreferencesLoaded = true;
    }

    private void LoadVolumePreferences()
    {
        GlobalMasterVolume = ReadOrInitializeVolumePref(MasterVolumePrefKey, DefaultVolume);
        SetMasterVolume(GlobalMasterVolume);

        GlobalBgmVolume = ReadOrInitializeVolumePref(BgmVolumePrefKey, DefaultVolume);
        SetBGMVolume(GlobalBgmVolume);

        GlobalSfxVolume = ReadOrInitializeVolumePref(SfxVolumePrefKey, DefaultVolume);
        SetSFXVolume(GlobalSfxVolume);

        if (masterVolume <= 0.0001f || sfxVolume <= 0.0001f)
        {
            Debug.LogWarning(
                $"[AudioManager] 효과음/BGM이 꺼져 있습니다. 설정 슬라이더 확인 (Master={masterVolume:F2}, SFX={sfxVolume:F2})"); //안건준 추가 - 0629
        }
    }

    private static float ReadOrInitializeVolumePref(string key, float defaultValue) // 키 없으면 100% 저장 후 반환 //안건준 추가 - 0629
    {
        if (!PlayerPrefs.HasKey(key))
        {
            float initial = Mathf.Clamp01(defaultValue);
            PlayerPrefs.SetFloat(key, initial);
            PlayerPrefs.Save();
            return initial;
        }

        return Mathf.Clamp01(PlayerPrefs.GetFloat(key));
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryRecoverClipConfiguration(); // 타이틀/스테이지 재진입 시 클립 복구 //안건준 수정 - 0629
        EnsureRuntimeReady();
        PlayBGMForScene(scene.name);
        BindSceneSfxSources(scene);
    }

    private void BindSceneSfxSources(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        ScanUnboundSfxSources();
    }

    private void ScanUnboundSfxSources()
    {
        RefreshAllSfxSources();
    }

    private void RefreshAllSfxSources()
    {
        EnsureRuntimeReady();

        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null || ShouldSkipSfxVolumeApply(source))
            {
                continue;
            }

            EnsureSfxListener(source);
            ApplySfxVolumeToSource(source);
        }

        ApplySfxVolumeToListeners();
    }

    private void EnsureSfxListener(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        SfxVolumeListener listener = source.GetComponent<SfxVolumeListener>();
        if (listener != null)
        {
            return;
        }

        float baseVolume = GetOrCaptureBaseVolume(source);
        listener = source.gameObject.AddComponent<SfxVolumeListener>();
        listener.SetBaseVolume(baseVolume);
    }

    private void ApplySfxVolumeToSource(AudioSource source)
    {
        if (source == null || ShouldSkipSfxVolumeApply(source))
        {
            return;
        }

        float baseVolume = GetOrCaptureBaseVolume(source);
        source.volume = Mathf.Clamp01(baseVolume * sfxVolume * masterVolume);
    }

    private float GetOrCaptureBaseVolume(AudioSource source)
    {
        int id = source.GetInstanceID();
        if (sfxBaseVolumes.TryGetValue(id, out float storedBaseVolume))
        {
            return storedBaseVolume;
        }

        SfxVolumeListener listener = source.GetComponent<SfxVolumeListener>();
        float baseVolume = listener != null
            ? listener.BaseVolume
            : ReverseCalculateBaseVolume(source.volume);

        RegisterSfxBaseVolume(source, baseVolume);
        return baseVolume;
    }

    private float ReverseCalculateBaseVolume(float currentVolume)
    {
        float scale = Mathf.Max(sfxVolume * masterVolume, 0.0001f);
        return Mathf.Clamp01(currentVolume / scale);
    }

    private bool ShouldSkipSfxVolumeApply(AudioSource source)
    {
        return IsBgmSource(source) || source == sfxSource;
    }

    private bool IsBgmSource(AudioSource source)
    {
        if (source == null)
        {
            return false;
        }

        if (source == bgmSource)
        {
            return true;
        }

        return source.gameObject.name == "BGM Source";
    }

    private void ApplySfxVolumeToListeners()
    {
        foreach (SfxVolumeListener listener in sfxListeners)
        {
            if (listener != null)
            {
                ApplyVolumeToListener(listener);
            }
        }
    }

    private void ApplyVolumeToListener(SfxVolumeListener listener)
    {
        listener.ApplyVolume(sfxVolume, masterVolume);
    }

    private void PlayBGMForActiveScene()
    {
        PlayBGMForScene(SceneManager.GetActiveScene().name);
    }

    private void PlayBGMForScene(string sceneName)
    {
        BGMType bgmType = AudioSceneName.GetBGMType(sceneName);
        if (bgmType == BGMType.None)
        {
            return;
        }

        PlayBGM(bgmType);
    }

    //배열로 등록한 오디오 데이터를 딕셔너리에 저장
    private void InitializDictionary()
    {
        bgmDictionary = new Dictionary<BGMType, BGMClipData>();
        sfxDictionary = new Dictionary<SFXType, SFXClipData>();

        if (bgmClips != null)
        {
            for (int i = 0; i < bgmClips.Length; i++)
            {
                if (bgmClips[i] == null || bgmClips[i].clip == null)
                {
                    continue;
                }

                if (!bgmDictionary.ContainsKey(bgmClips[i].type))
                {
                    bgmDictionary.Add(bgmClips[i].type, bgmClips[i]);
                }
            }
        }

        if (sfxClips != null)
        {
            for (int i = 0; i < sfxClips.Length; i++)
            {
                if (sfxClips[i] == null || sfxClips[i].clip == null)
                {
                    continue;
                }

                if (!sfxDictionary.ContainsKey(sfxClips[i].type))
                {
                    sfxDictionary.Add(sfxClips[i].type, sfxClips[i]);
                }
            }
        }
    }
    //BGM 재생
    public void PlayBGM(BGMType type)
    {
        EnsureRuntimeReady();
        if (bgmSource == null || bgmDictionary == null || !bgmDictionary.ContainsKey(type))
        {
            return;
        }
        
        //딕셔너리에서 해당 BGM타입의 클립데이터 가져오기
        BGMClipData clipData = bgmDictionary[type];
        //현재 재생중인 BGM과 요청한 BGM이 같으면 건너뛰기
        if(bgmSource.clip == clipData.clip)
        {
            return;
        }
        //현재 재생중인 BGM데이터를 저장
        currentBGMClip = clipData;
        //BGM AudioSource에 클립 할당
        bgmSource.clip = clipData.clip;

        bgmSource.volume = clipData.volume * bgmVolume * masterVolume;

        //BGM재생
        bgmSource.Play();

    }
    //BGM 정지
    public void StopBGM()
    {
        //현재 재생중인 BGM정지
        bgmSource.Stop();
        //오디오 소스에 연결된 오디오 클립을 제거
        bgmSource.clip = null;
        //현재 재생중인 BGM데이터 초기화
        currentBGMClip = null;
    }
    //일시정지
    public void PauseBGM()
    {
        bgmSource.Pause();
    }
    //일시정지된 BGM 다시 재생
    public void ResumeBGM()
    {
        bgmSource.UnPause();
    }

    //효과음 재생
    public void PlaySFX(SFXType type)
    {
        EnsureRuntimeReady();
        if (sfxSource == null)
        {
            return;
        }

        if (sfxDictionary == null || !sfxDictionary.ContainsKey(type))
        {
            if (HasAssignedSfxClips())
            {
                InitializDictionary(); // 클립은 있는데 Dictionary만 비어 있을 때 재구성 //안건준 수정 - 0629
            }

            if (sfxDictionary == null || !sfxDictionary.ContainsKey(type))
            {
                TryRecoverClipConfiguration(); // StageScene 등에서 빈 DDOL AudioManager가 생긴 경우 복구 //안건준 수정 - 0629
            }
        }

        if (sfxDictionary == null || !sfxDictionary.ContainsKey(type))
        {
            return;
        }

        sfxSource.ignoreListenerPause = true; // UI 클릭음은 Listener Pause 영향 받지 않게 //안건준 추가 - 0629
        PrepareSfxSourceForUi();
        SFXClipData clipData = sfxDictionary[type];
        float volume = GetEffectiveSfxVolume(clipData.volume);
        sfxSource.PlayOneShot(clipData.clip, volume);
        UpdateBGMVolume();
    }

    // UI 버튼 등 Inspector 클립 직접 재생 — 마스터·효과음 볼륨 반영 //안건준 추가 - 0628
    public void PlayUIClickSfx(AudioClip clip, float localVolume = 1f)
    {
        PlaySfxOneShotDirect(clip, localVolume);
    }

    public static void PlayUiSfxClip(AudioClip clip, float localVolume = 1f)
    {
        AudioManager manager = EnsureExists();
        if (manager == null)
        {
            return;
        }

        manager.PlayUIClickSfx(clip, localVolume);
    }

    //BGM볼륨을 변경
    public void SetBGMVolume(float volume)
    {
        bgmVolume = GlobalBgmVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();

    }
    //효과음볼륨을 변경
    public void SetSFXVolume(float volume)
    {
        sfxVolume = GlobalSfxVolume = Mathf.Clamp01(volume);
        RefreshAllSfxSources();
    }

    //전체 볼륨을 변경 — BGM·효과음 전부 //안건준 수정 - 0628
    public void SetMasterVolume(float volume)
    {
        masterVolume = GlobalMasterVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
        RefreshAllSfxSources();
    }
    //현재 재생중인 BGM의 볼륨을 계산
    private void UpdateBGMVolume()
    {
        if(bgmSource == null) return;
        //현재 재생중인 BGM데이터가 없다면
        if(currentBGMClip == null)
        {
            bgmSource.volume = bgmVolume * masterVolume;
            return;
        }

        bgmSource.volume = currentBGMClip.volume * bgmVolume * masterVolume;


    }
    

}
