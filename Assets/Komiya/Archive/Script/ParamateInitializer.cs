using UnityEngine;

using Value;
namespace Paramete
{
    public class ParamateInitializer : MonoBehaviour
    {
        //===============================================
        //担当者:小宮純
        //機能:ゲーム内の値を初期化するスクリプト
        //===============================================


        [SerializeField] private ValueManagement valueManagement;

        private void Start()
        {
            AllInitialize();
        }

        /// <summary>
        /// 全ての値を初期化(随時追加)
        /// </summary>
        private void AllInitialize()
        {
            DayInitialize();
            ParamateInitialize();
            Debug.LogWarning("全てのパラメータを初期化しました!");
        }

        /// <summary>
        /// 日付を初期化
        /// </summary>
        private void DayInitialize()
        {
            valueManagement.WhatDay = valueManagement.InitialWhatDay;
            Debug.LogWarning("日付を初期化しました");
        }

        /// <summary>
        /// パラメータを初期化
        /// </summary>
        private void ParamateInitialize()
        {
            valueManagement.ParentParameter = valueManagement.InitialParentParamater;
            valueManagement.ChildParameter = valueManagement.InitialChildParamater;
            Debug.LogWarning("パラメータを初期化しました");
        }
    }
}