using Unity.VisualScripting;
using UnityEngine;

public class MissionInvoker : MonoBehaviour
{
    [SerializeField] private Mission Mission_;

    private void Start()
    {
        Mission_.randomMission();
    }

}
