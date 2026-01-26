using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using NUnit.Framework;
using Sound;

public class BannerController : MonoBehaviour
{
    public RectTransform content;
    public float bannerWidth = 800f;
    public Image[] dots;

    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;


    [Header("Animation Settings")]
    public float animationDuration = 0.5f;
    public float startPositionY = 1000f; // 画面外の高さ
    public float minScale = 0.8f;        // 縮小時のサイズ

    public RectTransform bannerMainPanel;

    // 2. Buttonの中にあるテキスト（TextMeshPro）への参照
    // インスペクターで直接アタッチするか、Startで自動取得します
    public TextMeshProUGUI nextButtonText;

    private int currentIndex = 0;
    private bool isFirst = false;
    public bool IsClosed { get; private set; } = false;
    [SerializeField] SoundManager soundManager;
    void Start()
    {
        // もしインスペクターでアタッチしていない場合、自動取得を試みる
        if (nextButtonText == null && rightButton != null)
        {
            nextButtonText = rightButton.GetComponentInChildren<TextMeshProUGUI>();
        }

        UpdateUI(true);
    }
    void Update()
    {
        // PC向け：左右の矢印キーでも操作可能にする
        if (Input.GetKeyDown(KeyCode.RightArrow)) OnClickRight();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) OnClickLeft();
    }

    public void OnClickRight()
    {
        soundManager.PlaySE("SE_Align");
        if (currentIndex < dots.Length - 1)
        {
            currentIndex++;
            UpdateUI();
        }
        else
        {
            // 最後のページでさらに「次へ」を押したら閉じる
            CloseBanner();
        }
    }

    public void OnClickLeft()
    {
        soundManager.PlaySE("SE_Align");
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }



    // 引数で即時移動かアニメーション移動かを選べるように変更
    void UpdateUI(bool immediate = false)
    {
        // 修正点：バナーが中心にくるための座標計算
        // 1枚目(index 0)のとき x = 0
        // 2枚目(index 1)のとき x = -800
        float targetPos = -currentIndex * bannerWidth;

        if (immediate)
        {
            content.DOKill();
            // Y軸の現在値を維持しつつX軸だけ上書き
            content.anchoredPosition = new Vector2(targetPos, content.anchoredPosition.y);
        }
        else
        {
            content.DOAnchorPosX(targetPos, 0.3f).SetEase(Ease.OutQuint);
        }

        // --- 以下、ボタン有効化とテキスト処理（前回のまま） ---
        if (leftButton != null) leftButton.interactable = (currentIndex > 0);
        if (rightButton != null) rightButton.interactable = true;

        if (nextButtonText != null)
        {
            nextButtonText.text = (currentIndex == dots.Length - 1) ? "とじる" : "つぎへ";
        }

        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].color = (i == currentIndex) ? Color.white : new Color(1, 1, 1, 0.3f);
        }
    }

    // --- バナーを開く演出 ---
    public void OpenBanner()
    {
        if (isFirst)
        {
            soundManager.PlaySE("SE_Align");
        }
        isFirst = true;
        IsClosed = false;

        // 1. 初期状態の設定
        gameObject.SetActive(true);
        currentIndex = 0;
        UpdateUI(true); // 座標リセット

        // アニメーション開始前の状態を作る
        bannerMainPanel.DOKill(); // 実行中のアニメーションがあれば停止
        bannerMainPanel.anchoredPosition = new Vector2(0, startPositionY);
        bannerMainPanel.localScale = Vector3.one * minScale;

        // 2. シーケンス（連続演出）の作成
        Sequence openSequence = DOTween.Sequence();

        openSequence
            // まずは上から中央へ移動
            .Append(bannerMainPanel.DOAnchorPosY(0, animationDuration).SetEase(Ease.OutQuad))
            // 移動が終わったら、元のサイズ（1.0）へ拡大
            .Append(bannerMainPanel.DOScale(1f, 0.2f).SetEase(Ease.OutBack));
    }

    // --- バナーを閉じる演出 ---
    public void CloseBanner()
    {
        // 1. 少し縮小する
        bannerMainPanel.DOScale(minScale, animationDuration * 0.5f).SetEase(Ease.InBack);

        // 2. 上に上がっていく
        bannerMainPanel.DOAnchorPosY(startPositionY, animationDuration)
            .SetEase(Ease.InBack)
            .SetDelay(0.1f) // 少し縮小してから動かすための微調整
            .OnComplete(() =>
            {
                IsClosed = true;
                // アニメーション完了後に非表示にする
                gameObject.SetActive(false);
            });
    }
}
