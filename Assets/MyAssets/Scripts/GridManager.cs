using Paramete;
using UnityEngine;
using UnityEngine.SceneManagement;
using Value;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("グリッド設定")]
    public int width = 4;
    public int height = 4;
    public float cellSize = 1.0f;
    public Vector3 originPosition = Vector3.zero;

    [Header("パラメーター管理")]
    public ValueManagement valueManagement; // ValueManagementの参照をInspectorで設定

    [Header("ゲージUI管理")]
    public ParameterGauge parameterGauge; // ParameterGaugeの参照をInspectorで設定

    [SerializeField] bool isMissionScene = false;
    private bool isFirstCall = false;
    [SerializeField] GameObject goButton;

    private Transform[,] grid;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
        grid = new Transform[width, height];
    }

    // ワールド座標 -> グリッド座標
    public Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
        int y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
        return new Vector2Int(x, y);
    }

    // グリッド座標 -> ワールド座標（セルの中心を返すように修正）
    public Vector3 GridToWorldPosition(int x, int y)
    {
        float worldX = originPosition.x + (x * cellSize) + (cellSize * 0.5f);
        float worldY = originPosition.y + (y * cellSize) + (cellSize * 0.5f);

        worldY -= 0.5f;

        return new Vector3(worldX, worldY, 0);
    }

    // ピースの配置可否をチェック
    public bool CanPlacePiece(PieceController piece, Vector2Int gridPos)
    {
        foreach (Transform block in piece.transform)
        {
            Vector3 worldOffset = piece.transform.TransformDirection(block.localPosition);
            int gridOffsetX = Mathf.RoundToInt(worldOffset.x / cellSize);
            int gridOffsetY = Mathf.RoundToInt(worldOffset.y / cellSize);
            Vector2Int blockOffset = new Vector2Int(gridOffsetX, gridOffsetY);

            Vector2Int checkPos = gridPos + blockOffset;

            if (checkPos.x < 0 || checkPos.x >= width || checkPos.y < 0 || checkPos.y >= height)
            {
                return false;
            }

            // gridにはblock自身が入るようにする
            if (grid[checkPos.x, checkPos.y] != null && grid[checkPos.x, checkPos.y] != block)
            {
                return false;
            }
        }
        return true;
    }

    // ピースをグリッドに登録
    public void RegisterPiece(PieceController piece, Vector2Int gridPos)
    {
        // 既に配置済みの場合は一度減算
        if (piece.isPlaced && valueManagement != null && piece.shapeData != null)
        {
            valueManagement.ParentParameter -= Mathf.RoundToInt(piece.shapeData.ParentParameter);
            valueManagement.ChildParameter -= Mathf.RoundToInt(piece.shapeData.ChildParameter);
        }

        if (!CanPlacePiece(piece, gridPos))
        {
            Debug.LogWarning("この位置にはピースを配置できません。");
            return;
        }

        UnregisterPiece(piece);

        foreach (Transform block in piece.transform)
        {
            Vector3 worldOffset = piece.transform.TransformDirection(block.localPosition);
            int gridOffsetX = Mathf.RoundToInt(worldOffset.x / cellSize);
            int gridOffsetY = Mathf.RoundToInt(worldOffset.y / cellSize);
            Vector2Int blockOffset = new Vector2Int(gridOffsetX, gridOffsetY);

            Vector2Int registerPos = gridPos + blockOffset;

            if (registerPos.x >= 0 && registerPos.x < width && registerPos.y >= 0 && registerPos.y < height)
            {
                grid[registerPos.x, registerPos.y] = block;
            }
        }

        // 加算処理
        if (valueManagement != null && piece.shapeData != null)
        {
            valueManagement.ParentParameter += Mathf.RoundToInt(piece.shapeData.ParentParameter);
            valueManagement.ChildParameter += Mathf.RoundToInt(piece.shapeData.ChildParameter);
        }

        // --- ここでゲージを更新 ---
        if (parameterGauge != null)
        {
            Debug.Log($"RegisterPiece: ParentParameter={valueManagement.ParentParameter}, ChildParameter={valueManagement.ChildParameter}");
            parameterGauge.ChangeGauge(valueManagement.ParentParameter, GetParentGaugeImage());
            parameterGauge.ChangeGauge(valueManagement.ChildParameter, GetChildGaugeImage());
        }
        else
        {
            Debug.LogError("parameterGaugeがnullです！");
        }

        piece.isPlaced = true;

        // グリッドが全て埋まっているかチェック（デバッグ用）
        if (IsGridFull() && !isFirstCall)
        {
            goButton.SetActive(true);
        }
    }

    // ピースの登録を解除
    public void UnregisterPiece(PieceController piece)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // gridに登録されているTransformの親がこのピースのTransformなら消す
                if (grid[x, y] != null && grid[x, y].parent == piece.transform)
                {
                    grid[x, y] = null;
                }
            }
        }

        if (piece.isPlaced && valueManagement != null && piece.shapeData != null)
        {
            valueManagement.ParentParameter -= Mathf.RoundToInt(piece.shapeData.ParentParameter);
            valueManagement.ChildParameter -= Mathf.RoundToInt(piece.shapeData.ChildParameter);
        }

        // --- ここでゲージを更新 ---
        if (parameterGauge != null)
        {
            Debug.Log($"UnregisterPiece: ParentParameter={valueManagement.ParentParameter}, ChildParameter={valueManagement.ChildParameter}");
            parameterGauge.ChangeGauge(valueManagement.ParentParameter, GetParentGaugeImage());
            parameterGauge.ChangeGauge(valueManagement.ChildParameter, GetChildGaugeImage());
        }
        else
        {
            Debug.LogError("parameterGaugeがnullです！");
        }

        piece.isPlaced = false;

        // グリッドが全て埋まっているかチェック（デバッグ用）
        IsGridFull();
        // grid配列の状態を出力
        //DebugGridState();
    }

    // グリッドが全て埋まっているか判定
    public bool IsGridFull()
    {
        int filled = GetFilledCellCount();
        Debug.Log($"埋まっているセル数: {filled}/{width * height}");
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == null)
                {
                    return false;
                }
            }
        }
        return true;
    }

    // 埋まっているセル数を返す
    public int GetFilledCellCount()
    {
        int count = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    count++;
                }
            }
        }
        return count;
    }

    // ParameterGaugeのImage取得用ヘルパー
    private UnityEngine.UI.Image GetParentGaugeImage()
    {
        return parameterGauge != null ? parameterGauge.GetParentGaugeImage() : null;
    }
    private UnityEngine.UI.Image GetChildGaugeImage()
    {
        return parameterGauge != null ? parameterGauge.GetChildGaugeImage() : null;
    }

    // grid配列の状態をデバッグ出力
    /*
    private void DebugGridState()
    {
        string s = "[Grid状態] ";
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                s += (grid[x, y] != null ? "■" : "□");
            }
            s += " ";
        }
        Debug.Log(s);
    }
    */

    void OnDrawGizmos()
    {
        Gizmos.color = Color.gray; // 見やすいようにグレーに変更
        Vector3 offset = new Vector3(0, -0.5f, 0); // ご要望のオフセットをGizmoにも適用

        // 縦線
        for (int i = 0; i < width + 1; i++)
        {
            Vector3 startPos = originPosition + new Vector3(i * cellSize, 0, 0) + offset;
            Vector3 endPos = originPosition + new Vector3(i * cellSize, height * cellSize, 0) + offset;
            Gizmos.DrawLine(startPos, endPos);
        }
        // 横線
        for (int i = 0; i < height + 1; i++)
        {
            Vector3 startPos = originPosition + new Vector3(0, i * cellSize, 0) + offset;
            Vector3 endPos = originPosition + new Vector3(width * cellSize, i * cellSize, 0) + offset;
            Gizmos.DrawLine(startPos, endPos);
        }
    }
}
