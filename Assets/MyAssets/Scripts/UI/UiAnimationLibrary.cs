using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UiAnimationLibrary : MonoBehaviour
{
    // Tween完了 or キャンセルまで待つ汎用待機
    static async UniTask AwaitTweenAsync(Tween tween, System.Threading.CancellationToken ct)
    {
        if (tween == null) return;

        // 念のため再生開始（既に再生中ならそのまま）
        tween.Play();

        // DOTweenの状態を監視しつつ、毎フレームキャンセルチェック
        await UniTask.WaitUntil(
            () => !tween.IsActive() || tween.IsComplete(),
            PlayerLoopTiming.Update,
            ct
        );
    }

    public static async UniTask FadeGraphicAsync(
        Graphic g, float to, float dur, System.Threading.CancellationToken ct)
    {
        if (!g) return;

        var tween = g.DOFade(to, dur).SetUpdate(true); // timeScale無視にしたい場合の例
        await AwaitTweenAsync(tween, ct);
    }

    public static async UniTask MaterialFloatAsync(
        Material mat, int pid, float to, float dur, System.Threading.CancellationToken ct)
    {
        if (!mat) return;

        float from = mat.GetFloat(pid);
        var tween = DOTween
            .To(() => from, x => { from = x; mat.SetFloat(pid, x); }, to, dur)
            .SetUpdate(true);

        await AwaitTweenAsync(tween, ct);
    }
}
