using System.Collections.Generic;
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

    [Header("Base Materials (乗算/通常)")]
    [SerializeField] private Material bgBaseMat;
    [SerializeField] private Material leftBaseMat;
    [SerializeField] private Material centerBaseMat;
    [SerializeField] private Material rightBaseMat;

    [Header("Glow Overlay (全体用1枚だけ)")]
    [SerializeField] private Image glowOverlay;     // 画面全面Image（RaycastTarget OFF推奨）
    [SerializeField] private Material glowBaseMat;  // Glow専用（_Glow:Color, _UseGlow:float を持つ）

    [Header("Timings")]
    [SerializeField, Min(0f)] private float defaultFade = 0.3f;          // CSV Fadeが0/空ならこれ
    [SerializeField, Min(0f)] private float glowAutoAdvanceDelay = 0.6f;  // Glow点灯中に自動前進させるまでの待機

    // 実体化マテリアル（共有汚染防止）
    Material _bgMat, _lMat, _cMat, _rMat, _glowMat;

    // プロパティID
    static readonly int ID_MulColor  = Shader.PropertyToID("_MulColor");
    static readonly int ID_UseGlow   = Shader.PropertyToID("_UseGlow");
    static readonly int ID_GlowColor = Shader.PropertyToID("_Glow");

    // フェード用キャンセル（色フェード）
    CancellationTokenSource _fadeBG, _fadeL, _fadeC, _fadeR, _fadeGlow;
    // クロスフェード用キャンセル（画像切替）
    CancellationTokenSource _xfBG, _xfL, _xfC, _xfR;

    // オーバーレイImageを使ったクロスフェード用一時コンテナ
    readonly Dictionary<Image, Image> _overlayMap = new();

    // 外部へ：Glow中に自動で次へ進めたい時に発火
    public static event System.Action OnGlowAutoAdvance;

    void Awake()
    {
        StoryCueRepo.EnsureLoaded(); // CSVを同期初期化

        // 実体化
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
            var c = glowOverlay.color;
            glowOverlay.color = new Color(c.r, c.g, c.b, 0f);
            glowOverlay.enabled = false;
        }
    }

    void OnEnable()  { TextManager.OnCommentChanged += OnCommentChanged; }
    void OnDisable()
    {
        TextManager.OnCommentChanged -= OnCommentChanged;
        CancelAll();
    }

    void OnDestroy()
    {
        CancelAll();
        DestroyIf(_bgMat); DestroyIf(_lMat); DestroyIf(_cMat); DestroyIf(_rMat); DestroyIf(_glowMat);
        foreach (var kv in _overlayMap) if (kv.Value) Destroy(kv.Value.gameObject);
        _overlayMap.Clear();
    }

    async void OnCommentChanged(string commentNo)
    {
        if (!StoryCueRepo.TryGet(commentNo, out var row))
        {
            Debug.LogWarning($"[CharacterManager] StoryCue not found: {commentNo}");
            return;
        }

        float dur = row.Fade > 0f ? row.Fade : defaultFade;

        // 画像のクロスフェード
        BeginCrossFade(bg,     row.BgId,     dur, ref _xfBG);
        BeginCrossFade(left,   row.LeftId,   dur, ref _xfL);
        BeginCrossFade(center, row.CenterId, dur, ref _xfC);
        BeginCrossFade(right,  row.RightId,  dur, ref _xfR);

        // 乗算色（Gray/Black）をフェード
        BeginMulFade(_bgMat, PickMulColor(row.GrayBG, row.BlackBG), dur, ref _fadeBG);
        BeginMulFade(_lMat,  PickMulColor(row.GrayL,  row.BlackL),  dur, ref _fadeL);
        BeginMulFade(_cMat,  PickMulColor(row.GrayC,  row.BlackC),  dur, ref _fadeC);
        BeginMulFade(_rMat,  PickMulColor(row.GrayR,  row.BlackR),  dur, ref _fadeR);

        // Glow（全体）
        BeginGlow(row.Glow == 1, dur, ref _fadeGlow);
    }

    //======== 画像クロスフェード =========
    void BeginCrossFade(Image baseImg, string id, float duration, ref CancellationTokenSource slot)
    {
        if (!baseImg) return;

        // カタログからSprite取得
        Sprite next = null;
        if (!string.IsNullOrWhiteSpace(id) && imageCatalog != null && imageCatalog.TryGetSprite(id, out var sp) && sp)
            next = sp;

        // 同一/未指定ならスキップ
        if (next == null || next == baseImg.sprite)
            return;

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;

        _ = CrossFadeAsync(baseImg, next, duration, ct);
    }

    async UniTaskVoid CrossFadeAsync(Image baseImg, Sprite next, float duration, CancellationToken ct)
    {
        var overlay = GetOrCreateOverlay(baseImg);

        // オーバーレイに新画像、アルファ0
        overlay.sprite = next;
        overlay.enabled = true;

        // 見た目整合（乗算/Glow）を複製
        if (baseImg.material)
        {
            if (!overlay.material) overlay.material = new Material(baseImg.material);
            CopyMulAndGlow(baseImg.material, overlay.material);
        }

        var bCol = baseImg.color;
        var oCol = overlay.color;
        float b0 = bCol.a;
        const float o0 = 0f;
        const float o1 = 1f;

        if (duration <= 0f)
        {
            overlay.color = new Color(oCol.r, oCol.g, oCol.b, 1f);
            baseImg.sprite = next;
            baseImg.color  = new Color(bCol.r, bCol.g, bCol.b, b0);
            overlay.enabled = false;
            return;
        }

        float t = 0f;
        while (t < duration)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / duration);
            overlay.color = new Color(oCol.r, oCol.g, oCol.b, Mathf.Lerp(o0, o1, u));
            baseImg.color = new Color(bCol.r, bCol.g, bCol.b, Mathf.Lerp(b0, 0f, u));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        // 完了
        baseImg.sprite = next;
        baseImg.color  = new Color(bCol.r, bCol.g, bCol.b, b0);
        overlay.enabled = false;
    }

    Image GetOrCreateOverlay(Image baseImg)
    {
        if (_overlayMap.TryGetValue(baseImg, out var ov) && ov) return ov;

        var go = new GameObject(baseImg.name + "_XFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(baseImg.transform.parent, false);
        go.transform.SetSiblingIndex(baseImg.transform.GetSiblingIndex() + 1);

        var rt  = (RectTransform)go.transform;
        var brt = (RectTransform)baseImg.transform;
        rt.anchorMin = brt.anchorMin; rt.anchorMax = brt.anchorMax;
        rt.pivot = brt.pivot; rt.anchoredPosition = brt.anchoredPosition;
        rt.sizeDelta = brt.sizeDelta; rt.localScale = brt.localScale; rt.localRotation = brt.localRotation;

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.enabled = false;

        _overlayMap[baseImg] = img;
        return img;
    }

    //======== 乗算色フェード =========
    static readonly Color COL_WHITE = Color.white;
    static readonly Color COL_BLACK = Color.black;
    static readonly Color COL_GRAY  = FromHex("#6A6A6A");

    static Color PickMulColor(int grayFlag, int blackFlag)
        => (blackFlag == 1) ? COL_BLACK : (grayFlag == 1 ? COL_GRAY : COL_WHITE);

    void BeginMulFade(Material mat, Color to, float duration, ref CancellationTokenSource slot)
    {
        if (!mat || !mat.HasProperty(ID_MulColor)) return;

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;
        _ = FadeMulAsync(mat, to, duration, ct);
    }

    async UniTaskVoid FadeMulAsync(Material mat, Color to, float duration, CancellationToken ct)
    {
        var from = mat.GetColor(ID_MulColor);
        if (duration <= 0f) { mat.SetColor(ID_MulColor, to); return; }

        float t = 0f;
        while (t < duration)
        {
            if (ct.IsCancellationRequested) return;
            t += Time.unscaledDeltaTime;
            mat.SetColor(ID_MulColor, Color.Lerp(from, to, t / duration));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
        mat.SetColor(ID_MulColor, to);
    }

    //======== Glow（全体1枚のみ、白加算） =========
    void BeginGlow(bool toOn, float duration, ref CancellationTokenSource slot)
    {
        if (!glowOverlay) return;

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;

        _ = FadeGlowAsync(toOn, duration, ct);
    }

    async UniTaskVoid FadeGlowAsync(bool toOn, float duration, CancellationToken ct)
    {
        if (!glowOverlay) return;

        // マテリアル設定（白加算固定）
        if (_glowMat)
        {
            if (_glowMat.HasProperty(ID_GlowColor)) _glowMat.SetColor(ID_GlowColor, Color.white);
            if (_glowMat.HasProperty(ID_UseGlow))   _glowMat.SetFloat(ID_UseGlow, 1f);
        }

        var c = glowOverlay.color;
        float a0 = c.a;
        float a1 = toOn ? 1f : 0f;

        glowOverlay.enabled = true;

        if (duration <= 0f)
        {
            glowOverlay.color = new Color(c.r, c.g, c.b, a1);
        }
        else
        {
            float t = 0f;
            while (t < duration)
            {
                if (ct.IsCancellationRequested) return;
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(a0, a1, t / duration);
                glowOverlay.color = new Color(c.r, c.g, c.b, a);
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            glowOverlay.color = new Color(c.r, c.g, c.b, a1);
        }

        if (a1 <= 0f)
        {
            if (_glowMat && _glowMat.HasProperty(ID_UseGlow)) _glowMat.SetFloat(ID_UseGlow, 0f);
            glowOverlay.enabled = false;
        }
        else
        {
            await UniTask.Delay((int)(glowAutoAdvanceDelay * 1000f), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            if (!ct.IsCancellationRequested) OnGlowAutoAdvance?.Invoke();
        }
    }

    //======== ヘルパ =========
    static Material InstantiateIf(Material src) => src ? new Material(src) : null;
    static void DestroyIf(Object o) { if (o) Destroy(o); }
    static Color FromHex(string s) { Color c; ColorUtility.TryParseHtmlString(s, out c); return c; }

    static void CopyMulAndGlow(Material from, Material to)
    {
        if (!from || !to) return;
        if (from.HasProperty(ID_MulColor)  && to.HasProperty(ID_MulColor))  to.SetColor(ID_MulColor,  from.GetColor(ID_MulColor));
        if (from.HasProperty(ID_UseGlow)   && to.HasProperty(ID_UseGlow))   to.SetFloat(ID_UseGlow,   from.GetFloat(ID_UseGlow));
        if (from.HasProperty(ID_GlowColor) && to.HasProperty(ID_GlowColor)) to.SetColor(ID_GlowColor, from.GetColor(ID_GlowColor));
    }

    void CancelAll()
    {
        _fadeBG?.Cancel(); _fadeL?.Cancel(); _fadeC?.Cancel(); _fadeR?.Cancel(); _fadeGlow?.Cancel();
        _xfBG?.Cancel(); _xfL?.Cancel(); _xfC?.Cancel(); _xfR?.Cancel();
        _fadeBG = _fadeL = _fadeC = _fadeR = _fadeGlow = null;
        _xfBG = _xfL = _xfC = _xfR = null;
    }
}
