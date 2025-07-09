using System;
using System.Collections.Generic;
using UnityEngine;


namespace DB
{

    [CreateAssetMenu(fileName = "DB_Image", menuName = "Scriptable Objects/DB_Image")]
    public class DB_Image : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "DB_Image";

        // CakeParamsDBの実体
        private static DB_Image _entity = null;
        public static DB_Image Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<DB_Image>(PATH);

                    //ロード出来なかった場合はエラーログを表示
                    if (_entity == null)
                    {
                        Debug.LogError(PATH + " not found");
                    }
                }

                return _entity;
            }
        }
        #endregion

        [Header("DB_Image")] public List<NameSprite> ItemSprites;
    }
    [Serializable]
    public class NameSprite
    {
        public string Name;
        public Sprite[] Sprite;
    }
}