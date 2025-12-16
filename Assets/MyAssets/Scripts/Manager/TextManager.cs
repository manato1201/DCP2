using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public sealed class TextManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button textBoxButton;     // クリック受付
    [SerializeField] private TypingText typing;        // 文字送り（TypingText は ForceComplete() 実装必須）
    [SerializeField] private Button transitionButton;  // 最後に出すボタン

    [Header("開始CHAP")]
    [SerializeField] private SceneTransitData transit; // payload から chap を受ける（string か int 想定）
    [SerializeField] private string defaultChap = "CHAP1"; // 例: "1" や "CHAP1"。StoryCsv 側のキー仕様に合わせる

    // 内部
    readonly List<StoryLine> _lines = new();
    int _index = 0;
    bool _isTyping = false;

    // シーン生存用（破棄時キャンセル）
    CancellationTokenSource _ctsScene;
    // 行ごとのスキップ用（毎行作り直す）
    CancellationTokenSource _ctsLine;

    void Awake()
    {
        _ctsScene = new();
        if (transitionButton) transitionButton.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        if (textBoxButton) textBoxButton.onClick.AddListener(OnClickBox);
    }

    void OnDisable()
    {
        if (textBoxButton) textBoxButton.onClick.RemoveListener(OnClickBox);
    }

    void OnDestroy()
    {
        _ctsLine?.Cancel();
        _ctsScene?.Cancel();
    }

    private async void Start()
    {
        // CSVロード（Addressables 内部で実施）
        await StoryCsv.EnsureLoadedAsync();

        // 開始CHAP（Payload に chap が無ければ default）
        var chapKey = !string.IsNullOrEmpty(transit?.payload.chap) ? transit.payload.chap : defaultChap;

        _lines.Clear();
        _lines.AddRange(StoryCsv.GetLines(chapKey));

        if (_lines.Count == 0)
        {
            Debug.LogError($"[TextManager] CHAP '{chapKey}' の行が見つかりません。CSV/アドレッサブル設定を確認してください。");
            return;
        }

        _index = 0;
        await PlayCurrentAsync(_ctsScene.Token);
    }

    // クリック
    private void OnClickBox()
    {
        if (_lines.Count == 0) return;

        if (_isTyping)
        {
            // タイプ中 → スキップ（行用CTSをCancel→TypingTextに全表示を指示）
            _ctsLine?.Cancel();
            typing?.ForceComplete();
            _isTyping = false;
            return;
        }

        // 次へ
        NextAsync().Forget();
    }

    private async UniTask NextAsync()
    {
        _index++;
        if (_index >= _lines.Count)
        {
            // 最後の子番号 → 遷移ボタンON、以降クリック無効
            if (transitionButton)
            {
                transitionButton.gameObject.SetActive(true);
                transitionButton.interactable = true;
            }
            if (textBoxButton) textBoxButton.interactable = false;
            return;
        }

        await PlayCurrentAsync(_ctsScene.Token);
    }

    private async UniTask PlayCurrentAsync(CancellationToken ctScene)
    {
        if (!typing)
        {
            Debug.LogError("[TextManager] TypingText 未設定");
            return;
        }

        var line = _lines[_index];

        // 必要なら整形
        var formatted = CommentFormatter.FormatForTextbox(line.text, FormatCommentLineLen.MoreGameTraining);

        // 古い行のスキップを確実に停止してから新しいCTSを張る
        _ctsLine?.Cancel();
        _ctsLine = CancellationTokenSource.CreateLinkedTokenSource(ctScene);

        _isTyping = true;
        try
        {
            // 1文をタイプ表示（TypingText 側は PlayAsync(string, CancellationToken) を実装している想定）
            await typing.PlayAsync(formatted, _ctsLine.Token);
        }
        finally
        {
            _isTyping = false;
        }
    }
}
