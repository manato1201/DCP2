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
}