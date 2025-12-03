public interface IPuzzleRule
{
    //---パズルゲーム全体のひな型---

    //ゲーム開始時の初期化処理
    void Initialize(PuzzleController controller);

    //毎フレームの更新処理
    void OnUpdate();

    //プレイヤーの入力処理
    void HandleInput();

    //ブロックが着地したときの処理
    void OnBlockLanded();

    //ライン消去などのチェック処理
    bool CheckForClear();

    //ゲームオーバー判定
    bool IsGameOver();

    /// <summary>
    /// このルールの制限時間を返す
    /// -1または0の場合「時間制限無し」とする
    /// </summary>
    /// <returns></returns>
    float GetTimeLimit();

    /// <summary>
    /// 制限時間が切れた時の処理をルール側で設定する
    /// ゲームオーバーや次のターンへの進行などの処理を作る
    /// </summary>
    void OnTimerEnded();

    //ブロックの回転に利用する関数
    void TryRotatePaletteBlock(int index);

}