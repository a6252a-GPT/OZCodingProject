// 안건준 추가 - 0624
// 카드 등장 / 선택 사운드 재생 매니저
// CardUI 가 PlayCardAppear() / PlayCardSelect() 를 호출
// CardSoundSelector 가 PreviewClip() 으로 미리듣기 + 클립 할당

using UnityEngine;

public class CardSoundManager : MonoBehaviour
{
    [Header("카드 사운드 클립")]
    [Tooltip("카드 등장 시 재생되는 사운드")]
    [SerializeField] private AudioClip cardAppearClip;

    [Tooltip("카드 선택(클릭) 시 재생되는 사운드")]
    [SerializeField] private AudioClip cardSelectClip;

    [Header("볼륨")]
    [Range(0f, 1f)] public float appearVolume = 1f;
    [Range(0f, 1f)] public float selectVolume = 1f;
    [Range(0f, 1f)] public float previewVolume = 1f;

    // 외부에서 현재 할당 클립 조회/변경
    public AudioClip CardAppearClip
    {
        get => cardAppearClip;
        set => cardAppearClip = value;
    }

    public AudioClip CardSelectClip
    {
        get => cardSelectClip;
        set => cardSelectClip = value;
    }

    // Awake/Start 타이밍 이전에도 안전하게 접근하기 위해 프로퍼티로 lazy 초기화
    private AudioSource _src;
    private AudioSource Src
    {
        get
        {
            if (_src != null) return _src;
            _src = GetComponent<AudioSource>();
            if (_src == null) _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake  = false;
            _src.spatialBlend = 0f;
            return _src;
        }
    }

    private void Awake()
    {
        _ = Src; // 미리 초기화
    }

    // ─── 공개 API ─────────────────────────────

    /// <summary>카드가 화면에 등장할 때 호출 (CardUI.PlaySpawnOpenTween)</summary>
    public void PlayCardAppear()
    {
        if (cardAppearClip == null) return;
        Src.PlayOneShot(cardAppearClip, appearVolume);
    }

    /// <summary>카드를 선택(클릭)했을 때 호출 (CardUI.HandleCardClicked)</summary>
    public void PlayCardSelect()
    {
        if (cardSelectClip == null) return;
        Src.PlayOneShot(cardSelectClip, selectVolume);
    }

    /// <summary>CardSoundSelector 에서 미리듣기 용으로 임의 클립 재생</summary>
    public void PreviewClip(AudioClip clip)
    {
        if (clip == null) return;
        Src.Stop();
        Src.clip   = clip;
        Src.volume = previewVolume;
        Src.Play();
    }
}
