using UnityEngine;
using TMPro;

public sealed class piece : APooledObject
{
    [SerializeField] private TextMeshPro fogText;

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
