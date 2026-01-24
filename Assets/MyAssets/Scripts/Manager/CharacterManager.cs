using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterManager : MonoBehaviour
{
    [Header("Targets (UI Images)")]
    [SerializeField] private Image bg;
    [SerializeField] private Image left;
    [SerializeField] private Image center;
    [SerializeField] private Image right;

    [Header("Catalog (Sprite直参照)")]
    [SerializeField] private ImageAddressCatalog imageCatalog;

    [Header("Base Materials (乗算用)")]
    [SerializeField] private Material bgBaseMat;
    [SerializeField] private Material leftBaseMat;
    [SerializeField] private Material centerBaseMat;
    [SerializeField] private Material rightBaseMat;

    [Header("Glow Overlay")]
    [SerializeField] private Image glowOverlay;       // フルスクリーン Image
    [SerializeField] private Material glowBaseMat;    // _UseGlow(float), _Glow(Color) を持つ

    // 実体化したマテリアル（共有汚染防止）
    Material _bgMat, _lMat, _cMat, _rMat, _glowMat;

    // フェード用CTS（refを使うのは同期メソッド内だけ）
    CancellationTokenSource _fadeBG, _fadeL, _fadeC, _fadeR, _fadeGlow;

    void OnEnable()  { TextManager.OnCommentChanged += OnCommentChanged; }
    void OnDisable() { TextManager.OnCommentChanged -= OnCommentChanged; CancelAllFades(); }

    void Awake()
    {
        StoryCueRepo.EnsureLoaded();

        _bgMat = InstantiateIf(bgBaseMat);
        _lMat  = InstantiateIf(leftBaseMat);
        _cMat  = InstantiateIf(centerBaseMat);
        _rMat  = InstantiateIf(rightBaseMat);
        if (bg && _bgMat) bg.material = _bgMat;
        if (left && _lMat) left.material = _lMat;
        if (center && _cMat) center.material = _cMat;
        if (right && _rMat) right.material = _rMat;

        _glowMat = InstantiateIf(glowBaseMat);
        if (glowOverlay)
        {
            if (_glowMat) glowOverlay.material = _glowMat;
            glowOverlay.enabled = false;
            var col = glowOverlay.color;
            glowOverlay.color = new Color(col.r, col.g, col.b, 0f);
        }
    }

    void OnDestroy()
    {
        CancelAllFades();
        DestroyIf(_bgMat); DestroyIf(_lMat); DestroyIf(_cMat); DestroyIf(_rMat); DestroyIf(_glowMat);
    }

    async void OnCommentChanged(string commentNo)
    {
        if (!StoryCueRepo.TryGet(commentNo, out var row))
        {
            Debug.LogWarning($"[CharacterManager] StoryCue not found: {commentNo}");
            return;
        }

        AssignSprite(bg,     row.BgId);
        AssignSprite(left,   row.LeftId);
        AssignSprite(center, row.CenterId);
        AssignSprite(right,  row.RightId);

        var targetBg = PickMulColor(row.GrayBG, row.BlackBG);
        var targetL  = PickMulColor(row.GrayL,  row.BlackL);
        var targetC  = PickMulColor(row.GrayC,  row.BlackC);
        var targetR  = PickMulColor(row.GrayR,  row.BlackR);
        float dur    = Mathf.Max(0f, row.Fade);

        // --- フェード起動（同期）→ 実処理は async 側。ref はここだけで使用 ---
        BeginFadeMul(_bgMat, ref _fadeBG, targetBg, dur);
        BeginFadeMul(_lMat,  ref _fadeL,  targetL,  dur);
        BeginFadeMul(_cMat,  ref _fadeC,  targetC,  dur);
        BeginFadeMul(_rMat,  ref _fadeR,  targetR,  dur);

        BeginFadeGlow(row.Glow == 1, ref _fadeGlow, dur);
    }

    //========================
    // 画像割り当て（Catalogのみ）
    //========================
    void AssignSprite(Image target, string id)
    {
        if (!target) return;

        if (string.IsNullOrWhiteSpace(id))
        {
            target.enabled = false;
            target.sprite  = null;
            return;
        }

        if (imageCatalog != null && imageCatalog.TryGetSprite(id, out var sp) && sp)
        {
            target.enabled = true;
            target.sprite  = sp;
        }
        else
        {
            Debug.LogWarning($"[CharacterManager] Sprite not found in catalog: id={id}");
            target.enabled = false;
            target.sprite  = null;
        }
    }

    //========================
    // 乗算色の決定
    //========================
    static readonly Color COLOR_GRAY  = Hex("#6A6A6A");
    static readonly Color COLOR_BLACK = Color.black;
    static readonly Color COLOR_WHITE = Color.white;

    static Color PickMulColor(int gray, int black)
        => (black == 1) ? COLOR_BLACK : (gray == 1 ? COLOR_GRAY : COLOR_WHITE);

    static Color Hex(string s) { Color c; ColorUtility.TryParseHtmlString(s, out c); return c; }

    //========================
    // フェード：乗算色
    //========================
    static readonly int ID_MultiplyColor = Shader.PropertyToID("_MulColor");

    // ← 同期の“起動関数”。ここだけ ref OK（asyncではない）
    void BeginFadeMul(Material mat, ref CancellationTokenSource slot, Color to, float duration)
    {
        if (!mat || !mat.HasProperty(ID_MultiplyColor)) return;

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;

        // 実処理は async 側（ref なし）
        _ = FadeMulAsync(mat, to, duration, ct);
    }

    async UniTaskVoid FadeMulAsync(Material mat, Color to, float duration, CancellationToken ct)
    {
        if (!mat || !mat.HasProperty(ID_MultiplyColor)) return;

        var from = mat.GetColor(ID_MultiplyColor);
        if (duration <= 0f) { mat.SetColor(ID_MultiplyColor, to); return; }

        float t = 0f;
        while (t < duration)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime;
            mat.SetColor(ID_MultiplyColor, Color.Lerp(from, to, t / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        mat.SetColor(ID_MultiplyColor, to);
    }

    //========================
    // フェード：Glow（白加算）
    //========================
    static readonly int ID_UseGlow = Shader.PropertyToID("_OverallAlpha");
    static readonly int ID_Glow    = Shader.PropertyToID("_Glow"); // Color（加算色）

    void BeginFadeGlow(bool toOn, ref CancellationTokenSource slot, float duration)
    {
        if (!glowOverlay || !_glowMat) return;

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;

        _ = FadeGlowAsync(toOn, duration, ct);
    }

    async UniTaskVoid FadeGlowAsync(bool toOn, float duration, CancellationToken ct)
    {
        if (!glowOverlay || !_glowMat) return;

        // 加算色は常に白
        if (_glowMat.HasProperty(ID_Glow))    _glowMat.SetColor(ID_Glow, Color.white);
        if (_glowMat.HasProperty(ID_UseGlow)) _glowMat.SetFloat(ID_UseGlow, 1f);

        var col  = glowOverlay.color;
        float a0 = col.a;
        float a1 = toOn ? 1f : 0f;

        glowOverlay.enabled = true;

        if (duration <= 0f)
        {
            glowOverlay.color = new Color(col.r, col.g, col.b, a1);
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                if (ct.IsCancellationRequested) return;
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(a0, a1, t / duration);
                glowOverlay.color = new Color(col.r, col.g, col.b, a);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            glowOverlay.color = new Color(col.r, col.g, col.b, a1);
        }

        if (a1 <= 0f)
        {
            if (_glowMat.HasProperty(ID_UseGlow)) _glowMat.SetFloat(ID_UseGlow, 0f);
            glowOverlay.enabled = false;
        }
    }

    //========================
    // ヘルパ
    //========================
    static Material InstantiateIf(Material src) => src ? new Material(src) : null;
    static void DestroyIf(Object o) { if (o) Destroy(o); }

    void CancelAllFades()
    {
        _fadeBG?.Cancel(); _fadeL?.Cancel(); _fadeC?.Cancel(); _fadeR?.Cancel(); _fadeGlow?.Cancel();
        _fadeBG = _fadeL = _fadeC = _fadeR = _fadeGlow = null;
    }
}
