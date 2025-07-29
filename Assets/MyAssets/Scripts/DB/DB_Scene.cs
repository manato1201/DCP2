using System.Collections.Generic;
using System;
using UnityEngine;





namespace DB
{
    [CreateAssetMenu(fileName = "DB_Scene", menuName = "Scriptable Objects/DB_Scene")]

    
    public class DB_Scene : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "DB_Scene";

        // CakeParamsDBの実体
        private static DB_Scene _entity = null;
        public static DB_Scene Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<DB_Scene>(PATH);

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

        [Header("DB_Scene")] public List<SceneObj> SceneName;
    }
    [Serializable]
    public class SceneObj
    {
        public string Name;
        
    }
}