using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
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
    [SerializeField] private float currentTimer;
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

    [Header("Game Effect settings")]
    [SerializeField] private EnemyDeathEffect enemyDeath;
    [SerializeField] private ClearProduction clearProduction;

    [SerializeField] private ClearProduction overProduction;


    [Header("chapter settings")]
    [SerializeField] private SetChapterImages chapterImages;

    [Header("banner settings")]
    [SerializeField] BannerController bannerController;
    private bool isGameStarted = false; // ゲーム開始シーケンスが一度走ったかどうかのフラグ

    [SerializeField] private string dbChap;

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

        await UniTask.Delay(1000);

        if (bannerController != null)
        {
            bannerController.OpenBanner();

            await UniTask.WaitUntil(() => bannerController.IsClosed, cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        await UniTask.Delay(1000);

        if (countdownPanel != null) countdownPanel.SetActive(true);
        if (mainParentObject != null) mainParentObject.SetActive(false);

        if (countdownText != null)
        {
            // 1. 初期化：テキストをReadyにし、サイズを0にしておく
            countdownText.text = "よーい";
            countdownText.transform.localScale = Vector3.zero;
            countdownText.transform.localRotation = Quaternion.identity;

            // DOTweenのシーケンス作成
            Sequence seq = DOTween.Sequence();

            // 2. 「Ready?」が弾けるように登場 (Scale 0 -> 1.2 -> 1.0)
            seq.Append(countdownText.transform.DOScale(1.2f, 0.3f).SetEase(Ease.OutBack));
            seq.Append(countdownText.transform.DOScale(1.0f, 0.1f));

            // 3. 少し待機（Readyを見せる時間）
            seq.AppendInterval(0.6f);

            // 4. 回転しながら縮小（切り替えの準備）
            // Y軸で一回転しながら小さくする
            seq.Append(countdownText.transform.DORotate(new Vector3(0, 0, 360), 0.4f, RotateMode.LocalAxisAdd).SetEase(Ease.InBack));
            seq.Join(countdownText.transform.DOScale(0f, 0.4f).SetEase(Ease.InBack));

            // 5. 文字を「GO!」に切り替える
            seq.AppendCallback(() => {
                countdownText.text = "スタート!";
                countdownText.color = Color.yellow; // GO!だけ色を変えるのも効果的です
            });

            // 6. 「GO!」が爆発するように登場
            // 少し大きめの1.5倍まで弾けさせてから戻す
            seq.Append(countdownText.transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutElastic));
            seq.Append(countdownText.transform.DOScale(1.0f, 0.1f));

            // 7. 最後まで再生されるのを待つ
            seq.Play();
            await seq.AsyncWaitForCompletion().AsUniTask();
            // GO!を表示したまま少し余韻を残す
            await UniTask.Delay(500, cancellationToken: this.GetCancellationTokenOnDestroy());
        }
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

        //string nowChap = data.payload.chap;
        string nowChap = dbChap;

        if (nowChap == "") nowChap = "CHAP2";

        chapterImages.SetImagesForChapter(nowChap);

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

        if (targetEnemy != null)
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
        await overProduction.PlayFullAnimationAsync();
        await UniTask.Delay(3000);
        await sceneTransitionManager.LoadSceneAsync(catalog.Get(SceneId.Title), data.payload);
    }

    async public void GameClear()
    {
        isGameClear = true;

        data.payload.chap = "CHAP2";    //チャプターを移行


        await enemyDeath.PlayDeathEffectAsync();
        await UniTask.Delay(3000);
        await clearProduction.PlayFullAnimationAsync();

        await UniTask.Delay(3000);
        await sceneTransitionManager.LoadSceneAsync(catalog.Get(SceneId.Story), data.payload);


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

        if (isGameEnd)    //ゲームエンドがtrueならば
        {
            //GameOver();
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

        if (currentTotalDamage > maxDamageCap)
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
