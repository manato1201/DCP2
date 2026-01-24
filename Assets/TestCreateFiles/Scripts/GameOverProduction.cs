using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameOverProduction : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image _bannerImage;
    [SerializeField] private TextMeshProUGUI _gameOverText;
    [SerializeField] private CanvasGroup _backgroundGroup; // 背景を暗くする場合用

    [Header("Fill Settings")]
    [SerializeField] private float _fillDuration = 1.0f;

    [Header("Pulse Settings")]
    [SerializeField] private Color _baseColor = new Color(0.2f, 0f, 0f, 1f); // 暗い赤
    [SerializeField] private Color _pulseColor = new Color(0.6f, 0f, 0f, 1f); // 警告のような赤
    [SerializeField] private float _pulseSpeed = 2.0f;

    [Header("Text Animation Settings")]
    [SerializeField] private float _textFallDuration = 0.6f;
    [SerializeField] private float _shakeIntensity = 5.0f;

    private CancellationTokenSource _cts;
    private Vector3 _originalTextPosition;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayGameOverAnimationAsync().Forget();
        }
    }

    private void Awake()
    {
        if (_gameOverText != null)
        {
            _originalTextPosition = _gameOverText.transform.localPosition;
            _gameOverText.gameObject.SetActive(false);
        }

        if (_bannerImage != null)
        {
            _bannerImage.fillAmount = 0f;
            _bannerImage.color = _baseColor;
        }

        if (_backgroundGroup != null)
        {
            _backgroundGroup.alpha = 0f;
        }
    }

    public async UniTask PlayGameOverAnimationAsync()
    {
        ResetEffect();
        _cts = new CancellationTokenSource();
        var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, this.GetCancellationTokenOnDestroy());

        try
        {
            // 1. 背景をじわっと暗くしつつ、バナーを広げる
            await UniTask.WhenAll(
                FadeBackgroundAsync(0.7f, _fillDuration, linkedToken.Token),
                FillBannerAsync(_fillDuration, linkedToken.Token)
            );

            // 2. 鈍い鼓動のような明滅を開始
            StartPulseLoop(linkedToken.Token).Forget();

            // 3. 文字が上から落ちてきて、着地時に少し震える
            await AnimateTextFallAsync(linkedToken.Token);
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
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _bannerImage.fillAmount = Mathf.Clamp01(elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _bannerImage.fillAmount = 1f;
    }

    private async UniTask FadeBackgroundAsync(float targetAlpha, float duration, CancellationToken ct)
    {
        if (_backgroundGroup == null) return;
        float startAlpha = _backgroundGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _backgroundGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _backgroundGroup.alpha = targetAlpha;
    }

    private async UniTask AnimateTextFallAsync(CancellationToken ct)
    {
        if (_gameOverText == null) return;
        _gameOverText.gameObject.SetActive(true);

        Vector3 startPos = _originalTextPosition + new Vector3(0, 100f, 0); // 100px上から
        float elapsed = 0f;

        // 落下アニメーション
        while (elapsed < _textFallDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _textFallDuration;
            // 落下（少し加速するような動き）
            float easeInQuad = t * t;
            _gameOverText.transform.localPosition = Vector3.Lerp(startPos, _originalTextPosition, easeInQuad);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        // 着地後の振動（シェイク）演出
        float shakeElapsed = 0f;
        float shakeDuration = 0.3f;
        while (shakeElapsed < shakeDuration)
        {
            shakeElapsed += Time.deltaTime;
            float strength = 1.0f - (shakeElapsed / shakeDuration);
            _gameOverText.transform.localPosition = _originalTextPosition + (UnityEngine.Random.insideUnitSphere * _shakeIntensity * strength);
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
        _gameOverText.transform.localPosition = _originalTextPosition;
    }

    private async UniTaskVoid StartPulseLoop(CancellationToken ct)
    {
        if (_bannerImage == null) return;
        try
        {
            while (!ct.IsCancellationRequested && _bannerImage != null)
            {
                // クリア時よりゆっくりとした明滅
                float t = (Mathf.Sin(Time.time * _pulseSpeed) + 1f) / 2f;
                _bannerImage.color = Color.Lerp(_baseColor, _pulseColor, t);
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

        if (_bannerImage != null)
        {
            _bannerImage.fillAmount = 0f;
            _bannerImage.color = _baseColor;
        }

        if (_gameOverText != null)
        {
            _gameOverText.gameObject.SetActive(false);
            _gameOverText.transform.localPosition = _originalTextPosition;
        }

        if (_backgroundGroup != null) _backgroundGroup.alpha = 0f;
    }

    private void OnDestroy() => ResetEffect();

    [ContextMenu("Play Test")]
    private void PlayTest() => PlayGameOverAnimationAsync().Forget();
}
