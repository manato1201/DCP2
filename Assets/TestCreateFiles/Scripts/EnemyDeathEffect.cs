using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class EnemyDeathEffect : MonoBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("Beam Settings")]
    [SerializeField] private GameObject beamPrefab;
    [SerializeField] private int beamCount = 8;
    [SerializeField] private float baseWidth = 0.05f;
    [SerializeField] private float minLength = 1.5f;
    [SerializeField] private float maxLength = 3.5f;
    [SerializeField] private int appearanceDelayMs = 100;
    [SerializeField] private float rotationSpeed = 60f; // 回転速度

    private Material enemyMaterial;
    private float totalElapsedTime = 0f; // 全体の共有時間

    void Start()
    {
        enemyMaterial = GetComponent<SpriteRenderer>().material;
        enemyMaterial.SetFloat("_IsBeam", 0);
    }

    public async UniTask PlayDeathEffectAsync()
    {
        totalElapsedTime = 0f;
        var enemyFlashTask = FlashWhiteAsync();

        // 共有時間を更新し続けるループを裏で回す
        UpdateGlobalTime().Forget();

        await SpawnBeamsSequentially();

        await enemyFlashTask;
        await UniTask.Delay(500);

        Destroy(gameObject);
    }

    // 全てのビームが参照する「共通の時間」をカウントする
    private async UniTaskVoid UpdateGlobalTime()
    {
        while (this != null)
        {
            totalElapsedTime += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    private async UniTask SpawnBeamsSequentially()
    {
        List<int> indices = Enumerable.Range(0, beamCount).ToList();

        // シャッフル
        for (int i = 0; i < indices.Count; i++)
        {
            int temp = indices[i];
            int randomIndex = Random.Range(i, indices.Count);
            indices[i] = indices[randomIndex];
            indices[randomIndex] = temp;
        }

        float startOffsetAngle = Random.Range(0f, 360f);

        for (int i = 0; i < indices.Count; i++)
        {
            int index = indices[i];
            float baseAngle = index * (360f / beamCount);
            float finalAngle = baseAngle + startOffsetAngle;

            GameObject beam = Instantiate(beamPrefab, transform.position, Quaternion.Euler(0, 0, finalAngle));

            // ★個別の速度倍率を決定
            // 半分（iが半分の値より大きい場合）は 1.4倍〜1.8倍、それ以外は 0.8倍〜1.2倍 など
            float speedMultiplier = (i > beamCount / 2) ? Random.Range(1.4f, 1.8f) : Random.Range(0.8f, 1.2f);

            AnimateSingleBeam(beam, finalAngle, speedMultiplier).Forget();

            await UniTask.Delay(appearanceDelayMs);
        }
    }


    private async UniTaskVoid AnimateSingleBeam(GameObject beam, float startAngle, float speedMultiplier)
    {
        var sr = beam.GetComponent<SpriteRenderer>();
        sr.material.SetFloat("_IsBeam", 1);
        sr.material.SetFloat("_FlashAmount", 1);

        float randomWidthMultiplier = Random.Range(0.5f, 2.0f);
        float initialWidth = baseWidth * randomWidthMultiplier;
        float randomLength = Random.Range(minLength, maxLength);
        beam.transform.localScale = new Vector3(initialWidth, randomLength, 1f);

        float beamLifeTime = 0f;

        // ★余韻の時間（敵が消えてからさらに 0.8秒ほど残す）
        float totalLifeTime = fadeDuration + 0.8f;
        Vector3 centerPos = transform.position;

        while (beamLifeTime < totalLifeTime)
        {
            if (beam == null) return;
            beamLifeTime += Time.deltaTime;
            float p = beamLifeTime / totalLifeTime;

            // ★透明度の計算：最初はパッと出て、後半にゆっくり消える
            // 0.7（7割）くらいの時間までは明るさを保ち、そこから急激に消える設定
            float alpha = 1.0f;
            if (p > 0.7f)
            {
                // 残り3割の時間でフェードアウト
                alpha = Mathf.Lerp(1.0f, 0.0f, (p - 0.7f) / 0.3f);
            }
            else
            {
                // 出現時は 0.1秒ほどでパッと明るくする
                alpha = Mathf.Min(beamLifeTime / 0.1f, 1.0f);
            }

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;

            // ★余韻の動き：消え際に「細く」していく
            // 0.7（7割）を超えたら、横幅(X)を0に近づける
            float currentWidth = initialWidth;
            if (p > 0.7f)
            {
                currentWidth = Mathf.Lerp(initialWidth, 0f, (p - 0.7f) / 0.3f);
            }

            // 長さは伸び続け、幅は消え際に細くなる
            beam.transform.localScale = new Vector3(currentWidth, beam.transform.localScale.y + Time.deltaTime * 1.5f, 1f);

            // 回転継続
            float currentRotation = totalElapsedTime * rotationSpeed * speedMultiplier;
            beam.transform.rotation = Quaternion.Euler(0, 0, startAngle + currentRotation);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        if (beam != null) Destroy(beam);
    }

    private async UniTask FlashWhiteAsync()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (this == null) return;
            elapsed += Time.deltaTime;
            enemyMaterial.SetFloat("_FlashAmount", Mathf.Min(elapsed / fadeDuration, 1.0f));
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }
}
