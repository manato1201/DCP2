
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Sound
{
    public sealed class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundService service;   // SO
        [Header("Runtime Sources")]
        [SerializeField] private AudioSource bgmSource1;
        [SerializeField] private AudioSource bgmSource2;
        [SerializeField] private List<AudioSource> seSources = new();

        CancellationTokenSource _cts;

        void Awake()
        {
            _cts = new();
            service.Init(bgmSource1, bgmSource2, seSources, _cts.Token);
        }
        void OnDestroy() => _cts?.Cancel();

        // 互換API（既存呼び出しを極力壊さない）
        public UniTask PlayBGMAsync(string key, bool loop = true, float fade = 0.7f) => service.PlayBGMAsync(key, loop, fade);
        public UniTask CrossFadeBGMAsync(string key, float fade = 0.7f, bool loop = true) => service.CrossFadeToAsync(key, fade, loop);
        public UniTask StopBGMAsync(float fade = 0.5f) => service.StopBGMAsync(fade);
        public void PlaySE(string key, float volume = 1f, float pan = 0f, float pitch = 1f) => service.PlaySE(key, volume, pan, pitch);
    }
}
