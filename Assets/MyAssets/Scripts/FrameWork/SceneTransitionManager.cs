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

    public async UniTask LoadSceneAsync(AssetReference sceneRef, SceneTransitData.Payload p)
    {
        if (transitData) transitData.payload = p;
        var ct = this.GetCancellationTokenOnDestroy();
        if (transition) await transition.PlayOutAsync(ct);
        await Addressables.LoadSceneAsync(sceneRef, LoadSceneMode.Single, true).Task;
        if (transition) await transition.PlayInAsync(ct);
    }

    public static T GetPayloadAs<T>(SceneTransitData data) where T : struct
        => data ? (T)(object)data.payload : default;
}
