using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class StoryLine
{
    public int no;               // CSVのNo
    public string chap;          // "CHAP1"
    public int index;            // 1,2,3...
    public string speaker;       // "H" 等（無ければ空）
    public string text;          // 本文
    public string[] extra;       // 任意の追加列（SE名など）
}

public static class StoryCsv
{
    // Addressables で参照するキー（ラベルでも可）
    public const string AddressablesKey = "CommuLog";

    // chap → index昇順の行
    private static readonly Dictionary<string, List<StoryLine>> ByChap = new();
    private static bool _loaded;

    public static async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        TextAsset ta = null;
        try
        {
            var h = Addressables.LoadAssetAsync<TextAsset>(AddressablesKey);
            ta = await h.Task;
        }
        catch (Exception e)
        {
            Debug.LogError($"[StoryCsv] Addressables 読み込み失敗: {e.Message}");
        }

        if (ta == null)
        {
            Debug.LogError("[StoryCsv] CSV(TextAsset)がAddressablesで見つかりません。キー(ラベル) 'CommuLog' をCSVに付けてビルドしてください。");
            return;
        }

        ParseCsv(ta.text);
        _loaded = true;
    }

    public static IReadOnlyList<StoryLine> GetLines(string chap)
    {
        if (string.IsNullOrEmpty(chap)) return Array.Empty<StoryLine>();
        return ByChap.TryGetValue(chap, out var list) ? list : Array.Empty<StoryLine>();
    }

    // ------------- CSV 解析 -------------
    // 期待ヘッダ例: No,CommentNo,Comment,(任意…)
    private static void ParseCsv(string csvText)
    {
        ByChap.Clear();
        using var reader = new StringReader(csvText);
        string line;
        bool headerSkipped = false;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            // シンプルCSV分割（カンマ+ダブルクォート対応が必要ならCSVパーサを導入してください）
            var cols = SplitCsvLine(line);
            if (!headerSkipped)
            {
                headerSkipped = true;
                continue; // ヘッダ行スキップ
            }
            if (cols.Count < 3) continue;

            // No
            int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var no);

            // CommentNo -> chap/index/speaker
            var (chap, index, spk) = ParseCommentNo(cols[1]);

            // Comment（本文）
            var body = cols[2]?.Trim() ?? string.Empty;

            var extra = cols.Count > 3 ? cols.Skip(3).ToArray() : Array.Empty<string>();

            if (string.IsNullOrEmpty(chap) || index <= 0 || string.IsNullOrEmpty(body)) continue;

            var item = new StoryLine
            {
                no = no,
                chap = chap,
                index = index,
                speaker = spk,
                text = body,
                extra = extra
            };

            if (!ByChap.TryGetValue(chap, out var list))
            {
                list = new List<StoryLine>();
                ByChap.Add(chap, list);
            }
            list.Add(item);
        }

        // インデックス昇順ソート
        foreach (var kv in ByChap) kv.Value.Sort((a, b) => a.index.CompareTo(b.index));
    }

    // 例: "CHAP1-3-H" → ("CHAP1",3,"H"), "CHAP2-10" → ("CHAP2",10,"")
    private static (string chap, int index, string speaker) ParseCommentNo(string token)
    {
        var t = (token ?? "").Trim();
        if (string.IsNullOrEmpty(t)) return (null, 0, null);

        var hy = t.Split('-'); // ["CHAP1","3","H"] or ["CHAP1","10"]
        if (hy.Length < 2) return (null, 0, null);

        var chap = hy[0];
        var idxStr = hy[1];
        int index = 0;
        int.TryParse(idxStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out index);

        var spk = hy.Length >= 3 ? hy[2] : string.Empty;
        return (chap, index, spk);
    }

    // カンマとクォートを最低限処理
    private static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        bool inQ = false;
        var buf = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '\"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { buf.Append('\"'); i++; }
                else inQ = !inQ;
            }
            else if (c == ',' && !inQ)
            {
                result.Add(buf.ToString());
                buf.Clear();
            }
            else
            {
                buf.Append(c);
            }
        }
        result.Add(buf.ToString());
        return result;
    }
}
