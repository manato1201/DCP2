using Shape;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Pieces
{
    public class PieceInvoker : MonoBehaviour
    {

        [SerializeField] GameObject piecePrefab;
        [SerializeField] PieceHandler PieceHandler_;
        [SerializeField] List<ShapeData> Shapes = new List<ShapeData>();


        /// <summary>
        /// Shapesにはいっている全てのピースを生成
        /// </summary>
        public void GenerateAllPieces()
        {
            foreach (ShapeData data in Shapes)
            {
                GameObject obj = Instantiate(piecePrefab);
                PieceHandler piece = obj.GetComponent<PieceHandler>();
                piece.Init(data); // それぞれのShapeDataで初期化
            }
        }


        /// <summary>
        /// 配列の番号を指定してピースを生成
        /// </summary>
        /// <param name="pieceIndex"></param>
        public void GenerateSelectPieces(int pieceIndex)
        {
            GameObject obj = Instantiate(piecePrefab);
            PieceHandler piece = obj.GetComponent<PieceHandler>();
            piece.Init(Shapes[pieceIndex]);
        }



    }
}
