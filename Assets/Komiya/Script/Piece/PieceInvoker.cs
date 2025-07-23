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
        /// Shapes�ɂ͂����Ă���S�Ẵs�[�X�𐶐�
        /// </summary>
        public void GenerateAllPieces()
        {
            foreach (ShapeData data in Shapes)
            {
                GameObject obj = Instantiate(piecePrefab);
                PieceHandler piece = obj.GetComponent<PieceHandler>();
                piece.Init(data); // ���ꂼ���ShapeData�ŏ�����
            }
        }


        /// <summary>
        /// �z��̔ԍ����w�肵�ăs�[�X�𐶐�
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
