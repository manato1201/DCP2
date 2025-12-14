using UnityEngine;

using UnityEngine.SceneManagement;
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
    [SerializeField] private int currentTurn;

    private bool isTimerActive;

    public GameState currentState;

    [SerializeField] private BattleUIManager uiManager;
    [SerializeField] private UnitStatus targetEnemy;
    [SerializeField] private bool isGameClear = false; 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //OnControllerStart();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log(currentRule.ToString());
        }
        //OnControllerUpdate();
    }

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

    /// <summary>
    /// Startで呼び出し
    /// </summary>
    public void OnControllerStart()
    {
        currentRule = ruleObject.GetComponent<IPuzzleRule>();

        if (currentRule == null)
        {
            Debug.LogError("ruleObjectが設定されていません！ :PuzzleController");
            return;
        }
        OnTimerStart();
        
        StartGame();
    }

    /// <summary>
    /// Updateで呼び出し
    /// </summary>
    public void OnControllerUpdate()
    {
        //ルールに従って更新
        currentRule.OnUpdate();

        //入力処理
        currentRule.HandleInput();


        //タイマー処理
        if (isTimerActive)
        {
            currentTimer -= Time.deltaTime;
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
        currentTurn = currentRule.GetTurnLimit();
    }

    bool IsGameEnd()
    {
        if(currentTurn == 0)
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
        uiManager.SetHPUI(currentTurn);

        if (limit > 0)
        {
            currentTimer = limit;
            isTimerActive = true;
        }
        else if (limit <= 0)
        {
            isTimerActive = false;
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
    /// ゲームシステムに応じて変更
    /// </summary>
    /// <param name="score"></param>
    public void AddClearScore(int score)
    {
        Debug.Log("スコアは" +  score + "です");
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
    public void GameOver()
    {
        SceneManager.LoadScene("Over");
        
    }

    public void GameClear()
    {
        isGameClear = true;
        SceneManager.LoadScene("Clear");
    }


    //パズル内容からダメージを参照しバトルシーンへ移行
    public void SwitchToBattleRule(int damage)
    {
        battleRule.SetBattleData(damage);

        currentRule = battleRule;
        currentRule.Initialize(this);
        isTimerActive = false;
        currentState = GameState.Battle;
        puzzleParent.SetActive(false);

    }

    //戦闘からパズルに移行
    public void SwitchToPuzzleRule()
    {
        currentTurn--;
        uiManager.SetHPUI(currentTurn);

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

        }
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
