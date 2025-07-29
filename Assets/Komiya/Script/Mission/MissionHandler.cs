using UnityEngine;
using Value;
using UnityEngine.SceneManagement;



namespace Mission
{
    public class MissionHandler : MonoBehaviour
    {
        //==================================
        //�S����:���{��
        //�@�\:�~�b�V�����n���h��
        //==================================

        [Header("�f�[�^")]
        [SerializeField] private MissionData missionData;   //MissionValue��z��ɂ���ScriptableObject
        [SerializeField] private ValueManagement valueManagement;   //�l�Ǘ��f�[�^

        [SerializeField] private GameObject button;

        private int dataNum = 0; //missionData�̔z����w�肷�邽�߂̕ϐ�

        private string MissionExplain = null; //�������������p������
        public string Explain => MissionExplain; //���J�p������

        public int debugMax = 0;


        private void Awake()
        {
            SetDataNum();
        }



        //==================================================================
        //���J����(public)
        //==================================================================

        /// <summary>
        /// �����_���Ƀ~�b�V�������o��
        /// </summary>
        public void RandomMission()
        {
            //0�`MissionValue�̐��������_���Ŏ擾
            dataNum = Random.Range(0, missionData.MissionValues.Length);

            //dataNum�Ɋ�Â��ă~�b�V������ݒ�
            SetMission();
            //�e�q�̃p�����[�^�����Ƀ~�b�V������B�����Ă����ꍇ : �Ē��I
            while (IsExceed())
            {
                //0�`MissionValue�̐��������_���Ŏ擾
                dataNum = Random.Range(0, missionData.MissionValues.Length);
                SetMission();
            }

        }

        /// <summary>
        /// �w�肵�ă~�b�V�������o��
        /// </summary>
        public void SelectMission(int missionNum)
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
            SetMission();
            if (IsExceed())
            {
                Debug.LogWarning("�p�����[�^�����ɖڕW�ɒB���Ă��܂�");
            }

        }

        /// <summary>
        /// �~�b�V�����J�n
        /// </summary>
        public void StartMission()
        {
            Debug.Log("�~�b�V�����J�n�I");
            SetMission(); //�~�b�V�������e���Ăяo��
            CallShader(); //�V�F�[�_�[�������Ăяo�� : ��X�ǉ�
            CallEnemy();  //�G�L�����������Ăяo�� : ��X�ǉ�
        }

        /// <summary>
        ///�~�b�V�����I�� 
        /// </summary>
        public void EndMission()
        {
            Debug.Log("�~�b�V�����I���I");
            EndShader();
            EndEnemy();

            button.SetActive(true);
        }









        //=======================================================================
        //��������(private)
        //=======================================================================


        /// <summary>
        /// dataNum��MissionValues�̒���������
        /// </summary>
        private void SetDataNum()
        {
            dataNum = missionData.MissionValues.Length;
        }


        /// <summary>
        /// �~�b�V�����f�[�^[�~�b�V�����ԍ�]��ݒ�
        /// </summary>
        private void SetMission()
        {
            valueManagement.ParentMission = missionData.MissionValues[dataNum].parentPurpose;   //�e�̃~�b�V�����p�����[�^��`
            valueManagement.ChildMission = missionData.MissionValues[dataNum].childPurpose;     //�q�̃~�b�V�����p�����[�^��`
            MissionExplain = missionData.MissionValues[dataNum].explain;    //��������`


            Debug.Log("�e�̒l: " + valueManagement.ParentMission);
            Debug.Log("�q�̒l: " + valueManagement.ChildMission);
            Debug.Log("����: " + MissionExplain);
        }

        /// <summary>
        /// �V�F�[�_�[�����Ăяo�� : ��X�L�q
        /// </summary>
        private void CallShader()
        {
            Debug.LogWarning("Developing ; callShader");
        }

        /// <summary>
        /// �G�L���������Ăяo�� : ��X�L�q
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
        /// �e�q�����̃p�����[�^���~�b�V�����̖ڕW�ɒB���Ă��邩
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