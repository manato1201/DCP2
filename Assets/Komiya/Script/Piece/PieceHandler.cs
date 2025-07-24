using UnityEngine;
using Shape;
using System.Collections.Generic;

namespace Pieces
{
    public class PieceHandler : MonoBehaviour
    {


        [Header("ブロック１つのプレファブ(これを1つの四角形として形を形成する)")]
        [SerializeField] private GameObject cellPrefab; // ブロック1つ分のプレハブ
        [Header("セルのサイズ")]
        [SerializeField] private Vector2 CellSize = new Vector2(1.0f, 1.0f);

        //生み出すセルのデータ
        private ShapeData shapeData;
        // 現在のピースの状態を保持するリスト
        private List<Vector2Int> currentCellPositions;

        //=======================================
        //公開処理(public)
        //=======================================

        /// <summary>
        /// ピース生成
        /// </summary>
        /// <param name="data"></param>
        public void Init(ShapeData data)
        {
            shapeData = data;
            currentCellPositions = new List<Vector2Int>(shapeData.Cells);

            GenerateCells();
        }
        // 回転処理の本体
        public void Rotate(bool clockwise)
        {
            // 1. 各座標リストを回転させる
            // セルの回転
            for (int i = 0; i < currentCellPositions.Count; i++)
            {
                Vector2Int pos = currentCellPositions[i];
                currentCellPositions[i] = clockwise ? new Vector2Int(pos.y, -pos.x) : new Vector2Int(-pos.y, pos.x);
            }
            Debug.Log("Rotate!");
            // 2. 再描画する
            RedrawPiece();
        }







        //=======================================
        //内部処理(private)
        //=======================================

        // ブロックのセルを生成する処理
        private void GenerateCells()
        {
            if (shapeData == null) return;

            foreach (Vector2Int cellPosition in currentCellPositions)
            {
                Vector3 worldPosition = new Vector3(cellPosition.x * CellSize.x, cellPosition.y * CellSize.y, 0);
                Instantiate(cellPrefab, transform.position + worldPosition, Quaternion.identity, this.transform);
            }
        }

        // ピースを再描画する処理
        private void RedrawPiece()
        {
            // 既存のセルとテキストをすべて削除
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // 新しい座標で再生成
            GenerateCells();
        }
    }
}
