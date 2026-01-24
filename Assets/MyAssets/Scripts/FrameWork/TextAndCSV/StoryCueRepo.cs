using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class StoryCueRepo
{
    // Resources/StoryCue.csv （※拡張子不要）
    public const string ResourcesKeyWithoutExt = "StoryCue";

    static Dictionary<string, Row> _map; // key = CommentNo

    public struct Row
    {
        public string CommentNo;
        public string BgId, LeftId, CenterId, RightId;
        public int GrayBG, GrayL, GrayC, GrayR;
        public int BlackBG, BlackL, BlackC, BlackR;
        public int Glow;        // 0/1
        public float Fade;      // 秒（>=0）
    }

    public static bool IsReady => _map != null;

    public static bool TryGet(string commentNo, out Row row)
    {
        if (_map != null && _map.TryGetValue(commentNo, out row)) return true;
        row = default;
        return false;
    }

    public static void EnsureLoaded()
    {
        if (_map != null) return;

        var ta = Resources.Load<TextAsset>(ResourcesKeyWithoutExt);
        if (!ta)
        {
            Debug.LogError($"[StoryCueRepo] Resources.Load 失敗: {ResourcesKeyWithoutExt}.csv が見つかりません。");
            _map = new Dictionary<string, Row>(1);
            return;
        }

        _map = new Dictionary<string, Row>(128);

        // 期待ヘッダ：
        // CommentNo,BgId,LeftId,CenterId,RightId,
        // GrayBG,GrayL,GrayC,GrayR,BlackBG,BlackL,BlackC,BlackR,Glow,Fade
        var lines = ta.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var raw = lines[i];
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var line = raw.TrimEnd('\r');

            var cols = line.Split(',');
            // 足りない列は補完
            string Get(int idx) => idx < cols.Length ? cols[idx].Trim() : string.Empty;

            var r = new Row
            {
                CommentNo = Get(0),
                BgId      = Get(1),
                LeftId    = Get(2),
                CenterId  = Get(3),
                RightId   = Get(4),
                GrayBG    = Parse01(Get(5)),  GrayL  = Parse01(Get(6)),
                GrayC     = Parse01(Get(7)),  GrayR  = Parse01(Get(8)),
                BlackBG   = Parse01(Get(9)),  BlackL = Parse01(Get(10)),
                BlackC    = Parse01(Get(11)), BlackR = Parse01(Get(12)),
                Glow      = Parse01(Get(13)),
                Fade      = ParseF(Get(14))
            };

            if (string.IsNullOrEmpty(r.CommentNo)) continue;
            if (!_map.ContainsKey(r.CommentNo)) _map.Add(r.CommentNo, r);
        }

        static int Parse01(string s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v != 0 ? 1 : 0;

        static float ParseF(string s)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? Mathf.Max(0f, f) : 0f;
    }
}
