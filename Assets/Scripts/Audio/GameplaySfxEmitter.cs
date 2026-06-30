using System;
using System.Collections.Generic;
using UnityEngine;

namespace TeamProject01.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class GameplaySfxEmitter : MonoBehaviour
    {
        private const float MaxLocalVolume = 2f;

        public sealed class LoopHandle
        {
            private readonly List<GameObject> loopObjects = new List<GameObject>(2);

            public bool IsPlaying => loopObjects.Count > 0;

            internal void Add(GameObject loopObject)
            {
                if (loopObject != null)
                {
                    loopObjects.Add(loopObject);
                }
            }

            public void Stop()
            {
                for (int i = 0; i < loopObjects.Count; i++)
                {
                    GameObject loopObject = loopObjects[i];
                    if (loopObject == null)
                    {
                        continue;
                    }

                    if (Application.isPlaying)
                    {
                        Destroy(loopObject);
                    }
                    else
                    {
                        DestroyImmediate(loopObject);
                    }
                }

                loopObjects.Clear();
            }
        }

        [SerializeField] private GameplaySfxCue cue = GameplaySfxCue.None;
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip[] clips = Array.Empty<AudioClip>();
        [SerializeField, Range(0f, MaxLocalVolume)] private float volume = 1f;
        [SerializeField, Range(0.1f, 3f)] private float minPitch = 1f;
        [SerializeField, Range(0.1f, 3f)] private float maxPitch = 1f;
        [SerializeField, Min(0f)] private float cooldown = 0.03f;
        [SerializeField] private bool forceDetachedPlayback;
        [SerializeField, Min(0.1f)] private float oneShotLifetimePadding = 0.25f;

        private float lastPlayTime = -999f;

        public GameplaySfxCue Cue => cue;
        public AudioClip[] Clips => clips;
        public float Volume => volume;

        public void Configure(
            GameplaySfxCue newCue,
            AudioClip[] newClips,
            float newVolume,
            float newCooldown,
            bool detachedPlayback)
        {
            cue = newCue;
            clips = newClips ?? Array.Empty<AudioClip>();
            volume = Mathf.Clamp(newVolume, 0f, MaxLocalVolume);
            cooldown = Mathf.Max(0f, newCooldown);
            forceDetachedPlayback = detachedPlayback;
            EnsureSource();
        }

        public bool Play()
        {
            return PlayAt(transform.position, false);
        }

        public bool PlayAt(Vector3 position, bool detached)
        {
            AudioClip clip = PickClip();
            if (clip == null || !CanPlayNow())
            {
                return false;
            }

            lastPlayTime = GetTime();

            if (detached || forceDetachedPlayback || !gameObject.activeInHierarchy)
            {
                PlayDetachedClip(clip, position);
                return true;
            }

            AudioSource playbackSource = EnsureSource();
            if (playbackSource == null)
            {
                return false;
            }

            playbackSource.pitch = PickPitch();
            playbackSource.volume = ResolveEffectiveSourceVolume(volume);
            playbackSource.PlayOneShot(clip, ResolveOneShotVolumeScale(volume));
            return true;
        }

        public bool PlayDetachedAt(Vector3 position)
        {
            return PlayAt(position, true);
        }

        public bool StartLoop()
        {
            AudioClip clip = PickClip();
            if (clip == null)
            {
                return false;
            }

            AudioSource playbackSource = EnsureSource();
            if (playbackSource == null)
            {
                return false;
            }

            playbackSource.clip = clip;
            playbackSource.loop = true;
            playbackSource.pitch = PickPitch();
            playbackSource.volume = ResolveEffectiveSourceVolume(volume);
            if (!playbackSource.isPlaying)
            {
                playbackSource.Play();
            }

            return true;
        }

        public void StopLoop()
        {
            AudioSource playbackSource = EnsureSource();
            if (playbackSource == null)
            {
                return;
            }

            if (playbackSource.isPlaying)
            {
                playbackSource.Stop();
            }

            playbackSource.loop = false;
            playbackSource.clip = null;
        }

        public static bool TryPlay(Transform root, GameplaySfxCue cue)
        {
            GameplaySfxEmitter emitter = FindEmitter(root, cue);
            return emitter != null && emitter.Play();
        }

        public static bool TryPlayAt(Transform root, GameplaySfxCue cue, Vector3 position, bool detached)
        {
            GameplaySfxEmitter emitter = FindEmitter(root, cue);
            return emitter != null && emitter.PlayAt(position, detached);
        }

        public static bool TryStartLoop(Transform root, GameplaySfxCue cue)
        {
            GameplaySfxEmitter emitter = FindEmitter(root, cue);
            return emitter != null && emitter.StartLoop();
        }

        public static bool TryStopLoop(Transform root, GameplaySfxCue cue)
        {
            GameplaySfxEmitter emitter = FindEmitter(root, cue);
            if (emitter == null)
            {
                return false;
            }

            emitter.StopLoop();
            return true;
        }

        public static bool TryPlayCatalogAt(GameplaySfxCue cue, Vector3 position)
        {
            GameplaySfxCatalog catalog = Resources.Load<GameplaySfxCatalog>(GameplaySfxCatalog.ResourcePath);
            if (catalog == null || !catalog.TryGetEntry(cue, out GameplaySfxCatalogEntry entry))
            {
                return false;
            }

            AudioClip clip = PickClip(entry.Clips);
            if (clip == null)
            {
                return false;
            }

            PlayDetachedClip(
                clip,
                position,
                entry.Volume,
                Mathf.Min(entry.MinPitch, entry.MaxPitch),
                Mathf.Max(entry.MinPitch, entry.MaxPitch),
                entry.Spatial,
                entry.MinDistance,
                entry.MaxDistance,
                0.25f);
            return true;
        }

        public static LoopHandle StartCatalogLoop(GameplaySfxCue cue, Vector3 position, Transform parent = null)
        {
            GameplaySfxCatalog catalog = Resources.Load<GameplaySfxCatalog>(GameplaySfxCatalog.ResourcePath);
            if (catalog == null || !catalog.TryGetEntry(cue, out GameplaySfxCatalogEntry entry))
            {
                return null;
            }

            AudioClip clip = PickClip(entry.Clips);
            if (clip == null)
            {
                return null;
            }

            LoopHandle handle = new LoopHandle();
            GameObject loopObject = new GameObject("SFX_Loop_" + clip.name);
            loopObject.transform.position = position;
            if (parent != null)
            {
                loopObject.transform.SetParent(parent, true);
            }

            AudioSource playbackSource = loopObject.AddComponent<AudioSource>();
            playbackSource.clip = clip;
            playbackSource.playOnAwake = false;
            playbackSource.loop = true;
            playbackSource.dopplerLevel = 0f;
            playbackSource.ignoreListenerPause = true;
            playbackSource.spatialBlend = entry.Spatial ? 1f : 0f;
            playbackSource.rolloffMode = AudioRolloffMode.Logarithmic;
            playbackSource.minDistance = Mathf.Max(0.1f, entry.MinDistance);
            playbackSource.maxDistance = Mathf.Max(playbackSource.minDistance + 0.1f, entry.MaxDistance);
            playbackSource.pitch = Mathf.Approximately(entry.MinPitch, entry.MaxPitch)
                ? entry.MinPitch
                : UnityEngine.Random.Range(Mathf.Min(entry.MinPitch, entry.MaxPitch), Mathf.Max(entry.MinPitch, entry.MaxPitch));
            playbackSource.volume = ResolveEffectiveSourceVolume(entry.Volume);

            SfxVolumeListener listener = loopObject.AddComponent<SfxVolumeListener>();
            listener.SetBaseVolume(entry.Volume);

            playbackSource.Play();
            handle.Add(loopObject);
            return handle;
        }

        public static GameplaySfxEmitter FindEmitter(Transform root, GameplaySfxCue cue)
        {
            if (root == null || cue == GameplaySfxCue.None)
            {
                return null;
            }

            GameplaySfxEmitter[] emitters = root.GetComponentsInChildren<GameplaySfxEmitter>(true);
            for (int i = 0; i < emitters.Length; i++)
            {
                GameplaySfxEmitter emitter = emitters[i];
                if (emitter != null && emitter.cue == cue && emitter.HasUsableClip())
                {
                    return emitter;
                }
            }

            return null;
        }

        private bool HasUsableClip()
        {
            return PickClip() != null;
        }

        private AudioSource EnsureSource()
        {
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.dopplerLevel = 0f;
            source.ignoreListenerPause = true;

            SfxVolumeListener listener = source.GetComponent<SfxVolumeListener>();
            if (listener == null)
            {
                listener = source.gameObject.AddComponent<SfxVolumeListener>();
            }

            listener.SetBaseVolume(volume);
            return source;
        }

        private bool CanPlayNow()
        {
            return cooldown <= 0f || GetTime() >= lastPlayTime + cooldown;
        }

        private AudioClip PickClip()
        {
            return PickClip(clips);
        }

        private static AudioClip PickClip(AudioClip[] candidates)
        {
            if (candidates == null || candidates.Length == 0)
            {
                return null;
            }

            int start = UnityEngine.Random.Range(0, candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                AudioClip clip = candidates[(start + i) % candidates.Length];
                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private float PickPitch()
        {
            float low = Mathf.Min(minPitch, maxPitch);
            float high = Mathf.Max(minPitch, maxPitch);
            return Mathf.Approximately(low, high) ? low : UnityEngine.Random.Range(low, high);
        }

        private void PlayDetachedClip(AudioClip clip, Vector3 position)
        {
            AudioSource template = EnsureSource();
            PlayDetachedClip(
                clip,
                position,
                volume,
                minPitch,
                maxPitch,
                template == null || template.spatialBlend > 0.5f,
                template != null ? template.minDistance : 2f,
                template != null ? template.maxDistance : 45f,
                oneShotLifetimePadding,
                template);
        }

        private static void PlayDetachedClip(
            AudioClip clip,
            Vector3 position,
            float localVolume,
            float minPitch,
            float maxPitch,
            bool spatial,
            float minDistance,
            float maxDistance,
            float lifetimePadding,
            AudioSource template = null)
        {
            if (clip == null)
            {
                return;
            }

            GameObject oneShot = new GameObject("SFX_OneShot_" + clip.name);
            oneShot.transform.position = position;

            AudioSource playbackSource = oneShot.AddComponent<AudioSource>();
            CopySourceSettings(template, playbackSource);
            playbackSource.playOnAwake = false;
            playbackSource.loop = false;
            playbackSource.dopplerLevel = 0f;
            playbackSource.ignoreListenerPause = true;
            playbackSource.spatialBlend = spatial ? 1f : 0f;
            playbackSource.minDistance = Mathf.Max(0.1f, minDistance);
            playbackSource.maxDistance = Mathf.Max(playbackSource.minDistance + 0.1f, maxDistance);
            playbackSource.pitch = Mathf.Approximately(minPitch, maxPitch)
                ? minPitch
                : UnityEngine.Random.Range(Mathf.Min(minPitch, maxPitch), Mathf.Max(minPitch, maxPitch));
            playbackSource.volume = ResolveEffectiveSourceVolume(localVolume);

            SfxVolumeListener listener = oneShot.AddComponent<SfxVolumeListener>();
            listener.SetBaseVolume(localVolume);

            playbackSource.PlayOneShot(clip, ResolveOneShotVolumeScale(localVolume));
            Destroy(oneShot, Mathf.Max(0.1f, clip.length / Mathf.Max(0.1f, playbackSource.pitch)) + Mathf.Max(0f, lifetimePadding));
        }

        private static void CopySourceSettings(AudioSource from, AudioSource to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.outputAudioMixerGroup = from.outputAudioMixerGroup;
            to.rolloffMode = from.rolloffMode;
            to.minDistance = from.minDistance;
            to.maxDistance = from.maxDistance;
            to.spatialBlend = from.spatialBlend;
            to.priority = from.priority;
        }

        private static float ResolveEffectiveSourceVolume(float localVolume)
        {
            float safeVolume = Mathf.Clamp01(localVolume);
            if (!Application.isPlaying)
            {
                return safeVolume;
            }

            AudioManager manager = AudioManager.EnsureExists();
            return manager != null ? manager.GetEffectiveSfxVolume(safeVolume) : safeVolume;
        }

        private static float ResolveOneShotVolumeScale(float localVolume)
        {
            float safeVolume = Mathf.Clamp(localVolume, 0f, MaxLocalVolume);
            return safeVolume > 1f ? safeVolume : 1f;
        }

        private static float GetTime()
        {
            return Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            volume = Mathf.Clamp(volume, 0f, MaxLocalVolume);
            cooldown = Mathf.Max(0f, cooldown);
            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }
        }
#endif
    }
}
