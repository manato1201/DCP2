using UnityEngine;
using System.Collections.Generic;

public class TestRule : GridPuzzleBase
{
    private PuzzleController controller;
    //タイマー
    [Header("Timer Settings")]
    [SerializeField] private float timeLimit;

    //プレファブ
    [Header("Prefabs")]
    [SerializeField] private GameObject blockPiecePrefab;
    [SerializeField] private GameObject gridCellPrefab;

    [Header("Block Shape[設計図]")]
    [SerializeField] private BlockShape[] availableShapes;

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
    private Transform gridCellParent;
    private Transform blocksParent;

    //パレットの設置座標
    [Header("Pallete Settings")]
    [SerializeField] private Transform[] paletteSpawnSlots;

    //各スロットに現在入っているブロックのインスタンスを保持する配列
    private BlockGroup[] paletteGroups;

    //ブロックインスタンスからどのスロットか(index)を逆引きするための辞書
    private Dictionary<BlockGroup, int> paletteSlotMap;

    //どのスロットからドラッグ開始したか
    private int draggedSlotIndex = -1;
    #region インターフェース
    public override void Initialize(PuzzleController controller)
    {
        base.Initialize(controller);

        GameObject originObj = GameObject.FindWithTag("GridOrigin");

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

        //("GridCells"という名前の空オブジェクトを生成)
        gridCellParent = new GameObject("GridCells").transform;
        //("Blocks"という名前の空オブジェクトを生成)
        blocksParent = new GameObject("Blocks").transform;

        gridCellParent.SetParent(this.transform);
        blocksParent.SetParent(this.transform);

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
        if (Input.GetMouseButtonDown(0))
        {
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

            // 回転
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                draggedBlockGroup.transform.Rotate(0, 0, -90);

            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                draggedBlockGroup.transform.Rotate(0, 0, 90);

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
                }

                Destroy(draggedBlockGroup.gameObject);
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

        // ---行（横）のチェック ---
        for (int y = 0; y < gridHeight; y++)
        {
            bool isRowFull = true;
            for (int x = 0; x < gridWidth; x++)
            {
                // intで判定 (0なら空いている)
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
                // intで判定
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
            controller.AddClearScore(linesClearedCount * 100);

            if (coordsToClear.Count > 0)
            {
                Debug.Log("Clear!");
                foreach (Vector2Int coord in coordsToClear)
                {
                    // 既に処理済みならスキップ
                    if (gridInt[coord.x, coord.y] == 0) continue;

                    //論理データをクリア
                    gridInt[coord.x, coord.y] = 0;

                    //表示オブジェクトを処理
                    GameObject blockObj = gridVisuals[coord.x, coord.y];
                    if (blockObj != null)
                    {
                        // 親への通知
                        BlockGroup parentGroup = blockObj.GetComponentInParent<BlockGroup>();
                        if (parentGroup != null) parentGroup.NotifyChildDestroyed(blockObj);

                        
                        Destroy(blockObj);

                        gridVisuals[coord.x, coord.y] = null;
                    }

                    if (cellObjects[coord.x, coord.y] != null)
                    {
                        SetCellColor(cellObjects[coord.x, coord.y], 0);
                    }
                }
                return true;
            }
        }
        return false;
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

    /// <summary>
    /// タイマーが終了したときに呼び出される関数
    /// </summary>
    public override void OnTimerEnded()
    {
        Debug.Log("タイマーが終了しました。");

    }




    #endregion

    #region ヘルパー関数

    private void CreateVisualGrid()
    {
        if (gridCellPrefab == null) return; //プレファブがnullならなにもしない

        Transform gridCellParent = new GameObject("GridCells").transform;

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
        //BlockGroupの親オブジェクト
        GameObject groupObj = new GameObject($"BlockGroup_{shape.name}");
        groupObj.transform.position = worldPos;
        groupObj.transform.SetParent(blocksParent);

        //BlockGroupコンポーネントを追加し、欠片を生成
        BlockGroup newGroup = groupObj.AddComponent<BlockGroup>();


        newGroup.Initialize(shape, blockPiecePrefab, groupObj.transform);

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
    /// 指定されたスロットに、ランダムな形のブロックを生成(補充)する
    /// </summary>
    /// <param name="slotIndex"></param>
    private void SpawnRandomBlockInSlot(int slotIndex)
    {
        //設計図がなければなにもしない
        if (availableShapes.Length == 0) return;

        if (paletteGroups[slotIndex] != null)
        {
            paletteSlotMap.Remove(paletteGroups[slotIndex]);
            Destroy(paletteGroups[slotIndex].gameObject);
            paletteGroups[slotIndex] = null;
        }

        BlockShape randomShape = availableShapes[Random.Range(0, availableShapes.Length)];

        Vector3 spawnPos = paletteSpawnSlots[slotIndex].position;
        BlockGroup newGroup = SpawnBlockAt(randomShape, spawnPos);

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
        //本体を複製して影を作る
        GameObject shadowObj = Instantiate(sourceGroup.gameObject, sourceGroup.transform.position, sourceGroup.transform.rotation, sourceGroup.transform);
        shadowObj.name = "ShadowBlock";
        shadowGroup = shadowObj.GetComponent<BlockGroup>();

        shadowGroup.isPaletteBlock = false;

        foreach (Transform child in shadowObj.transform)
        {
            Collider2D col = child.GetComponent<Collider2D>();
            if (col != null) Destroy(col);

            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
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
            Destroy(shadowGroup.gameObject);
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
            _ => Color.black,
        };

    }


    /// <summary>
    /// 待機所にある対応するインデックスのブロックを回転
    /// </summary>
    /// <param name="index"></param>
    public override void TryRotatePaletteBlock(int index)
    {
        if (index < 0 || index >= paletteGroups.Length) return;
        if(paletteGroups[index] == null) return;

        paletteGroups[index].transform.Rotate(0, 0, +90);
    }

    #endregion


}