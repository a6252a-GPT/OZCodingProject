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

    private Dictionary<BGMType, BGMClipData> bgmDictionary;
    private Dictionary<SFXType, SFXClipData> sfxDictionary;

    private BGMClipData currentBGMClip;
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;

    public const string BgmVolumePrefKey = "Settings.BGMVolume";
    public const string SfxVolumePrefKey = "Settings.SFXVolume";

    public float BgmVolume => bgmVolume;
    public float SfxVolume => sfxVolume;

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

    public float GetEffectiveSfxVolume(float localVolume = 1f)
    {
        return Mathf.Clamp01(localVolume * sfxVolume * masterVolume);
    }



    private float sfxScanAccumulator;
    private const float SfxScanInterval = 0.25f; // 런타임 생성 AudioSource 탐색 주기 //안건준 추가 - 0628

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

        if (!HasAssignedBgmClips() && donor.HasAssignedBgmClips())
        {
            bgmClips = donor.bgmClips;
        }

        if (!HasAssignedSfxClips() && donor.HasAssignedSfxClips())
        {
            sfxClips = donor.sfxClips;
        }

        EnsureRuntimeReady();
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
        LoadVolumePreferences();
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

    private void LoadVolumePreferences()
    {
        if (PlayerPrefs.HasKey(BgmVolumePrefKey))
        {
            SetBGMVolume(PlayerPrefs.GetFloat(BgmVolumePrefKey));
        }

        if (PlayerPrefs.HasKey(SfxVolumePrefKey))
        {
            SetSFXVolume(PlayerPrefs.GetFloat(SfxVolumePrefKey));
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        AudioSource[] sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool addedAny = false;

        for (int i = 0; i < sources.Length; i++)
        {
            if (TryBindSfxSource(sources[i]))
            {
                addedAny = true;
            }
        }

        if (addedAny)
        {
            ApplySfxVolumeToListeners();
        }
    }

    private bool TryBindSfxSource(AudioSource source)
    {
        if (source == null || source == bgmSource || source == sfxSource)
        {
            return false; // BGM 전용 소스·AudioManager PlaySFX 소스만 제외 //안건준 수정 - 0628
        }

        if (source.GetComponent<SfxVolumeListener>() != null)
        {
            return false;
        }

        source.gameObject.AddComponent<SfxVolumeListener>();
        return true;
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
        if (sfxSource == null || sfxDictionary == null || !sfxDictionary.ContainsKey(type))
        {
            return;
        }
        SFXClipData clipData = sfxDictionary[type];
        float volume = clipData.volume * sfxVolume * masterVolume;
        sfxSource.PlayOneShot(clipData.clip, volume);
        UpdateBGMVolume();
    }

    //BGM볼륨을 변경
    public void SetBGMVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();

    }
    //효과음볼륨을 변경
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ScanUnboundSfxSources();
        ApplySfxVolumeToListeners();
    }

    //전체 볼륨을 변경
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateBGMVolume();
        ApplySfxVolumeToListeners();
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
