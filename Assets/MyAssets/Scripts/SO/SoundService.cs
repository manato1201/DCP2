using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;


namespace Sound
{
    public enum SoundType { BGM, SE }

    [CreateAssetMenu(menuName = "Services/SoundService")]
    public sealed class SoundService : ScriptableObject
    {
        [Serializable] public sealed class Entry { public string key; public AudioClip clip; public SoundType type; }

        [Header("Mixer (任意)")]
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private AudioMixerGroup bgmGroup;
        [SerializeField] private AudioMixerGroup seGroup;

        [Header("Catalog")]
        [SerializeField] private List<Entry> entries = new();

        // 実行時注入
        AudioSource _bgmA, _bgmB;
        bool _useA = true;
        readonly List<AudioSource> _sePool = new();
        CancellationToken _ct;

        Dictionary<string, Entry> _dict;

        public void Init(AudioSource bgmA, AudioSource bgmB, IReadOnlyList<AudioSource> sePool, CancellationToken ct)
        {
            _bgmA = bgmA; _bgmB = bgmB; _ct = ct;

            if (_bgmA) _bgmA.outputAudioMixerGroup = bgmGroup;
            if (_bgmB) _bgmB.outputAudioMixerGroup = bgmGroup;

            _sePool.Clear();
            for (int i = 0; i < sePool.Count; i++)
            {
                var s = sePool[i];
                if (!s) continue;
                s.outputAudioMixerGroup = seGroup;
                _sePool.Add(s);
            }

            _dict = new(entries.Count);
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.key) && !_dict.ContainsKey(e.key))
                    _dict.Add(e.key, e);
        }

        public bool Has(string key) => _dict != null && _dict.ContainsKey(key);

        public async UniTask PlayBGMAsync(string key, bool loop = true, float fadeSeconds = 0.7f)
        {
            if (!TryGetBGM(key, out var clip)) return;

            var next = _useA ? _bgmB : _bgmA;
            var prev = _useA ? _bgmA : _bgmB;
            _useA = !_useA;

            if (!next) return;

            next.Stop();
            next.clip = clip;
            next.loop = loop;
            next.volume = 0f;
            next.Play();

            // 手動クロスフェード (DOTween非依存)
            float t = 0f, dur = Mathf.Max(0.0001f, fadeSeconds);
            float prevStart = prev ? prev.volume : 0f;
            while (t < dur && !_ct.IsCancellationRequested)
            {
                float r = t / dur;
                if (prev) prev.volume = Mathf.Lerp(prevStart, 0f, r);
                next.volume = Mathf.Lerp(0f, 1f, r);
                t += Time.unscaledDeltaTime; // timeScale無視で安定
                await UniTask.Yield();
            }
            if (prev) { prev.volume = 0f; prev.Stop(); }
            next.volume = 1f;
        }

        public async UniTask CrossFadeToAsync(string key, float fadeSeconds = 0.7f, bool loop = true)
            => await PlayBGMAsync(key, loop, fadeSeconds);

        public async UniTask StopBGMAsync(float fadeSeconds = 0.5f)
        {
            await UniTask.WhenAll(FadeOutAsync(_bgmA, fadeSeconds), FadeOutAsync(_bgmB, fadeSeconds));
        }

        static async UniTask FadeOutAsync(AudioSource src, float fade)
        {
            if (!src || !src.isPlaying) return;
            float t = 0f, dur = Mathf.Max(0.0001f, fade);
            float start = src.volume;
            while (t < dur)
            {
                src.volume = Mathf.Lerp(start, 0f, t / dur);
                t += Time.unscaledDeltaTime;
                await UniTask.Yield();
            }
            src.volume = 0f; src.Stop();
        }

        public void PlaySE(string key, float volume = 1f, float pan = 0f, float pitch = 1f)
        {
            if (!TryGetSE(key, out var clip)) return;

            // 空いているソースに割当（全部埋まってたら先頭を上書き）
            AudioSource target = null;
            foreach (var s in _sePool) { if (!s.isPlaying) { target = s; break; } }
            target ??= _sePool.Count > 0 ? _sePool[0] : null;
            if (!target) return;

            target.Stop();
            target.clip = clip;
            target.volume = Mathf.Clamp01(volume);
            target.panStereo = Mathf.Clamp(pan, -1f, 1f);
            target.pitch = Mathf.Clamp(pitch, -3f, 3f);
            target.Play();
        }

        bool TryGetBGM(string key, out AudioClip clip)
        {
            clip = null;
            if (_dict == null || !_dict.TryGetValue(key, out var e)) return false;
            if (e.type != SoundType.BGM || !e.clip) return false;
            clip = e.clip; return true;
        }

        bool TryGetSE(string key, out AudioClip clip)
        {
            clip = null;
            if (_dict == null || !_dict.TryGetValue(key, out var e)) return false;
            if (e.type != SoundType.SE || !e.clip) return false;
            clip = e.clip; return true;
        }
    }
}
