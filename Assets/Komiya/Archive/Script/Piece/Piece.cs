using UnityEngine;
using TMPro; // TextMeshProを使うために必要！
using System.Collections.Generic;

using Shape;


    public class Piece : MonoBehaviour
    {
        //==============================================
        //担当者:小宮純
        //機能:ピースの生成/回転の管理
        //==============================================


        [Header("ScriptableObject:ShapeData")]
        [SerializeField]private ShapeData shapeData;
        [Header("ブロック１つのプレファブ(これを1つの四角形として形を形成する)")]
        [SerializeField]private GameObject cellPrefab; // ブロック1つ分のプレハブ

        [Header("セルのサイズ")]
        [SerializeField] private Vector2 CellSize = new Vector2(1.0f, 1.0f);

        [Header("テキスト設定")]
        [SerializeField] private TextMeshPro textPrefab; // TextMeshProのプレハブをここに設定

        // 現在のピースの状態を保持するリスト
        private List<Vector2Int> currentCellPositions;
        private List<Vector2Int> currentTextPositions;

        private void Start()
        {
            // ShapeDataから現在の状態へデータをコピーして初期化
            currentCellPositions = new List<Vector2Int>(shapeData.Cells);
            currentTextPositions = new List<Vector2Int>(shapeData.TextPos);


            GenerateCells();
            GenerateTexts(); // テキストを生成する処理を呼び出す
        }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Rotate(true);
        }
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

        // テキスト位置の回転
        for (int i = 0; i < currentTextPositions.Count; i++)
        {
            Vector2Int pos = currentTextPositions[i];
            currentTextPositions[i] = clockwise ? new Vector2Int(pos.y, -pos.x) : new Vector2Int(-pos.y, pos.x);
        }

        // 2. 再描画する
        RedrawPiece();
    }



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
        // 文字を生成する処理
        private void GenerateTexts()
        {
            if (shapeData == null || textPrefab == null || shapeData.BlockChar.Count != currentTextPositions.Count)
            {
                if (shapeData.BlockChar.Count != currentTextPositions.Count)
                {
                    Debug.LogError("ShapeData内の文字リストと座標リストの数が一致しません！");
                }
                return;
            }



            for (int i = 0; i < shapeData.BlockChar.Count; i++)
            {
                string character = shapeData.BlockChar[i];
                Vector2Int textGridPos = currentTextPositions[i];
                Vector3 textWorldPos = new Vector3(textGridPos.x * CellSize.x, textGridPos.y * CellSize.y, -0.1f);

                TextMeshPro newText = Instantiate(textPrefab, this.transform);
                newText.transform.localPosition = textWorldPos;

                newText.text = character.ToString();
                newText.fontSize = shapeData.TextSize;
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
            GenerateTexts();
        }
    }