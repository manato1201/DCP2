using UnityEngine;
using Value;
using UnityEngine.SceneManagement;



namespace Mission
{
    public class MissionHandler : MonoBehaviour
    {
        //==================================
        //担当者:小宮純
        //機能:ミッションハンドル
        //==================================

        [Header("データ")]
        [SerializeField] private MissionData missionData;   //MissionValueを配列にしたScriptableObject
        [SerializeField] private ValueManagement valueManagement;   //値管理データ

        [SerializeField] private GameObject button;

        private int dataNum = 0; //missionDataの配列を指定するための変数

        private string MissionExplain = null; //内部書き換え用説明文
        public string Explain => MissionExplain; //公開用説明文

        public int debugMax = 0;


        private void Awake()
        {
            SetDataNum();
        }



        //==================================================================
        //公開処理(public)
        //==================================================================

        /// <summary>
        /// ランダムにミッションを出す
        /// </summary>
        public void RandomMission()
        {
            //0～MissionValueの数をランダムで取得
            dataNum = Random.Range(0, missionData.MissionValues.Length);

            //dataNumに基づいてミッションを設定
            SetMission();
            //親子のパラメータが既にミッションを達成していた場合 : 再抽選
            while (IsExceed())
            {
                //0～MissionValueの数をランダムで取得
                dataNum = Random.Range(0, missionData.MissionValues.Length);
                SetMission();
            }

        }

        /// <summary>
        /// 指定してミッションを出す
        /// </summary>
        public void SelectMission(int missionNum)
        {
            //0～MissionValueの数をランダムで取得
            dataNum = missionNum;

            //指定数がdataNumの範囲を超えていた場合エラーを返す
            if (missionNum > dataNum)
            {
                Debug.LogWarning("missionNumは0～" + dataNum + "で選んでください");
                return;
            }

            //dataNumに基づいてミッションを設定
            SetMission();
            if (IsExceed())
            {
                Debug.LogWarning("パラメータが既に目標に達しています");
            }

        }

        /// <summary>
        /// ミッション開始
        /// </summary>
        public void StartMission()
        {
            Debug.Log("ミッション開始！");
            SetMission(); //ミッション内容を呼び出し
            CallShader(); //シェーダー処理を呼び出し : 後々追加
            CallEnemy();  //敵キャラ処理を呼び出し : 後々追加
        }

        /// <summary>
        ///ミッション終了 
        /// </summary>
        public void EndMission()
        {
            Debug.Log("ミッション終了！");
            EndShader();
            EndEnemy();

            button.SetActive(true);
        }









        //=======================================================================
        //内部処理(private)
        //=======================================================================


        /// <summary>
        /// dataNumにMissionValuesの長さを入れる
        /// </summary>
        private void SetDataNum()
        {
            dataNum = missionData.MissionValues.Length;
        }


        /// <summary>
        /// ミッションデータ[ミッション番号]を設定
        /// </summary>
        private void SetMission()
        {
            valueManagement.ParentMission = missionData.MissionValues[dataNum].parentPurpose;   //親のミッションパラメータ定義
            valueManagement.ChildMission = missionData.MissionValues[dataNum].childPurpose;     //子のミッションパラメータ定義
            MissionExplain = missionData.MissionValues[dataNum].explain;    //説明文定義


            Debug.Log("親の値: " + valueManagement.ParentMission);
            Debug.Log("子の値: " + valueManagement.ChildMission);
            Debug.Log("説明: " + MissionExplain);
        }

        /// <summary>
        /// シェーダー処理呼び出し : 後々記述
        /// </summary>
        private void CallShader()
        {
            Debug.LogWarning("Developing ; callShader");
        }

        /// <summary>
        /// 敵キャラ処理呼び出し : 後々記述
        /// </summary>
        private void CallEnemy()
        {
            Debug.LogWarning("Developing : callEnemy");
        }

        private void EndShader()
        {
            Debug.Log("EndShader");
        }

        private void EndEnemy()
        {
            Debug.Log("EndEnemy");
        }




        /// <summary>
        /// 親子両方のパラメータがミッションの目標に達しているか
        /// </summary>
        /// <param name="Parent"></param>
        /// <param name="Child"></param>
        /// <returns></returns>
        private bool IsExceed()
        {
            return (valueManagement.ParentMission <= valueManagement.ParentParameter && valueManagement.ChildMission <= valueManagement.ChildParameter);
        }


    }

}