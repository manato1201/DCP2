using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class CharacterManager : MonoBehaviour
{
    [Header("Targets (UI Images)")]
    [SerializeField] private Image bg;
    [SerializeField] private Image left;
    [SerializeField] private Image center;
    [SerializeField] private Image right;

    [Header("Catalog (optional)")]
    [SerializeField] private ImageAddressCatalog imageCatalog; // Id→AssetReferenceSprite を持つ想定

    [Header("Materials (optional)")]
    [SerializeField] private Material bgBaseMat;
    [SerializeField] private Material leftBaseMat;
    [SerializeField] private Material centerBaseMat;
    [SerializeField] private Material rightBaseMat;

    // 実体化したマテリアル（共有破壊を避ける）
    Material _bgMat, _lMat, _cMat, _rMat;

    // プロパティID（存在チェックしてから設定）
    static readonly int ID_UseGrayscale = Shader.PropertyToID("_OverallAlpha");
    static readonly int ID_MultiplyColor = Shader.PropertyToID("_MulColor");
    static readonly int ID_Glow         = Shader.PropertyToID("_Glow");
    static readonly int ID_UseGlow      = Shader.PropertyToID("_UseGlow");

    // ロード中キャンセル管理＆キャッシュ
    CancellationTokenSource _cts;
    readonly Dictionary<string, AsyncOperationHandle<Sprite>> _spriteHandles = new();
    readonly Dictionary<string, Sprite> _spriteCache = new();

    void Awake()
    {
        _bgMat = InstantiateIf(bgBaseMat);
        _lMat  = InstantiateIf(leftBaseMat);
        _cMat  = InstantiateIf(centerBaseMat);
        _rMat  = InstantiateIf(rightBaseMat);

        if (_bgMat && bg)     bg.material    = _bgMat;
        if (_lMat  && left)   left.material  = _lMat;
        if (_cMat  && center) center.material= _cMat;
        if (_rMat  && right)  right.material = _rMat;
    }

    void OnEnable()
    {
        TextManager.OnCommentChanged += OnCommentChanged;
    }

    void OnDisable()
    {
        TextManager.OnCommentChanged -= OnCommentChanged;
        _cts?.Cancel();
        _cts = null;
    }

    void OnDestroy()
    {
        foreach (var h in _spriteHandles.Values)
            if (h.IsValid()) Addressables.Release(h);
        _spriteHandles.Clear();

        foreach (var s in _spriteCache.Values)
            if (s) Destroy(s);
        _spriteCache.Clear();

        DestroyIf(_bgMat); DestroyIf(_lMat); DestroyIf(_cMat); DestroyIf(_rMat);
    }

    // TextManager から通知：commentNo は "CHAP1-3" など
    async void OnCommentChanged(string commentNo)
    {
        _cts?.Cancel();
        _cts = new();
        var ct = _cts.Token;

        await StoryCueRepo.EnsureLoadedAsync(ct);  // 初回のみロード

        if (!StoryCueRepo.TryGet(commentNo, out var row))
        {
            Debug.LogWarning($"[CharacterManager] StoryCue not found: {commentNo}");
            return;
        }

        // 画像適用
        await AssignAsync(bg,     row.BgId,     ct);
        await AssignAsync(left,   row.LeftId,   ct);
        await AssignAsync(center, row.CenterId, ct);
        await AssignAsync(right,  row.RightId,  ct);

        // グレー（0/1）
        ApplyGray(bg,     _bgMat, row.GrayBG == 1);
        ApplyGray(left,   _lMat,  row.GrayL  == 1);
        ApplyGray(center, _cMat,  row.GrayC  == 1);
        ApplyGray(right,  _rMat,  row.GrayR  == 1);

        // Glow を使うならここで適用（列を追加している場合）
        // ApplyGlow(_bgMat, row.Glow);
        // ApplyGlow(_lMat,  row.Glow);
        // ApplyGlow(_cMat,  row.Glow);
        // ApplyGlow(_rMat,  row.Glow);
    }

    // ------- internals -------

    async UniTask AssignAsync(Image target, string id, CancellationToken ct)
    {
        if (!target) return;

        if (string.IsNullOrWhiteSpace(id))
        {
            target.enabled = false;
            return;
        }
        target.enabled = true;

        // 1) キャッシュ
        if (_spriteCache.TryGetValue(id, out var cached) && cached)
        {
            target.sprite = cached;
            return;
        }

        // 2) Catalog 経由 → AssetReferenceSprite
        if (imageCatalog != null && imageCatalog.TryGetSpriteRef(id, out var aref) && aref.RuntimeKeyIsValid())
        {
            var handle = aref.LoadAssetAsync();
            var sp = await handle.Task.AsUniTask().AttachExternalCancellation(ct);
            target.sprite = sp;
            CacheHandle(id, handle, sp);
            return;
        }

        // 3) 直接キー（Addressables のアドレス or ラベル）でロード
        if (!_spriteHandles.TryGetValue(id, out var h) || !h.IsValid())
        {
            h = Addressables.LoadAssetAsync<Sprite>(id);
            _spriteHandles[id] = h;
        }
        var sprite = await h.Task.AsUniTask().AttachExternalCancellation(ct);
        target.sprite = sprite;
        _spriteCache[id] = sprite;
    }

    void ApplyGray(Image target, Material mat, bool on)
    {
        if (!target) return;
        if (mat)
        {
            if (mat.HasProperty(ID_UseGrayscale)) mat.SetFloat(ID_UseGrayscale, on ? 1f : 0f);
            if (mat.HasProperty(ID_MultiplyColor)) mat.SetColor(ID_MultiplyColor, on ? Color.white :Color.black );
        }
    }

    void ApplyGlow(Material mat, int flag)
    {
        if (!mat) return;
        if (mat.HasProperty(ID_Glow))    mat.SetFloat(ID_Glow, flag);
        if (mat.HasProperty(ID_UseGlow)) mat.SetFloat(ID_UseGlow, flag);
    }

    void CacheHandle(string key, AsyncOperationHandle<Sprite> h, Sprite sp)
    {
        if (h.IsValid()) _spriteHandles[key] = h;
        if (sp) _spriteCache[key] = sp;
    }

    static Material InstantiateIf(Material src) => src ? new Material(src) : null;
    static void DestroyIf(UnityEngine.Object o) { if (o) Destroy(o); }
}
