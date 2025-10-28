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

    public async UniTask LoadSceneAsync(string addressableSceneKey, SceneTransitData.Payload payload)
    {
        if (transitData) transitData.payload = payload;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var ct = linked.Token;

        if (transition) await transition.PlayOutAsync(ct);

        // SceneInstance は ResourceProviders 名前空間にあります
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(addressableSceneKey, LoadSceneMode.Single, true);

        await handle.Task;

        if (transition) await transition.PlayInAsync(ct);
    }

    public static T GetPayloadAs<T>(SceneTransitData data) where T : struct
        => data ? (T)(object)data.payload : default;
}
