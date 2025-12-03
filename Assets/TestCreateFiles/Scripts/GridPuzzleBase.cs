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
    protected virtual void OnDrawGizmos()
    {
        if (gridInt == null) return;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                int value = gridInt[x, y];
                Vector3 pos = GridToWorld(x, y);

                // ★修正箇所: 固定値(0.5fなど)ではなく、インスペクターの変数を足す
                Vector3 drawPos = pos + gizmoOffset;

#if UNITY_EDITOR
                // 文字の表示
                GUIStyle style = new GUIStyle();
                style.normal.textColor = (value == 0) ? Color.gray : Color.white;
                style.fontSize = 20;
                style.fontStyle = FontStyle.Bold;

                // 文字位置（文字は少し中央からずれることがあるので微調整用）
                style.alignment = TextAnchor.MiddleCenter;

                Handles.Label(drawPos, value.ToString(), style);
#endif

                // 枠線の表示
                if (value != 0)
                {
                    Gizmos.color = new Color(1, 0, 0, 0.5f);
                    Gizmos.DrawWireCube(drawPos, new Vector3(cellSize * 0.9f, cellSize * 0.9f, 0.1f));
                }
            }
        }
    }

}
