using Unity.VisualScripting;
using UnityEngine;

namespace Mission
{
    //========================================================
    //担当者:小宮純
    //機能:ミッション管理 & 操作
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