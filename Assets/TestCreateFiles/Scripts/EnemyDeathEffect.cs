using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading; // 追加
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
    [SerializeField] private float rotationSpeed = 60f;

    [Header("Particle Settings")]
    [SerializeField] private GameObject particlePrefab;
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float startRadius = 3.0f;

    [Header("Burst Settings")]
    [SerializeField] private int burstCount = 15;
    [SerializeField] private float burstSpeed = 10.0f;
    [SerializeField] private float burstDuration = 0.5f;

    private Material enemyMaterial;
    private float totalElapsedTime = 0f;

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

        // 全体の共有時間を更新するループを開始
        UpdateGlobalTime(this.GetCancellationTokenOnDestroy()).Forget();
    }

    public async UniTask PlayDeathEffectAsync()
    {
        // オブジェクトが破棄されたらキャンセルされるトークン
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            totalElapsedTime = 0f;

            // 1. ビームと収束パーティクルを同時に開始
            SpawnBeamsSequentially(ct).Forget();
            FlashWhiteAsync(ct).Forget();

            // 2. 収束が終わるのを待つ
            await SpawnAndConvergeParticles(ct);

            // 3. 実行前に生存確認
            if (this == null) return;

            // バースト位置を確定させてから実行
            Vector3 finalPos = transform.position;
            PlayBurstEffect(finalPos, ct).Forget();

            // 4. 敵を消す
            gameObject.SetActive(false);
        }
        catch (System.OperationCanceledException)
        {
            // 破棄された場合は静かに終了
        }
    }

    private async UniTaskVoid UpdateGlobalTime(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            totalElapsedTime += Time.deltaTime;
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private async UniTask SpawnBeamsSequentially(CancellationToken ct)
    {
        List<int> indices = Enumerable.Range(0, beamCount).ToList();

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
            if (this == null) return;

            float finalAngle = (indices[i] * (360f / beamCount)) + startOffsetAngle;
            GameObject beam = Instantiate(beamPrefab, transform.position, Quaternion.Euler(0, 0, finalAngle));
            float speedMultiplier = (i > beamCount / 2) ? Random.Range(1.4f, 1.8f) : Random.Range(0.8f, 1.2f);

            AnimateSingleBeam(beam, finalAngle, speedMultiplier, ct).Forget();

            await UniTask.Delay(appearanceDelayMs, cancellationToken: ct);
        }
    }

    private async UniTaskVoid AnimateSingleBeam(GameObject beam, float startAngle, float speedMultiplier, CancellationToken ct)
    {
        var sr = beam.GetComponent<SpriteRenderer>();
        sr.material.SetFloat("_IsBeam", 1);
        sr.material.SetFloat("_FlashAmount", 1);

        float initialWidth = baseWidth * Random.Range(0.5f, 2.0f);
        float randomLength = Random.Range(minLength, maxLength);
        beam.transform.localScale = new Vector3(initialWidth, randomLength, 1f);

        float beamLifeTime = 0f;
        float totalLifeTime = fadeDuration + 0.8f;

        // 生成時の敵の位置を基準にする（敵が消えた後も位置を保つため）
        Vector3 centerPos = (this != null) ? transform.position : beam.transform.position;

        while (beamLifeTime < totalLifeTime && !ct.IsCancellationRequested)
        {
            if (beam == null) return;
            beamLifeTime += Time.deltaTime;
            float p = beamLifeTime / totalLifeTime;

            float alpha = (p > 0.7f) ? Mathf.Lerp(1.0f, 0.0f, (p - 0.7f) / 0.3f) : Mathf.Min(beamLifeTime / 0.1f, 1.0f);

            Color c = sr.color;
            c.a = alpha;
            sr.color = c;

            float currentWidth = (p > 0.7f) ? Mathf.Lerp(initialWidth, 0f, (p - 0.7f) / 0.3f) : initialWidth;
            beam.transform.localScale = new Vector3(currentWidth, beam.transform.localScale.y + Time.deltaTime * 1.5f, 1f);

            float currentRotation = totalElapsedTime * rotationSpeed * speedMultiplier;
            beam.transform.rotation = Quaternion.Euler(0, 0, startAngle + currentRotation);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (beam != null) Destroy(beam);
    }

    private async UniTask FlashWhiteAsync(CancellationToken ct)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration && !ct.IsCancellationRequested)
        {
            if (this == null) return;
            elapsed += Time.deltaTime;
            enemyMaterial.SetFloat("_FlashAmount", Mathf.Min(elapsed / fadeDuration, 1.0f));
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }
    }

    private async UniTask SpawnAndConvergeParticles(CancellationToken ct)
    {
        List<ParticleType> types = new List<ParticleType>();
        types.Add(new ParticleType(Color.green, 0.5f));
        types.Add(new ParticleType(Color.yellow, 1.0f));
        types.Add(new ParticleType(Color.red, 2.0f));

        for (int i = types.Count; i < particleCount; i++)
        {
            float r = Random.value;
            if (r < 0.33f) types.Add(new ParticleType(Color.green, 2.0f));
            else if (r < 0.66f) types.Add(new ParticleType(Color.yellow, 4.0f));
            else types.Add(new ParticleType(Color.red, 7.0f));
        }

        for (int i = 0; i < types.Count; i++)
        {
            int randomIndex = Random.Range(i, types.Count);
            var temp = types[i];
            types[i] = types[randomIndex];
            types[randomIndex] = temp;
        }

        List<UniTask> particleTasks = new List<UniTask>();
        foreach (var type in types)
        {
            if (this == null) break;
            float startAngle = Random.Range(0f, 360f);
            GameObject p = Instantiate(particlePrefab, transform.position, Quaternion.identity);
            particleTasks.Add(AnimateSingleParticle(p, startAngle, type, ct));
            await UniTask.Delay(30, cancellationToken: ct);
        }

        await UniTask.WhenAll(particleTasks);
    }

    private async UniTask AnimateSingleParticle(GameObject p, float startAngle, ParticleType type, CancellationToken ct)
    {
        var sr = p.GetComponent<SpriteRenderer>();
        sr.color = type.color;
        sr.material.SetFloat("_FlashAmount", 1f);

        float elapsed = 0f;
        float duration = fadeDuration + 0.5f;
        float individualSpeed = Random.Range(200f, 400f);
        Vector3 baseScale = Vector3.one * type.sizeScale * 0.2f;

        // 収束地点を固定
        Vector3 targetPos = (this != null) ? transform.position : p.transform.position;

        while (elapsed < duration && !ct.IsCancellationRequested)
        {
            if (p == null) return;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentRadius = Mathf.Lerp(startRadius, 0f, t);
            float angle = startAngle + (elapsed * individualSpeed);
            Vector3 offset = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0) * currentRadius;

            p.transform.position = targetPos + offset;
            p.transform.localScale = Vector3.Lerp(baseScale, Vector3.zero, t);

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (p != null) Destroy(p);
    }

    private async UniTaskVoid PlayBurstEffect(Vector3 burstPos, CancellationToken ct)
    {
        for (int i = 0; i < burstCount; i++)
        {
            GameObject p = Instantiate(particlePrefab, burstPos, Quaternion.identity);
            AnimateBurstParticle(p, ct).Forget();
        }
    }

    private async UniTaskVoid AnimateBurstParticle(GameObject p, CancellationToken ct)
    {
        var sr = p.GetComponent<SpriteRenderer>();
        sr.color = Color.Lerp(Color.white, Color.yellow, Random.value);
        sr.material.SetFloat("_FlashAmount", 1f);

        Vector3 direction = Random.insideUnitCircle.normalized;
        float speed = Random.Range(burstSpeed * 0.5f, burstSpeed);
        float elapsed = 0f;

        while (elapsed < burstDuration && !ct.IsCancellationRequested)
        {
            if (p == null) return;
            elapsed += Time.deltaTime;
            float t = elapsed / burstDuration;

            p.transform.position += direction * speed * Time.deltaTime;
            speed *= 0.9f;
            p.transform.localScale = Vector3.Lerp(new Vector3(0.1f, 0.5f, 1f), Vector3.zero, t);
            p.transform.up = direction;

            Color c = sr.color;
            c.a = 1f - t;
            sr.color = c;

            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        if (p != null) Destroy(p);
    }
}
