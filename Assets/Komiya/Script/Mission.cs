using UnityEngine;
using Value;




public class Mission : MonoBehaviour
{
    //==================================
    //担当者:小宮純
    //機能:ミッション管理
    //==================================

    [Header("データ")]
    [SerializeField] private MissionData missionData;   //MissionValueを配列にしたScriptableObject
    [SerializeField] private ValueManagement valueManagement;   //値管理データ

    private int dataNum = 0; //missionDataの配列を指定するための変数
    private bool isDuringMission = false; //ミッション中か?
    private string MissionExplain = null; //説明文

    public int debugMax = 0;


    private void Awake()
    {
        setDataNum();
    }



    //==================================================================
    //公開処理(public)
    //==================================================================

    /// <summary>
    /// ランダムにミッションを出す
    /// </summary>
    public void randomMission()
    {
        //0～MissionValueの数をランダムで取得
        dataNum = Random.Range(0, missionData.MissionValues.Length);

        //dataNumに基づいてミッションを設定
        setMission();
        //親子のパラメータが既にミッションを達成していた場合 : 再抽選
        while (isExceed() && debugMax < 10)
        {
            //Whileが続く限り再抽選し続ける
            dataNum = Random.Range(0, missionData.MissionValues.Length);
            setMission();

            debugMax++;
        }

    }

    /// <summary>
    /// 指定してミッションを出す
    /// </summary>
    public void selectMission(int missionNum)
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
            setMission();
        if (isExceed())
        {
            Debug.LogWarning("パラメータが既に目標に達しています");
        }

    }

    /// <summary>
    /// シェーダー処理呼び出し : 後々記述
    /// </summary>
    public void callShader()
    {

    }

    /// <summary>
    /// 敵キャラ処理呼び出し : 後々記述
    /// </summary>
    public void callEnemy()
    {

    }










    //=======================================================================
    //内部処理(private)
    //=======================================================================

    /// <summary>
    /// dataNumにMissionValuesの長さを入れる
    /// </summary>
    private void setDataNum()
    {
        dataNum = missionData.MissionValues.Length;
    }


    /// <summary>
    /// ミッションデータ[ミッション番号]を設定
    /// </summary>
    private void setMission()
    {
        valueManagement.ParentMission = missionData.MissionValues[dataNum].parentPurpose;
        valueManagement.ChildMission = missionData.MissionValues[dataNum].childPurpose;
        MissionExplain = missionData.MissionValues[dataNum].explain;

        Debug.Log("親の値: " + valueManagement.ParentMission);
        Debug.Log("子の値: " + valueManagement.ChildMission);
        Debug.Log("説明: " + MissionExplain);
    }

    /// <summary>
    /// 親子両方のパラメータがミッションの目標に達しているか
    /// </summary>
    /// <param name="Parent"></param>
    /// <param name="Child"></param>
    /// <returns></returns>
    private bool isExceed()
    {
        return (valueManagement.ParentMission <= valueManagement.ParentParameter && valueManagement.ChildMission <= valueManagement.ChildParameter);
    }

}
