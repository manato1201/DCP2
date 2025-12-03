using Unity.VisualScripting;
using UnityEngine;

using Value;
namespace Day
{
    public class DayManagement : MonoBehaviour
    {
        //==================================
        //担当者:小宮純
        //機能:日付変更&初期化
        //==================================

        [SerializeField] private ValueManagement valueManagement;

        /// <summary>
        /// 初期化
        /// </summary>
        private void InitialDay()
        {
            Debug.LogWarning("日付が初期化されました");
            valueManagement.WhatDay = valueManagement.InitialWhatDay;
        }

        /// <summary>
        /// 次の日へ
        /// </summary>
        public void NextDay()
        {
            valueManagement.WhatDay++;
        }

        /// <summary>
        /// 前の日へ
        /// </summary>
        public void PreviousDay()
        {
            valueManagement.WhatDay--;
        }
    }
}