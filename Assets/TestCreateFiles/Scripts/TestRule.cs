using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class TestRule : GridPuzzleBase
{
    private PuzzleController controller;
    //ゲーム内の制限時間等
    [Header("Limit Settings")]
    [SerializeField] private float timeLimit;

    //プレファブ
    [Header("Prefabs")]
    [SerializeField] private GameObject blockPiecePrefab;
    [SerializeField] private GameObject gridCellPrefab;

    [Header("Block Shape[設計図]")]
    [SerializeField] private BlockShape[] level1Shapes;
    [SerializeField] private BlockShape[] level2Shapes;
    [SerializeField] private BlockShape[] level3Shapes;

    [Header("Damage Multipliers")]
    [SerializeField] private float level1Multipliers = 1.0f;
    [SerializeField] private float level2Multipliers = 1.2f;
    [SerializeField] private float level3Multipliers = 1.4f;


    [Header("Effect Settings")]
    [SerializeField] private GameObject clearEffectPrefab; // 粒子のプレハブ
    [SerializeField] private Transform effectTarget;       // 飛ばしたい先のGameObject
    [SerializeField] private Transform effectTarget2;
    [SerializeField] private float effectDuration = 2f;  // 飛んでいく時間

    [Header("Fog Attack Settings")]
    [SerializeField] private GameObject fogProjectilePrefab; // 飛んでくるパーティクルのPrefab
    [SerializeField] private float flightDuration = 0.5f;    // 飛ぶのにかかる時間

    //ドラッグ操作用
    private BlockGroup draggedBlockGroup;        //現在ドラッグ中のブロック
    private BlockGroup shadowGroup;

    private Vector3 mouseOffset;        //マウスとブロックの中心のずれ
    private Vector3 blockOriginalPosition;      //ドラッグ開始時のブロックのワールド位置
    private Vector2Int blockOriginalGridCoord;      //ドラッグ開始時のグリッド座標
    private Quaternion blockOriginalRotation;   //ドラッグ開始時のブロックの回転

    //影の設定
    [Header("Shadow Settings")]
    [SerializeField] private Vector3 shadowOffset;
    [SerializeField] private float shadowAlpha;

    //オブジェクト親を設定するためのTransform
    [SerializeField] private Transform gridCellParent;
    [SerializeField] private Transform blocksParent;

    [Header("Enemy Settings")]
    [SerializeField] private EnemyAnimation enemyAnimation;
    [SerializeField] private UnitStatus targetEnemy;


    //パレットの設置座標
    [Header("Pallete Settings")]
    [SerializeField] private Transform[] paletteSpawnSlots;

    //各スロットに現在入っているブロックのインスタンスを保持する配列
    private BlockGroup[] paletteGroups; 

    //ブロックインスタンスからどのスロットか(index)を逆引きするための辞書
    private Dictionary<BlockGroup, int> paletteSlotMap;

    //どのスロットからドラッグ開始したか
    private int draggedSlotIndex = -1;

    //制限ターン数
    [SerializeField] private int limitTurn = 3;

    [Header("Fog UI")]
    [SerializeField] private GameObject fogTurnTextPrefab; // 作成したプレハブをセット
    private GameObject[,] fogTextGrid; // テキストのインスタンスを管理する配列


    [Header("Pool Settings")]
    [SerializeField] private PrefabFactory groupFactory;
    [SerializeField] private PrefabFactory pieceFactory;

    private ObjectPool<BlockGroup> groupPool;
    private ObjectPool<piece> piecePool;

    //消去ライン数
    private int clearedLine = 0;

    //各グリッドに配置されているブロックのレベル
    private int[,] gridLevels;

    [Header("Palette Settings")]
    [SerializeField] private float paletteBlockScale = 0.6f; // 例: 60%のサイズにする

    private List<ClearEffect> activeEffects = new List<ClearEffect>();  //ClearEffectのリスト
    [SerializeField] private bool isBlockChainedThisTurn = false;    //そのターンでブロックがつながったか
    [Header("Text Effect")]
    [SerializeField] private GameObject popupTextPrefab;
    #region インターフェース
    public override void Initialize(PuzzleController controller)
    {
        base.Initialize(controller);

        gridLevels = new int[gridWidth, gridHeight];

        GameObject originObj = GameObject.FindWithTag("GridOrigin");

        //オブジェクトプール
        groupPool = new ObjectPool<BlockGroup>(groupFactory, initial: 3, max: 5);
        piecePool = new ObjectPool<piece>(pieceFactory, initial: 20, max: 50);
        groupPool.Prewarm();
        piecePool.Prewarm();

        InitializeFogTextGrid();

        if (originObj != null)
        {
            this.gridOrigin = originObj.transform;
            Debug.Log("GridOriginを見つけました");
        }
        else
        {
            Debug.LogWarning("GridOriginが見つからないため、ワールド原点(0,0)を試用します");
        }

        this.controller = controller;


        // インスペクターで設定されていない場合のみ、自動生成する
        if (gridCellParent == null)
        {
            GameObject cellObj = new GameObject("GridCells");
            cellObj.transform.SetParent(this.transform);
            cellObj.transform.localPosition = Vector3.zero; // 位置ズレ防止
            gridCellParent = cellObj.transform;
        }

        if (blocksParent == null)
        {
            GameObject blockObj = new GameObject("Blocks");
            blockObj.transform.SetParent(this.transform);
            blockObj.transform.localPosition = Vector3.zero; // 位置ズレ防止
            blocksParent = blockObj.transform;
        }

        //背景グリッドを生成
        CreateVisualGrid();

        paletteGroups = new BlockGroup[paletteSpawnSlots.Length];
        paletteSlotMap = new Dictionary<BlockGroup, int>();

        for (int i = 0; i < paletteSpawnSlots.Length; i++)
        {
            SpawnRandomBlockInSlot(i);
        }
    }

    public override void OnUpdate()
    {
        //Updateで呼び出す処理を作る場合、ここに追加する
    }

    /// <summary>
    /// 毎フレームの入力処理
    /// </summary>
    public override void HandleInput()
    {
        // ---マウスダウン (ブロックを掴む) ---
        if (Input.GetMouseButtonDown(1))
        {
            // ドラッグ中はパレット操作を受け付けないようにする（必要に応じて）
            if (draggedBlockGroup != null) return;

            Vector3 worldMousePos = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(worldMousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Block"))
            {
                BlockGroup hitGroup = hit.collider.GetComponentInParent<BlockGroup>();

                // パレットにあるブロックか確認し、スロット番号を特定して回転させる
                if (hitGroup != null && hitGroup.isPaletteBlock && paletteSlotMap.ContainsKey(hitGroup))
                {
                    int slotIndex = paletteSlotMap[hitGroup];
                    TryRotatePaletteBlock(slotIndex);
                }
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            if (isBlockChainedThisTurn) return; //攻撃演出中は無効

            //常にリセット
            draggedSlotIndex = -1;

            Vector3 worldMousePos = GetMouseWorldPosition();
            RaycastHit2D hit = Physics2D.Raycast(worldMousePos, Vector2.zero);

            if (hit.collider != null && hit.collider.CompareTag("Block"))
            {
                BlockGroup hitGroup = hit.collider.GetComponentInParent<BlockGroup>();
                if (hitGroup == null) return;

                //分岐: クリックしたのがパレットのブロックか？
                if (hitGroup.isPaletteBlock)
                {
                    draggedBlockGroup = hitGroup;
                    draggedBlockGroup.transform.localScale = Vector3.one;
                    //どのスロットから掴んだか記憶する
                    draggedSlotIndex = paletteSlotMap[hitGroup];
                }
                else
                {
                    // グリッド上のブロック
                    draggedBlockGroup = hitGroup;
                    blockOriginalGridCoord = WorldToGrid(draggedBlockGroup.transform.position);
                    RemoveBlockGroupFromGrid(draggedBlockGroup, blockOriginalGridCoord);
                    // (draggedSlotIndex は -1 のまま)
                }

                // 共通のドラッグ開始処理
                blockOriginalPosition = draggedBlockGroup.transform.position;
                blockOriginalRotation = draggedBlockGroup.transform.rotation;
                mouseOffset = draggedBlockGroup.transform.position - worldMousePos;

                //影を作成
                CreateShadow(draggedBlockGroup,2);
            }
        }

        // --- マウスホールド (ドラッグ と 回転) ---
        if (draggedBlockGroup != null) // (ドラッグ中であれば...という条件に)
        {
            // マウスに追従
            if (Input.GetMouseButton(0))
            {
                Vector3 targetPos = GetMouseWorldPosition() + mouseOffset;
                draggedBlockGroup.transform.position = targetPos;

                if (shadowGroup != null)
                {
                    shadowGroup.transform.position = targetPos + shadowOffset;
                }
            }

        }

        // ---マウスアップ (ドロップ) ---
        if (Input.GetMouseButtonUp(0) && draggedBlockGroup != null)
        {
            Vector3 checkPosition = (shadowGroup != null) ? shadowGroup.transform.position : draggedBlockGroup.transform.position;
            Vector2Int gridCoord = WorldToGrid(checkPosition);

            if (CanPlaceBlock(draggedBlockGroup, gridCoord))
            {
                // グリッドにスナップして配置
                draggedBlockGroup.transform.position = GridToWorld(gridCoord.x, gridCoord.y);
                PlaceBlockGroupOnGrid(draggedBlockGroup, gridCoord);
                controller.NotifyBlockLanded();

                // パレットから掴んだブロックの配置が成功した場合
                if (draggedSlotIndex != -1)
                {
                    //パレット内のブロックではなくなったので、フラグをfalseにする
                    draggedBlockGroup.isPaletteBlock = false;

                    //パレットからの参照を切る
                    paletteGroups[draggedSlotIndex] = null;
                    paletteSlotMap.Remove(draggedBlockGroup);

                    // そのスロットに新しいブロックを補充する
                    SpawnRandomBlockInSlot(draggedSlotIndex);

                    //そのターンにブロックがつながっていなかったら
                    if (!isBlockChainedThisTurn)
                    {
                        //敵の行動へ移行
                        ChangeGameStep();
                    }
                }
                    draggedBlockGroup.Release();

            }
            else
            {
                // 配置失敗
                // パレットから持ってきたブロックか？
                if (draggedSlotIndex != -1)
                {
                    // パレットからで置けなかった場合、そのクローンは元の位置、回転に戻す
                    draggedBlockGroup.transform.position = blockOriginalPosition;
                    draggedBlockGroup.transform.rotation = blockOriginalRotation;
                    draggedBlockGroup.transform.localScale = Vector3.one * paletteBlockScale;
                }
                else
                {
                    // グリッドからで置けなかった場合、元の位置・回転に戻す
                    draggedBlockGroup.transform.position = blockOriginalPosition;
                    draggedBlockGroup.transform.rotation = blockOriginalRotation;
                    PlaceBlockGroupOnGrid(draggedBlockGroup, blockOriginalGridCoord);
                }
            }

            //影を削除
            DestroyShadow();

            // ドラッグ状態を解除
            draggedBlockGroup = null;
            draggedSlotIndex = -1;

        }
    }
    /// <summary>
    /// ブロックが着地したときの処理
    /// </summary>
    public override void OnBlockLanded()
    {
        Debug.Log("ブロックが配置されました");

        //ブロック配置時、消去チェックやゲームオーバー処理を行う
        if (CheckForClear())
        {
            isBlockChainedThisTurn = true;
            Debug.Log("ライン消去");
        }

        if (IsGameOver())
        {
            controller.GameOver();
        }
    }


    /// <summary>
    /// 消去処理（行と列が埋まっているかスキャン）
    /// </summary>
    /// <returns></returns>
    public override bool CheckForClear()
    {
        // 消去すべきブロックの座標リスト
        List<Vector2Int> coordsToClear = new List<Vector2Int>();
        int linesClearedCount = 0;


        // もやを消した際の倍率計算用
        float damageMultiplier = 1.0f;
        int baseDamagePerLine = 10; // 1ラインあたりの基礎ダメージ

        // ---行（横）のチェック ---
        for (int y = 0; y < gridHeight; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x < gridWidth; x++)
            {
                if (gridInt[x, y] == 0)
                {
                    isRowFull = false;
                    break;
                }
            }
            if (isRowFull)
            {
                linesClearedCount++;
                for (int x = 0; x < gridWidth; x++) coordsToClear.Add(new Vector2Int(x, y));
            }
        }

        // ---列（縦）のチェック ---
        for (int x = 0; x < gridWidth; x++)
        {
            bool isColumnFull = true;
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridInt[x, y] == 0)
                {
                    isColumnFull = false;
                    break;
                }
            }
            if (isColumnFull)
            {
                linesClearedCount++;
                for (int y = 0; y < gridHeight; y++) coordsToClear.Add(new Vector2Int(x, y));
            }
        }

        // ---消去実行 ---
        if (linesClearedCount > 0)
        {

            List<PopupRequest> popupRequests = new List<PopupRequest>();

            if (coordsToClear.Count > 0)
            {
                Debug.Log("Clear!");
                foreach (Vector2Int coord in coordsToClear)
                {
                    // 既に処理済みならスキップ
                    if (gridInt[coord.x, coord.y] == 0) continue;

                    string popupMessage = "";
                    Color effectColor = Color.white;
                    float pertizleSize = 1.0f;
                    if (gridInt[coord.x, coord.y] == GridPuzzleBase.FOG_BLOCK_ID)
                    {
                        popupMessage = "大アップ";
                        effectColor = Color.white;
                    }
                    else
                    {
                        // レベルに応じた色（お好みの色に調整してください）
                        int level = gridLevels[coord.x, coord.y];
                        switch (level)
                        {
                            case 1:
                                popupMessage = ""; // レベル1は表示なし
                                effectColor = Color.white;
                                pertizleSize = 1.0f;
                                break;
                            case 2:
                                popupMessage = "小アップ";
                                effectColor = Color.yellow;
                                pertizleSize = 1.5f;
                                break;
                            case 3:
                                popupMessage = "中アップ";
                                effectColor = Color.cyan;
                                pertizleSize = 2.0f;
                                break;
                            default:
                                effectColor = Color.white;
                                pertizleSize = 1.0f;
                                break;
                        }
                    }

                    // --- 2. リストに追加 (位置をターゲットAの下に固定) ---
                    if (!string.IsNullOrEmpty(popupMessage) && effectTarget != null)
                    {
                        Vector3 spawnPos = effectTarget.position + new Vector3(0, -1.5f, 0);
                        spawnPos.z = -5f;

                        // 第三引数に effectColor を追加
                        popupRequests.Add(new PopupRequest(spawnPos, popupMessage, effectColor));
                    }

                    // --- ここでエフェクトを生成 ---
                    if (clearEffectPrefab != null && effectTarget != null)
                    {
                        Vector3 spawnPos = GridToWorld(coord.x, coord.y);
                        GameObject effectObj = Instantiate(clearEffectPrefab, spawnPos, Quaternion.identity);
                        effectObj.SetActive(true);

                        ClearEffect ce = effectObj.GetComponent<ClearEffect>();
                        if (ce != null)
                        {
                            ce.Play(effectTarget, 10f, effectColor, pertizleSize);
                            activeEffects.Add(ce); // リストに追加
                        }
                    }

                    // もやブロックかどうかの判定
                    // GridPuzzleBase.FOG_BLOCK_ID は定義した定数(例:99)を使ってください
                    if (gridInt[coord.x, coord.y] == GridPuzzleBase.FOG_BLOCK_ID)
                    {
                        damageMultiplier += 0.5f; // 例: 1つにつき50%アップ

                        // もやの寿命管理配列がある場合はリセットしておく
                        if (fogLifeGrid != null) fogLifeGrid[coord.x, coord.y] = 0;
                        RemoveFogText(coord.x, coord.y);
                    }
                    else
                    {
                        int level = gridLevels[coord.x, coord.y];
                        switch (level)
                        {
                            case 1:
                                damageMultiplier += level1Multipliers;
                                break;
                            case 2:
                                damageMultiplier += level2Multipliers;
                                break;
                            case 3:
                                damageMultiplier += level3Multipliers;
                                break;
                        }
                    }

                    //論理データをクリア
                    gridInt[coord.x, coord.y] = 0;
                    gridLevels[coord.x, coord.y] = 0;
                    //表示オブジェクトを処理
                    GameObject blockObj = gridVisuals[coord.x, coord.y];
                    if (blockObj != null)
                    {
                        // 親への通知 (必要なら)
                        // BlockGroup parentGroup = blockObj.GetComponentInParent<BlockGroup>();
                        // if (parentGroup != null) parentGroup.NotifyChildDestroyed(blockObj);

                        var p = blockObj.GetComponent<piece>();
                        if (p != null)
                        {
                            p.Release();
                        }
                        else
                        {
                            blockObj.SetActive(false);
                            Destroy(blockObj); // Poolを使っていないオブジェクトの場合のみDestroy
                        }

                        gridVisuals[coord.x, coord.y] = null;
                    }

                    if (cellObjects[coord.x, coord.y] != null)
                    {   
                        SetCellColor(cellObjects[coord.x, coord.y], 0);
                    }
                }
            }

            // 全てのブロックを確認した後で、倍率を適用してダメージを与える
            // (基礎攻撃力 * ライン数) * (もや倍率)
            int finalDamage = Mathf.FloorToInt((baseDamagePerLine * linesClearedCount) * damageMultiplier);
            //controller.AddDamage(finalDamage);

            Debug.Log($"Damage: {finalDamage} (Multiplier: {damageMultiplier})");
            ProcessClearSequence(popupRequests, effectTarget2, finalDamage).Forget();
            return true;
        }

        return false;
    }


    // 引数に int damage を追加
    public void TriggerAttack(Transform enemyTransform, int damage)
    {
        int totalEffects = activeEffects.Count;
        int finishedCount = 0;

        if (totalEffects == 0)
        {
            OnEffectHitEnemy(damage); // ここにも渡す
            return;
        }

        foreach (var effect in activeEffects)
        {
            if (effect != null)
            {
                effect.LaunchToTargetB(enemyTransform, () =>
                {
                    finishedCount++;
                    if (finishedCount >= totalEffects)
                    {
                        OnEffectHitEnemy(damage); // 全弾命中時に渡す
                    }
                });
            }
        }
        activeEffects.Clear();
    }

    // 引数でダメージを受け取る
    private void OnEffectHitEnemy(int damage)
    {
        // 非同期処理を呼ぶため、FireAndForget形式でラップして実行
        HandleEnemyHitSequence(damage).Forget();
    }
    private async UniTaskVoid HandleEnemyHitSequence(int damage)
    {
        // 1. ダメージをコントローラーに反映（HP減少など）
        controller.AddDamage(damage);


        // 2. 敵のシェイクアニメーション再生（待機する）
        if (enemyAnimation != null)
        {
            await enemyAnimation.PlayDamageShake(damage);
        }

        // 3. アニメーションが終わったらターン経過等の処理へ
        ChangeGameStep(); // もしここでターンを経過させるなら

        Debug.Log("ダメージ演出終了。ターン処理へ。");
    }

    /// <summary>     
    /// ゲームオーバー処理
    /// </summary>
    /// <returns></returns>
    public override bool IsGameOver()
    {
        //ゲームオーバーロジックを実装
        return false;
    }

    /// <summary>
    /// Controller側に制限時間を返す
    /// </summary>
    /// <returns></returns>
    public override float GetTimeLimit()
    {
        return timeLimit;
    }

    public override int GetTurnLimit()
    {
        return limitTurn;
    }

    /// <summary>
    /// タイマーが終了したときに呼び出される関数
    /// </summary>
    public override void OnTimerEnded()
    {
        //タイマーは廃止

        //Debug.Log("タイマーが終了しました。");
        //OnChainFinish();  
    }


    /// <summary>
    /// パズルシーンから戦闘シーンに移行
    /// </summary>
    public override void ChangeGameStep()
    {
        if (controller == null) Debug.LogError("Controllerがnullです！");
        controller.SwitchToBattleRule();
    }



    /// <summary>
    /// プールから出したブロックを初期状態(新品)に戻す
    /// </summary>
    private void ResetBlockState(GameObject blockObj)
    {
        // 1. コライダーを復活させる
        var col = blockObj.GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false; // Triggerにしていた場合は戻す
        }

        // 2. 描画設定を戻す
        var sr = blockObj.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white; // 透明になっていたのを不透明(白)に戻す
            sr.sortingOrder = 0;    // 奥に行っていたのを手前に戻す（0または適切な値）

            // もし影用のマテリアルなどをセットしていた場合はここで元に戻す
            // sr.material = defaultMaterial; 
        }

        // 3. レイヤーを戻す (影用レイヤーに変更していた場合)
        // Default または 設定しているレイヤー名("Block"など) に戻す
        blockObj.layer = LayerMask.NameToLayer("Default");

        // 4. 回転やスケールも念のためリセット
        blockObj.transform.localScale = Vector3.one;
        blockObj.transform.rotation = Quaternion.identity;
    }

    #endregion

    #region ヘルパー関数

    private void CreateVisualGrid()
    {
        if (gridCellPrefab == null) return; //プレファブがnullならなにもしない


        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                //(x,y)の位置にgridCellPrefabを生成
                GameObject cell = Instantiate(gridCellPrefab, GridToWorld(x, y), Quaternion.identity, gridCellParent);

                cellObjects[x, y] = cell;

                ////初期化時に盤面の色を設定するコードだが、一旦コメントアウト
                SetCellColor(cell, 0);
            }
        }
    }

    /// <summary>
    /// 指定したワールド座標にブロックの塊を生成
    /// </summary>
    /// <param name="prefab"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    private BlockGroup SpawnBlockAt(BlockShape shape, Vector3 worldPos)
    {
        BlockGroup newGroup = groupPool.Get();

        // 【修正】先に初期化を行います（ここで一度位置がリセットされます）
        newGroup.Initialize(shape, piecePool, blocksParent, this.cellSize);

        if (newGroup.childBlocks != null)
        {
            foreach (GameObject child in newGroup.childBlocks)
            {
                if (child != null)
                {
                    ResetBlockState(child);
                }
            }
        }


        // 【修正】初期化が終わった後に、本来置きたい位置で上書きします
        newGroup.transform.position = worldPos;

        newGroup.name = $"BlockGroup_{shape.name}";

        return newGroup;
    }





    /// <summary>
    /// マウスのスクリーン座標をワールド座標に変換
    /// </summary>
    /// <returns></returns>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Camera.main.nearClipPlane + 10f;   //Z座標をカメラの少し前に設定
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// ブロックの塊を指定のグリッド座標に配置可能かチェック
    /// </summary>
    /// <param name="group"></param>
    /// <param name="centerCoord"></param>
    /// <returns></returns>
    private bool CanPlaceBlock(BlockGroup group, Vector2Int centerCoord)
    {
        List<Vector2Int> rotatedShape = GetRotatedShapeCoordinates(group);

        //設計図に従って、全ての欠片の座標をチェック
        foreach (Vector2Int rotatedLocalCoord in rotatedShape)
        {
            //中心からの相対座標を、絶対グリッド座標に変換
            Vector2Int targetCoord = centerCoord + rotatedLocalCoord;

            //グリッドの範囲内か?
            if (!IsValidGridPosition(targetCoord))
            {
                return false; //範囲外
            }

            //既にほかのブロックがあるか?
            if (gridInt[targetCoord.x, targetCoord.y] != 0)
            {
                return false; //空いていない
            }

        }
        return true;
    }

    /// <summary>
    /// ブロックの塊をグリッドデータに書き込む
    /// </summary>
    /// <param name="group"></param>
    /// <param name="centerCoord"></param>
    private void PlaceBlockGroupOnGrid(BlockGroup group, Vector2Int centerCoord)
    {
        List<Vector2Int> rotatedShape = GetRotatedShapeCoordinates(group);

        //子オブジェクトへの参照をグリッドに書き込む
        int i = 0;

        //ブロックの種類を書き込む
        int elementId = (int)group.shape.blockElement;
        int blockId = elementId + 1;

        foreach (Vector2Int rotatedLocalCoord in rotatedShape)
        {
            Vector2Int targetCoord = centerCoord + rotatedLocalCoord;
            if (IsValidGridPosition(targetCoord))
            {


                Debug.Log("ID" + blockId);
                gridInt[targetCoord.x, targetCoord.y] = blockId;
                gridLevels[targetCoord.x,targetCoord.y] = group.blockLevel;
                //背景セルの色を変更する
                if (cellObjects[targetCoord.x, targetCoord.y] != null)
                {
                    Color color = GetColorFromID(blockId);
                    SetGridCellColor(targetCoord,color);
                }

                gridVisuals[targetCoord.x, targetCoord.y] = null;
            }
            i++;
        }
    }

    /// <summary>
    /// グリッドからブロックグループを取り除く
    /// </summary>
    /// <param name="group"></param>
    /// <param name="centerCoord"></param>
    private void RemoveBlockGroupFromGrid(BlockGroup group, Vector2Int centerCoord)
    {
        List<Vector2Int> rotatedShpae = GetRotatedShapeCoordinates(group);

        foreach (Vector2Int rotatedLocalCoord in rotatedShpae)
        {
            Vector2Int targetCoord = centerCoord + rotatedLocalCoord;

            if (IsValidGridPosition(targetCoord))
            {
                GameObject currentObj = gridVisuals[targetCoord.x, targetCoord.y];

                if (currentObj != null && currentObj.transform.IsChildOf(group.transform))
                {
                    gridLevels[targetCoord.x, targetCoord.y] = 0;
                    gridInt[targetCoord.x, targetCoord.y] = 0;
                    gridVisuals[targetCoord.x, targetCoord.y] = null;
                }

            }
        }
    }

    private List<Vector2Int> GetRotatedShapeCoordinates(BlockGroup group)
    {
        List<Vector2Int> rotatedCoords = new List<Vector2Int>();
        Quaternion rotation = group.transform.rotation;

        foreach (Vector2Int localCoord in group.shape.ShapeCoordinates)
        {
            Vector3 rotatedCoord = rotation * new Vector3(localCoord.x, localCoord.y, 0);
            Vector2Int intCoord = new Vector2Int(Mathf.RoundToInt(rotatedCoord.x), Mathf.RoundToInt(rotatedCoord.y));
            rotatedCoords.Add(intCoord);
        }
        return rotatedCoords;
    }

    /// <summary>
    /// 指定されたスロットに、スロットに応じたレベルのブロックを生成(補充)する
    /// </summary>
    /// <param name="slotIndex"></param>
    private void SpawnRandomBlockInSlot(int slotIndex)
    {
        // 1. スロット番号に応じて、使用する設計図リストを選択する
        BlockShape[] targetShapes = null;

        int targetLevel = 1;


        switch (slotIndex)
        {
            case 0:
                targetShapes = level1Shapes;
                targetLevel = 1;
                break;
            case 1:
                targetLevel = 2;
                targetShapes = level2Shapes;
                break;
            case 2:
                targetLevel = 3;
                targetShapes = level3Shapes;
                break;
            default:
                // スロットが3つ以上ある場合のフォールバック（例：Level1を使う）
                targetLevel = 1;
                targetShapes = level1Shapes;
                break;
        }

        // リストが空、またはnullの場合は処理を中断
        if (targetShapes == null || targetShapes.Length == 0) return;

        // ---------------------------------------------------------
        if (paletteGroups[slotIndex] != null)
        {
            paletteSlotMap.Remove(paletteGroups[slotIndex]);
            paletteGroups[slotIndex].Release();
            paletteGroups[slotIndex] = null;
        }

        // 選択されたリストからランダムに取得
        BlockShape randomShape = targetShapes[UnityEngine.Random.Range(0, targetShapes.Length)];

        // 1. 本来のスロット位置
        Vector3 spawnPos = paletteSpawnSlots[slotIndex].position;

        // 2. ブロックを生成（一旦スロット位置に置く）
        BlockGroup newGroup = SpawnBlockAt(randomShape, spawnPos);

        newGroup.transform.rotation = Quaternion.identity;
        newGroup.blockLevel = targetLevel;

        // 3. スケールを適用
        newGroup.transform.localScale = Vector3.one * paletteBlockScale;

        // 4. 重心補正を行う
        //    形状の中心ズレ(Vector3) × セルサイズ × 表示スケール
        Vector3 centerOffset = GetShapeCenter(randomShape);

        //    現在の位置から、中心ズレ分だけ「引く」ことで、見た目の重心をスロット中央に合わせる
        newGroup.transform.position -= centerOffset * this.cellSize * paletteBlockScale;

        newGroup.transform.localScale = Vector3.one * paletteBlockScale;
        int colorId = (int)randomShape.blockElement + 1;
        foreach (GameObject blocks in newGroup.childBlocks)
        {
            SetCellColor(blocks, colorId);
        }

        newGroup.isPaletteBlock = true;
        paletteGroups[slotIndex] = newGroup;
        paletteSlotMap[newGroup] = slotIndex;
    }

    /// <summary>
    /// ドラッグ中のブロックから影を作成する
    /// </summary>
    /// <param name="sourceGroup"></param>
    private void CreateShadow(BlockGroup sourceGroup, int rotationIndex)
    {
        shadowGroup = groupPool.Get();

        // 【修正】先に初期化を行います
        shadowGroup.Initialize(sourceGroup.shape, piecePool, sourceGroup.transform.parent, this.cellSize);

        // 【修正】その後に位置と回転をコピーして上書きします
        shadowGroup.transform.position = sourceGroup.transform.position;
        shadowGroup.transform.rotation = sourceGroup.transform.rotation;

        shadowGroup.isPaletteBlock = false;
        shadowGroup.name = "ShadowBlock";

        // 色を半透明にする処理（以下変更なし）
        foreach (var childBlock in shadowGroup.childBlocks)
        {
            if (childBlock == null) continue;

            Collider2D col = childBlock.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            SpriteRenderer sr = childBlock.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0, 0, 0, shadowAlpha);
                sr.sortingOrder = -1;
            }
        }
    }
    /// <summary>
    /// 影を削除する
    /// </summary>
    /// <param name="shadowGroup"></param>
    private void DestroyShadow()
    {
        if (shadowGroup != null)
        {
            shadowGroup.Release();
            shadowGroup = null;
        }
    }
    /// <summary>
    /// blockIdに応じてgridCellの色を設定
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="blockId"></param>
    private void SetCellColor(GameObject cell, int blockId)
    {
        if (cell == null) return;

        SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();

        Color targetColor = GetColorFromID(blockId);

        if (sr == null) return;


        sr.color = targetColor;
    }

    /// <summary>
    /// IDに対応する色を返す
    /// </summary>
    /// <param name="blockId"></param>
    /// <returns></returns>
    private Color GetColorFromID(int blockId)
    {
        return blockId switch
        {
            0 => Color.gray,
            1 => Color.red,
            2 => Color.green,
            3 => Color.blue,
            4 => Color.yellow,
            GridPuzzleBase.FOG_BLOCK_ID => new Color(0.5f,0,0.5f),
            _ => Color.black,
        };

    }

    public void SpawnFog(int count, int lifeTurn)
    {
        for(int i = 0; i < count; i++)
        {
            int tryCount = 0;
            while(tryCount < 100)
            {
                int rx = UnityEngine.Random.Range(0, gridWidth);
                int ry = UnityEngine.Random.Range(0, gridHeight);

                if (gridInt[rx,ry] == 0)
                {
                    gridInt[rx, ry] = FOG_BLOCK_ID;
                    fogLifeGrid[rx,ry] = lifeTurn;

                    SetCellColor(cellObjects[rx, ry], FOG_BLOCK_ID);
                    break;
                }
                tryCount++;
            }
        }
    }


    /// <summary>
    /// 敵の位置から「もや」を飛ばし、着弾後にグリッドを書き換える
    /// </summary>
    public async UniTask SpawnFogWithAnimation(int count, int lifeTurn, Vector3 startWorldPos)
    {
        List<UniTask> flightTasks = new List<UniTask>();

        for (int i = 0; i < count; i++)
        {
            int tryCount = 0;
            while (tryCount < 100)
            {
                int rx = UnityEngine.Random.Range(0, gridWidth);
                int ry = UnityEngine.Random.Range(0, gridHeight);

                // まだ空きマス、かつ予約済みでない場所を探す
                // (アニメーション中に重複して選ばれないように一時的なチェックが必要ですが、簡易的に0チェックのみにします)
                if (gridInt[rx, ry] == 0)
                {
                    // 重複を防ぐため、先にIDだけ埋めておく（「予約」状態）
                    // ※これをしないと、アニメーション中に次のループが同じマスを選んでしまう可能性があります
                    gridInt[rx, ry] = FOG_BLOCK_ID;

                    // まだ色は変えず、寿命だけセットしておく
                    fogLifeGrid[rx, ry] = lifeTurn;

                    // 1個分の「飛んでいく処理」を開始し、タスクリストに追加
                    flightTasks.Add(FlyAndSpawnFog(rx, ry, startWorldPos, lifeTurn));
                    break;
                }
                tryCount++;
            }
        }

        // 全てのもやが着弾するのを待つ
        await UniTask.WhenAll(flightTasks);
    }

    private async UniTask FlyAndSpawnFog(int x, int y, Vector3 startPos, int lifeTurn)
    {
        Vector3 targetPos = GridToWorld(x, y);

        // --- 1. 飛んでいく演出 ---
        if (fogProjectilePrefab != null)
        {
            GameObject proj = Instantiate(fogProjectilePrefab, startPos, Quaternion.identity);
            float duration = 0.5f;
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                proj.transform.position = Vector3.Lerp(startPos, targetPos, timer / duration);
                await UniTask.Yield(this.GetCancellationTokenOnDestroy());
            }
            Destroy(proj);
        }

        // --- 2. データ確定 ---
        gridInt[x, y] = FOG_BLOCK_ID;
        fogLifeGrid[x, y] = lifeTurn;

        // --- 3. 色の変更 (修正済み) ---
        // x,y を Vector2Int にまとめて渡します
        SetGridCellColor(new Vector2Int(x, y), new Color(0.5f, 0f, 0.5f));

        // --- 4. 文字表示 ---
        UpdateFogText(x, y, lifeTurn);
    }


    /// <summary>
    /// 待機所にある対応するインデックスのブロックを回転
    /// </summary>
    /// <param name="index"></param>
    // TestRule.cs

    public override void TryRotatePaletteBlock(int index)
    {
        if (index < 0 || index >= paletteGroups.Length) return;
        BlockGroup group = paletteGroups[index];
        if (group == null) return;

        // 1. 回転させる
        group.transform.Rotate(0, 0, 90);

        // 2. パレットのスロット位置に戻すための補正計算
        // 回転によって重心位置がずれるため、再度センタリング計算を行う

        // スロットの基準位置を取得
        Vector3 slotPos = paletteSpawnSlots[index].position;

        // 現在の形状（回転後）の中心ズレを計算
        // ※ GetShapeCenterは設計図(local)基準なので、回転後のローカル座標を考慮する必要がありますが、
        //   簡易的に「見た目のズレ」を修正するため、現在位置を再設定します。

        // 一旦スケールと回転を考慮して再配置
        // (SpawnRandomBlockInSlotで行っている重心補正と同じロジックを回転後にも適用する)

        // 回転後の形状座標を取得
        List<Vector2Int> rotatedCoords = GetRotatedShapeCoordinates(group);

        // 回転後の座標リストから中心(min/max)を算出
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (Vector2Int coord in rotatedCoords)
        {
            if (coord.x < minX) minX = coord.x;
            if (coord.x > maxX) maxX = coord.x;
            if (coord.y < minY) minY = coord.y;
            if (coord.y > maxY) maxY = coord.y;
        }

        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        Vector3 centerOffset = new Vector3(centerX, centerY, 0);

        // 新しい位置 = スロット位置 - (回転後の重心ズレ * セルサイズ * スケール)
        group.transform.position = slotPos - (centerOffset * this.cellSize * paletteBlockScale);
    }
    public void ProcessFogTurnChange()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (gridInt[x, y] == FOG_BLOCK_ID)
                {
                    fogLifeGrid[x, y]--;

                    // --- 追加: テキストの更新処理 ---
                    if (fogLifeGrid[x, y] > 0)
                    {
                        // 【修正】ここが空だったため、UpdateFogText を呼び出します
                        UpdateFogText(x, y, fogLifeGrid[x, y]);
                    }
                    else
                    {
                        // 爆発！ テキストを削除
                        RemoveFogText(x, y);

                        gridInt[x, y] = 0;
                        controller.IsGameEndTrue();
                        Debug.Log("もやが爆発した！");
                    }
                }
                else
                {
                    // もやじゃなくなった場所（何らかの理由で消えた等）のテキストは消しておく
                    RemoveFogText(x, y);
                }
            }
        }
    }

    /// <summary>
         /// 設計図の座標リストから、形状の中心（重心）を計算して返す
         /// </summary>
    private Vector3 GetShapeCenter(BlockShape shape)
    {
        // 最小値と最大値を見つける
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector2Int coord in shape.ShapeCoordinates)
        {
            if (coord.x < minX) minX = coord.x;
            if (coord.x > maxX) maxX = coord.x;
            if (coord.y < minY) minY = coord.y;
            if (coord.y > maxY) maxY = coord.y;
        }

        // 中心点を計算 ( (min + max) / 2 )
        float centerX = (minX + maxX) / 2f;
        float centerY = (minY + maxY) / 2f;

        return new Vector3(centerX, centerY, 0);
    }


    /// <summary>
    /// テキストを順番に出し、最後に攻撃を行うシーケンス
    /// </summary>
    private async UniTaskVoid ProcessClearSequence(List<PopupRequest> requests, Transform attackTarget, int damage)
    {
        float interval = 0.7f;

        foreach (var req in requests)
        {
            if (popupTextPrefab != null)
            {
                GameObject textObj = Instantiate(popupTextPrefab, req.Position, Quaternion.identity);
                var moveText = textObj.GetComponent<MoveTextDisplay>();
                if (moveText != null)
                {
                    // メッセージと一緒に色も渡す
                    moveText.Setup(req.Message, req.TextColor);
                }
            }

            await UniTask.Delay(TimeSpan.FromSeconds(interval));
        }
        // 全部のテキストが出終わった後、エフェクトが十分回るのを見せるための待機時間
        // テキストが少なかった場合でも最低限待つ時間を確保するため、必要なら調整してください
        await UniTask.Delay(TimeSpan.FromSeconds(1.5f));
        isBlockChainedThisTurn = false;
        // 最後に攻撃発射！
        TriggerAttack(attackTarget, damage);
    }

    // 初期化メソッド（Start、または Initialize メソッド内に追加してください）
    private void InitializeFogTextGrid()
    {
        // gridWidth, gridHeight が確定した後に呼び出す必要があります
        if (fogTextGrid == null)
        {
            fogTextGrid = new GameObject[gridWidth, gridHeight];
        }
    }

    /// <summary>
    /// 指定座標のもやの残りターン表示を更新（なければ生成）する
    /// </summary>
    public void UpdateFogText(int x, int y, int turns)
    {
        // 1. まずブロック(gridVisuals)があるか確認
        GameObject targetObj = gridVisuals[x, y];

        // 2. もしブロックがなければ、背景タイル(cellObjects)をもやの対象にする（仕様に合わせて選んでください）
        if (targetObj == null)
        {
            targetObj = cellObjects[x, y];
        }

        if (targetObj == null) return;

        piece p = targetObj.GetComponent<piece>();
        if (p != null)
        {
            p.SetFogDisplay(turns, true);
        }
        else
        {
            // デバッグログ：そもそもpieceスクリプトが見つかっているか？
            Debug.LogError($"{x},{y} のオブジェクト {targetObj.name} に piece.cs が付いていません！");
        }
    }

    // もやが消えた時の処理
    private void RemoveFogText(int x, int y)
    {
        GameObject blockObj = gridVisuals[x, y];
        if (blockObj == null) return;

        piece p = blockObj.GetComponent<piece>();
        if (p != null)
        {
            p.SetFogDisplay(0, false); // 非表示にする
        }
    }
    #endregion
    private class PopupRequest
    {
        public Vector3 Position;
        public string Message;
        public Color TextColor; // 色情報を追加

        public PopupRequest(Vector3 pos, string msg, Color color)
        {
            Position = pos;
            Message = msg;
            TextColor = color;
        }
    }

}

