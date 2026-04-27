using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace EightAID.EIGHTAIDLib.Audio
{
    public abstract class SoundControllerBase : MonoBehaviour
    {
        public static float MasterVolume = 1f;
        public static float BGMVolume = 0.2f;
        public static float SeVolume = 0.2f;

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;
        public AudioSource onlySource;
        public AudioSource loopSource;

        [Header("Audio Clips")]
        public AudioClip[] bgmClips;
        public AudioClip[] sfxClips;

        protected virtual bool ShouldMute() => false;

        protected virtual void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            bgmSource = EnsureAudioSource(bgmSource, "BGM");
            sfxSource = EnsureAudioSource(sfxSource, "SFX");
            onlySource = EnsureAudioSource(onlySource, "Only");
            loopSource = EnsureAudioSource(loopSource, "Loop");
            SetVolume();
        }

        private AudioSource EnsureAudioSource(AudioSource source, string sourceName)
        {
            if (IsUsableAudioSource(source))
            {
                source.playOnAwake = false;
                return source;
            }

            var child = transform.Find(sourceName);
            var target = child != null ? child.gameObject : new GameObject(sourceName);
            if (child == null)
            {
                target.transform.SetParent(transform, false);
            }

            if (!target.TryGetComponent(out source))
            {
                source = target.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            return source;
        }

        private static bool IsUsableAudioSource(AudioSource source)
        {
            if (source == null)
            {
                return false;
            }

            try
            {
                _ = source.enabled;
                return true;
            }
            catch (MissingComponentException)
            {
                return false;
            }
        }

        protected static bool IsAlive(AudioSource source) => source != null;

        public void SetVolume()
        {
            if (IsAlive(bgmSource))
            {
                bgmSource.volume = GetEffectiveBgmVolume();
            }

            if (IsAlive(sfxSource))
            {
                sfxSource.volume = GetEffectiveSeVolume();
            }

            if (IsAlive(loopSource))
            {
                loopSource.volume = GetEffectiveBgmVolume();
            }
        }

        protected static float GetEffectiveBgmVolume() => Mathf.Clamp01(MasterVolume) * Mathf.Clamp01(BGMVolume);

        protected static float GetEffectiveSeVolume() => Mathf.Clamp01(MasterVolume) * Mathf.Clamp01(SeVolume);

        public void PlayBGM(int index, float duration = 1f)
        {
            if (ShouldMute() || bgmClips == null || index < 0 || index >= bgmClips.Length || !IsAlive(bgmSource))
            {
                return;
            }

            bgmSource.clip = bgmClips[index];
            bgmSource.loop = true;
            bgmSource.Play();
        }

        public void PlaySfx(int index)
        {
            if (ShouldMute() || sfxClips == null || index < 0 || index >= sfxClips.Length || !IsAlive(sfxSource))
            {
                return;
            }

            var clip = sfxClips[index];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }

        public void PlaySoundReference(AudioClip clip)
        {
            if (ShouldMute() || clip == null || !IsAlive(sfxSource))
            {
                return;
            }

            sfxSource.PlayOneShot(clip);
        }

        public void PlayOnlySFX(AudioClip[] clips, int dex = 99)
        {
            if (ShouldMute() || !IsAlive(onlySource) || clips == null || clips.Length == 0)
            {
                return;
            }

            int index = dex == 99 ? Random.Range(0, clips.Length) : dex;
            if (index < 0 || index >= clips.Length || clips[index] == null)
            {
                return;
            }

            onlySource.PlayOneShot(clips[index]);
        }

        public async UniTask PlayMusic(int index, AudioClip clip, CancellationToken token = default)
        {
            switch (index)
            {
                case 0:
                    if (!IsAlive(bgmSource) || clip == null)
                    {
                        break;
                    }

                    await FadeOutAsync(1f, token);
                    if (!IsAlive(bgmSource))
                    {
                        break;
                    }

                    bgmSource.clip = clip;
                    bgmSource.loop = true;
                    bgmSource.Play();
                    await FadeInAsync(1f, GetEffectiveBgmVolume(), token);
                    break;
                case 1:
                    if (IsAlive(sfxSource) && clip != null)
                    {
                        sfxSource.PlayOneShot(clip);
                    }

                    break;
                case 2:
                    if (IsAlive(onlySource) && clip != null)
                    {
                        onlySource.PlayOneShot(clip);
                    }

                    break;
            }
        }

        public void StopAllAudio()
        {
            if (!IsAlive(bgmSource) || !IsAlive(sfxSource))
            {
                return;
            }

            bgmSource.volume = 0f;
            sfxSource.volume = 0f;
            bgmSource.Stop();
            sfxSource.Stop();
        }

        public async UniTask StopAllAudioUt()
        {
            if (!IsAlive(bgmSource) || !IsAlive(sfxSource))
            {
                return;
            }

            float startBgm = bgmSource.volume;
            float startSfx = sfxSource.volume;
            float time = 0f;

            while (time < 0.5f)
            {
                if (!IsAlive(bgmSource) || !IsAlive(sfxSource))
                {
                    return;
                }

                time += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startBgm, 0f, time / 0.5f);
                sfxSource.volume = Mathf.Lerp(startSfx, 0f, time / 0.5f);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            bgmSource.volume = 0f;
            sfxSource.volume = 0f;
            bgmSource.Stop();
            sfxSource.Stop();
        }

        public async UniTask FadeInAsync(float duration, float targetVolume = 1f, CancellationToken token = default)
        {
            if (!IsAlive(bgmSource))
            {
                return;
            }

            bgmSource.volume = 0f;
            await FadeVolumeAsync(0f, targetVolume, duration, token);
        }

        public async UniTask FadeOutAsync(float duration, CancellationToken token = default)
        {
            if (!IsAlive(bgmSource))
            {
                return;
            }

            float startVolume = bgmSource.volume;
            await FadeVolumeAsync(startVolume, 0f, duration, token);
            if (IsAlive(bgmSource))
            {
                bgmSource.Stop();
            }
        }

        public async UniTask FadeVolumeAsync(float from, float to, float duration, CancellationToken token)
        {
            if (!IsAlive(bgmSource))
            {
                return;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (token.IsCancellationRequested || !IsAlive(bgmSource))
                {
                    return;
                }

                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            if (IsAlive(bgmSource))
            {
                bgmSource.volume = to;
            }
        }
    }
}
