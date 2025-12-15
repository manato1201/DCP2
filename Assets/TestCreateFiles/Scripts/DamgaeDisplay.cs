using UnityEngine;
using TMPro; // 必須
using Cysharp.Threading.Tasks;

public class DamgaeDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshPro damageText;

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float disappearTime = 1f;

    // 初期化
    public void Setup(int damageAmount)
    {
        damageText.text = damageAmount.ToString();
        AnimatePopup().Forget();
    }

    private async UniTaskVoid AnimatePopup()
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Color originalColor = damageText.color;

        while (timer < disappearTime)
        {
            timer += Time.deltaTime;
            float progress = timer / disappearTime;

            // 上に移動
            transform.position = startPos + new Vector3(0, moveSpeed * progress, 0);

            // フェードアウト
            float alpha = Mathf.Lerp(1f, 0f, progress);
            damageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            await UniTask.Yield();
        }

        Destroy(gameObject);
    }
}
