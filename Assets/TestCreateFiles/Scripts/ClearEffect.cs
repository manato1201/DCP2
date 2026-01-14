using UnityEngine;

public class ClearEffect : MonoBehaviour
{
    private ParticleSystem partSystem;
    private ParticleSystem.Particle[] particles;
    private Transform target;
    private float speed = 3f;
    private bool initialized = false;

    public void Play(Transform target, float speed)
    {
        this.target = target;
        this.speed = speed;
        partSystem = GetComponent<ParticleSystem>();

        // パーティクルの最大数分の配列を確保
        particles = new ParticleSystem.Particle[partSystem.main.maxParticles];
        initialized = true;
    }

    void LateUpdate()
    {
        if (!initialized || target == null) return;

        // アクティブなパーティクルを取得
        int numParticlesAlive = partSystem.GetParticles(particles);
        float step = speed * Time.deltaTime;

        for (int i = 0; i < numParticlesAlive; i++)
        {
            // 各粒子のワールド座標を計算（Simulation SpaceがLocalの場合を考慮）
            Vector3 particleWorldPos;
            if (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                particleWorldPos = transform.TransformPoint(particles[i].position);
            }
            else
            {
                particleWorldPos = particles[i].position;
            }

            // ターゲットに向かって移動
            particleWorldPos = Vector3.MoveTowards(particleWorldPos, target.position, step);

            // 到着判定：ターゲットに非常に近づいたら寿命を0にして消す
            if (Vector3.Distance(particleWorldPos, target.position) < 0.1f)
            {
                particles[i].remainingLifetime = -1f;
            }

            // 座標を戻す
            if (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
            {
                particles[i].position = transform.InverseTransformPoint(particleWorldPos);
            }
            else
            {
                particles[i].position = particleWorldPos;
            }

            // 徐々に加速させる演出（お好みで）
            speed += 0.1f;
        }

        // 変更したパーティクル情報を適用
        partSystem.SetParticles(particles, numParticlesAlive);

        // 全ての粒子が消えたら自分自身を削除
        if (numParticlesAlive == 0)
        {
            Destroy(gameObject);
        }
    }
}
