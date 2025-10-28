using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class ShakeByPerlinNoise : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float strengthPos = 0.2f;
    [SerializeField] private float strengthRot = 3f;
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private int vibrato = 20;

    Vector3 _basePos; Quaternion _baseRot;
    CancellationTokenSource _cts;

    void Awake()
    {
        if (!target) target = transform;
        _basePos = target.localPosition;
        _baseRot = target.localRotation;
        _cts = new();
    }

    void OnDestroy() => _cts?.Cancel();

    static async UniTask AwaitTweenAsync(Tween tween, CancellationToken ct)
    {
        if (tween == null) return;
        tween.Play();
        await UniTask.WaitUntil(() => !tween.IsActive() || tween.IsComplete(), PlayerLoopTiming.Update, ct);
    }

    public async UniTask StartShakeAsync(CancellationToken external)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, external);
        var ct = linked.Token;

        target.localPosition = _basePos;
        target.localRotation = _baseRot;

        var tPos = target.DOShakePosition(duration, strengthPos, vibrato: vibrato).SetAutoKill(false);
        var tRot = target.DOShakeRotation(duration, strengthRot, vibrato: vibrato).SetAutoKill(false);

        await UniTask.WhenAll(AwaitTweenAsync(tPos, ct), AwaitTweenAsync(tRot, ct));

        target.localPosition = _basePos;
        target.localRotation = _baseRot;
    }

    public void StopShake()
    {
        target.DOKill();
        target.localPosition = _basePos;
        target.localRotation = _baseRot;
    }

}
