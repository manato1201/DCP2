using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


[CreateAssetMenu(menuName = "Services/BattleSpeedService")]
public class BattleSpeedService : MonoBehaviour
{
    [Range(0f, 3f)] public float timeScale = 1f;    // ゲーム内速度
    public event Action<float> OnSpeedChanged;

    CancellationToken _ct;

    public void Init(CancellationToken ct)
    {
        _ct = ct;
        Apply(); // 初期反映
    }

    public void Set(float scale)
    {
        timeScale = Mathf.Max(0f, scale);
        Apply();
    }

    public void Pause(bool pause) => Set(pause ? 0f : Mathf.Max(0.01f, timeScale));

    void Apply()
    {
        Time.timeScale = timeScale;
        OnSpeedChanged?.Invoke(timeScale);
    }
}
