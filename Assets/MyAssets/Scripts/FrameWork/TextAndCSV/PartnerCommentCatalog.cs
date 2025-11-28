using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif


public enum PartnerCommentState
{
    // 定型文 1～10（CSV: PCT-XXXX0001 ～ 0010）
    StartTraining = 1,
    FinishTraining = 2,
    CompleteTraining = 3,
    Fixed4 = 4,
    Alerts = 5,
    Age = 6,
    ThankWork = 7,
    FinishTraining2 = 8,
    ThankWork2 = 9,
    FinishTraining3 = 10,

    // 付加コメント（CSV: 12～20 のうち存在行からランダム）
    AdditionalRandom = 1000,
}

public static class PartnerCommentStateExtensions
{
    public static bool TryGetFixedNumber(this PartnerCommentState s, out int number)
    {
        int v = (int)s;
        if (v >= 1 && v <= 10) { number = v; return true; }
        number = 0; return false;
    }
}


public sealed class PartnerCommentCatalog
{


   

    public static Task<string> GetCommentAsync(int characterId, PartnerCommentState state)
    {
        ResolvedState resolved;
        if (state.TryGetFixedNumber(out var n))
        {
            resolved = new ResolvedState { Mode = StateMode.Single, Number = n };
        }
        else if (state == PartnerCommentState.AdditionalRandom)
        {
            resolved = new ResolvedState { Mode = StateMode.Additional, Number = 0 };
        }
        else
        {
            resolved = new ResolvedState { Mode = StateMode.Single, Number = 1 }; // デフォルト
        }
        return GetByResolvedAsync(characterId, resolved);
    }

    // ===== 共通コア（両オーバーロードからここを呼ぶ） =====
    private static async Task<string> GetByResolvedAsync(int characterId, ResolvedState resolved)
    {
        await EnsureLoadedAsync();

        if (resolved.Mode == StateMode.Single)
        {
            var code = BuildCommentId(characterId, resolved.Number);
            if (Map.TryGetValue(code, out var text)) return text;

            var fallback = BuildCommentId(0, resolved.Number);
            if (Map.TryGetValue(fallback, out var text2)) return text2;

            return string.Empty;
        }
        else // Additional (12-20 ランダム)
        {
            var pool = GatherExistingNumbers(characterId, 12, 20);
            if (pool.Count == 0) pool = GatherExistingNumbers(0, 12, 20);
            if (pool.Count == 0) return string.Empty;

            var num = pool[Rng.Next(pool.Count)];
            var code = BuildCommentId(characterId, num);
            if (Map.TryGetValue(code, out var text)) return text;

            var fallback = BuildCommentId(0, num);
            return Map.TryGetValue(fallback, out var text2) ? text2 : string.Empty;
        }
    }

    // Addressables での TextAsset アドレス（使うならアドレス名をここに）
    public const string AddressablesKey = "CSV/PartnerComments";
    // Resources フォールバック用のパス（拡張子なし）
    // 例: Assets/Resources/PartnerComments.csv なら "PartnerComments"
    public const string ResourcesPath = "PartnerComments";

    // ===== 内部 =====
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase);
    private static Task LoadingTask;
    private static volatile bool Loaded = false;
    private static readonly System.Random Rng = new();

    private static async Task EnsureLoadedAsync()
    {
        if (Loaded) return;
        if (LoadingTask != null) { await LoadingTask; return; }
        LoadingTask = LoadAsync();           // ← ここでasyncメソッドをセット
        await LoadingTask;
        Loaded = true;
    }


    private static async Task LoadAsync()
    {
        TextAsset ta = null;

#if ADDRESSABLES
    try
    {
        var handle = Addressables.LoadAssetAsync<TextAsset>(AddressablesKey);
        ta = await handle.Task;
        if (ta == null)
            Debug.LogWarning($"[PartnerCommentCatalog] Addressables '{AddressablesKey}' が見つからず → Resourcesへ");
    }
    catch (Exception e)
    {
        Debug.LogWarning($"[PartnerCommentCatalog] Addressables 読み込み失敗: {e.Message} → Resourcesへ");
    }
#endif

        if (ta == null)
        {
            ta = Resources.Load<TextAsset>(ResourcesPath);
            if (ta == null)
            {
                Debug.LogError($"[PartnerCommentCatalog] CSV未検出 Addressables:'{AddressablesKey}' / Resources:'{ResourcesPath}'");
                return;
            }
        }
        ParseCsv(ta.text);
        await Task.Yield();
    }

    // CSV: ヘッダは 'CommentNo' / 'Comment' を必須として解釈
    private static void ParseCsv(string csvText)
    {
        Map.Clear();

        using var sr = new StringReader(csvText);
        var header = sr.ReadLine();
        if (header == null) { Debug.LogError("[PartnerCommentCatalog] CSVが空です"); return; }

        var cols = SplitCsvLine(header);
        int cId = FindCol(cols, "CommentNo"); // PCT-XXXXNNNN
        int cText = FindCol(cols, "Comment");   // 本文

        if (cId < 0 || cText < 0)
        {
            Debug.LogError("[PartnerCommentCatalog] 必須列 'CommentNo' と 'Comment' が見つかりません。");
            return;
        }

        string line;
        while ((line = sr.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var p = SplitCsvLine(line);
            var key = Get(p, cId)?.Trim();
            var val = Get(p, cText);

            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val)) continue; // 空行や説明行はスキップ
            if (!Map.ContainsKey(key)) Map.Add(key, val);
        }
#if UNITY_EDITOR
        Debug.Log($"[PartnerCommentCatalog] Loaded rows: {Map.Count}");
#endif
    }

    private static string Get(string[] arr, int idx) => (idx >= 0 && idx < arr.Length) ? arr[idx] : string.Empty;

    private static int FindCol(string[] cols, params string[] names)
    {
        for (int i = 0; i < cols.Length; i++)
        {
            var c = cols[i].Trim();
            foreach (var n in names)
                if (string.Equals(c, n, StringComparison.OrdinalIgnoreCase)) return i;
        }
        return -1;
    }

    private static string[] SplitCsvLine(string line)
    {
        var list = new List<string>();
        bool inQ = false;
        var cur = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '\"')
            {
                if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { cur.Append('\"'); i++; }
                else inQ = !inQ;
            }
            else if (ch == ',' && !inQ)
            {
                list.Add(cur.ToString());
                cur.Clear();
            }
            else cur.Append(ch);
        }
        list.Add(cur.ToString());
        return list.ToArray();
    }

    private static string BuildCommentId(int characterId, int stateNumber)
    {
        var char4 = Mathf.Clamp(characterId, 0, 9999).ToString("D4", CultureInfo.InvariantCulture);
        var state4 = Mathf.Clamp(stateNumber, 0, 9999).ToString("D4", CultureInfo.InvariantCulture);
        return $"PCT-{char4}{state4}";
    }

    private enum StateMode { Single, Additional }
    private struct ResolvedState { public StateMode Mode; public int Number; }

    private static ResolvedState ResolveStateNumber(string commentState)
    {
        if (TryParseStateNumber(commentState, out var n))
            return new ResolvedState { Mode = StateMode.Single, Number = n };

        var token = (commentState ?? string.Empty).Trim().ToLowerInvariant();
        if (token is "付加コメント" or "additional" or "add" or "rand12_20" or "random12_20")
            return new ResolvedState { Mode = StateMode.Additional, Number = 0 };

        return new ResolvedState { Mode = StateMode.Single, Number = 1 };
    }

    private static bool TryParseStateNumber(string input, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(input)) return false;
        if (int.TryParse(input, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
        {
            number = Mathf.Clamp(n, 0, 9999);
            return true;
        }
        return false;
    }

    private static List<int> GatherExistingNumbers(int characterId, int fromInclusive, int toInclusive)
    {
        var list = new List<int>();
        fromInclusive = Mathf.Max(0, fromInclusive);
        toInclusive = Mathf.Min(9999, toInclusive);
        for (int s = fromInclusive; s <= toInclusive; s++)
        {
            var k = BuildCommentId(characterId, s);
            if (Map.ContainsKey(k)) list.Add(s);
        }
        return list;
    }
}