#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public abstract class GridPuzzleBase : MonoBehaviour,IPuzzleRule
{
    //---グリッドを使ったパズルゲームのひな型---

    [Header("Base Grid Settings")]
    [SerializeField] protected int gridWidth = 8;
    [SerializeField] protected int gridHeight = 10;
    [SerializeField] protected float cellSize = 1.0f;
    [SerializeField] protected Transform gridOrigin;
    [SerializeField] protected Vector3 gizmoOffset;

    public const int FOG_BLOCK_ID = 99;
    protected int[,] fogLifeGrid;

    protected GameObject[,] gridVisuals;    //ブロックの管理
    protected int[,] gridInt;   //数値の管理
    protected GameObject[,] cellObjects;

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="controller"></param>
    public virtual void Initialize(PuzzleController controller)
    {
        //グリッドの初期化
        gridVisuals = new GameObject[gridWidth, gridHeight];
        gridInt = new int[gridWidth, gridHeight];
        fogLifeGrid = new int[gridWidth, gridHeight];
        cellObjects = new GameObject[gridWidth, gridHeight];

    }

    //---ゲーム内容によった処理の上書を想定している関数
    public abstract void OnUpdate();
    public abstract void HandleInput();
    public abstract void OnBlockLanded();
    public abstract bool CheckForClear();
    public abstract bool IsGameOver();

    //---デフォルトがあるメソッド---

    /// <summary>
    /// 制限時間を返す タイマーが不要な場合、-1または0を返すようにする
    /// ルールに応じてoverrideを行う　初期状態では-1を返す
    /// </summary>
    /// <returns></returns>
    public virtual float GetTimeLimit()
    {
        return -1;  //デフォルトでは時間制限は無い
    }

    /// <summary>
    /// ターン数を返す　基本は制限時間と同様
    /// </summary>
    /// <returns></returns>
    public virtual int GetTurnLimit()
    {
        return -1;
    }


    /// <summary>
    /// 時間切れの処理
    /// ルールに応じてoverrideを行う　初期状態ではDebug.Logのみ
    /// </summary>
    public virtual void OnTimerEnded()
    {
        Debug.Log("タイマー終了時の処理が呼び出されました");
    }

    /// <summary>
    /// パレットが存在するルールにおいて、パレット内のブロックを回転するためのメソッド
    /// </summary>
    /// <param name="index"></param>
    public virtual void TryRotatePaletteBlock(int index)
    {
        //回転が必要なルールの場合、継承先で処理を実装
    }

    public virtual void ChangeGameStep()
    {
        //パズル<->戦闘　の要素があるゲームの場合、内容を実装
    }

    //---共通関数---

    //グリッド座標→ワールド座標
    protected Vector3 GridToWorld(int x, int y)
    {
        Vector3 originPos = (gridOrigin != null)? gridOrigin.position : Vector3.zero;
        return originPos + new Vector3(x * cellSize, y * cellSize, 0);
    }

    //ワールド座標→グリッド座標
    protected Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 originPos = (gridOrigin != null)? gridOrigin.position : Vector3.zero; 
        float relativeX = worldPos.x - originPos.x;
        float relativeY = worldPos.y - originPos.y;
        return new Vector2Int(Mathf.RoundToInt(relativeX / cellSize), Mathf.RoundToInt(relativeY /cellSize));
    }

    //範囲のチェック
    protected bool IsValidGridPosition(Vector2Int coord)
    {
        return coord.x >= 0 && coord.x < gridWidth && coord.y >= 0 && coord.y < gridHeight;
    }

    //グリッドへの書き込み
    protected void SetGridValue(Vector2Int coord, int value, GameObject visualObj)
    {
        if (!IsValidGridPosition(coord)) return;
        gridInt[coord.x, coord.y] = value;
        gridVisuals[coord.x, coord.y] = visualObj;
    }

    //背景セルの色を変更
    protected void SetGridCellColor(Vector2Int coord, Color color)
    {
        if (!IsValidGridPosition(coord)) return;

        GameObject cell = cellObjects[coord.x, coord.y];
        if (cell != null)
        {
            SpriteRenderer sr = cell.GetComponent<SpriteRenderer>();
            if(sr != null)
            {
                sr.color = color;
            }
        }
    }



    /// <summary>
    /// デバッグ用：シーンビューにグリッドの内部データを描画
    /// </summary>
    /// <summary>
    /// デバッグ用：シーンビューにグリッドの内部データを描画
    /// </summary>
    protected virtual void OnDrawGizmos()
    {
        // データがない、またはUnityエディタ外でのビルド時は実行しない
#if !UNITY_EDITOR
        return;
#endif
        // gridIntが初期化されていない場合は中止
        if (gridInt == null) return;

        // もやの寿命配列もnullチェック
        bool showFogLife = (fogLifeGrid != null);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // グリッド座標からワールド座標へ変換
                // ※GridToWorld関数がない場合は、以下の計算式を使ってください
                // Vector3 pos = gridOrigin.position + new Vector3(x * cellSize, y * cellSize, 0);
                Vector3 pos = GridToWorld(x, y);

                // 位置調整（インスペクターのgizmoOffsetを加算）
                Vector3 drawPos = pos + gizmoOffset;

                int id = gridInt[x, y];

                // --- ここからエディタ専用描画処理 ---
#if UNITY_EDITOR
                GUIStyle style = new GUIStyle();
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;

                // IDによって色を変える
                if (id == 0)
                {
                    style.normal.textColor = Color.gray; // 空きマス
                }
                else if (id == FOG_BLOCK_ID)
                {
                    style.normal.textColor = Color.magenta; // もやは紫
                }
                else
                {
                    style.normal.textColor = Color.white; // 通常ブロック
                }

                // 表示する文字
                string labelText = id.ToString();

                // もやの場合は、改行して「寿命」も表示する
                if (id == FOG_BLOCK_ID && showFogLife)
                {
                    labelText += $"\n({fogLifeGrid[x, y]})";
                }

                // シーンビューに文字を描画
                Handles.Label(drawPos, labelText, style);
#endif
            }
        }
    }
}
