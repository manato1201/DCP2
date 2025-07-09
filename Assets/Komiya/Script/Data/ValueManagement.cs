using UnityEngine;

namespace Value
{
    [CreateAssetMenu(fileName = "ValueManagement", menuName = "Scriptable Objects/ValueManagement")]
    public class ValueManagement : ScriptableObject
    {
        //==============================================
        //�S����:���{��
        //�@�\:�p�����[�^���̒l��S�ĊǗ�����ScriptableObject
        //==============================================




        [Header("�p�����[�^�[�֘A")]
        [Header("�����l")]
        public int InitialParentParamater = 3;
        public int InitialChildParamater = 3;
        public int InitialWhatDay = 0;


        [Space(32)]
        [Header("�e�q�̒l")]
        [Tooltip("�e�q�̃p�����[�^�̍ő�l")]
        public int MaxParameter = 10;

        [Tooltip("�e�̒l")]
        public int ParentParameter = 3;

        [Tooltip("�q���̒l")]
        public int ChildParameter = 3;


        [Header("�����ڂ�")]
        public int WhatDay = 0;

    }
}
