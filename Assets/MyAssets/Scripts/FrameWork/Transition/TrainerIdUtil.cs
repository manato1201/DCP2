using System.Text.RegularExpressions;
using UnityEngine;

public static class TrainerIdUtil
{
    // 末尾の「-数字」を優先して拾う（PTM-1011 → 1011）
    private static readonly Regex TailDigits = new Regex(@"-(\d{1,5})$", RegexOptions.Compiled);

    /// <summary> "PTM-1011" → 1011（失敗時 false）。0..9999 にクランプ。 </summary>
    public static bool TryGetCharacterId(string trainerId, out int characterId)
    {
        characterId = 0;
        if (string.IsNullOrEmpty(trainerId)) return false;

        // ① 末尾 "-####" を最優先
        var m = TailDigits.Match(trainerId);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
        {
            characterId = Mathf.Clamp(n, 0, 9999);
            return true;
        }

        // ② フォールバック：文字列中の最初の連続数字（保険）
        var any = Regex.Match(trainerId, @"\d+");
        if (any.Success && int.TryParse(any.Value, out n))
        {
            characterId = Mathf.Clamp(n, 0, 9999);
            return true;
        }

        return false;
    }

    /// <summary> 失敗時は defaultValue を返す簡易版。 </summary>
    public static int ParseOrDefault(string trainerId, int defaultValue = 0)
        => TryGetCharacterId(trainerId, out var id) ? id : defaultValue;
}