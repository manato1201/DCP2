using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private TransitionEffectController transition;
    [SerializeField] private SceneTransitData transitData;

    CancellationTokenSource _cts;

    void Awake() { _cts = new(); }
    void OnDestroy() { _cts?.Cancel(); }

    private void Start()
    {
        if (transition == null || transitData == null) return;

        var p = GetPayloadAs<SceneTransitData.Payload>(transitData);
        if (!p.isFade)
        {
            Debug.Log("明転");
            transition.PrepareForPlayIn();
            transition.PlayInAsync(CancellationToken.None).Forget();
        }

        // 使い捨て：読み終えたら初期化（次遷移に持ち越さない）
        transitData.payload = default;
    }

    public async UniTask LoadSceneAsync(string address, SceneTransitData.Payload p)
    {
        if (string.IsNullOrEmpty(address)) { Debug.LogError("[Addr] empty scene address"); return; }
        if (transitData) transitData.payload = p;

        var ct = this.GetCancellationTokenOnDestroy();
        if (transition) await transition.PlayOutAsync(ct);

        await Addressables.InitializeAsync().Task; // 保険

        var h = Addressables.LoadSceneAsync(address, LoadSceneMode.Single, true);
        h.Completed += op =>
        {
            if (op.Status != AsyncOperationStatus.Succeeded)
                Debug.LogError($"[Addr] LoadScene FAILED addr={address} ex={op.OperationException}");
            else
                Debug.Log($"[Addr] LoadScene OK addr={address}");
        };
    }

    public static T GetPayloadAs<T>(SceneTransitData data) where T : struct
        => data ? (T)(object)data.payload : default;
}
