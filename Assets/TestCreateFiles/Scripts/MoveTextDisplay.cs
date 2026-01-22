using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;

public class MoveTextDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro moverText;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float disappearTime = 1f;

    // 文字列と色を受け取るように変更
    public void Setup(string text, Color color)
    {
        moverText.text = text;
        moverText.color = color; // 初期色をセット
        AnimatePopup(color).Forget(); // アニメーションに渡す
    }

    // 既存のint用（もし使っているなら修正、使っていなければ削除可）
    public void Setup(int textAmount)
    {
        Setup(textAmount.ToString(), Color.white);
    }

    private async UniTaskVoid AnimatePopup(Color startColor)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        // startColor を基準にする

        while (timer < disappearTime)
        {
            timer += Time.deltaTime;
            float progress = timer / disappearTime;

            transform.position = startPos + new Vector3(0, moveSpeed * progress, 0);

            float alpha = Mathf.Lerp(1f, 0f, progress);
            // 受け取った色をベースに透明度だけ変える
            moverText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            await UniTask.Yield();
        }

        Destroy(gameObject);
    }
}
