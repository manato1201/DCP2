using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

public sealed class piece : APooledObject
{
    [SerializeField] private TextMeshPro fogText;

    [SerializeField] private ParticleSystem _fogParticlePrefab; // インスペクタで紫のもやパーティクルを割当
    [SerializeField] private float _effectDuration = 3.0f;    // もくもくさせる時間

    private void Awake()
    {
        // もしインスペクターでアタッチし忘れていても、子オブジェクトから自動で見つけてくる（保険）
        if (fogText == null)
        {
            fogText = GetComponentInChildren<TextMeshPro>();
        }
        ResetDisplay();
    }

    public void ResetDisplay()
    {
        if (fogText != null)
        {
            fogText.text = "";
            fogText.gameObject.SetActive(false);
        }
    }

    // piece.cs 内に追加
    public async UniTask OnFogExpired()
    {
        Debug.Log($"{gameObject.name} のもやが期限切れになりました！");

        // 1. パーティクルを生成
        // 自身の位置（または少し上など）に生成
        ParticleSystem effect = Instantiate(_fogParticlePrefab, transform.position, Quaternion.identity);

        // 2. パーティクルを再生
        effect.Play();

        // 3. 指定した時間（秒）だけ非同期で待機
        // ※Time.timeScaleの影響を受ける場合は DelayType.DeltaTime を使用
        await UniTask.Delay(System.TimeSpan.FromSeconds(_effectDuration));

        // 4. 後処理（パーティクルを止めたり、自身を削除したり）
        if (effect != null)
        {
            effect.Stop();
            Destroy(effect.gameObject, 2.0f); // パーティクルの残響が消えるまで少し待ってから削除
        }

        Debug.Log("演出が終了したのでオブジェクトを削除します");
    }
    public void SetFogDisplay(int turns, bool isFog)
    {
        // 参照がない場合は再度取得を試みる
        if (fogText == null) fogText = GetComponentInChildren<TextMeshPro>();
        if (fogText == null) return;

        if (isFog)
        {
            // ここで確実に表示と数値をセット
            fogText.gameObject.SetActive(true);
            fogText.text = turns.ToString();

            // 色が透明だったり背景と同化していないか確認用（デバッグ時は白にする）
            fogText.color = Color.white;

            Debug.Log($"[FogUpdate] {gameObject.name} に数値 {turns} をセットしました");
        }
        else
        {
            ResetDisplay();
        }
    }

    protected override void OnSpawn() => ResetDisplay();
    protected override void OnDespawn() => ResetDisplay();
}
