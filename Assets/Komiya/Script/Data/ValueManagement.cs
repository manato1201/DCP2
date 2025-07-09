using UnityEngine;

namespace Value
{
    [CreateAssetMenu(fileName = "ValueManagement", menuName = "Scriptable Objects/ValueManagement")]
    public class ValueManagement : ScriptableObject
    {
        //==============================================
        //担当者:小宮純
        //機能:パラメータ等の値を全て管理するScriptableObject
        //==============================================




        [Header("パラメーター関連")]
        [Header("初期値")]
        public int InitialParentParamater = 3;
        public int InitialChildParamater = 3;
        public int InitialWhatDay = 0;


        [Space(32)]
        [Header("親子の値")]
        [Tooltip("親子のパラメータの最大値")]
        public int MaxParameter = 10;

        [Tooltip("親の値")]
        public int ParentParameter = 3;

        [Tooltip("子供の値")]
        public int ChildParameter = 3;


        [Header("何日目か")]
        public int WhatDay = 0;

    }
}
