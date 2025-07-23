using UnityEngine;
using Shape;

public class PieceController : MonoBehaviour
{
    public ShapeData shapeData; // 追加: このピースのShapeData

    private Vector3 offset; // マウスカーソルとオブジェクト中心の差分
    private Vector3 initialPosition; // ドラッグ開始時の位置
    private Vector3 initialScale; // ドラッグ開始時のスケール

    [HideInInspector]
    public bool isPlaced = false; // グリッドに配置済みか

    // ドラッグ開始時
    private void OnMouseDown()
    {
        GridManager.Instance.UnregisterPiece(this);
        // ...他の処理
    }

    // ドラッグ終了時（ドロップ時）
    private void OnMouseUp()
    {
        Vector3 worldPos = transform.position;
        Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(worldPos);

        if (GridManager.Instance.CanPlacePiece(this, gridPos))
        {
            GridManager.Instance.RegisterPiece(this, gridPos);
            isPlaced = true;
            // 必要ならtransform.positionをGridToWorldPositionでスナップ
            transform.position = GridManager.Instance.GridToWorldPosition(gridPos.x, gridPos.y);
        }
        else
        {
            // 配置できない場合の処理（元の位置に戻す等）
            isPlaced = false;
        }
    }

    // ドラッグ中
    private void OnMouseDrag()
    {
        // マウスの動きに合わせてピースを移動
        transform.position = GetMouseWorldPos() + offset;
    }

    // マウスを離した後の処理（重複していたOnMouseUpを削除し、既存のOnMouseUpに統合）
    private void HandleMouseRelease()
    {
        // 最も近いグリッドの中心座標を計算
        Vector2Int gridPos = GridManager.Instance.WorldToGridPosition(transform.position);

        // その場所に配置可能かチェック
        if (GridManager.Instance.CanPlacePiece(this, gridPos))
        {
            // --- 配置成功 ---
            // グリッドにスナップさせる
            transform.position = GridManager.Instance.GridToWorldPosition(gridPos.x, gridPos.y);
            // グリッドマネージャーにピースを登録する
            GridManager.Instance.RegisterPiece(this, gridPos);
            isPlaced = true;
        }
        else
        {
            // --- 配置失敗 ---
            // 元の位置に戻す
            transform.position = initialPosition;
            // もしドラッグ開始時に配置済みだったなら、再登録する
            if (GridManager.Instance.CanPlacePiece(this, GridManager.Instance.WorldToGridPosition(initialPosition)))
            {
                GridManager.Instance.RegisterPiece(this, GridManager.Instance.WorldToGridPosition(initialPosition));
                isPlaced = true;
            }
        }

        // Z座標を元に戻す
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        // ゲームクリアチェックを呼び出す
        GameController.Instance.CheckGameCompletion();
    }

    // マウスカーソルの位置をワールド座標で取得するヘルパー関数
    private Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.WorldToScreenPoint(transform.position).z;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
