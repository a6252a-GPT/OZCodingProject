using UnityEngine;

[DisallowMultipleComponent]
public sealed class SfxVolumeListener : MonoBehaviour // 씬 SFX AudioSource — 설정 SFX 볼륨 연동 //안건준 추가 - 0628
{
    [SerializeField] private AudioSource target;
    [SerializeField] private float baseVolume = 1f;
    [SerializeField] private bool captureBaseVolumeOnAwake = true;

    private void Awake()
    {
        if (target == null)
        {
            target = GetComponent<AudioSource>();
        }

        if (target != null && captureBaseVolumeOnAwake)
        {
            baseVolume = target.volume;
        }
    }

    private void OnEnable()
    {
        AudioManager.RegisterSfxListener(this);
    }

    private void OnDisable()
    {
        AudioManager.UnregisterSfxListener(this);
    }

    public void ApplyVolume(float sfxVolume, float masterVolume)
    {
        if (target == null)
        {
            return;
        }

        target.volume = Mathf.Clamp01(baseVolume * sfxVolume * masterVolume);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (target == null)
        {
            target = GetComponent<AudioSource>();
        }
    }
#endif
}
