using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PuzzleController : MonoBehaviour
{
    //現在使用するゲームルール
    [SerializeField] private GameObject ruleObject; //パズルルール

    [SerializeField] private TestRule puzzleRule;   
    [SerializeField] private BattleRule battleRule;   //バトルルール

    [SerializeField] private GameObject puzzleParent;
    [SerializeField] private GameObject battleParent;

    private IPuzzleRule currentRule;
    [SerializeField]private float currentTimer;
    [SerializeField] private int playerLife;
    [SerializeField] private int maxDamageCap = 500;
    private int currentTotalDamage = 0;
    private bool isTimerActive;

    public GameState currentState;

    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private UnitStatus targetEnemy;
    [SerializeField] private bool isGameClear = false;
    [SerializeField] private Slider timerSlider;


    [Header("CountDown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject mainParentObject;

    //---ボタン参照---

    /// <summary>
    /// ボタンで呼び出すことを想定した、ブロックの回転メソッド
    /// </summary>
    /// <param name="index"></param>
    public void OnRotateButtonPressed(int index)
    {
        if (currentState != GameState.Playing) return;
        
        currentRule.TryRotatePaletteBlock(index);
    }


    private async UniTaskVoid StartGameSequence()
    {
        // 1. 準備状態にする（この間は操作不能にする）
        // もしGameStateに "Preparing" がなければ追加するか、Pause扱いにしておく
        currentState = GameState.Paused;

        // カウントダウンUIを表示、盤面はまだ隠しておく（必要なら）
        if (countdownPanel != null) countdownPanel.SetActive(true);
        if (mainParentObject != null) mainParentObject.SetActive(false);

        // 2. カウントダウン処理 (3 -> 2 -> 1 -> GO)
        int count = 3;
        while (count > 0)
        {
            if (countdownText != null) countdownText.text = count.ToString();

            // 1秒待機 (キャンセル対応付き)
            await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

            count--;
        }

        // "GO!" 表示
        if (countdownText != null) countdownText.text = "GO!";
        await UniTask.Delay(1000, cancellationToken: this.GetCancellationTokenOnDestroy());

        // 3. ゲーム開始
        if (countdownPanel != null) countdownPanel.SetActive(false); // パネルを消す
        if (mainParentObject != null) mainParentObject.SetActive(true); // ★指定のオブジェクトを表示

        // ステートをプレイ中に変更
        currentState = GameState.Playing;

        // タイマー計測開始などの処理があればここで呼ぶ
        OnTimerStart();
        StartGame();
        Debug.Log("Game Started!");
    }

    


    /// <summary>
    /// Startで呼び出し
    /// </summary>
    public void OnControllerStart()
    {
        currentRule = ruleObject.GetComponent<IPuzzleRule>();

        if (currentRule == null)
        {
            Debug.LogError("ruleObjectにIPuzzleRuleが実装されていません！ :PuzzleController");
            return;
        }

        ResetDamage();

        StartGameSequence().Forget();
    }
    /// <summary>
    /// Updateで呼び出し
    /// </summary>
    public void OnControllerUpdate()
    {
        if (currentState != GameState.Playing) return;
        if (currentRule == null) return;

        //ルールに従って更新
        currentRule.OnUpdate();

        //入力処理
        currentRule.HandleInput();


        //タイマー処理
        if (isTimerActive)
        {
            currentTimer -= Time.deltaTime;

            if (timerSlider != null)
            {
                timerSlider.value = currentTimer;
            }

            //タイマーのUIの更新処理はここでの呼び出しを想定
            if (currentTimer <= 0)
            {
                currentTimer = 0;
                isTimerActive = false;

                currentRule.OnTimerEnded();
                //ルール側で攻撃力の確定
                //パズル→戦闘への遷移
            }
        }
    }

    public void OnTimerStart()
    {
        isTimerActive = true;
    }

    public void OnTimerRestart()
    {
        currentTimer = currentRule.GetTimeLimit();
        isTimerActive = true;
    }

    public void OnTurnStart()
    {
        playerLife = currentRule.GetTurnLimit();
    }

    bool IsGameEnd()
    {
        if(playerLife == 0)
        {
            return true;
        }

        return false;
    }

    //ゲーム開始
    private void StartGame()
    {        
        //タイマーの設定
        float limit = currentRule.GetTimeLimit();
        OnTurnStart();
        uiManager.SetHPUI(playerLife);

        battleParent.SetActive(false);
        puzzleParent.SetActive(true);

        if (limit > 0)
        {
            currentTimer = limit;
            isTimerActive = true;
        }
        else if (limit <= 0)
        {
            isTimerActive = false;
        }

        if (targetEnemy != null && uiManager != null)
        {
            // 重複登録を防ぐために一度削除してから登録
            targetEnemy.OnHPChanged.RemoveListener(uiManager.SetHPSlider);
            targetEnemy.OnHPChanged.AddListener(uiManager.SetHPSlider);

            // 死亡時のイベントなども必要ならここで登録
            // targetEnemy.OnDead.RemoveListener(OnEnemyDead);
            // targetEnemy.OnDead.AddListener(OnEnemyDead);
        }


        if (timerSlider != null)
        {
            timerSlider.maxValue = limit;
            timerSlider.value = limit;
        }

        if(targetEnemy != null)
        {
            targetEnemy.OnUIStart();
        }
        currentState = GameState.Playing;
        //ルールに従って初期化
        currentRule.Initialize(this);
    }


    /// <summary>
    /// ルール側でブロックを設置した際に通知
    /// </summary>
    public void NotifyBlockLanded()
    {
        //ルール側に着地を通知
        currentRule.OnBlockLanded();
    }

    /// <summary>
    /// ゲームオーバー
    /// </summary>
    async public void GameOver()
    {
        await UniTask.Delay(5000);
        SceneManager.LoadScene("Over");
        
    }

    async public void GameClear()
    {
        isGameClear = true;

        await UniTask.Delay(5000);
        SceneManager.LoadScene("Clear");
    }


    //パズル内容からダメージを参照しバトルシーンへ移行
    public void SwitchToBattleRule()
    {
        battleRule.SetBattleData(currentTotalDamage);
        battleRule.SetTarget(targetEnemy);
        currentRule = battleRule;
        currentRule.Initialize(this);
        isTimerActive = false;
        currentState = GameState.Battle;
        puzzleParent.SetActive(false);
        battleParent.SetActive(true);

        uiManager.ShowDamage(currentTotalDamage, targetEnemy.transform.position);
    }

    //戦闘からパズルに移行
    public void SwitchToPuzzleRule()
    {
        puzzleRule.ProcessFogTurnChange();
        uiManager.SetHPUI(playerLife);
        ResetDamage();

        if (IsGameEnd())    //残りターン数が０ならば
        {
            GameOver();
        }
        else
        {
            currentRule = puzzleRule;
            OnTimerRestart();
            currentState = GameState.Playing;
            puzzleParent.SetActive(true);
            battleParent.SetActive(false);

        }
    }

    public void DamagePlayer(int damageAmount)
    {
        playerLife -= damageAmount;
        if (playerLife < 0) playerLife = 0;
        uiManager.SetHPUI(playerLife);
    }

    public void ResetDamage()
    {
        currentTotalDamage = 0;
        uiManager.UpdateAttackGauge(currentTotalDamage, maxDamageCap);
    }

    public void AddDamage(int damage)
    {
        currentTotalDamage += damage;

        if(currentTotalDamage > maxDamageCap)
        {
            currentTotalDamage = maxDamageCap;
        }

        uiManager.UpdateAttackGauge(currentTotalDamage, maxDamageCap);

    }

    public void EnemyAttackSpawnFog(int count, int life)
    {
        puzzleRule.SpawnFog(count, life);
    }

}


public enum GameState
{
    Null,
    Playing,
    Paused,
    GameOver,
    Battle,
}
