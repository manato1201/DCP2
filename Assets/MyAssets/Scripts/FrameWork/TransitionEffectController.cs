using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

//public class AnimationManager : MonoBehaviour
//{
//    // シングルトンにする場合
//    public static AnimationManager Instance { get; private set; }

//    private void Awake()
//    {
//        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
//        Instance = this;
//    }

//    // ========== 1. フェードイン/フェードアウト ==========

//    // Image用（UI）
//    public void FadeImage(Image img, float targetAlpha, float duration, TweenCallback onComplete = null)
//    {
//        img.DOFade(targetAlpha, duration).OnComplete(onComplete);
//    }

//    // SpriteRenderer用
//    public void FadeSprite(SpriteRenderer sr, float targetAlpha, float duration, TweenCallback onComplete = null)
//    {
//        sr.DOFade(targetAlpha, duration).OnComplete(onComplete);
//    }

//    // ========== 2. カウントアニメーション ==========
//    // TMP_Textに0→指定数までカウントアップ
//    public void AnimateCount(TMP_Text text, int from, int to, float duration, TweenCallback onComplete = null)
//    {
//        DOTween.To(() => from, x => {
//            from = x;
//            text.text = from.ToString();
//        }, to, duration).OnComplete(onComplete);
//    }


//}


public class TransitionEffectController : MonoBehaviour
{
    [Header("Mask/Overlay")]
    [SerializeField] private Image overlay;                 // 全画面フェード
    [SerializeField] private List<Material> transitionMats; // _Value を使うマテリアル群
    [SerializeField] private float durationIn = 0.6f;
    [SerializeField] private float durationOut = 0.6f;
    readonly int PID_Value = Shader.PropertyToID("_Value");

    CancellationTokenSource _cts;

    void Awake() { _cts = new(); InstantiateMaterials(); }
    void OnDestroy() { _cts?.Cancel(); }

    void InstantiateMaterials()
    {
        for (int i = 0; i < transitionMats.Count; i++)
            if (transitionMats[i]) transitionMats[i] = new Material(transitionMats[i]); // 共有副作用防止
    }

    public async UniTask PlayInAsync(CancellationToken external)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, external);
        var ct = linked.Token;

        var tasks = new List<UniTask>();
        if (overlay) tasks.Add(UiAnimationLibrary.FadeGraphicAsync(overlay, 0f, durationIn, ct));
        foreach (var m in transitionMats) if (m) tasks.Add(UiAnimationLibrary.MaterialFloatAsync(m, PID_Value, 0f, durationIn, ct));
        await UniTask.WhenAll(tasks);
    }

    public async UniTask PlayOutAsync(CancellationToken external)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, external);
        var ct = linked.Token;

        var tasks = new List<UniTask>();
        if (overlay) tasks.Add(UiAnimationLibrary.FadeGraphicAsync(overlay, 1f, durationOut, ct));
        foreach (var m in transitionMats) if (m) tasks.Add(UiAnimationLibrary.MaterialFloatAsync(m, PID_Value, 1f, durationOut, ct));
        await UniTask.WhenAll(tasks);
    }
}
