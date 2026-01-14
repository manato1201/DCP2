using System;
using System.Text;
using UnityEngine;

public enum FormatCommentLineLen
{
    // 例：用途に応じて増やしてOK（数字は1行の最大“文字数”）
    TodayGameTraining = 14,
    MoreGameTraining = 47,
}

public static class CommentFormatter
{
    // 句読点候補（必要に応じて追加可能）
    private static readonly char[] DefaultPunctuations = new[]
    {
        '。','！','？','．','，', // 全角
        '!', '?', '.',           // 半角
    };

    private static readonly string[] ImplicitEnds = new[]
    {
    "でした","だった",
    "ですな","だよ","だね","だわ","だな","じゃよ","じゃね",
    "のよ","んじゃ","のじゃ"
    };

    private static bool EndsWithAny(System.Text.StringBuilder buf)
    {
        for (int i = 0; i < ImplicitEnds.Length; i++)
        {
            var p = ImplicitEnds[i];
            if (buf.Length >= p.Length &&
                buf.ToString(buf.Length - p.Length, p.Length) == p)
                return true;
        }
        return false;
    }

    /// <summary>
    /// enum指定版：用途（状態）に応じて行長を切替。
    /// 13文字以内はそのまま。14文字以上は「句読点直後で改行」を優先し、
    /// それでも長ければ maxLineLen 文字で強制改行。既存の改行は尊重。
    /// </summary>
    public static string FormatForTextbox(string input, FormatCommentLineLen lineLenKind, char[] customPunctuations = null)
    {
        // enum → 実値（1以上にクランプ）
        int maxLineLen = Mathf.Max(1, (int)lineLenKind);
        return FormatForTextbox(input, maxLineLen, customPunctuations);
    }

    /// <summary>
    /// 既存互換の int 版（内部コア）。外部から直接使ってもOK。
    /// </summary>
    public static string FormatForTextbox(string input, int maxLineLen = 13, char[] customPunctuations = null)
        => FormatCore(input, Mathf.Max(1, maxLineLen), customPunctuations ?? DefaultPunctuations);

    // ------- 内部コア実装 -------
    private static string FormatCore(string input, int maxLineLen, char[] punctuations)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // 改行をLFに正規化
        input = input.Replace("\r\n", "\n").Replace("\r", "\n");

        var sb = new System.Text.StringBuilder(input.Length + 16);
        int lineLen = 0;
        bool prevWasNewline = false;

        // i の次から、<...> を飛ばして最初の“見える文字”を覗く
        char PeekNextVisibleChar(int start)
        {
            int j = start;
            while (j < input.Length)
            {
                if (input[j] == '<')
                {
                    int close = input.IndexOf('>', j);
                    if (close < 0) break;
                    j = close + 1;
                    continue;
                }
                return input[j];
            }
            return '\0';
        }

        // 次の“見える文字”が改行なら入れない安全改行
        void AppendNewlineOnceSkippingIfNextIsNewline(int nextIndex)
        {
            char nextVis = PeekNextVisibleChar(nextIndex);
            if (nextVis == '\n') { lineLen = 0; return; }
            if (!prevWasNewline) { sb.Append('\n'); prevWasNewline = true; }
            lineLen = 0;
        }

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[i];

            // 元の改行は1回だけ反映
            if (ch == '\n')
            {
                if (!prevWasNewline) { sb.Append('\n'); prevWasNewline = true; }
                lineLen = 0;
                continue;
            }

            // タグ <...> は0カウントでそのまま出力（改行は入れない）
            if (ch == '<')
            {
                int close = input.IndexOf('>', i);
                if (close >= 0)
                {
                    sb.Append(input, i, close - i + 1);
                    i = close; // 次へ
                               // 改行状態は維持（prevWasNewline は触らない）
                    continue;
                }
                // '>' が無い場合は通常文字扱いへ
            }

            // 通常文字
            sb.Append(ch);
            lineLen++;
            prevWasNewline = false;

            //文末（句読点なしでも語尾で改行）
            //if (EndsWithAny(sb))
            //{
            //    AppendNewlineOnceSkippingIfNextIsNewline(i + 1);
            //    continue;
            //}

            //句読点で改行
            if (System.Array.IndexOf(punctuations, ch) >= 0)
            {
                AppendNewlineOnceSkippingIfNextIsNewline(i + 1);
                continue;
            }

            //行長で強制改行
            if (lineLen >= maxLineLen)
            {
                // 次の“見える文字”（<...>はスキップ）が句読点なら改行を延期して
                // 句読点を前行の末尾にくっつける
                char nextVis = PeekNextVisibleChar(i + 1);
                if (System.Array.IndexOf(punctuations, nextVis) >= 0)
                {
                    // 何もしない（＝改行しない）。次の反復で句読点を出力→句読点規則で改行される
                    continue;
                }

                AppendNewlineOnceSkippingIfNextIsNewline(i + 1);
            }
        }

        return sb.ToString();
    }
}
