using System.Runtime.CompilerServices;
using UnityEngine;
using Cysharp.Threading.Tasks;
public class PuzzleController : MonoBehaviour
{
    //現在使用するゲームルール
    [SerializeField] private GameObject ruleObject;

    private IPuzzleRule currentRule;
    [SerializeField]private float currentTimer;
    private bool isTimerActive;

    public GameState currentState;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //OnControllerStart();
    }

    // Update is called once per frame
    void Update()
    {
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
        
        StartGame();
    }

    /// <summary>
    /// Updateで呼び出し
    /// </summary>
    public void OnControllerUpdate()
    {
        if (currentState != GameState.Playing) return; //プレイ中ではない場合は行わない

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

    //ゲーム開始
    private void StartGame()
    {        
        //タイマーの設定
        float limit = currentRule.GetTimeLimit();
        if (limit > 0)
        {
            currentTimer = limit;
            isTimerActive = true;
        }
        else if (limit <= 0)
        {
            isTimerActive = false;
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

    public void GameOver()
    {
        currentState = GameState.GameOver;
        
    }

    
}

public enum GameState
{
    Null,
    Playing,
    Paused,
    GameOver,
}
