using UnityEngine;
using UnityEngine.UI;
using Value;

public class SpawnLine : MonoBehaviour
{
    //===========================================================
    //担当者:小宮純
    //機能:ProposalLineを付けたミッションの目標値のラインの生成
    //===========================================================

    [Header("対象のイメージ")]
    [SerializeField] private RectTransform targetImage;
    [Header("生成するラインプレファブ")]
    [SerializeField] private GameObject prefab;
    [Header("UIならcanvas、カメラならNull")]
    [SerializeField] private Transform spawnParent;
    [Header("値管理データ")]
    [SerializeField] private ValueManagement valueManagement;
    [Header("trueなら親,falseなら子")]
    [SerializeField] private bool isParent = false;
    private int maxValue = 0;

    private int proposalValue = 0;

    private GameObject objectLine;

    private void Update()
    {
        if (isParent && Input.GetKeyDown(KeyCode.W))
        {
            DeleteLine();
        }
        if (!isParent && Input.GetKeyDown(KeyCode.S))
        {
            DeleteLine();
        }
    }

    private void Start()
    {
        maxValue = valueManagement.MaxParameter;

        SetProposalValue();

        SetProposalLine(proposalValue, maxValue);
    }


    /// <summary>
    /// 設定した値/MaxValueの位置にラインを生成
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxValue"></param>
    public void SetProposalLine(int value, int maxValue)
    {
        // Imageの上下のワールド座標を取得
        Vector3[] worldCorners = new Vector3[4];
        targetImage.GetWorldCorners(worldCorners);

        float minY = worldCorners[0].y;
        float maxY = worldCorners[1].y;

        // ✅ int同士の割り算は0になる可能性があるので、floatにキャスト
        float t = Mathf.Clamp01((float)value / (float)maxValue);

        // 線形補間でY座標を計算
        float targetY = Mathf.Lerp(minY, maxY, t);

        // 中央のX座標
        float centerX = (worldCorners[0].x + worldCorners[3].x) / 2f;

        // 生成位置
        Vector3 spawnPos = new Vector3(centerX, targetY, 0);

        // プレファブを生成
        objectLine = Instantiate(prefab, spawnPos, Quaternion.identity, spawnParent);
    }


    /// <summary>
    /// 親/子それぞれのミッションの目標値を代入
    /// </summary>
    private void SetProposalValue()
    {
        if (isParent)
        {
            proposalValue = valueManagement.ParentMission;
        }
        else if (!isParent)
        {
            proposalValue = valueManagement.ChildMission;
        }
    }

    public void DeleteLine()
    {
        Destroy(objectLine);
    }

}
