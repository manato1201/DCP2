using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClearProduction : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _bannerImage;
    [SerializeField] private TextMeshProUGUI _clearText;

    [Header("Fill Settings")]
    [SerializeField] private float _fillDuration = 0.8f;

    [Header("Shine Settings")]
    [SerializeField] private Color _baseColor = Color.white;
    [SerializeField] private Color _flashColor = new Color(1f, 0.95f, 0.6f);
    [SerializeField] private float _shineSpeed = 5.0f;

    [Header("Text Animation Settings")]
    [SerializeField] private float _textScaleDuration = 0.5f;
    [SerializeField] private float _overshootMultiplier = 1.5f;

    private CancellationTokenSource _cts;
    private Vector3 _originalTextScale;

    private void Awake()
    {
        if (_clearText != null)
        {
            _originalTextScale = _clearText.transform.localScale;
            _clearText.gameObject.SetActive(false);
        }

        if (_bannerImage != null)
        {
            // 最初に透明にするか、Fillを0にする設定
            _bannerImage.fillAmount = 0f;
        }
    }



    public async UniTask PlayFullAnimationAsync()
    {
        ResetEffect();
        _cts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, this.GetCancellationTokenOnDestroy());

        try
        {
            // 1. 帯のFill（塗りつぶし）演出
            await FillBannerAsync(_fillDuration, linkedToken.Token);

            // 2. 帯の色の明滅（キラキラ）開始
            StartShiningLoop(linkedToken.Token).Forget();

            // 3. 文字のポップアップ登場
            await AnimateTextScaleAsync(linkedToken.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            linkedToken.Dispose();
        }
    }

    private async UniTask FillBannerAsync(float duration, CancellationToken ct)
    {
        if (_bannerImage == null) return;

        _bannerImage.fillAmount = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bannerImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        _bannerImage.fillAmount = 1f;
    }

    private async UniTask AnimateTextScaleAsync(CancellationToken ct)
    {
        if (_clearText == null) return;
        _clearText.gameObject.SetActive(true);
        _clearText.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < _textScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _textScaleDuration;
            // 弾むような動き（イージング）
            float curve = Mathf.Sin(t * Mathf.PI * 0.8f) * _overshootMultiplier;
            float finalT = Mathf.Lerp(curve, 1.0f, t);

            _clearText.transform.localScale = _originalTextScale * finalT;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _clearText.transform.localScale = _originalTextScale;
    }

    private async UniTaskVoid StartShiningLoop(CancellationToken ct)
    {
        // _bannerImage 自体が null（未アサイン）なら即終了
        if (_bannerImage == null) return;

        try
        {
            // キャンセルリクエストがなく、かつ Image が破壊されていない間ループ
            while (!ct.IsCancellationRequested && _bannerImage != null)
            {
                float t = (Mathf.Sin(Time.time * _shineSpeed) + 1f) / 2f;

                // 書き換え直前にもう一度チェック（UnityのObjectとしての生存確認）
                if (_bannerImage == null) break;

                _bannerImage.color = Color.Lerp(_baseColor, _flashColor, t);

                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセル時は静かに終了
        }
    }
    private void ResetEffect()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        if (_bannerImage != null)
        {
            _bannerImage.fillAmount = 0f;
            _bannerImage.color = _baseColor;
        }

        if (_clearText != null)
        {
            _clearText.gameObject.SetActive(false);
            _clearText.transform.localScale = _originalTextScale;
        }
    }

    private void OnDestroy() => ResetEffect();

    [ContextMenu("Play Test")]
    private void PlayTest() => PlayFullAnimationAsync().Forget();
}
