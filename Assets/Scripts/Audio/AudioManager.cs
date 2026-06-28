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
        base.Awake();
        CreateAudioSources();
        InitializDictionary();
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
        LoadVolumePreferences();
        PlayBGMForActiveScene();
        BindSceneSfxSources(SceneManager.GetActiveScene()); // 첫 씬 SFX 볼륨 연동 //안건준 추가 - 0628
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
        PlayBGMForScene(scene.name);
        BindSceneSfxSources(scene); // 씬 전환 시 SFX AudioSource 자동 연동 //안건준 추가 - 0628
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

    //오디오소스가 없을경우 자동으로 생성
    private void CreateAudioSources()
    {
        if(bgmSource == null)
        {
            //BGM Source 생성
            GameObject bgmObject = new GameObject("BGM Source");
            bgmObject.transform.SetParent(transform);
            //생성한 오브젝트에 AudioSource 컴포넌트 추가
            bgmSource = bgmObject.AddComponent<AudioSource>();

            bgmSource.loop = true; //반복재생


        }
        if(sfxSource == null)
        {
            //SFX Source 생성
            GameObject sfxObject = new GameObject("SFX Source");
            sfxObject.transform.SetParent(transform);
            //생성한 오브젝트에 AudioSource 컴포넌트 추가
            sfxSource = sfxObject.AddComponent<AudioSource>();
            sfxSource.loop = false; //반복 재생 안함
        }
    }

    //배열로 등록한 오디오 데이터를 딕셔너리에 저장
    private void InitializDictionary()
    {
        bgmDictionary = new Dictionary<BGMType, BGMClipData>();
        sfxDictionary = new Dictionary<SFXType, SFXClipData>();
        
        if (bgmClips == null) return;

        for(int i = 0; i < bgmClips.Length; i++)
        {
            if(bgmClips[i] == null) continue;
            if(bgmClips[i].clip == null) continue;
            //딕셔너리에 같은 BGM타입이 없으면 추가
            if(!bgmDictionary.ContainsKey(bgmClips[i].type))
            {
                bgmDictionary.Add(bgmClips[i].type, bgmClips[i]);
            }
        }
        if (sfxClips == null) return;

        for(int i = 0; i < sfxClips.Length; i++)
        {
            if(sfxClips[i] == null) continue;
            if(sfxClips[i].clip == null) continue;
            //딕셔너리에 같은 효과음 타입이 없으면 추가
            if(!sfxDictionary.ContainsKey(sfxClips[i].type))
            {
                sfxDictionary.Add(sfxClips[i].type, sfxClips[i]);
            }
        }
    }
    //BGM 재생
    public void PlayBGM(BGMType type)
    {
        if(!bgmDictionary.ContainsKey(type))
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
        if(!sfxDictionary.ContainsKey(type))
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
