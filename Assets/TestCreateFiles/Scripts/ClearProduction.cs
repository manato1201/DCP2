using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ClearProduction : MonoBehaviour
{
    [Header("State Settings")]
    public bool isGameOver = false;

    [Header("Components")]
    [SerializeField] private Image _bannerImage;
    [SerializeField] private Image _backgroundImage; // 新しく追加：背景パネルなど
    [SerializeField] private TextMeshProUGUI _clearText;

    [Header("Fill/Fade Settings")]
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

        ResetEffect(); // 初期状態をセット
    }

    public async UniTask PlayFullAnimationAsync()
    {
        ResetEffect();
        _cts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, this.GetCancellationTokenOnDestroy());

        try
        {
            // 1. 帯のFill ＋ 背景のフェード（ゲームオーバー時のみ）を同時に実行
            await UniTask.WhenAll(
                FillBannerAsync(_fillDuration, linkedToken.Token),
                FadeBackgroundAsync(_fillDuration, linkedToken.Token)
            );

            // 2. 帯の色の明滅
            StartShiningLoop(linkedToken.Token).Forget();

            // 3. 文字のポップアップ
            await AnimateTextScaleAsync(linkedToken.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            linkedToken.Dispose();
        }
    }

    // 既存のFill演出（そのまま）
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

    // 背景のフェードイン処理
    private async UniTask FadeBackgroundAsync(float duration, CancellationToken ct)
    {
        if (_backgroundImage == null || !isGameOver) return;

        _backgroundImage.gameObject.SetActive(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            SetImageAlpha(_backgroundImage, alpha);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        SetImageAlpha(_backgroundImage, 0.5f);
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
            float curve = Mathf.Sin(t * Mathf.PI * 0.8f) * _overshootMultiplier;
            float finalT = Mathf.Lerp(curve, 1.0f, t);

            _clearText.transform.localScale = _originalTextScale * finalT;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _clearText.transform.localScale = _originalTextScale;
    }

    private async UniTaskVoid StartShiningLoop(CancellationToken ct)
    {
        if (_bannerImage == null) return;

        try
        {
            while (!ct.IsCancellationRequested && _bannerImage != null)
            {
                float t = (Mathf.Sin(Time.time * _shineSpeed) + 1f) / 2f;
                _bannerImage.color = Color.Lerp(_baseColor, _flashColor, t);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ResetEffect()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;

        // バナーのリセット
        if (_bannerImage != null)
        {
            _bannerImage.fillAmount = 0f;
            _bannerImage.color = _baseColor;
        }

        // 背景のリセット
        if (_backgroundImage != null)
        {
            SetImageAlpha(_backgroundImage, 0f);
            _backgroundImage.gameObject.SetActive(false);
        }

        // テキストのリセット
        if (_clearText != null)
        {
            _clearText.gameObject.SetActive(false);
            _clearText.transform.localScale = _originalTextScale;
        }
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void OnDestroy() => ResetEffect();

    [ContextMenu("Play Test")]
    private void PlayTest() => PlayFullAnimationAsync().Forget();
}
