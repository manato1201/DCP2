using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class StoryCueRepo
{
    const string AddressKey = "Assets/MyAssets/Resources/StoryCue.csv";

    static Dictionary<string, Row> _map;

    public struct Row
    {
        public string CommentNo;
        public string BgId, LeftId, CenterId, RightId; // 画像ID（空=透明へ）
        public int GrayBG, GrayL, GrayC, GrayR;        // 0/1
        public int BlackBG, BlackL, BlackC, BlackR;    // 0/1
        public int Glow;                                // 0/1（全体）
        public float Fade;                              // 秒（0/空=default）
        public string BGMId;                            // 空=再生指示なし
        public string SEId;                             // 空=再生指示なし
    }

    public static bool TryGet(string commentNo, out Row row)
    {
        row = default; // ★ 先に代入しておく
        return _map != null && _map.TryGetValue(commentNo, out row);
    }

    public static void EnsureLoaded()
    {
        if (_map != null) return;

        var ta = UnityEngine.AddressableAssets.Addressables
                    .LoadAssetAsync<TextAsset>(AddressKey)
                    .WaitForCompletion();
        if (!ta)
        {
            Debug.LogError($"[StoryCueRepo] CSV not found: {AddressKey}");
            _map = new Dictionary<string, Row>();
            return;
        }

        _map = new Dictionary<string, Row>(256);

        // 想定ヘッダ：
        // CommentNo,BgId,LeftId,CenterId,RightId,GrayBG,GrayL,GrayC,GrayR,BlackBG,BlackL,BlackC,BlackR,Glow,Fade,BGMId,SEId
        var lines = ta.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cols = line.Split(',');
            if (cols.Length < 15) continue;

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
                BlackBG   = Parse01(cols[9]),
                BlackL    = Parse01(cols[10]),
                BlackC    = Parse01(cols[11]),
                BlackR    = Parse01(cols[12]),
                BGMId     = cols.Length > 13 ? cols[13].Trim() : string.Empty,
                SEId      = cols.Length > 14 ? cols[14].Trim() : string.Empty,
                Glow      = Parse01(cols[15]),
                Fade      = ParseF(cols[16]),

            };
            if (!_map.ContainsKey(r.CommentNo)) _map.Add(r.CommentNo, r);
        }

        static int Parse01(string s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? (v != 0 ? 1 : 0) : 0;

        static float ParseF(string s)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? Mathf.Max(0f, v) : 0f;
    }
}
