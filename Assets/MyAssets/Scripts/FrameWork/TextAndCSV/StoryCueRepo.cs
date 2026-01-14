using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class StoryCueRepo
{
    // Addressables のキー。変更したければここを編集（例: ラベル "CommuLog" ではなく "StoryCue.csv" のアドレスでも可）
    const string AddressKey = "Assets/MyAssets/Resources/StoryCue.csv";

    static Dictionary<string, Row> _map; // key = CommentNo

    public struct Row
    {
        public string CommentNo;
        public string BgId, LeftId, CenterId, RightId;
        public int GrayBG, GrayL, GrayC, GrayR;
        // public int Glow;
    }

    public static bool TryGet(string commentNo, out Row row)
    {
        if (_map != null && _map.TryGetValue(commentNo, out row))
            return true;

        row = default;
        return false;
    }

    public static async UniTask EnsureLoadedAsync(CancellationToken ct)
    {
        if (_map != null) return;

        var ta = await Addressables.LoadAssetAsync<TextAsset>(AddressKey)
                                   .Task.AsUniTask().AttachExternalCancellation(ct);

        _map = new Dictionary<string, Row>(128);

        // 期待ヘッダ：
        // CommentNo,BgId,LeftId,CenterId,RightId,GrayBG,GrayL,GrayC,GrayR,Glow
        var lines = ta.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 9) continue;

            var r = new Row
            {
                CommentNo = cols[0].Trim(),
                BgId      = cols[1].Trim(),
                LeftId    = cols[2].Trim(),
                CenterId  = cols[3].Trim(),
                RightId   = cols[4].Trim(),
                GrayBG    = Parse01(cols[5]),
                GrayL     = Parse01(cols[6]),
                GrayC     = Parse01(cols[7]),
                GrayR     = Parse01(cols[8]),
                // Glow      = cols.Length > 9 ? Parse01(cols[9]) : 0,
            };
            if (!_map.ContainsKey(r.CommentNo))
                _map.Add(r.CommentNo, r);
        }

        static int Parse01(string s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? (v != 0 ? 1 : 0) : 0;
    }
}
