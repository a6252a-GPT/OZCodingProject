using UnityEngine;

/// <summary>
/// TitleScene AudioManager와 동일한 BGM/SFX 목록 — 테스트 씬 직접 실행 시 Resources에서 로드.
/// </summary>
[CreateAssetMenu(fileName = "AudioManagerCatalog", menuName = "Audio/Audio Manager Catalog")]
public class AudioManagerCatalog : ScriptableObject
{
    public BGMClipData[] bgmClips;
    public SFXClipData[] sfxClips;
}
