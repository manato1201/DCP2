using Unity.VisualScripting;
using UnityEngine;

using Value;
namespace Day
{
    public class DayManagement : MonoBehaviour
    {
        //==================================
        //�S����:���{��
        //�@�\:���t�ύX&������
        //==================================

        [SerializeField] private ValueManagement valueManagement;

        /// <summary>
        /// ������
        /// </summary>
        private void InitialDay()
        {
            Debug.LogWarning("���t������������܂���");
            valueManagement.WhatDay = valueManagement.InitialWhatDay;
        }

        /// <summary>
        /// ���̓���
        /// </summary>
        public void NextDay()
        {
            valueManagement.WhatDay++;
        }

        /// <summary>
        /// �O�̓���
        /// </summary>
        public void PreviousDay()
        {
            valueManagement.WhatDay--;
        }
    }
}