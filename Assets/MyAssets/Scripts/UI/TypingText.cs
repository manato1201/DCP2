using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TypingText : MonoBehaviour
{
    [Header("表示先")]
    [SerializeField] private TMP_Text[] targets;
    [SerializeField] private float charInterval = 0.03f; // 秒
    [SerializeField] private bool obeyTimeScale = false; // WebGL配慮で既定はUnscaled

    [Header("CSV（AddressablesのTextAsset）")]
    [SerializeField] private AssetReference csvAsset;

    // キー→本文。キーは "chap-no" 形式（例: "1-3"）
    private Dictionary<string, string> _db;
    private bool _dbLoaded;
    private AsyncOperationHandle<TextAsset> _csvHandle;

    // 表示中のキャンセル（スキップ用）
    private CancellationTokenSource _ctsTyping;

    void OnDestroy()
    {
        _ctsTyping?.Cancel();
        if (_dbLoaded && _csvHandle.IsValid())
        {
            // 運用で常駐させたいならこのReleaseは外す
            Addressables.Release(_csvHandle);
        }
    }

    // -------- 公開API --------

    // そのままキー or 生テキスト
    public async UniTask PlayAsync(string keyOrText, CancellationToken ct = default)
    {
        if (!_dbLoaded && csvAsset.RuntimeKeyIsValid())
            await EnsureDbLoadedAsync(ct);

        var text = (_dbLoaded && _db.TryGetValue(keyOrText, out var v)) ? v : keyOrText;
        await TypeToAllTargetsAsync(text, ct);
    }

    // CHAP & No 指定（例: chap=1, no=3 -> "1-3"）
    public UniTask PlayAsync(int chap, int no, CancellationToken ct = default)
        => PlayAsync(MakeKey(chap, no), ct);

    // 途中で全表示（TextManagerのクリックで呼ぶ）
    public void ForceComplete()
    {
        _ctsTyping?.Cancel(); // 現在のタイプを止める
        foreach (var t in targets) if (t) t.maxVisibleCharacters = int.MaxValue;
    }

    // -------- 内部処理 --------

    private async UniTask EnsureDbLoadedAsync(CancellationToken ct)
    {
        _db = new Dictionary<string, string>(256);

        _csvHandle = csvAsset.LoadAssetAsync<TextAsset>();
        var ta = await _csvHandle.Task;
        if (!ta)
        {
            Debug.LogWarning("[TypingText] CSV(TextAsset) が null。キー参照はスキップ。");
            _dbLoaded = false;
            return;
        }
        ParseCsvToDict_CSV(ta.text, _db);
        _dbLoaded = true;
    }

    // 期待する列名:
    //   CHAP, No, Comment  （Comment が無ければ Text を見る）
    // キーは "CHAP-No"
    private void ParseCsvToDict_CSV(string csvText, System.Collections.Generic.Dictionary<string, string> outDict)
    {
        outDict.Clear();
        if (string.IsNullOrEmpty(csvText)) return;

        using var sr = new System.IO.StringReader(csvText);

        // ---- ヘッダー行 ----
        var headerLine = sr.ReadLine();
        if (headerLine == null)
        {
            Debug.LogError("[TypingText] CSV が空です。");
            return;
        }

        var header = TextTemplateUtil.SplitCsvLine(headerLine);
        // 期待：No, CommentNo, Comment
        int idxNo        = header.FindIndex(h => string.Equals(h, "No", System.StringComparison.OrdinalIgnoreCase));
        int idxCommentNo = header.FindIndex(h => string.Equals(h, "CommentNo", System.StringComparison.OrdinalIgnoreCase));
        int idxComment   = header.FindIndex(h => string.Equals(h, "Comment", System.StringComparison.OrdinalIgnoreCase));

        if (idxNo < 0 || idxCommentNo < 0 || idxComment < 0)
        {
            Debug.LogError("[TypingText] CSV ヘッダが期待と違う（No, CommentNo, Comment が必要）");
            return;
        }

        // ---- データ行 ----
        string line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = TextTemplateUtil.SplitCsvLine(line);
            // 足りない列はスキップ
            if (cols.Count <= idxComment) continue;

            var noStr        = cols[idxNo].Trim();
            var commentNoStr = cols[idxCommentNo].Trim();
            var comment      = cols[idxComment];

            if (string.IsNullOrEmpty(noStr) || string.IsNullOrEmpty(commentNoStr)) continue;

            // キーは「No-CommentNo」に統一（例: 1-3）
            var key = $"{noStr}-{commentNoStr}";
            if (!outDict.ContainsKey(key))
            {
                outDict.Add(key, comment);
            }
            else
            {
                // 重複は最後を優先する or 無視する。必要ならログ
                outDict[key] = comment;
            }
        }
    }

    private static string MakeKey(int chap, int no) => $"{chap}-{no}";

    // すべてのターゲットに“同時に”タイプ表示
    private async UniTask TypeToAllTargetsAsync(string text, CancellationToken external)
    {
        foreach (var t in targets) if (t) { t.text = text; t.maxVisibleCharacters = 0; } // 先に文字列を入れる

        _ctsTyping?.Cancel();
        _ctsTyping = CancellationTokenSource.CreateLinkedTokenSource(external);
        var ct = _ctsTyping.Token;

        var tasks = new List<UniTask>(targets.Length);
        foreach (var t in targets)
            tasks.Add(TypeCoroutineAsync(t, text, charInterval, obeyTimeScale, ct));

        await UniTask.WhenAll(tasks);
    }

    // TMPの maxVisibleCharacters を使ったタイプ表示（高速・GC少なめ）
    private static async UniTask TypeCoroutineAsync(TMP_Text target, string content, float interval, bool obeyScale, CancellationToken ct)
    {
        if (!target) return;

        target.ForceMeshUpdate();
        target.maxVisibleCharacters = 0;

        // 既に target.text に全文が入っている前提
        int total = target.textInfo.characterCount;
        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();
            target.maxVisibleCharacters = i + 1;

            if (interval > 0f)
            {
                if (obeyScale) await UniTask.Delay((int)(interval * 1000f), cancellationToken: ct);
                else await UniTask.Delay((int)(interval * 1000f), DelayType.UnscaledDeltaTime, cancellationToken: ct);
            }
        }
    }

    // ダブルクォート対応の簡易スプリット（既存流用）
    private static string[] SplitCsvLine(string line)
    {
        var list = new List<string>();
        var sb = new StringBuilder();
        bool inQ = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; }
                else inQ = !inQ;
            }
            else if (c == ',' && !inQ) { list.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        list.Add(sb.ToString());
        return list.ToArray();
    }
}
