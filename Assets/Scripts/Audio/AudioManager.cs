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

    private Dictionary<BGMType, BGMClipData> bgmDictionary;
    private Dictionary<SFXType, SFXClipData> sfxDictionary;

    private BGMClipData currentBGMClip;
    private float masterVolume = 1f;
    private float bgmVolume = 1f;
    private float sfxVolume = 1f;



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
        PlayBGMForActiveScene();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayBGMForScene(scene.name);
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
        
    }

    //전체 볼륨을 변경
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
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
