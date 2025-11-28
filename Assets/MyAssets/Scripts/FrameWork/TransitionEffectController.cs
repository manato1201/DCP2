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
    [Header("Overlay (全画面Image/TMP)")]
    [SerializeField] private Graphic overlay;
    [SerializeField] private bool useOverlayFade = true;     // αフェードを使うか
    [SerializeField] private float overlayFadeDur = 0.3f;    // αアニメ時間

    [Header("マテリアル演出 (_Value を 0⇄1)")]
    [SerializeField] private bool useMaterialAnimation = true;
    [SerializeField] private Material[] transitionMatPrototypes;
    [SerializeField] private float matDur = 0.6f;
    [SerializeField] private bool instantiateMaterials = true;

    private static readonly int PID_VALUE = Shader.PropertyToID("_Value");
    private Material[] _mats;

    void Awake()
    {
        // マテリアルは共有を汚さない
        if (transitionMatPrototypes != null && transitionMatPrototypes.Length > 0)
        {
            _mats = new Material[transitionMatPrototypes.Length];
            for (int i = 0; i < transitionMatPrototypes.Length; i++)
            {
                var src = transitionMatPrototypes[i];
                _mats[i] = (instantiateMaterials && src) ? new Material(src) : src;
                if (_mats[i]) _mats[i].SetFloat(PID_VALUE, 0f); // 初期は“明転側”
            }
        }

        // Overlay初期は明転（α=0）
        if (overlay)
        {
            var c = overlay.color; c.a = 0f; overlay.color = c;
            overlay.material = null; // 初期は未割当
        }
    }

    /// <summary>
    /// 暗転：(_Value 0→1) →（必要ならα 0→1）
    /// </summary>
    public async UniTask PlayOutAsync(CancellationToken ct, int matIndex = 0)
    {
        if (overlay == null) return;

        // まず見える状態に（α=1に即時設定：ここは“見せる”ための固定）
        SetOverlayAlpha(1f);

        // マテリアル演出 0→1
        if (useMaterialAnimation)
        {
            var mat = GetMat(matIndex);
            if (mat)
            {
                AssignOverlayMaterial(mat);
                mat.SetFloat(PID_VALUE, 0f);
                await UiAnimationLibrary.MaterialFloatAsync(mat, PID_VALUE, 1f, matDur, ct);
            }
        }

        // 追加のαフェード（必要な場合のみ。ここは0→1のアニメ）
        if (useOverlayFade)
        {
            await UiAnimationLibrary.FadeGraphicAsync(overlay, 1f, overlayFadeDur, ct); // ★ αアニメ（フェード）
        }
    }

    /// <summary>
    /// 明転：(_Value 1→0) → α 1→0
    /// </summary>
    public async UniTask PlayInAsync(CancellationToken ct, int matIndex = 0)
    {
        if (overlay == null) return;

        // マテリアル演出 1→0
        if (useMaterialAnimation)
        {
            var mat = GetMat(matIndex);
            if (mat)
            {
                AssignOverlayMaterial(mat);
                mat.SetFloat(PID_VALUE, 1f);
                await UiAnimationLibrary.MaterialFloatAsync(mat, PID_VALUE, 0f, matDur, ct);
            }
        }

        // αを下げて画面復帰（ここがフェードアウト＝透明にする処理）
        if (useOverlayFade)
        {
            await UiAnimationLibrary.FadeGraphicAsync(overlay, 0f, overlayFadeDur, ct); // ★ αアニメ（フェード）
        }
        else
        {
            SetOverlayAlpha(0f);
        }

        // 終了後はマテリアルを外す（任意）
        //AssignOverlayMaterial(null);
    }

    public void PrepareForPlayIn()
    {
        // 黒で覆う（α=1）
        if (overlay) {
            var c = overlay.color; c.a = 1f; overlay.color = c;
        }
        // マテリアル側も1に
        if (_mats != null) foreach (var m in _mats) if (m) m.SetFloat(PID_VALUE, 1f);

        // オーバーレイにマテリアルを割り当て（必要なら0番など）
        AssignOverlayMaterial(GetMat(0));
    }

    // -------------------- helpers --------------------
    private Material GetMat(int index)
    {
        if (_mats == null || _mats.Length == 0) return null;
        index = Mathf.Clamp(index, 0, _mats.Length - 1);
        return _mats[index];
    }

    private void AssignOverlayMaterial(Material mat)
    {
        overlay.material = mat;       // UIはPropertyBlock不可。直接差し替え
        overlay.SetMaterialDirty();
    }

    private void SetOverlayAlpha(float a)
    {
        var c = overlay.color; c.a = a; overlay.color = c;
    }
}
