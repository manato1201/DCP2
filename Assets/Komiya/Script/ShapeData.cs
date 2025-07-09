using System.Collections.Generic;
using UnityEngine;

namespace Shape
{
    [CreateAssetMenu(fileName = "NewShapeData", menuName = "Puzzle/Shape Data")]
    public class ShapeData : ScriptableObject
    {
        //=======================================
        //�S����:���{��
        //�@�\:�u���b�N�̏���Z�߂�ScriptableObject
        //=======================================

        //�u���b�N
        [Header("���̌`�̊�_(Pivot)����̑��ΓI�ȃZ���̍��W���X�g")]
        public List<Vector2Int> Cells;


        //�e�L�X�g�֘A
        [Header("BockText���o�����W���X�g")]
        public List<Vector2Int> TextPos;
        [Header("�u���b�N��ɏo������")]
        public List<string> BlockChar;
        //[Header("�u���b�N��ɏo������")]
        //public List<char> BlockChar;
        [Header("�����̃T�C�Y")]
        public float TextSize = 8;


        //�Q�[���֘A
        [Header("�~�m�̃^�C�v")]
        [Tooltip("�h���׋���")]
        public string MinoType;
        [Header("�p�����[�^�̕ϓ�")]
        public float ChildParameter;
        public float ParentParameter;

        //�f�o�b�O�֘A
        [Header("�f�o�b�O")]
        [Tooltip("�f�o�b�O�p��Inspector��ŃO���b�h��`�悷�邽�߂̃T�C�Y")]
        [Range(1, 10)]
        public int GridSize = 5;


        // OnValidate��Inspector�Œl���ύX���ꂽ�Ƃ��ɌĂяo����܂�
        // ������g���āA�Z���̈ʒu���O���b�h�ɃX�i�b�v������Ȃǂ̕⏕���ł��܂�
        private void OnValidate()
        {
            // �K�v�ł���΁A�����ŃZ���̈ʒu�𐮗����郍�W�b�N��ǉ��ł��܂�
        }
    }
}