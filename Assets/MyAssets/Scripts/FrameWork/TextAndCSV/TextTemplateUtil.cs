using System.Text.RegularExpressions;

public static class TextTemplateUtil
{
    // 単独の 'X' だけを置換（EXP等の誤爆防止）
    static readonly Regex LoneX = new Regex(@"(?<![A-Za-z0-9])X(?![A-Za-z0-9])", RegexOptions.Compiled);

    /// <summary>
    /// [分野] → fieldText、 X → (必要なら) <color=...>diffAge</color> に差し込み。
    /// colorAge=false で数値のみ差し込みにも対応。
    /// </summary>
    public static string ApplyStandard(string template, string fieldText, int diffAge,
                                       bool colorAge = true, string ageColor = "red")
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        string s = template;

        // [分野]
        if (!string.IsNullOrEmpty(fieldText) && s.Contains("[分野]"))
            s = s.Replace("[分野]", fieldText);

        // X（単独のみ）
        if (LoneX.IsMatch(s))
        {
            string ageText = diffAge.ToString();
            if (colorAge) ageText = $"<color={ageColor}>{ageText}</color>";
            s = LoneX.Replace(s, ageText);
        }
        return s;
    }
    // CSV 1行を分解（ダブルクォート対応, " 内の , を保護）
    public static System.Collections.Generic.List<string> SplitCsvLine(string line)
    {
        var cells = new System.Collections.Generic.List<string>();
        if (string.IsNullOrEmpty(line)) { cells.Add(string.Empty); return cells; }

        bool inQuote = false;
        var sb = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                // 連続する "" はエスケープされた " とみなす
                if (inQuote && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++; // もう1つ進める
                }
                else
                {
                    inQuote = !inQuote; // クォートの開閉を反転
                }
            }
            else if (c == ',' && !inQuote)
            {
                cells.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }
        cells.Add(sb.ToString());
        return cells;
    }

}
