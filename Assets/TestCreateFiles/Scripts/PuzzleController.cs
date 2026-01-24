using Cysharp.Threading.Tasks;
using System.Transactions;
using TMPro;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static SceneTransitData;
public class PuzzleController : MonoBehaviour
{
    //現在使用するゲームルール
    [SerializeField] private GameObject ruleObject; //パズルルール

    [SerializeField] private TestRule puzzleRule;   
    [SerializeField] private BattleRule battleRule;   //バトルルール

    [SerializeField] private GameObject puzzleParent;
    [SerializeField] private GameObject battleParent;
    [SerializeField] private GameObject tutorialObject;

    private IPuzzleRule currentRule;
    [SerializeField]private float currentTimer;
    [SerializeField] private int maxDamageCap = 500;
    private int currentTotalDamage = 0;
    private bool isTimerActive;
    private bool isGameEnd = false;
    public GameState currentState;

    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private UnitStatus targetEnemy;
    [SerializeField] private bool isGameClear = false;
    [SerializeField] private Slider timerSlider;


    [Header("CountDown")]
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private GameObject mainParentObject;

    [Header("Enemy Attack Settings")]
    [SerializeField] private Transform enemyAttackOrigin; // 敵の口元など、パーティクル発生源


    [Header("SceneTransition")]
    [SerializeField] private SceneTransitData data;
    [SerializeField] private SceneTransitionManager sceneTransitionManager;
    [SerializeField] private SceneAddressCatalog catalog;

    [Header("clear settings")]
    [SerializeField] private EnemyDeathEffect enemyDeath;
    [SerializeField] private ClearProduction clearProduction;
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
        currentState = GameState.Paused;

        // --- 【変更点】チュートリアル表示とクリック待ち処理 ---
        if (tutorialObject != null)
        {
            // チュートリアルを表示
            tutorialObject.SetActive(true);


            // ユーザーがクリック（タップ）するまで待機
            await UniTask.WaitUntil(() => Input.GetMouseButtonDown(0), cancellationToken: this.GetCancellationTokenOnDestroy());

            // クリックされたらチュートリアルを非表示にする
            tutorialObject.SetActive(false);
        }
        // ---------------------------------------------------

        // カウントダウンUIを表示
        if (countdownPanel != null) countdownPanel.SetActive(true);
        // 盤面はまだ隠しておく（チュートリアル中に見えていた場合はここで隠される）
        if (mainParentObject != null) mainParentObject.SetActive(false);

        int count = 1;
        while (count > 0)
        {
            if (countdownText != null) countdownText.text = "Ready?";

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
        isGameEnd = false;
    }

    


    //ゲーム開始
    private void StartGame()
    {        
        //タイマーの設定
        float limit = currentRule.GetTimeLimit();
        OnTurnStart();

        battleParent.SetActive(true);
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

    public void IsGameEndTrue()
    {
        isGameEnd = true;
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

        data.payload.chap = "CHAP1";    //チャプターを移行

        await enemyDeath.PlayDeathEffectAsync();
        await UniTask.Delay(3000);
        await clearProduction.PlayFullAnimationAsync();

        await UniTask.Delay(3000);
        await sceneTransitionManager.LoadSceneAsync(catalog.Get(SceneId.BookUI), data.payload);


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

        uiManager.ShowDamage(currentTotalDamage, targetEnemy.transform.position);
    }

    //戦闘からパズルに移行
    public void SwitchToPuzzleRule()
    {
        puzzleRule.ProcessFogTurnChange();
        ResetDamage();

        if (isGameEnd)    //残りターン数が０ならば
        {
            GameOver();
        }
        else
        {
            currentRule = puzzleRule;
            OnTimerRestart();
            currentState = GameState.Playing;

        }
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

        if (targetEnemy != null) targetEnemy.TakeDamage(damage);

    }

    public async UniTask EnemyAttackSpawnFog(int count, int lifeTurn)
    {
        if (puzzleRule == null) return;

        // 発生源が設定されていなければ、とりあえず敵のTargetの位置を使う等の安全策
        Vector3 startPos = Vector3.zero;
        if (enemyAttackOrigin != null)
        {
            startPos = enemyAttackOrigin.position;
        }
        else if (targetEnemy != null)
        {
            startPos = targetEnemy.transform.position;
        }


        // TestRuleのアニメーション付き生成を呼び出し、完了を待つ
        await puzzleRule.SpawnFogWithAnimation(count, lifeTurn, startPos);


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
