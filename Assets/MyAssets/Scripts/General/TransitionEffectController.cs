using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using System;

public class AnimationManager : MonoBehaviour
{
    // シングルトンにする場合
    public static AnimationManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ========== 1. フェードイン/フェードアウト ==========

    // Image用（UI）
    public void FadeImage(Image img, float targetAlpha, float duration, TweenCallback onComplete = null)
    {
        img.DOFade(targetAlpha, duration).OnComplete(onComplete);
    }

    // SpriteRenderer用
    public void FadeSprite(SpriteRenderer sr, float targetAlpha, float duration, TweenCallback onComplete = null)
    {
        sr.DOFade(targetAlpha, duration).OnComplete(onComplete);
    }

    // ========== 2. カウントアニメーション ==========
    // TMP_Textに0→指定数までカウントアップ
    public void AnimateCount(TMP_Text text, int from, int to, float duration, TweenCallback onComplete = null)
    {
        DOTween.To(() => from, x => {
            from = x;
            text.text = from.ToString();
        }, to, duration).OnComplete(onComplete);
    }


}


public class TransitionEffectController : MonoBehaviour
{
    [Header("トランジション用マテリアルリスト")]
    public List<Material> transitionMaterials; // Inspectorで複数セット

    [Header("遷移時間(秒)")]
    public float transitionDuration = 1.5f;

    /// <summary>
    /// 遷移開始（0→1へ）: indexでマテリアル指定
    /// </summary>
    public void PlayTransitionIn(int materialIndex = 0, Action onComplete = null)
    {
        if (transitionMaterials == null || transitionMaterials.Count == 0)
        {
            Debug.LogError("トランジションマテリアルが設定されていません！");
            return;
        }
        if (materialIndex < 0 || materialIndex >= transitionMaterials.Count)
        {
            Debug.LogWarning("指定されたマテリアルインデックスが範囲外です。0番を使います。");
            materialIndex = 0;
        }

        Material mat = transitionMaterials[materialIndex];
        mat.SetFloat("_Value", 0f);
        DOTween.To(() => mat.GetFloat("_Value"),
                   v => mat.SetFloat("_Value", v),
                   1f, transitionDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => { onComplete?.Invoke(); });
    }

    /// <summary>
    /// 遷移終了（1→0へ）: indexでマテリアル指定
    /// </summary>
    public void PlayTransitionOut(int materialIndex = 0, Action onComplete = null)
    {
        if (transitionMaterials == null || transitionMaterials.Count == 0)
        {
            Debug.LogError("トランジションマテリアルが設定されていません！");
            return;
        }
        if (materialIndex < 0 || materialIndex >= transitionMaterials.Count)
        {
            Debug.LogWarning("指定されたマテリアルインデックスが範囲外です。0番を使います。");
            materialIndex = 0;
        }

        Material mat = transitionMaterials[materialIndex];
        mat.SetFloat("_Value", 1f);
        DOTween.To(() => mat.GetFloat("_Value"),
                   v => mat.SetFloat("_Value", v),
                   0f, transitionDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => { onComplete?.Invoke(); });
    }
}