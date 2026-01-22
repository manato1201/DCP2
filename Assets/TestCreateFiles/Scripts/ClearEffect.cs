using System;
using UnityEngine;

public class ClearEffect : MonoBehaviour
{
    private enum EffectState { MovingToA, RandomOrbit, MovingToB }
    private EffectState currentState = EffectState.MovingToA;

    private ParticleSystem partSystem;
    private ParticleSystem.Particle[] particles;
    private Vector3[] individualTargets; // 各粒子の現在の目的地

    private Transform targetA;
    private Transform targetB;

    [Header("Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float orbitRadius = 0.8f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    private bool initialized = false;

    private Action onHitCallback;
    private bool hasHitTriggered = false; // 二重呼び出し防止フラグ
    public void Play(Transform targetA, float speed, Color color)
    {
        this.targetA = targetA;
        this.speed = speed;
        partSystem = GetComponent<ParticleSystem>();

        var main = partSystem.main;
        main.startColor = color;

        int maxParticles = partSystem.main.maxParticles;
        particles = new ParticleSystem.Particle[maxParticles];
        individualTargets = new Vector3[maxParticles];

        // 初期目的地をセット
        for (int i = 0; i < maxParticles; i++)
        {
            individualTargets[i] = GetRandomPointOnCircle(targetA.position);
        }

        initialized = true;
        currentState = EffectState.MovingToA;
    }

    // 引数に Action onHit = null を追加
    public void LaunchToTargetB(Transform targetB, Action onHit = null)
    {
        this.targetB = targetB;
        this.onHitCallback = onHit; // 受け取った関数を保存
        this.hasHitTriggered = false; // フラグをリセット
        currentState = EffectState.MovingToB;
    }
    private Vector3 GetRandomPointOnCircle(Vector3 center)
    {
        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * orbitRadius;
        return center + offset;
    }

    void LateUpdate()
    {
        if (!initialized || targetA == null) return;

        int numParticlesAlive = partSystem.GetParticles(particles);
        float deltaTimeStep = speed * Time.deltaTime;

        for (int i = 0; i < numParticlesAlive; i++)
        {
            Vector3 currentWorldPos = (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                ? transform.TransformPoint(particles[i].position)
                : particles[i].position;

            switch (currentState)
            {
                case EffectState.MovingToA:
                    // 1. 最初は中心(A)に向かう
                    currentWorldPos = Vector3.MoveTowards(currentWorldPos, targetA.position, deltaTimeStep);
                    if (Vector3.Distance(currentWorldPos, targetA.position) < arrivalThreshold)
                    {
                        currentState = EffectState.RandomOrbit;
                    }
                    break;

                case EffectState.RandomOrbit:
                    // 2. 円周上のランダムな目的地に向かって直線移動
                    currentWorldPos = Vector3.MoveTowards(currentWorldPos, individualTargets[i], deltaTimeStep);

                    // 目的地に付いたら新しい目的地を円周上から選ぶ
                    if (Vector3.Distance(currentWorldPos, individualTargets[i]) < arrivalThreshold)
                    {
                        individualTargets[i] = GetRandomPointOnCircle(targetA.position);
                    }
                    break;

                case EffectState.MovingToB:
                    if (targetB != null)
                    {
                        currentWorldPos = Vector3.MoveTowards(currentWorldPos, targetB.position, deltaTimeStep * 1.5f);

                        // ターゲットに到達したか判定
                        if (Vector3.Distance(currentWorldPos, targetB.position) < arrivalThreshold)
                        {
                            // 最初の1粒が到達した瞬間にだけ関数を実行
                            if (!hasHitTriggered)
                            {
                                hasHitTriggered = true;
                                onHitCallback?.Invoke(); // 保存しておいた関数を実行！
                            }

                            particles[i].remainingLifetime = -1f;
                        }
                    }
                    break;
            }

            if (partSystem.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                particles[i].position = transform.InverseTransformPoint(currentWorldPos);
            else
                particles[i].position = currentWorldPos;
        }

        partSystem.SetParticles(particles, numParticlesAlive);

        // 全て消えたらオブジェクトを破棄
        if (numParticlesAlive == 0 && currentState == EffectState.MovingToB)
        {
            Destroy(gameObject);
        }
    }
}
