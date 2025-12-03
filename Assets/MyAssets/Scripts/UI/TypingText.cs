using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TypingText : MonoBehaviour
{
    [Header("表示先とタイミング")]
    [SerializeField] private TMP_Text[] targets;
    [SerializeField] private float charInterval = 0.03f; // 秒
    [SerializeField] private bool obeyTimeScale = true;

    [Header("CSV（Addressables）")]
    [SerializeField] private AssetReference csvAsset; // ← CSVをAddressables登録して割り当て
    private Dictionary<string, string> _db;                   // キー→本文
    private bool _dbLoaded;

    CancellationTokenSource _cts;

    void Awake() => _cts = new();
    void OnDestroy() => _cts?.Cancel();

    public async UniTask PlayAsync(string keyOrText)
    {
        // 1) DBを必要時ロード
        if (!_dbLoaded && csvAsset.RuntimeKeyIsValid())
            await EnsureDbLoadedAsync(_cts.Token);

        // 2) キーが存在するなら本文へ展開。無ければ引数そのままを本文扱い
        var text = (_dbLoaded && _db.TryGetValue(keyOrText, out var val)) ? val : keyOrText;

        // 3) 全ターゲットをクリアしてからタイプ開始
        foreach (var t in targets) if (t) t.text = "";

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var ct = linked.Token;

        foreach (var t in targets)
            await TypeToAsync(t, text, charInterval, obeyTimeScale, ct);
    }

    // -------- CSV読込・パース --------
    private async UniTask EnsureDbLoadedAsync(CancellationToken ct)
    {
        _db = new Dictionary<string, string>(256);
        var handle = csvAsset.LoadAssetAsync<TextAsset>();
        var ta = await handle.Task;
        if (ta == null)
        {
            Debug.LogWarning("[TypingText] CSV(TextAsset) が null。キー参照はスキップします。");
            _dbLoaded = false;
            return;
        }
        ParseCsvToDict(ta.text, _db);
        _dbLoaded = true;
        // Addressables.Release(handle); // 常時使うなら保持でOK。サイズが辛ければReleaseへ
    }

    // 最小限のCSVパーサ（想定：1行目ヘッダ。キー列名に 'CommentNo'、本文列名に 'Comment'）
    private static void ParseCsvToDict(string csv, Dictionary<string,string> dict)
    {
        if (string.IsNullOrEmpty(csv)) return;

        using var reader = new System.IO.StringReader(csv);
        string? line = reader.ReadLine(); // header
        if (line == null) return;

        // ヘッダ走査（単純CSV想定。必要なら正規のCSVパーサに置換）
        var headers = line.Split(',');
        int keyIdx = System.Array.FindIndex(headers, h => h.Trim().Equals("CommentNo"));
        int txtIdx = System.Array.FindIndex(headers, h => h.Trim().Equals("Comment"));
        if (keyIdx < 0 || txtIdx < 0) { keyIdx = 0; txtIdx = 1; } // フォールバック

        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = SplitCsvLine(line);
            if (cols.Length <= System.Math.Max(keyIdx, txtIdx)) continue;

            var key = cols[keyIdx].Trim();
            var val = cols[txtIdx];
            if (!string.IsNullOrEmpty(key))
                dict[key] = val;
        }
    }

    // ダブルクォート対応の簡易スプリット
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
                // 連続二重引用符はエスケープ
                if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; }
                else inQ = !inQ;
            }
            else if (c == ',' && !inQ)
            {
                list.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        list.Add(sb.ToString());
        return list.ToArray();
    }

    // -------- タイプ処理 --------
    static async UniTask TypeToAsync(TMP_Text target, string content, float interval, bool obeyScale, CancellationToken ct)
    {
        if (!target) return;
        var sb = new StringBuilder(content.Length);
        for (int i = 0; i < content.Length; i++)
        {
            sb.Append(content[i]);
            target.text = sb.ToString();

            if (interval > 0f)
            {
                if (obeyScale) await UniTask.Delay((int)(interval * 1000f), cancellationToken: ct);
                else await UniTask.Delay((int)(interval * 1000f), DelayType.UnscaledDeltaTime, cancellationToken: ct);
            }
        }
    }
}
