using Unity.VisualScripting;
using UnityEngine;

namespace Mission
{
    //========================================================
    //�S����:���{��
    //�@�\:�~�b�V�����Ǘ� & ����
    //========================================================


    public class MissionInvoker : MonoBehaviour
    {
        [SerializeField] private MissionHandler Mission_;

        private void Start()
        {
            Mission_.RandomMission();
            Mission_.StartMission();
        }

    }
}