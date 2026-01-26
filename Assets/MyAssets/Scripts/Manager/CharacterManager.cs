using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Sound;

public sealed class CharacterManager : MonoBehaviour
{
    [Header("Targets (UI Images)")]
    [SerializeField] private Image bg;
    [SerializeField] private Image left;
    [SerializeField] private Image center;
    [SerializeField] private Image right;

    [Header("Catalog (Sprite直参照)")]
    [SerializeField] private ImageAddressCatalog imageCatalog;

    [Header("Base Materials")]
    [SerializeField] private Material bgBaseMat;
    [SerializeField] private Material leftBaseMat;
    [SerializeField] private Material centerBaseMat;
    [SerializeField] private Material rightBaseMat;

    [Header("Glow Overlay (全体1枚)")]
    [SerializeField] private Image glowOverlay;    // 画面全面に被せるImage（RaycastTarget OFF推奨）
    [SerializeField] private Material glowBaseMat;

    [Header("Timings")]
    [SerializeField, Min(0f)] private float defaultFade = 0.3f;     // CSVのFadeが0の時に使わない。>0ならそちら優先
    [SerializeField, Min(0f)] private float glowAutoAdvanceDelay = 0.6f;

    [SerializeField] SoundManager sound;
    // 共有汚染防止のインスタンス化マテリアル
    Material _bgMat, _lMat, _cMat, _rMat, _glowMat;

    // Shader Property IDs（あなたのシェーダに合わせる）
    static readonly int ID_MulColor  = Shader.PropertyToID("_MulColor");
    static readonly int ID_UseGlow   = Shader.PropertyToID("_OverallAlpha");
    static readonly int ID_GlowColor = Shader.PropertyToID("_Glow");

    // カラー定義
    static readonly Color COL_WHITE = Color.white;
    static readonly Color COL_BLACK = Color.black;
    static readonly Color COL_GRAY  = FromHex("#6A6A6A");

    // フェード用キャンセル
    CancellationTokenSource _fadeBG, _fadeL, _fadeC, _fadeR, _fadeGlow;
    // クロスフェード用キャンセル
    CancellationTokenSource _xfBG, _xfL, _xfC, _xfR;

    // 画像クロスフェード用オーバーレイ Image
    readonly Dictionary<Image, Image> _overlayMap = new();

    public static event System.Action OnGlowAutoAdvance;
    private string BGMCash;

    void Awake()
    {
        StoryCueRepo.EnsureLoaded();

        _bgMat = InstantiateIf(bgBaseMat);
        _lMat  = InstantiateIf(leftBaseMat);
        _cMat  = InstantiateIf(centerBaseMat);
        _rMat  = InstantiateIf(rightBaseMat);

        if (_bgMat && bg)     bg.material     = _bgMat;
        if (_lMat  && left)   left.material   = _lMat;
        if (_cMat  && center) center.material = _cMat;
        if (_rMat  && right)  right.material  = _rMat;

        _glowMat = InstantiateIf(glowBaseMat);
        if (glowOverlay)
        {
            if (_glowMat) glowOverlay.material = _glowMat;
            var c = glowOverlay.color;
            glowOverlay.color = new Color(c.r, c.g, c.b, 0f);
            glowOverlay.raycastTarget = false;
            glowOverlay.enabled = false;
        }
    }

    void OnEnable()  => TextManager.OnCommentChanged += OnCommentChanged;
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

        // フェード時間（CSVが0なら即時）
        float dur = (row.Fade > 0f) ? row.Fade : 0f;

        // ===== 画像（空IDは透明扱い。enabledは触らない） =====
        BeginCrossFade(bg,     row.BgId,     dur, ref _xfBG);
        BeginCrossFade(left,   row.LeftId,   dur, ref _xfL);
        BeginCrossFade(center, row.CenterId, dur, ref _xfC);
        BeginCrossFade(right,  row.RightId,  dur, ref _xfR);

        // ===== 乗算色（Gray / Black） =====
        BeginMulFade(_bgMat, PickMulColor(row.GrayBG, row.BlackBG), dur, ref _fadeBG);
        BeginMulFade(_lMat,  PickMulColor(row.GrayL,  row.BlackL),  dur, ref _fadeL);
        BeginMulFade(_cMat,  PickMulColor(row.GrayC,  row.BlackC),  dur, ref _fadeC);
        BeginMulFade(_rMat,  PickMulColor(row.GrayR,  row.BlackR),  dur, ref _fadeR);

        // ===== Glow（Fadeの有無に関係なく）=====
        BeginGlow(row.Glow == 1, (row.Fade > 0f ? row.Fade : defaultFade), ref _fadeGlow);
        sound.PlaySE(row.SEId);
        if (BGMCash != row.BGMId)sound.PlayBGMAsync(row.BGMId, loop:true).Forget();
        BGMCash = row.BGMId;
        Debug.Log($"[Sound] BGM try '{row.BGMId}' fade={row.Fade}");
        await UniTask.Yield(); // フレーム分割

    }

    //================= 画像クロスフェード =================
    void BeginCrossFade(Image baseImg, string idOrEmpty, float duration, ref CancellationTokenSource slot)
    {
        if (!baseImg) return;

        // 目的スプライト解決（無いなら null＝透明）
        Sprite next = null;
        if (!string.IsNullOrWhiteSpace(idOrEmpty))
        {
            if (imageCatalog != null && imageCatalog.TryGetSprite(idOrEmpty, out var sp) && sp)
                next = sp;
            else
                Debug.LogWarning($"[CharacterManager] Sprite not found in catalog: '{idOrEmpty}'");
        }

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;
        _ = CrossFadeAsync(baseImg, next, duration, ct);
    }

    async UniTask CrossFadeAsync(Image baseImg, Sprite nextOrNull, float duration, CancellationToken ct)
    {
        var bCol = baseImg.color;
        float bAlpha0 = bCol.a;

        // 即時切替（Fade==0）
        if (duration <= 0f)
        {
            if (nextOrNull == null)
            {
                baseImg.color  = new Color(bCol.r, bCol.g, bCol.b, 0f); // 透明に
                // spriteはそのままでも良いが、明示的に消したいなら下の1行を有効化
                // baseImg.sprite = null;
            }
            else
            {
                baseImg.sprite = nextOrNull;
                baseImg.color  = new Color(bCol.r, bCol.g, bCol.b, 1f);
            }
            return;
        }

        // フェードあり
        var overlay = GetOrCreateOverlay(baseImg);

        if (nextOrNull == null)
        {
            // 透明へクロスフェード（オーバーレイ不要）
            float t = 0f;
            while (t < duration)
            {
                if (ct.IsCancellationRequested) return;
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                baseImg.color = new Color(bCol.r, bCol.g, bCol.b, Mathf.Lerp(bAlpha0, 0f, u));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }
            baseImg.color = new Color(bCol.r, bCol.g, bCol.b, 0f);
            return;
        }

        // 画像あり：オーバーレイでクロスフェード
        overlay.enabled = true;
        overlay.sprite  = nextOrNull;

        // マテリアルの乗算/Glow状態をコピー
        if (baseImg.material)
        {
            if (!overlay.material) overlay.material = new Material(baseImg.material);
            CopyMulAndGlow(baseImg.material, overlay.material);
        }

        var oCol = overlay.color;
        float t2 = 0f;
        while (t2 < duration)
        {
            if (ct.IsCancellationRequested) return;
            t2 += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t2 / duration);
            overlay.color = new Color(oCol.r, oCol.g, oCol.b, u);
            baseImg.color = new Color(bCol.r, bCol.g, bCol.b, Mathf.Lerp(bAlpha0, 0f, u));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        // 完了：本体に確定、オーバーレイを消す
        baseImg.sprite = nextOrNull;
        baseImg.color  = new Color(bCol.r, bCol.g, bCol.b, 1f);
        overlay.enabled = false;
        overlay.color    = new Color(oCol.r, oCol.g, oCol.b, 0f);
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

    //================= 乗算色（Gray/Black） =================
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

    async UniTask FadeMulAsync(Material mat, Color to, float duration, CancellationToken ct)
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

    //================= Glow（全体） =================
    void BeginGlow(bool toOn, float duration, ref CancellationTokenSource slot)
    {
        if (!glowOverlay)
        {
            Debug.LogWarning("[CharacterManager] Glow overlay Image is not assigned.");
            return;
        }

        slot?.Cancel();
        slot = new CancellationTokenSource();
        var ct = slot.Token;
        _ = FadeGlowAsync(toOn, duration, ct);
    }

    async UniTask FadeGlowAsync(bool toOn, float duration, CancellationToken ct)
    {
        if (!glowOverlay) return;

        if (_glowMat)
        {
            if (_glowMat.HasProperty(ID_GlowColor)) _glowMat.SetColor(ID_GlowColor, Color.white); // 全体白で加算
            if (_glowMat.HasProperty(ID_UseGlow))   _glowMat.SetFloat(ID_UseGlow, 1f);
        }

        var c  = glowOverlay.color;
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
            // Glow行（例：5, 33）はここで一定時間待って自動進行
            await UniTask.Delay((int)(glowAutoAdvanceDelay * 1000f), DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ct);
            if (!ct.IsCancellationRequested) OnGlowAutoAdvance?.Invoke();
        }
    }

    //================= ヘルパ =================
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
