using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 依存: PartnerCommentCatalog / CommentFormatter / TypingText
public sealed class TextManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button textBoxButton;     // テキストボックス（クリック受付）
    [SerializeField] private TypingText typing;        // 文字送り担当（targetsはTypingText側で設定）
    [SerializeField] private Button transitionButton;  // ← 最後に表示するボタン（最初は非表示にしておく）

    [Header("CSV 取得条件")]
    [SerializeField] private int characterId = 0;
    [SerializeField] private PartnerCommentState[] flow;  // 再生順（空ならデフォルト採用）

    [Header("表示整形")]
    [SerializeField] private int maxLineLen = 14; // 句読点優先の最大行長

    // 内部状態
    private readonly List<string> _lines = new();
    private int _index;
    private bool _isTyping;
    private string _currentFullText;
    private CancellationTokenSource _cts;

    void Awake()
    {
        _cts = new();
        if (transitionButton) transitionButton.gameObject.SetActive(false); // 最初は隠す
    }

    void OnEnable()
    {
        if (textBoxButton) textBoxButton.onClick.AddListener(OnClickTextBox);
    }

    void OnDisable()
    {
        if (textBoxButton) textBoxButton.onClick.RemoveListener(OnClickTextBox);
    }

    void OnDestroy()
    {
        _cts?.Cancel();
    }

    async void Start()
    {
        // CSV→行列構築→先頭をタイプ開始
        await BuildFlowAsync(_cts.Token);
        _index = 0;
        await PlayCurrentAsync(_cts.Token);
    }

    // ---------------- CSV→文面構築 ----------------
    private async UniTask BuildFlowAsync(CancellationToken ct)
    {
        _lines.Clear();

        // デフォルトの流れ（空なら）
        if (flow == null || flow.Length == 0)
        {
            flow = new[]
            {
                PartnerCommentState.StartTraining,
                PartnerCommentState.FinishTraining,
                PartnerCommentState.CompleteTraining
            };
        }

        foreach (var st in flow)
        {
            var raw = await PartnerCommentCatalog.GetCommentAsync(characterId, st);
            if (string.IsNullOrWhiteSpace(raw)) continue;

            var formatted = CommentFormatter.FormatForTextbox(raw, maxLineLen);
            _lines.Add(formatted);
            await UniTask.Yield(ct);
        }
    }

    // ---------------- クリック処理 ----------------
    private void OnClickTextBox()
    {
        if (_lines.Count == 0) return;

        if (_isTyping)
        {
            // 1回目クリック：全文即時表示
            ForceShowFullText();
            _isTyping = false;
            return;
        }

        // 2回目クリック：次の文章へ
        NextAsync().Forget();
    }

    private async UniTask NextAsync()
    {
        _index++;
        if (_index >= _lines.Count)
        {
            // 最後のIDを表示済み → 遷移ボタンを出す（Scene遷移はUIFlow側で）
            if (transitionButton)
            {
                transitionButton.gameObject.SetActive(true);
                transitionButton.interactable = true;
            }
            // テキストボックスは不要なら押せなくしておく
            if (textBoxButton) textBoxButton.interactable = false;
            return;
        }

        await PlayCurrentAsync(_cts.Token);
    }

    // ---------------- 1文のタイプ再生 ----------------
    private async UniTask PlayCurrentAsync(CancellationToken ct)
    {
        if (!typing)
        {
            Debug.LogError("[TextManager] TypingText が未設定");
            return;
        }

        // 次文開始時は遷移ボタンを隠す（再利用時の保険）
        if (transitionButton) transitionButton.gameObject.SetActive(false);
        if (textBoxButton) textBoxButton.interactable = true;

        _currentFullText = _lines[_index];
        _isTyping = true;

        try
        {
            await typing.PlayAsync(_currentFullText); // TypingTextに任せる
        }
        //catch (OperationCanceledException) { /* 無視 */ }
        finally
        {
            _isTyping = false;
        }
    }

    // ---------------- 即時全文表示 ----------------
    private void ForceShowFullText()
    {
        if (!typing) return;

        // TypingText の targets と同じ階層にある TMP_Text へ一括反映
        var targets = GetComponentsInChildren<TMP_Text>(includeInactive: true);
        foreach (var t in targets) if (t) t.text = _currentFullText;
    }
}
