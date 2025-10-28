using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BattleSpeedEntry : MonoBehaviour
{
    [SerializeField] private BattleSpeedService service;
    CancellationTokenSource _cts;

    void Awake()
    {
        _cts = new();
        service.Init(_cts.Token);
    }
    void OnDestroy() => _cts?.Cancel();

    // 既存呼び出しの互換メソッド例
    public void SetSpeed(float s) => service.Set(s);
    public void Pause(bool p) => service.Pause(p);
}
