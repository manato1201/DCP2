using UnityEngine;
using Shape;
using System.Collections.Generic;

namespace Pieces
{
    public class PieceHandler : MonoBehaviour
    {


        [Header("�u���b�N�P�̃v���t�@�u(�����1�̎l�p�`�Ƃ��Č`���`������)")]
        [SerializeField] private GameObject cellPrefab; // �u���b�N1���̃v���n�u
        [Header("�Z���̃T�C�Y")]
        [SerializeField] private Vector2 CellSize = new Vector2(1.0f, 1.0f);

        //���ݏo���Z���̃f�[�^
        private ShapeData shapeData;
        // ���݂̃s�[�X�̏�Ԃ�ێ����郊�X�g
        private List<Vector2Int> currentCellPositions;

        //=======================================
        //���J����(public)
        //=======================================

        /// <summary>
        /// �s�[�X����
        /// </summary>
        /// <param name="data"></param>
        public void Init(ShapeData data)
        {
            shapeData = data;
            currentCellPositions = new List<Vector2Int>(shapeData.Cells);

            GenerateCells();
        }
        // ��]�����̖{��
        public void Rotate(bool clockwise)
        {
            // 1. �e���W���X�g����]������
            // �Z���̉�]
            for (int i = 0; i < currentCellPositions.Count; i++)
            {
                Vector2Int pos = currentCellPositions[i];
                currentCellPositions[i] = clockwise ? new Vector2Int(pos.y, -pos.x) : new Vector2Int(-pos.y, pos.x);
            }
            Debug.Log("Rotate!");
            // 2. �ĕ`�悷��
            RedrawPiece();
        }







        //=======================================
        //��������(private)
        //=======================================

        // �u���b�N�̃Z���𐶐����鏈��
        private void GenerateCells()
        {
            if (shapeData == null) return;

            foreach (Vector2Int cellPosition in currentCellPositions)
            {
                Vector3 worldPosition = new Vector3(cellPosition.x * CellSize.x, cellPosition.y * CellSize.y, 0);
                Instantiate(cellPrefab, transform.position + worldPosition, Quaternion.identity, this.transform);
            }
        }

        // �s�[�X���ĕ`�悷�鏈��
        private void RedrawPiece()
        {
            // �����̃Z���ƃe�L�X�g�����ׂč폜
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }

            // �V�������W�ōĐ���
            GenerateCells();
        }
    }
}
