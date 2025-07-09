using UnityEngine;

using Value;
namespace Paramete
{
    public class ParamateInitializer : MonoBehaviour
    {
        //===============================================
        //�S����:���{��
        //�@�\:�Q�[�����̒l������������X�N���v�g
        //===============================================


        [SerializeField] private ValueManagement valueManagement;

        private void Start()
        {
            AllInitialize();
        }

        /// <summary>
        /// �S�Ă̒l��������(�����ǉ�)
        /// </summary>
        private void AllInitialize()
        {
            DayInitialize();
            ParamateInitialize();
            Debug.LogWarning("�S�Ẵp�����[�^�����������܂���!");
        }

        /// <summary>
        /// ���t��������
        /// </summary>
        private void DayInitialize()
        {
            valueManagement.WhatDay = valueManagement.InitialWhatDay;
            Debug.LogWarning("���t�����������܂���");
        }

        /// <summary>
        /// �p�����[�^��������
        /// </summary>
        private void ParamateInitialize()
        {
            valueManagement.ParentParameter = valueManagement.InitialParentParamater;
            valueManagement.ChildParameter = valueManagement.InitialChildParamater;
            Debug.LogWarning("�p�����[�^�����������܂���");
        }
    }
}