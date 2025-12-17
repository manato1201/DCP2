using UnityEngine;
using System;
using Cysharp.Threading.Tasks;

// 戦闘パートのロジック担当
public class BattleRule : MonoBehaviour, IPuzzleRule
{
    private PuzzleController controller;
    private int damageToDeal; // パズルから引き継いだダメージ量

    [SerializeField] private UnitStatus targetEnemy;

    // パズルからデータを受け取って初期化できるようにする
    public void SetBattleData(int damage)
    {
        this.damageToDeal = damage;
    }

    public void Initialize(PuzzleController controller)
    {
        this.controller = controller;
        Debug.Log($"戦闘開始！ ダメージ: {damageToDeal}");
        AttackSequence();
    }
    public void SetTarget(UnitStatus target)
    {
        this.targetEnemy = target;
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


        if (targetEnemy != null)
        {
            targetEnemy.TakeDamage(damageToDeal);
        }
        canceled = await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy())
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
