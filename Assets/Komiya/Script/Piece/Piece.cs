using UnityEngine;
using TMPro; // TextMeshPro���g�����߂ɕK�v�I
using System.Collections.Generic;

using Shape;


    public class Piece : MonoBehaviour
    {
        //==============================================
        //�S����:���{��
        //�@�\:�s�[�X�̐���/��]�̊Ǘ�
        //==============================================


        [Header("ScriptableObject:ShapeData")]
        [SerializeField]private ShapeData shapeData;
        [Header("�u���b�N�P�̃v���t�@�u(�����1�̎l�p�`�Ƃ��Č`���`������)")]
        [SerializeField]private GameObject cellPrefab; // �u���b�N1���̃v���n�u

        [Header("�Z���̃T�C�Y")]
        [SerializeField] private Vector2 CellSize = new Vector2(1.0f, 1.0f);

        [Header("�e�L�X�g�ݒ�")]
        [SerializeField] private TextMeshPro textPrefab; // TextMeshPro�̃v���n�u�������ɐݒ�

        // ���݂̃s�[�X�̏�Ԃ�ێ����郊�X�g
        private List<Vector2Int> currentCellPositions;
        private List<Vector2Int> currentTextPositions;

        private void Start()
        {
            // ShapeData���猻�݂̏�Ԃփf�[�^���R�s�[���ď�����
            currentCellPositions = new List<Vector2Int>(shapeData.Cells);
            currentTextPositions = new List<Vector2Int>(shapeData.TextPos);


            GenerateCells();
            GenerateTexts(); // �e�L�X�g�𐶐����鏈�����Ăяo��
        }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Rotate(true);
        }
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

        // �e�L�X�g�ʒu�̉�]
        for (int i = 0; i < currentTextPositions.Count; i++)
        {
            Vector2Int pos = currentTextPositions[i];
            currentTextPositions[i] = clockwise ? new Vector2Int(pos.y, -pos.x) : new Vector2Int(-pos.y, pos.x);
        }

        // 2. �ĕ`�悷��
        RedrawPiece();
    }



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
        // �����𐶐����鏈��
        private void GenerateTexts()
        {
            if (shapeData == null || textPrefab == null || shapeData.BlockChar.Count != currentTextPositions.Count)
            {
                if (shapeData.BlockChar.Count != currentTextPositions.Count)
                {
                    Debug.LogError("ShapeData���̕������X�g�ƍ��W���X�g�̐�����v���܂���I");
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
            GenerateTexts();
        }
    }