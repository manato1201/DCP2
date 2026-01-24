using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

// 戦闘パートのロジック担当
public class BattleRule : MonoBehaviour, IPuzzleRule
{
    private PuzzleController controller;
    private int damageToDeal; // パズルから引き継いだダメージ量

    [SerializeField] private UnitStatus targetEnemy;

    [SerializeField] private int fogCount = 1;
    [SerializeField] private int fogLife = 3;

    [Header("Enemy Settings")]
    [SerializeField] private EnemyAnimation enemyAnimation;
    // パズルからデータを受け取って初期化できるようにする
    public void SetBattleData(int damage)
    {
        this.damageToDeal = damage;
    }

    public void Initialize(PuzzleController controller)
    {
        this.controller = controller;

        if (targetEnemy.isHPExistYet()) //HPが残っていたら
        {
            Debug.Log($"戦闘開始！ ダメージ: {damageToDeal}");
            AttackSequence();
        }
        else　　//HPが無かったら
        {
            Debug.Log("戦闘終了");
        }
    }
    public void SetTarget(UnitStatus target)
    {
        this.targetEnemy = target;
    }

    // 敵の攻撃処理（例：ProcessFogTurnChange や EnemyTurn の中）
    public async UniTask ProcessEnemyAttack()
    {
        Debug.Log("敵の攻撃開始！");

        // ★ここでアニメーション再生＆待機
        if (enemyAnimation != null)
        {
            await enemyAnimation.PlayAttackMotion();
        }

        Debug.Log("攻撃終了");
    }
    public void OnUpdate()
    {
        // アニメーション待ちや、戦闘終了判定を行う
        // 例: アニメーションが終わったらパズルに戻る
        if (Input.GetKeyDown(KeyCode.K))
        {
            // パズルに戻す処理を呼ぶ
            controller.SwitchToPuzzleRule();
            controller.OnTimerStart();
        }
    }

    private async void AttackSequence()
    {
        bool canceled = await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy())
                                     .SuppressCancellationThrow();

        if (canceled) return;


        await ProcessEnemyAttack();
        await controller.EnemyAttackSpawnFog(fogCount, fogLife);

        canceled = await UniTask.Delay(0, cancellationToken: this.GetCancellationTokenOnDestroy())
                                .SuppressCancellationThrow();

        if (canceled) return;

        controller.SwitchToPuzzleRule();
    }
    // --- 戦闘で使わないパズル用メソッドは空実装でOK ---
    public void HandleInput() { } // 戦闘中にタップでスキップさせるならここに書く
    public void OnBlockLanded() { }
    public bool CheckForClear() => false;
    public bool IsGameOver() => false;
    public float GetTimeLimit() => 0; // 戦闘に制限時間がなければ0
    public void OnTimerEnded() { }
    public void TryRotatePaletteBlock(int index) { }
    public void ChangeGameStep() { }

    public int GetTurnLimit() { return -1; }
}
