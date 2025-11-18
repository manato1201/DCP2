using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// 必要: SceneTransitionManager / TransitionEffectController / SceneTransitData / SceneAddressCatalog(SO)
public sealed class SampleUIFlow : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SceneTransitionManager transitionManager;   // シーンのManager
    [SerializeField] private TransitionEffectController transitionFX;    // 手動でフェードしたい時に使う
    [SerializeField] private SceneTransitData transitData;               // 受け渡し箱（使わなくても可）
    [SerializeField] private SceneAddressCatalog catalog;           // シーンアドレスSO

    [Header("Buttons")]
    [SerializeField] private Button btnStartGame;
    [SerializeField] private Button btnBackTitle;
    [SerializeField] private Button btnFadeDemo;     // トランジション手動再生
    [SerializeField] private Button btnLoadAsset;    // Addressableロード
    [SerializeField] private Button btnUnloadAsset;  // Addressable解放

    [Header("Addressables Asset")]
    [SerializeField] private AssetReferenceGameObject prefabRef; // 任意のプレハブ参照
    [SerializeField] private AssetReference sceneRef;
    [SerializeField] private Transform spawnRoot;

    CancellationTokenSource _cts;

    AsyncOperationHandle<GameObject> _assetHandle;
    GameObject _spawned;

    void Awake()
    {
        _cts = new();
    }

    void OnEnable()
    {
        if (btnStartGame) btnStartGame.onClick.AddListener(OnClick_StartGame);
        if (btnBackTitle) btnBackTitle.onClick.AddListener(OnClick_BackTitle);
        if (btnFadeDemo) btnFadeDemo.onClick.AddListener(OnClick_FadeDemo);
        if (btnLoadAsset) btnLoadAsset.onClick.AddListener(OnClick_LoadAsset);
        if (btnUnloadAsset) btnUnloadAsset.onClick.AddListener(OnClick_UnloadAsset);
    }

    void OnDisable()
    {
        if (btnStartGame) btnStartGame.onClick.RemoveListener(OnClick_StartGame);
        if (btnBackTitle) btnBackTitle.onClick.RemoveListener(OnClick_BackTitle);
        if (btnFadeDemo) btnFadeDemo.onClick.RemoveListener(OnClick_FadeDemo);
        if (btnLoadAsset) btnLoadAsset.onClick.RemoveListener(OnClick_LoadAsset);
        if (btnUnloadAsset) btnUnloadAsset.onClick.RemoveListener(OnClick_UnloadAsset);
    }

    void OnDestroy()
    {
        _cts?.Cancel();
        // 念のためロード済みを解放
        if (_spawned) Destroy(_spawned);
        if (_assetHandle.IsValid()) Addressables.Release(_assetHandle);
    }

    // -----------------------------
    // Button handlers (非同期はForgetで起動)
    // -----------------------------
    void OnClick_StartGame() => StartGameAsync().Forget();
    void OnClick_BackTitle() => BackTitleAsync().Forget();
    void OnClick_FadeDemo() => FadeDemoAsync().Forget();
    void OnClick_LoadAsset() => LoadAssetAsync().Forget();
    void OnClick_UnloadAsset() => UnloadAsset();

    // -----------------------------
    // Scene: 遷移（Addressablesシーン）
    // -----------------------------
    async UniTaskVoid StartGameAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "stage", intVal = 1 };
        //await transitionManager.LoadSceneAsync(sceneCatalog.GameScene.Address, payload);
        FadeDemoAsync().Forget();
        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Select), payload);
        UnloadAsset();
    }

    async UniTaskVoid BackTitleAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "from", strVal = "game" };
        //await transitionManager.LoadSceneAsync(sceneCatalog.TitleScene.Address, payload);
        FadeDemoAsync().Forget();
        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Title), payload);
        UnloadAsset();
    }

    // -----------------------------
    // Transition: 手動再生（In/Out）
    // -----------------------------
    async UniTaskVoid FadeDemoAsync()
    {
        if (transitionFX == null) return;
        var ct = this.GetCancellationTokenOnDestroy();
        await transitionFX.PlayInAsync(ct);                  // 画面明転
        await UniTask.Delay(600, cancellationToken: ct);     // 何らかの処理
        
        await transitionFX.PlayOutAsync(ct);                 // 画面暗転
    }

    // -----------------------------
    // Addressables: アセットのロード/解放
    // -----------------------------
    async UniTaskVoid LoadAssetAsync()
    {
        if (!prefabRef.RuntimeKeyIsValid()) return;
        if (_assetHandle.IsValid() && _spawned != null) return; // 既にロード済み

        // ロード
        _assetHandle = prefabRef.LoadAssetAsync<GameObject>();
        var prefab = await _assetHandle.Task;
        if (prefab == null) return;

        // 生成
        _spawned = Instantiate(prefab, spawnRoot ? spawnRoot : transform);
        _spawned.name = $"{prefab.name}_Instance";
    }

    void UnloadAsset()
    {
        if (_spawned) { Destroy(_spawned); _spawned = null; }
        if (_assetHandle.IsValid())
        {
            Addressables.Release(_assetHandle);
            _assetHandle = default;
        }
    }
}
