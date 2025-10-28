using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public interface ITextSO { string T(string key); } // CSV辞書の最小インタフェース

public class TypingText : MonoBehaviour
{
    [SerializeField] private TMP_Text[] targets;
    [SerializeField] private float charInterval = 0.03f; // 秒
    [SerializeField] private bool obeyTimeScale = true;
    [Header("CSV")]
    [SerializeField] private ScriptableObject textSoObject; // ITextSO を実装したSOを想定
    ITextSO _db;

    CancellationTokenSource _cts;

    void Awake()
    {
        _cts = new();
        _db = textSoObject as ITextSO;
    }
    void OnDestroy() => _cts?.Cancel();

    public async UniTask PlayAsync(string key)
    {
        var text = _db != null ? _db.T(key) : key; // DBがなければkeyをそのまま
        foreach (var t in targets) t.text = "";

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        var ct = linked.Token;

        foreach (var t in targets)
            await TypeToAsync(t, text, charInterval, obeyTimeScale, ct);
    }

    static async UniTask TypeToAsync(TMP_Text target, string content, float interval, bool obeyScale, CancellationToken ct)
    {
        if (!target) return;
        var sb = new StringBuilder(content.Length);
        for (int i = 0; i < content.Length; i++)
        {
            sb.Append(content[i]);
            target.text = sb.ToString();

            if (interval > 0f)
            {
                if (obeyScale) await UniTask.Delay((int)(interval * 1000f), cancellationToken: ct);
                else await UniTask.Delay((int)(interval * 1000f), DelayType.UnscaledDeltaTime, cancellationToken: ct);
            }
        }
    }
}
