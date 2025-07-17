using UnityEngine;
using Value;




public class Mission : MonoBehaviour
{
    //==================================
    //�S����:���{��
    //�@�\:�~�b�V�����Ǘ�
    //==================================

    [Header("�f�[�^")]
    [SerializeField] private MissionData missionData;   //MissionValue��z��ɂ���ScriptableObject
    [SerializeField] private ValueManagement valueManagement;   //�l�Ǘ��f�[�^

    private int dataNum = 0; //missionData�̔z����w�肷�邽�߂̕ϐ�
    private bool isDuringMission = false; //�~�b�V��������?
    private string MissionExplain = null; //������

    public int debugMax = 0;


    private void Awake()
    {
        setDataNum();
    }



    //==================================================================
    //���J����(public)
    //==================================================================

    /// <summary>
    /// �����_���Ƀ~�b�V�������o��
    /// </summary>
    public void randomMission()
    {
        //0�`MissionValue�̐��������_���Ŏ擾
        dataNum = Random.Range(0, missionData.MissionValues.Length);

        //dataNum�Ɋ�Â��ă~�b�V������ݒ�
        setMission();
        //�e�q�̃p�����[�^�����Ƀ~�b�V������B�����Ă����ꍇ : �Ē��I
        while (isExceed() && debugMax < 10)
        {
            //While����������Ē��I��������
            dataNum = Random.Range(0, missionData.MissionValues.Length);
            setMission();

            debugMax++;
        }

    }

    /// <summary>
    /// �w�肵�ă~�b�V�������o��
    /// </summary>
    public void selectMission(int missionNum)
    {
        //0�`MissionValue�̐��������_���Ŏ擾
        dataNum = missionNum;

        //�w�萔��dataNum�͈̔͂𒴂��Ă����ꍇ�G���[��Ԃ�
        if (missionNum > dataNum)
        {
            Debug.LogWarning("missionNum��0�`" + dataNum + "�őI��ł�������");
            return;
        }

            //dataNum�Ɋ�Â��ă~�b�V������ݒ�
            setMission();
        if (isExceed())
        {
            Debug.LogWarning("�p�����[�^�����ɖڕW�ɒB���Ă��܂�");
        }

    }

    /// <summary>
    /// �V�F�[�_�[�����Ăяo�� : ��X�L�q
    /// </summary>
    public void callShader()
    {

    }

    /// <summary>
    /// �G�L���������Ăяo�� : ��X�L�q
    /// </summary>
    public void callEnemy()
    {

    }










    //=======================================================================
    //��������(private)
    //=======================================================================

    /// <summary>
    /// dataNum��MissionValues�̒���������
    /// </summary>
    private void setDataNum()
    {
        dataNum = missionData.MissionValues.Length;
    }


    /// <summary>
    /// �~�b�V�����f�[�^[�~�b�V�����ԍ�]��ݒ�
    /// </summary>
    private void setMission()
    {
        valueManagement.ParentMission = missionData.MissionValues[dataNum].parentPurpose;
        valueManagement.ChildMission = missionData.MissionValues[dataNum].childPurpose;
        MissionExplain = missionData.MissionValues[dataNum].explain;

        Debug.Log("�e�̒l: " + valueManagement.ParentMission);
        Debug.Log("�q�̒l: " + valueManagement.ChildMission);
        Debug.Log("����: " + MissionExplain);
    }

    /// <summary>
    /// �e�q�����̃p�����[�^���~�b�V�����̖ڕW�ɒB���Ă��邩
    /// </summary>
    /// <param name="Parent"></param>
    /// <param name="Child"></param>
    /// <returns></returns>
    private bool isExceed()
    {
        return (valueManagement.ParentMission <= valueManagement.ParentParameter && valueManagement.ChildMission <= valueManagement.ChildParameter);
    }

}
