using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

// 必要: SceneTransitionManager / TransitionEffectController / SceneTransitData / SceneAddressCatalog(SO)
public sealed class UIFlowManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SceneTransitionManager transitionManager;   // シーンのManager
    [SerializeField] private TransitionEffectController transitionFX;    // 手動でフェードしたい時に使う
    [SerializeField] private SceneTransitData transitData;               // 受け渡し箱（使わなくても可）
    [SerializeField] private SceneAddressCatalog catalog;           // シーンアドレスSO

    [Header("Buttons")]
    [SerializeField] private Button btnTitle;
    [SerializeField] private Button btnSelect;
    [SerializeField] private Button btnStory;
    [SerializeField] private Button btnBook;
    [SerializeField] private Button btnGame;
    [SerializeField] private Button btnResult;




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
        if (btnTitle) btnTitle.onClick.AddListener(OnClick_Title);
        if (btnSelect) btnSelect.onClick.AddListener(OnClick_Select);
        if (btnStory) btnStory.onClick.AddListener(OnClick_Story);
        if (btnBook) btnBook.onClick.AddListener(OnClick_Book);
        if (btnGame) btnGame.onClick.AddListener(OnClick_Game);
        if (btnResult) btnResult.onClick.AddListener(OnClick_Result);

    }

    void OnDisable()
    {

        if (btnTitle) btnTitle.onClick.RemoveListener(OnClick_Title);
        if (btnSelect) btnSelect.onClick.RemoveListener(OnClick_Select);
        if (btnStory) btnStory.onClick.RemoveListener(OnClick_Story);
        if (btnBook) btnBook.onClick.RemoveListener(OnClick_Book);
        if (btnGame) btnGame.onClick.RemoveListener(OnClick_Game);
        if (btnResult) btnResult.onClick.RemoveListener(OnClick_Result);

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
    void OnClick_Title() => TitleAsync().Forget();
    void OnClick_Select() => SelectAsync().Forget();
    void OnClick_Story() => StoryAsync().Forget();
    void OnClick_Book() => BookAsync().Forget();
    void OnClick_Game() => GameAsync().Forget();
    void OnClick_Result() => ResultAsync().Forget();



    // -----------------------------
    // Scene: 遷移（Addressablesシーン）
    // -----------------------------

    async UniTaskVoid TitleAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "from", isFade = false };

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Title), payload);
        UnloadAsset();
    }
    async UniTaskVoid SelectAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "from",  isFade = false };

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Select), payload);
        UnloadAsset();
    }
    async UniTaskVoid StoryAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { chap = "CHAP1"};
        SceneTransitBus.Set("CHAP1", "from", isFade:false);

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Story), payload);
        UnloadAsset();
    }

    async UniTaskVoid BookAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "from",  isFade = false };

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.BookUI), payload);
        UnloadAsset();
    }
    async UniTaskVoid GameAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "stage",  isFade = false };

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Puzzle), payload);
        UnloadAsset();
    }
    async UniTaskVoid ResultAsync()
    {
        if (transitionManager == null ) return;
        LoadAssetAsync().Forget();
        var payload = new SceneTransitData.Payload { key = "stage",  isFade = true };

        await transitionManager.LoadSceneAsync(catalog.Get(SceneId.Result), payload);
        UnloadAsset();
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
