using System.Collections.Generic;
using UnityEngine;

namespace Shape
{
    [CreateAssetMenu(fileName = "NewShapeData", menuName = "Puzzle/Shape Data")]
    public class ShapeData : ScriptableObject
    {
        //=======================================
        //担当者:小宮純
        //機能:ブロックの情報を纏めたScriptableObject
        //=======================================

        //ブロック
        [Header("この形の基準点(Pivot)からの相対的なセルの座標リスト")]
        public List<Vector2Int> Cells;


        //テキスト関連
        [Header("BockTextを出す座標リスト")]
        public List<Vector2Int> TextPos;
        [Header("ブロック上に出す文字")]
        public List<string> BlockChar;
        //[Header("ブロック上に出す文字")]
        //public List<char> BlockChar;
        [Header("文字のサイズ")]
        public float TextSize = 8;


        //ゲーム関連
        [Header("ミノのタイプ")]
        [Tooltip("宿題や勉強等")]
        public string MinoType;
        [Header("パラメータの変動")]
        public float ChildParameter;
        public float ParentParameter;

        //デバッグ関連
        [Header("デバッグ")]
        [Tooltip("デバッグ用にInspector上でグリッドを描画するためのサイズ")]
        [Range(1, 10)]
        public int GridSize = 5;


        // OnValidateはInspectorで値が変更されたときに呼び出されます
        // これを使って、セルの位置をグリッドにスナップさせるなどの補助ができます
        private void OnValidate()
        {
            // 必要であれば、ここでセルの位置を整理するロジックを追加できます
        }
    }
}