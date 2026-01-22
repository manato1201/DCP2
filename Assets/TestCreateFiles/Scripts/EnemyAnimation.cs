using UnityEngine;
using Cysharp.Threading.Tasks;

public class EnemyAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float jumpHeight = 1.5f; // ジャンプの高さ
    [SerializeField] private float duration = 0.5f;   // アニメーションにかかる時間
    [SerializeField] private int numberOfSpins = 1;   // 回転数

    // --- 追加: ダメージ演出の設定 ---
    [Header("Damage Settings")]
    [SerializeField] private float shakeDuration = 0.4f;   // 震える時間
    [SerializeField] private float baseShakeStrength = 0.05f; // ダメージ1あたりの震え幅係数
    [SerializeField] private float maxShakeStrength = 1.0f;   // 震え幅の最大値（制限）

    // 元の位置と回転を保存用
    private Vector3 originalPos;
    private Quaternion originalRot;

    private void Start()
    {
        // 初期位置を記憶しておく
        originalPos = transform.position;
        originalRot = transform.rotation;
    }

    /// <summary>
    /// ジャンプしながら横回転するアニメーションを再生
    /// </summary>
    public async UniTask PlayAttackMotion()
    {
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);

            // --- 1. ジャンプ処理 (Sin波を使って 0 -> 1 -> 0 の動きを作る) ---
            // Mathf.PI * progress で 0～180度(ラジアン)になり、Sinは 0→1→0 となる
            float yOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = originalPos + new Vector3(0, yOffset, 0);

            // --- 2. 横回転処理 (Y軸回転) ---
            // 360度 * 回転数 * 進捗率
            float yAngle = 360f * numberOfSpins * progress;
            // 元の回転 + 新しい回転
            transform.rotation = originalRot * Quaternion.Euler(0, yAngle, 0);

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }

        // 念のため最後にズレを補正してリセット
        transform.position = originalPos;
        transform.rotation = originalRot;
    }

    /// <summary>
    /// ダメージシェイクアニメーション
    /// </summary>
    /// <param name="damage">受けたダメージ量</param>
    public async UniTask PlayDamageShake(int damage)
    {
        // ダメージ量に応じて震えの強さを計算
        // 例: ダメージ10 * 0.02 = 0.2 の振幅。 最大1.0まで。
        float strength = Mathf.Clamp(damage * baseShakeStrength, 0.1f, maxShakeStrength);

        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            // ランダムな位置ずれを生成 (UnitSphereだとZ軸もずれるのでUnitCircleでXYのみ揺らす)
            Vector3 randomOffset = (Vector3)Random.insideUnitCircle * strength;

            transform.position = originalPos + randomOffset;

            await UniTask.Yield(this.GetCancellationTokenOnDestroy());
        }

        // 最後に必ず元の位置に戻す
        transform.position = originalPos;
    }
}
