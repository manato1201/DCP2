using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using Sound;

public sealed class TextManager : MonoBehaviour
{
     [Header("UI")]
    [SerializeField] private Button textBoxButton;
    [SerializeField] private TypingText typing;
    [SerializeField] private Button transitionButton;

    [Header("Speaker / Ruby")]
    [SerializeField] private TMP_Text speakerLabel; // Char を表示
    [SerializeField] private TMP_Text rubyLabel;    // 差分ルビを表示（任意）

    [Header("開始CHAP & 遅延")]
    [SerializeField] private SceneTransitData transit;
    [SerializeField] private string defaultChap = "CHAP1";
    [SerializeField, Min(0f)] private float firstLineDelay = 0.3f; // 秒
    [SerializeField] private bool delayOnlyFirst = true;
    [SerializeField] SoundManager sound;

    // 内部
    readonly List<StoryLine> _lines = new(); // StoryCsv 側で Char/Comment/RubyComment を持つ構造体にしておく
    int _index = 0;
    bool _isTyping = false;
    bool _firstPlayed = false;
    string _startChap;

    CancellationTokenSource _ctsScene;
    CancellationTokenSource _ctsLine;

    string _currentChap;
    List<StoryLine> _nextLines;      // 次 CHAP の本文
    string _nextChap;
    bool _next2Exists;               // 次の次 CHAP が存在するか

    public static event System.Action<string> OnCommentChanged;

    void Awake()
    {
        _ctsScene = new();
        if (transitionButton) transitionButton.gameObject.SetActive(false);
        CharacterManager.OnGlowAutoAdvance += HandleGlowAutoAdvance;

        // 起動時に一度だけ決定してキャッシュ
        var chapFromBus = SceneTransitBus.HasChap ? SceneTransitBus.Payload.chap : null;
        var chapFromSO  = transit ? transit.payload.chap : null;   // ScriptableObject 側

        _startChap = !string.IsNullOrEmpty(chapFromBus) ? chapFromBus
            : !string.IsNullOrEmpty(chapFromSO)  ? chapFromSO
            : !string.IsNullOrEmpty(defaultChap) ? defaultChap
            : "CHAP1";


#if UNITY_EDITOR
        var from = !string.IsNullOrEmpty(chapFromBus) ? "bus"
            : !string.IsNullOrEmpty(chapFromSO)  ? "transit"
            : "default";
        Debug.Log($"[TextManager] startChap='{_startChap}' (source={from}) transitObj={(transit ? transit.name : "null")}");
        try {
            var path = UnityEditor.AssetDatabase.GetAssetPath(transit);
            Debug.Log($"[TextManager] transit asset path: {path}");
        } catch {}
#endif

        if (transitionButton) transitionButton.gameObject.SetActive(false);
    }
    void OnEnable()  { if (textBoxButton) textBoxButton.onClick.AddListener(OnClickBox); }
    void OnDisable() { if (textBoxButton) textBoxButton.onClick.RemoveListener(OnClickBox); }

    void OnDestroy()
    {
        _ctsLine?.Cancel(); _ctsScene?.Cancel();
        CharacterManager.OnGlowAutoAdvance -= HandleGlowAutoAdvance;
    }
    void HandleGlowAutoAdvance()
    {
        // タイピング中なら即時表示→次へ
        if (_isTyping) { _ctsLine?.Cancel(); typing?.ForceComplete(); _isTyping = false; }
        NextAsync().Forget();
    }

    private async void Start()
    {
        // CSVロード（Addressables内で）
        await StoryCsv.EnsureLoadedAsync();

        var chapKey = _startChap;

        _lines.Clear();
        var src = StoryCsv.GetLines(chapKey);  // IReadOnlyList<StoryCsv.StoryLine>
        _lines.AddRange(src.Select(l => new StoryLine(l.no, l.commentNo, l.speaker, l.text, l.ruby)));

        if (_lines.Count == 0)
        {
            Debug.LogError($"[TextManager] CHAP '{chapKey}' の行が見つかりません。CSV/アドレッサブル設定を確認してください。");
            return;
        }

        _index = 0;
        _firstPlayed = false;
        await PlayCurrentAsync(_ctsScene.Token);
        // ---- Start() 内の最後に入れる初期化 ----
        _currentChap = !string.IsNullOrEmpty(transit?.payload.chap) ? transit.payload.chap : defaultChap;
        // 先読み
        PrefetchNextChapters(_currentChap);
    }



    private void OnClickBox()
    {
        if (_lines.Count == 0) return;

        if (_isTyping)
        {
            _ctsLine?.Cancel();
            typing?.ForceComplete(); // TypingText の既存APIを使って即時全表示 :contentReference[oaicite:2]{index=2}
            _isTyping = false;
            return;
        }
        sound.PlaySE("SE_TextClick");
        NextAsync().Forget();
    }





// ---- 先読み関数を追加 ----
    void PrefetchNextChapters(string baseChap)
    {
        _nextLines = null;
        _nextChap = null;
        _next2Exists = false;

        string chap1 = NextChapKey(baseChap, +1);
        var lines1 = StoryCsv.GetLines(chap1);
        if (lines1 != null && lines1.Count > 0)
        {
            _nextChap = chap1;
            _nextLines = lines1.Select(l => new StoryLine(l.no, l.commentNo, l.speaker, l.text, l.ruby)).ToList();

            string chap2 = NextChapKey(baseChap, +2);
            var lines2 = StoryCsv.GetLines(chap2);
            _next2Exists = (lines2 != null && lines2.Count > 0);
        }
    }

// "CHAP12" + offset → "CHAP13" のような簡易ジェネレータ
    string NextChapKey(string curr, int offset)
    {
        // CHAP + 数字 を前提にする。数字が取れない場合はそのまま返す。
        // 例: "CHAP1" → 1 を取り、+1 して "CHAP2"
        int num = 0;
        for (int i = curr.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(curr[i])) {
                var head = curr.Substring(0, i + 1);
                var tail = curr.Substring(i + 1);
                if (int.TryParse(tail, out num))
                {
                    return head + (num + offset).ToString();
                }
                return curr; // フォーマット違いは無視
            }
        }
        return curr;
    }


    private async UniTask NextAsync()
    {
        // CHAP内でまだ行が残っているときだけ進める
        if (_index + 1 < _lines.Count)
        {
            _index++;
            await PlayCurrentAsync(_ctsScene.Token);
            return;
        }

        // ここに来るのは「最終行のあと」だけ（最終行時にボタンを出しているので通常は来ない）
        // 念のための保険：ボタンが出ていなければ出しておく
        if (transitionButton)
        {
            transitionButton.gameObject.SetActive(true);
            transitionButton.interactable = true;
        }
        if (textBoxButton) textBoxButton.interactable = false;

    }

    private async UniTask PlayCurrentAsync(CancellationToken ctScene)
    {
        if (!typing) { Debug.LogError("[TextManager] TypingText 未設定"); return; }

        var line = _lines[_index];
        var cno = GetCurrentCommentNo();
        OnCommentChanged?.Invoke(cno);

        if (firstLineDelay > 0f && (!_firstPlayed || !delayOnlyFirst))
        {
            int ms = (int)(firstLineDelay * 1000f);
            await UniTask.Delay(ms, DelayType.UnscaledDeltaTime, PlayerLoopTiming.Update, ctScene);
        }

        // 話者
        if (speakerLabel) speakerLabel.text = string.IsNullOrEmpty(line.speaker) ? "" : line.speaker;

        // ① 埋め込みルビを優先的にパース
        var (plain, rubyInline) = ParseInlineRuby(line.text);

        // ② ルビ表示テキストの決定（埋め込みが無ければ差分抽出へフォールバック）
        string rubyForKanji =
            !string.IsNullOrEmpty(rubyInline)
                ? rubyInline
                : MakeRubyForKanjiOnly(line.text, line.ruby);

        if (rubyLabel) rubyLabel.text = rubyForKanji;

        // ③ 表示用本文は “plain”（埋め込みルビや記号を除去したもの）
        string formatted = plain;

        // 行のキャンセル張り替え
        _ctsLine?.Cancel();
        _ctsLine = CancellationTokenSource.CreateLinkedTokenSource(ctScene);

        _isTyping = true;
        try
        {
            await typing.PlayAsync(formatted, _ctsLine.Token);
        }
        finally
        {
            _isTyping = false;
            _firstPlayed = true;
        }

        // CHAP 最終行なら遷移ボタンを出す（既実装のまま）
        bool isLastLineInThisChap = (_index == _lines.Count - 1);
        if (isLastLineInThisChap)
        {
            if (transitionButton)
            {
                transitionButton.gameObject.SetActive(true);
                transitionButton.interactable = true;
            }
            if (textBoxButton) textBoxButton.interactable = false;
        }
    }

    // Comment と RubyComment の“差分だけ”を返す:
    // 例) Comment="私は学校へ行く" / RubyComment="わたしはがっこうへいく"
    // → "わたし がっこう いく"（単純差分抽出・空白区切り）
    // ひらがな正規化（カタカナ→ひらがな）
static char ToHira(char c)
{
    // カタカナ範囲
    return (c >= 'ァ' && c <= 'ン') ? (char)(c - 'ァ' + 'ぁ') : c;
}
static string ToHira(string s)
{
    if (string.IsNullOrEmpty(s)) return s ?? "";
    var sb = new System.Text.StringBuilder(s.Length);
    foreach (var ch in s) sb.Append(ToHira(ch));
    return sb.ToString();
}
static bool IsKanji(char c)
{
    // CJK統合漢字 + 拡張A のざっくり判定
    return (c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF);
}
static bool IsHira(char c) => (c >= 'ぁ' && c <= 'ゖ') || c == 'ー';
static bool IsKana(char c)
{
    // ひらがな or カタカナ or ー
    return IsHira(ToHira(c));
}

// 「漢字ブロック + 送り仮名」を1単位として、RubyCommentから対応読みを切り出す
// 出力は「がっこう しょうらい」みたいに“漢字ブロックに対応する読み”だけを空白区切りで返す
static string MakeRubyForKanjiOnly(string comment, string ruby)
{
    if (string.IsNullOrEmpty(comment) || string.IsNullOrEmpty(ruby)) return string.Empty;

    string rubyH = ToHira(ruby);
    int rp = 0; // ruby 側ポインタ
    var outParts = new System.Collections.Generic.List<string>();

    int i = 0;
    while (i < comment.Length)
    {
        char c = comment[i];

        // ひらがな/カナ/記号類は、ruby 側の同一文字を“消費”して進めるだけ（出力しない）
        if (!IsKanji(c))
        {
            // 可能なら ruby を同じだけ前進
            char target = ToHira(c);
            if (rp < rubyH.Length && rubyH[rp] == target) rp++;
            i++;
            continue;
        }

        // --- ここから「漢字ブロック + 送り仮名」を取る ---
        int kanjiStart = i;
        while (i < comment.Length && IsKanji(comment[i])) i++;
        int kanjiEnd = i; // [kanjiStart, kanjiEnd) が漢字の塊

        // 送り仮名（直後の連続ひらがな部分）を拾う
        int okStart = i;
        while (i < comment.Length && IsKana(comment[i])) i++;
        int okEnd = i;

        string okuri = ToHira(comment.Substring(okStart, okEnd - okStart)); // 送り仮名（ひらがな化）
        // ruby 側から、この okuri が“次に現れる位置”を探す
        int readStart = rp;
        int readEnd;

        if (okuri.Length > 0)
        {
            // rp 以降で okuri が出てくる直前までが“漢字ブロックの読み”
            int pos = rubyH.IndexOf(okuri, rp, System.StringComparison.Ordinal);
            if (pos >= 0)
            {
                readEnd = pos;
                // okuri 分は次ループに備えてあえて消費しない（上の非漢字分消費で自然に進む）
            }
            else
            {
                // 見つからない：残り全部を読みとみなす（保険）
                readEnd = rubyH.Length;
            }
        }
        else
        {
            // 送り仮名が無い場合：次の“同一のかな文字”が comment 側に出現するまで進めるのは難しい。
            // ヒューリスティック：次の非漢字（かな/記号）が見えるなら、ruby 側も1文字合わせに行く。
            // ここでは簡単に「次の非漢字文字が comment にあり、ruby 側で一致が取れた地点直前」までを読みとする。
            int j = i; // i はすでに漢字塊の直後
            int probeEnd = rubyH.Length;
            if (j < comment.Length)
            {
                char nextC = comment[j];
                char nextH = ToHira(nextC);
                // 次の非漢字が かな系なら、その最初一致手前までを読む
                if (!IsKanji(nextC) && IsKana(nextC))
                {
                    int pos = rubyH.IndexOf(nextH, rp);
                    if (pos >= 0) probeEnd = pos;
                }
            }
            readEnd = probeEnd;
        }

        string reading = (readEnd > readStart && readStart >= 0 && readEnd <= rubyH.Length)
            ? rubyH.Substring(readStart, readEnd - readStart)
            : "";

        if (!string.IsNullOrWhiteSpace(reading))
            outParts.Add(reading.Trim());

        // ruby 側ポインタを読み分だけ進める
        rp = System.Math.Max(rp, readEnd);
        // 送り仮名ぶんはこの後のループ先頭の“非漢字処理”で自然に ruby 側が1:1消費される
    }

    // 余り（comment 終了後、ruby に残渣があってもルビとしては不要なので捨て）
    return string.Join(" ", outParts);
}

/// <summary>
/// 埋め込みルビをパースして
///   plain : ルビを外した本文（例：「今日は学校へ行く」）
///   ruby  : “漢字ブロックのみ”の読みをスペース区切り（例：「がっこう」）
/// サポート記法：{漢字|かな} と ｜漢字《かな》
/// </summary>
static (string plain, string ruby) ParseInlineRuby(string src)
{
    if (string.IsNullOrEmpty(src)) return (string.Empty, string.Empty);

    var plain = new System.Text.StringBuilder(src.Length);
    var ruby  = new System.Text.StringBuilder(32);

    int i = 0;
    while (i < src.Length)
    {
        char c = src[i];

        // 記法A: {漢字|かな}
        if (c == '{')
        {
            int bar = src.IndexOf('|', i + 1);
            int end = src.IndexOf('}', (bar >= 0 ? bar + 1 : i + 1));
            if (bar > 0 && end > bar)
            {
                string baseText = src.Substring(i + 1, bar - (i + 1));
                string reading  = src.Substring(bar + 1, end - (bar + 1));

                plain.Append(baseText);
                if (ruby.Length > 0) ruby.Append(' ');
                ruby.Append(reading);

                i = end + 1;
                continue;
            }
        }

        // 記法B: ｜漢字《かな》
        if (c == '｜') // U+FF5C FULLWIDTH VERTICAL LINE
        {
            int startBase = i + 1;
            int openRb = src.IndexOf('《', startBase);
            int closeRb = (openRb >= 0) ? src.IndexOf('》', openRb + 1) : -1;

            if (openRb > startBase && closeRb > openRb)
            {
                string baseText = src.Substring(startBase, openRb - startBase);
                string reading  = src.Substring(openRb + 1, closeRb - (openRb + 1));

                plain.Append(baseText);
                if (ruby.Length > 0) ruby.Append(' ');
                ruby.Append(reading);

                i = closeRb + 1;
                continue;
            }
        }

        // どの記法でもなければ通常文字として追加
        plain.Append(c);
        i++;
    }

    return (plain.ToString(), ruby.ToString());
}
// 現在行から CommentNo を取り出すヘルパ
private string GetCurrentCommentNo()
{
    // 既存の _lines[_index] が StoryCsv の行を持つ前提
    // フィールド名は実装に合わせて置換
    return _lines[_index].commentNo; // 例: "CHAP1-3"
}

    // StoryCsv から受ける行データの想定
    public readonly struct StoryLine
    {
        public readonly int no;
        public readonly string commentNo;
        public readonly string speaker; // Char
        public readonly string text;    // Comment
        public readonly string ruby;    // RubyComment
        public StoryLine(int no, string cno, string spk, string txt, string rub) { this.no=no; this.commentNo=cno; speaker=spk; text=txt; ruby=rub; }
    }
}
