using System.Collections.Generic;
using System;
using UnityEngine;





namespace SO
{
    [CreateAssetMenu(fileName = "SO_Scene", menuName = "Scriptable Objects/SO_Scene")]

    
    public class SO_Scene : ScriptableObject
    {

        #region QOL向上処理
        // CakeParamsSOが保存してある場所のパス
        public const string PATH = "SO_Scene";

        // CakeParamsSOの実体
        private static SO_Scene _entity = null;
        public static SO_Scene Entity
        {
            get
            {
                // 初アクセス時にロードする
                if (_entity == null)
                {
                    _entity = Resources.Load<SO_Scene>(PATH);

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

        [Header("SO_Scene")] public List<SceneObj> SceneName;
    }
    [Serializable]
    public class SceneObj
    {
        public string Name;
        
    }
}
