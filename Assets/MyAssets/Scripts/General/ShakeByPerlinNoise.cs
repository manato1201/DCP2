using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.VisualScripting;
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

    public async UniTask StartShakeAsync(CancellationToken external)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, external);
        var ct = linked.Token;

        target.localPosition = _basePos;
        target.localRotation = _baseRot;

        // DOTween の標準シェイクで十分ならこちらでOK（timeScale無視にしたければ SetUpdate(true)）
        var t1 = target.DOShakePosition(duration, strengthPos, vibrato: vibrato).SetAutoKill(false).Play();
        var t2 = target.DOShakeRotation(duration, strengthRot, vibrato: vibrato).SetAutoKill(false).Play();

        await UniTask.WhenAll(t1.ToUniTask(cancellationToken: ct), t2.ToUniTask(cancellationToken: ct));

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
