using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class StoryCueRepo
{
    // アドレスはあなたの環境に合わせて
    const string AddressKey = "Assets/MyAssets/Resources/StoryCue.csv";

    public struct Row
    {
        public string CommentNo;
        public string BgId, LeftId, CenterId, RightId;

        // ← 5,6 列目に移動
        public string BGMId;
        public string SEId;

        public int GrayBG, GrayL, GrayC, GrayR;
        public int BlackBG, BlackL, BlackC, BlackR;
        public int Glow;      // 0/1
        public float Fade;    // 秒
    }

    static Dictionary<string, Row> _map;

    public static bool TryGet(string commentNo, out Row row)
    {
        row = default; // 終了時に必ず割り当て
        return _map != null && _map.TryGetValue(commentNo, out row);
    }

    public static async UniTask EnsureLoadedAsync(CancellationToken ct)
    {
        if (_map != null) return;

        var ta = await Addressables.LoadAssetAsync<TextAsset>(AddressKey)
                                   .Task.AsUniTask().AttachExternalCancellation(ct);

        _map = new Dictionary<string, Row>(256);

        // ヘッダ順：
        // 0:CommentNo,1:BgId,2:LeftId,3:CenterId,4:RightId,
        // 5:BGMId,6:SEId,
        // 7:GrayBG,8:GrayL,9:GrayC,10:GrayR,
        // 11:BlackBG,12:BlackL,13:BlackC,14:BlackR,
        // 15:Glow,16:Fade
        var lines = ta.text.Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(line)) continue;

            var cols = line.Split(',');
            if (cols.Length < 17) continue;

            var r = new Row
            {
                CommentNo = cols[0].Trim(),
                BgId      = cols[1].Trim(),
                LeftId    = cols[2].Trim(),
                CenterId  = cols[3].Trim(),
                RightId   = cols[4].Trim(),

                BGMId     = cols[5].Trim(),
                SEId      = cols[6].Trim(),

                GrayBG    = To01(cols[7]),
                GrayL     = To01(cols[8]),
                GrayC     = To01(cols[9]),
                GrayR     = To01(cols[10]),

                BlackBG   = To01(cols[11]),
                BlackL    = To01(cols[12]),
                BlackC    = To01(cols[13]),
                BlackR    = To01(cols[14]),

                Glow      = To01(cols[15]),
                Fade      = ToF (cols[16]),
            };

            if (!_map.ContainsKey(r.CommentNo))
                _map.Add(r.CommentNo, r);
        }

        static int   To01(string s)
            => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v != 0 ? 1 : 0;

        static float ToF(string s)
            => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? Mathf.Max(0f, v) : 0f;
    }
    // 引数なし版も欲しければこれも
    public static UniTask EnsureLoaded()
        => EnsureLoadedAsync(System.Threading.CancellationToken.None);
}
