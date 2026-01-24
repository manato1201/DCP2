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

    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab; // 小さな白い粒のPrefab
    [SerializeField] private int particleCount = 20;    // 粒の数
    [SerializeField] private float startRadius = 3.0f;  // 出現時の半径

    [Header("Burst Settings")]
    [SerializeField] private int burstCount = 15;        // はじける粒の数
    [SerializeField] private float burstSpeed = 10.0f;    // 弾ける速さ
    [SerializeField] private float burstDuration = 0.5f; // 消えるまでの時間

    private Material enemyMaterial;
    private float totalElapsedTime = 0f; // 全体の共有時間

    // 粒のタイプを定義する構造体
    private struct ParticleType
    {
        public Color color;
        public float sizeScale;
        public ParticleType(Color c, float s) { color = c; sizeScale = s; }
    }

    void Start()
    {
        enemyMaterial = GetComponent<SpriteRenderer>().material;
        enemyMaterial.SetFloat("_IsBeam", 0);
    }

    public async UniTask PlayDeathEffectAsync()
    {
        totalElapsedTime = 0f;

        // 1. ビームと収束パーティクルを同時に開始
        SpawnBeamsSequentially().Forget();
        FlashWhiteAsync().Forget();

        // 2. 収束が終わるのを待つ
        await SpawnAndConvergeParticles();

        // 3. 【重要】中心に集まった瞬間に「はじける」演出を実行
        PlayBurstEffect().Forget();

        // 4. 敵を消す
        gameObject.SetActive(false);
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


    private async UniTask SpawnAndConvergeParticles()
    {
        // 1. 設定リストの作成
        List<ParticleType> types = new List<ParticleType>();

        // 最低1つずつは必ず入れる
        types.Add(new ParticleType(Color.green, 0.5f));  // 緑・小
        types.Add(new ParticleType(Color.yellow, 1.0f)); // 黄・中
        types.Add(new ParticleType(Color.red, 2.0f));    // 赤・大

        // 残りの枠をランダムで埋める
        for (int i = types.Count; i < particleCount; i++)
        {
            float r = Random.value;
            if (r < 0.33f) types.Add(new ParticleType(Color.green, 2.0f));
            else if (r < 0.66f) types.Add(new ParticleType(Color.yellow, 4.0f));
            else types.Add(new ParticleType(Color.red, 7.0f));
        }

        // リストをシャッフル（出現順をバラバラにする）
        for (int i = 0; i < types.Count; i++)
        {
            int randomIndex = Random.Range(i, types.Count);
            var temp = types[i];
            types[i] = types[randomIndex];
            types[randomIndex] = temp;
        }

        // 2. 生成とアニメーション開始
        List<UniTask> particleTasks = new List<UniTask>();
        foreach (var type in types)
        {
            float startAngle = Random.Range(0f, 360f);
            GameObject p = Instantiate(particlePrefab, transform.position, Quaternion.identity);

            // 色とサイズを適用してアニメーション開始
            particleTasks.Add(AnimateSingleParticle(p, startAngle, type));

            await UniTask.Delay(30);
        }

        await UniTask.WhenAll(particleTasks);
    }

    private async UniTask AnimateSingleParticle(GameObject p, float startAngle, ParticleType type)
    {
        var sr = p.GetComponent<SpriteRenderer>();
        sr.color = type.color;
        // マテリアルの_FlashAmountを1にして光らせる（以前の仕様を継承）
        sr.material.SetFloat("_FlashAmount", 1f);

        float elapsed = 0f;
        float duration = fadeDuration + 0.5f;
        float currentRadius = startRadius;
        float individualSpeed = Random.Range(200f, 400f);

        // 基準となるサイズを決定
        Vector3 baseScale = Vector3.one * type.sizeScale * 0.2f;

        while (elapsed < duration)
        {
            if (p == null) return;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            currentRadius = Mathf.Lerp(startRadius, 0f, t);
            float angle = startAngle + (elapsed * individualSpeed);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * currentRadius;

            p.transform.position = transform.position + offset;

            // サイズの余韻：中心に近づくほどさらに細くなる
            p.transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t);

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        Destroy(p);
    }

    private async UniTaskVoid PlayBurstEffect()
    {
        for (int i = 0; i < burstCount; i++)
        {
            GameObject p = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            AnimateBurstParticle(p).Forget();
        }
        await UniTask.CompletedTask;
    }

    private async UniTaskVoid AnimateBurstParticle(GameObject p)
    {
        var sr = p.GetComponent<SpriteRenderer>();

        // はじける粒は「白〜黄色」などでランダムに光らせると爆発感が出ます
        sr.color = Color.Lerp(Color.white, Color.yellow, Random.value);
        sr.material.SetFloat("_FlashAmount", 1f);

        // ランダムな方向に飛ばす
        Vector3 direction = Random.insideUnitCircle.normalized;
        float speed = Random.Range(burstSpeed * 0.5f, burstSpeed);
        float elapsed = 0f;

        while (elapsed < burstDuration)
        {
            if (p == null) return;
            elapsed += Time.deltaTime;
            float t = elapsed / burstDuration;

            // 外側へ移動（少しずつ減速させるとリアル）
            p.transform.position += direction * speed * Time.deltaTime;
            speed *= 0.9f;

            // 消え際に細長くするか、小さくする
            p.transform.localScale = Vector3.Lerp(new Vector3(0.1f, 0.5f, 1f), Vector3.zero, t);

            // 進行方向に向ける
            p.transform.up = direction;

            // 透明度も下げる
            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        Destroy(p);
    }
}
